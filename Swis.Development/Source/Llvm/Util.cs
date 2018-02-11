﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		[AttributeUsage(AttributeTargets.Method)]
		private class IrInstructionAttribute : Attribute
		{
			public string Instruction { get; set; }
			public string Pattern { get; set; }
			public bool Static { get; set; }  // can it be reduced at compile time?
			public IrInstructionAttribute(string instruction, string pattern, bool is_static = false)
			{
				this.Instruction = instruction;
				this.Pattern = pattern;
				this.Static = is_static;
			}
		}

		private class TranslationUnit
		{
			public int OptimizationLevel = 0;
			public bool IntelSyntax = true;
			string[] _Registers = new string[] { "a", "b", "c", "d", "e", "f" };

			public string StackPointer { get { return "esp"; } }
			public string BasePointer { get { return "ebp"; } }
			public string InstructionPointer { get { return "eip"; } }
			public string[] Registers { get { return _Registers; } }

			public class StructInfo
			{
				public uint Size;
				public (string type, uint offset)[] Fields;
			}

			static Dictionary<string, string> NamedTypes = new Dictionary<string, string>();
			static Dictionary<string, StructInfo> StructInfoCache = new Dictionary<string, StructInfo>();

			public StructInfo GetStructInfo(string type)
			{
				if (StructInfoCache.TryGetValue(type, out var ret))
					return ret;

				bool aligned = type.StartsWith("<");
				if (aligned)
					type.Substring(1, type.Length - 2); // chop the <> off

				if (!type.StartsWith("{"))
					throw new ArgumentException();

				string stype = type.Substring(1, type.Length - 2); // trim the {}

				uint cur_size = 0;
				dynamic[] fields = stype.PatternMatches("<type:type>", IrPatterns);

				List<(string type, uint offset)> compfields = new List<(string type, uint offset)>();
				foreach (dynamic field in fields)
				{
					compfields.Add((field.type, cur_size));

					uint sz = SizeOfAsInt(field.type);

					if (sz % 8 != 0)
						sz += 8 - (sz % 8);

					cur_size += sz / 8;
				}

				return StructInfoCache[type] = new StructInfo
				{
					Size = cur_size,
					Fields = compfields.ToArray(),
				};
			}
			

			// gets the offset of a type, and returns the type it has gotten
			public (string type, int offset) StaticTypeIndex(string type, int index)
			{
				if (type.EndsWith("*"))
				{
					string deref = TypeDeref(type);
					int offset = (int)(SizeOfAsInt(deref) / 8) * index;
					return (deref, offset);
				}

				if (type.StartsWith("%"))
					type = NamedTypes[type];

				bool aligned = type.StartsWith("<");
				if (aligned)
					type.Substring(1, type.Length - 2); // chop the <> off

				if (type.StartsWith("{"))
				{
					StructInfo si = GetStructInfo(type);
					(string a, uint b) = si.Fields[index];
					return (a, (int)b);
				}
				else if (type.StartsWith("[")) // an array
				{
					dynamic arry = type.PatternMatch(@"\[<numeric:count> x <type:subtype>\]", IrPatterns);
					string subtype = arry.subtype;
					uint stride = SizeOfAsInt(arry.subtype) / 8;

					return (subtype, (int)stride * index);
				}
				else
					throw new Exception($"Can't index type {type}");
			}

			public (string type, string operand) DynamicTypeIndex(MethodBuilder output, string type, string operand_type, string operand)
			{
				if (type.EndsWith("*"))
				{
					string deref = TypeDeref(type);
					uint size = SizeOfAsInt(deref);
					if (size == 8)
						return (deref, $"{output.ToOperand(operand_type, operand)}");
					else
						return (deref, $"{output.ToOperand(operand_type, operand)} * {size / 8}");
				}

				if (type.StartsWith("%"))
					type = NamedTypes[type];

				bool aligned = type.StartsWith("<");
				if (aligned)
					type.Substring(1, type.Length - 2); // chop the <> off

				if (type.StartsWith("{"))
				{
					throw new ArgumentException(); // the offset *can* be found if we make a type-table, but, the type returned is unknowable.
				}
				else if (type.StartsWith("[")) // an array
				{
					dynamic arry = type.PatternMatch(@"\[<numeric:count> x <type:subtype>\]", IrPatterns);
					string subtype = arry.subtype;
					uint stride = SizeOfAsInt(arry.subtype);

					if (stride == 8)
						return (subtype, $"{output.ToOperand(operand_type, operand)}"); // stride * index
					else
						return (subtype, $"{output.ToOperand(operand_type, operand)} * {stride / 8}"); // stride * index
				}
				else
					throw new Exception($"Can't index type {type}");
			}




			/// <summary>
			/// returns 
			/// </summary>
			/// <param name="type"></param>
			/// <returns>"ptr" or /[0-9]+/</returns>
			public string SizeOf(string type)
			{
				if (type == "void")
					return "0";
				if (type[type.Length - 1] == '*')
					return "ptr";

				if (type[0] == '%')
					type = NamedTypes[type];

				if (type[0] == '@')
					type = "u32";

				if (type[0] == '{')
				{
					StructInfo info = GetStructInfo(type);
					return $"{info.Size * 8}";
				}

				if (type[0] == '[')
				{
					dynamic arry = type.PatternMatch(@"(?<sizes>(\[<numeric> x )*)<type:type>\]", IrPatterns);
					dynamic[] sizes = ((string)arry.sizes).PatternMatches("<numeric:sz>", IrPatterns);

					uint bits = SizeOfAsInt(arry.type);
					foreach (dynamic size in sizes)
						bits *= uint.Parse(size.sz);
					return bits.ToString();
				}

				if (type == "half")
					return "16";
				if (type == "float")
					return "32";
				if (type == "double")
					return "64";
				if (type == "fp128")
					return "128";
				if (type[0] == 'i' || type[0] == 'u' || type[0] == 'f')
					return Regex.Match(type, "[0-9]+").Value;
				throw new NotImplementedException(type);
			}

			/// ptr info is lost during this
			public uint SizeOfAsInt(string type)
			{
				string sz = SizeOf(type);
				if (sz == "ptr")
					return Cpu.NativeSizeBits;
				return uint.Parse(sz);
			}

			public string TypeDeref(string type)
			{
				if (type[type.Length - 1] == '*')
					return type.Substring(0, type.Length - 1);
				throw new Exception("deref non ptr");
			}
		}

		private class MethodBuilder
		{
			public TranslationUnit Unit;

			public StringBuilder Assembly;
			public string Id;
			public (string type, int bits) Return;
			public (string arg, string type)[] Arguments;

			public string Code;

			uint ssa = 0;
			public string CreateSSARegister(string hint = "tmp")
			{
				return $"%{hint}.{ssa++}";
			}

			public Dictionary<string, string> ConstantLocals;
			public MethodBuilder(TranslationUnit unit)
			{
				this.Unit = unit;
				this.Assembly = new StringBuilder();
				this.Return = (null, -1);
				this.Arguments = null;
				this.Code = null;
				this.Id = null;
				this.ConstantLocals = new Dictionary<string, string>();
			}

			public string EmitPrefix = "";
			public void Emit(string asm)
			{
				this.Assembly.Append(this.EmitPrefix);
				this.Assembly.AppendLine(asm);
			}


			// i16*, %value.addr, true = ptr16 [%value.addr:ptr]
			// i16**, %value.addr, true = ptr [%value.addr:ptr]
			// i16**, %5, true = ptr [%5:ptr]
			// i16*, %5, false = %5:ptr
			// i16, %5, false = %5:16
			public string ToOperand(string type, string operand, bool indirection = false)
			{
				string part = "";

				string size = this.Unit.SizeOf(type);

				if (operand[0] == '@')
					part = $"${operand}";
				else if (operand[0] != '%')
					part = $"{operand}"; // constants are always 32bits
										 // is it a calculated const size?
				else if (this.ConstantLocals.TryGetValue(operand, out var cl))
					part = cl;
				else // use a register
					part = $"{operand}:{size}";

				if (indirection)
				{
					string size_to = this.Unit.SizeOf(this.Unit.TypeDeref(type));
					part = $"ptr{size_to} [{part}]";
				}

				return part;
			}
		}

		static bool _IsSetup = false;
		static Dictionary<string, List<(string regex, MethodInfo func)>> IrInstructions = new Dictionary<string, List<(string regex, MethodInfo func)>>();
		static Dictionary<string, string> IrPatterns = new Dictionary<string, string>();

		

		static void Setup()
		{
			if (_IsSetup)
				return;
			_IsSetup = true;
			
			

			IrPatterns["keyword"]    = LlvmUtil.PatternCompile(@"[a-z]+", IrPatterns);
			IrPatterns["array"]      = LlvmUtil.PatternCompile(@"\[[0-9]+ x", IrPatterns);
			IrPatterns["type"]       = LlvmUtil.PatternCompile(@"([uif]<numeric:size>|void|half|float|double|fp128|x86_fp80|ppc_fp128|.+( )*<parentheses>|<braces>|<brackets>|<angled>|\%[a-zA-Z0-9_.]+)\**", IrPatterns);
			IrPatterns["const"]      = LlvmUtil.PatternCompile(@"-?[0-9\.]+f?", IrPatterns);
			IrPatterns["ident"]      = LlvmUtil.PatternCompile(@"[%@][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["namedlocal"] = LlvmUtil.PatternCompile(@"[%][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["global"]     = LlvmUtil.PatternCompile("[@][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["register"]   = LlvmUtil.PatternCompile(@"[%][0-9]+", IrPatterns);
			IrPatterns["local"]      = LlvmUtil.PatternCompile(@"<namedlocal>|<register>", IrPatterns);
			IrPatterns["operand"] = LlvmUtil.PatternCompile(@"<const>|<local>|<global>", IrPatterns);

			IrPatterns["paramattributes"] = LlvmUtil.PatternCompile("( (" +
				"zeroext|signext|inreg|byval|inalloca|sret|" +
				"align [0-9]+|noalias|nocapture|nest|nonnull|" +
				@"dereferenceable\((0|1)\)|" +
				@"dereferenceable_or_null\((0|1)\)|" +
				"swiftself|swifterror" + "))*", IrPatterns);
			
			IrPatterns["linkage"] = LlvmUtil.PatternCompile("private|internal|available_externally|linkonce|weak|common|appending|extern_weak|linkonce_odr|weak_odr|external", IrPatterns);
			IrPatterns["preemptionspecifier"] = LlvmUtil.PatternCompile("dso_preemptable|dso_local", IrPatterns);
			IrPatterns["visibility"] = LlvmUtil.PatternCompile("default|hidden|protected", IrPatterns);
			IrPatterns["dllstorageclass"] = LlvmUtil.PatternCompile("dllimport|dllexport", IrPatterns);
			IrPatterns["threadlocal"] = LlvmUtil.PatternCompile(@"thread_local\((localdynamic|initialexec|localexec)\)", IrPatterns);


			IrPatterns["constexp"] = LlvmUtil.PatternCompile(@"<type:type>\s*(?<op><keyword>(\s+<keyword>)*)\s*<parentheses:args>", IrPatterns);

			var funcs = typeof(LlvmIrCompiler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
			foreach (MethodInfo func in funcs)
			{
				var attrib = func.GetCustomAttribute<IrInstructionAttribute>();
				if (attrib == null)
					continue;

				if (!IrInstructions.TryGetValue(attrib.Instruction, out var list))
					list = IrInstructions[attrib.Instruction] = new List<(string regex, MethodInfo func)>();

				list.Add((attrib.Pattern, func));
			}

		}
		
	}
}