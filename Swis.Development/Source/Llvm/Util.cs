using System;
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
		private class MethodBuilder
		{
			public StringBuilder Assembly;
			public string Id;
			public (string type, int bits) Return;
			public (string arg, string type)[] Arguments;

			public string Code;

			public Dictionary<string, string> ConstantLocals;
			public MethodBuilder()
			{
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
		}

		static bool _IsSetup = false;
		static Dictionary<string, (string regex, MethodInfo func)> IrInstructions = new Dictionary<string, (string regex, MethodInfo func)>();
		static Dictionary<string, string> NamedPatternToRegex = new Dictionary<string, string>();

		static void Setup()
		{
			if (_IsSetup)
				return;
			_IsSetup = true;

			NamedPatternToRegex["keyword"] = PatternCompile(@"[a-z]+");
			NamedPatternToRegex["numeric"] = PatternCompile(@"[0-9]+");
			NamedPatternToRegex["type"] = PatternCompile(@"([uif]<numeric:size>|void)[\*]*");
			NamedPatternToRegex["const"] = PatternCompile(@"-?[0-9]+");
			NamedPatternToRegex["ident"] = PatternCompile(@"[%@][-a-zA-Z$._][-a-zA-Z$._0-9]*");
			NamedPatternToRegex["namedlocal"] = PatternCompile(@"[%][-a-zA-Z$._][-a-zA-Z$._0-9]*");
			NamedPatternToRegex["global"] = PatternCompile("[@][-a-zA-Z$._][-a-zA-Z$._0-9]*");
			NamedPatternToRegex["register"] = PatternCompile(@"[%][0-9]+");
			NamedPatternToRegex["local"] = PatternCompile(@"<namedlocal>|<register>");
			NamedPatternToRegex["operand"] = PatternCompile(@"<const>|<local>");


			var funcs = typeof(LlvmIrCompiler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
			foreach (MethodInfo func in funcs)
			{
				var attrib = func.GetCustomAttribute<IrInstructionAttribute>();
				if (attrib == null)
					continue;
				IrInstructions[attrib.Instruction] = (attrib.Pattern, func);
			}

		}

		public static string PatternCompile(string pattern) // <> = sub-regex
		{
			// test = "<register:abc>" -> "(?<abc>[%][0-9]+)"
			// finl = "<test:def>"     -> "(?<def>(?<def.abc>[%][0-9]+))"

			string ret = Regex.Replace(pattern, "[ ]+", @"\s+");

			ret = Regex.Replace(ret, @"(?<!\(\?)<(?<id>[a-z]+):(?<prefix>[a-zA-Z0-9_.-]+)>", delegate (Match m)
			{
				if (!NamedPatternToRegex.TryGetValue(m.Groups["id"].Value, out string sub_regex))
					throw new Exception($"unknown sub-pattern: {m.Groups["id"].Value}");
				string prefix = m.Groups["prefix"].Value;

				sub_regex = Regex.Replace(sub_regex, @"\(\?\<([a-zA-Z0-9_.-]+)\>", $@"(?<{prefix}_$1>");

				return $"(?<{prefix}>{sub_regex})";
			});

			ret = Regex.Replace(ret, @"(?<!\?)<(?<id>[a-z]+)>", delegate (Match m)
			{
				string sub_regex = NamedPatternToRegex[m.Groups["id"].Value];

				//sub_regex = Regex.Replace(sub_regex, @"\(\?\<([a-zA-Z0-9_.-]+)\>", $@"(?<{capture_prefix}.$1>)");

				return $"({sub_regex})";
			});

			return ret;
		}



		/// <summary>
		/// returns "ptr" or /[0-9]+/
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		static string SizeOf(string type)
		{
			if (type == "void")
				return "0";
			if (type[type.Length - 1] == '*')
				return "ptr";
			return Regex.Match(type, "[0-9]+").Value;
		}

		/// ptr info is lost during this
		static int SizeOfAsInt(string type)
		{
			string sz = SizeOf(type);
			if (sz == "ptr")
				return Cpu.NativeSizeBits;
			return int.Parse(sz);
		}

		static string TypeDeref(string type)
		{
			if (type[type.Length - 1] == '*')
				return type.Substring(0, type.Length - 1);
			throw new Exception("deref non ptr");
		}

		// i16*, %value.addr, true = ptr16 [%value.addr:ptr]
		// i16**, %value.addr, true = ptr [%value.addr:ptr]
		// i16**, %5, true = ptr [%5:ptr]
		// i16*, %5, false = %5:ptr
		// i16, %5, false = %5:16
		static string ToOperand(MethodBuilder b, string type, string operand, bool indirection = false)
		{
			string part = "";

			string size = SizeOf(type);

			if (operand[0] == '@')
				part = $"${operand}";
			if (operand[0] != '%')
				part = $"{operand}"; // constants are always 32bits
									 // is it a calculated const size?
			else if (b.ConstantLocals.TryGetValue(operand, out var cl))
				part = cl;
			else // use a register
				part = $"{operand}:{size}";

			if (indirection)
			{
				string size_to = SizeOf(TypeDeref(type));
				part = $"ptr{size_to} [{part}]";
			}

			return part;
		}
	}
}