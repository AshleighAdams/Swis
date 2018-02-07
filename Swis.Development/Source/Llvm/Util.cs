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
		static Dictionary<string, List<(string regex, MethodInfo func)>> IrInstructions = new Dictionary<string, List<(string regex, MethodInfo func)>>();
		static Dictionary<string, string> IrPatterns = new Dictionary<string, string>();

		

		static void Setup()
		{
			if (_IsSetup)
				return;
			_IsSetup = true;
			
			

			IrPatterns["keyword"]    = Util.PatternCompile(@"[a-z]+", IrPatterns);
			IrPatterns["array"]      = Util.PatternCompile(@"\[[0-9]+ x", IrPatterns);
			IrPatterns["type"]       = Util.PatternCompile(@"([uif]<numeric:size>|void|half|float|double|fp128|x86_fp80|ppc_fp128|.+( )*<parentheses>|<braces>|<brackets>|<angled>|\%[a-zA-Z0-9_.]+)\**", IrPatterns);
			IrPatterns["const"]      = Util.PatternCompile(@"-?[0-9]+", IrPatterns);
			IrPatterns["ident"]      = Util.PatternCompile(@"[%@][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["namedlocal"] = Util.PatternCompile(@"[%][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["global"]     = Util.PatternCompile("[@][-a-zA-Z$._][-a-zA-Z$._0-9]*", IrPatterns);
			IrPatterns["register"]   = Util.PatternCompile(@"[%][0-9]+", IrPatterns);
			IrPatterns["local"]      = Util.PatternCompile(@"<namedlocal>|<register>", IrPatterns);
			IrPatterns["operand"]    = Util.PatternCompile(@"<const>|<local>", IrPatterns);


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

		// { int x
		

		/*
		struct RT
		{
			char A;
			int B[10][20];
			char C;
		};
		struct ST
		{
			int X;
			double Y;
			struct RT Z;
		};

		int* foo(struct ST *s)
		{
			return &s[1].Z.B[5][13];
		}
		*/
		// ------------------
		/*
		%struct.RT = type { i8, [10 x [20 x i32]], i8 }
		%struct.ST = type { i32, double, %struct.RT }

		define i32* @foo(%struct.ST* %s) nounwind uwtable readnone optsize ssp {
		entry:
		  %arrayidx = getelementptr inbounds %struct.ST, %struct.ST* %s, i64 1, i32 2, i32 1, i64 5, i64 13
		  ret i32* %arrayidx
		}
		*/

		class StructInfo
		{
			public uint Size;
			public (string type, uint offset)[] Fields;
		}

		static Dictionary<string, string> NamedTypes = new Dictionary<string, string>();
		static Dictionary<string, StructInfo> StructInfoCache = new Dictionary<string, StructInfo>();

		static StructInfo GetStructInfo(string type)
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

				if(sz % 8 != 0)
					sz += 8 - (sz % 8);

				cur_size += sz / 8;
			}

			return StructInfoCache[type] = new StructInfo
			{
				Size = cur_size,
				Fields = compfields.ToArray(),
			};
		}

		public static void _teststrutinfo()
		{
			Setup();
			/*%struct.RT = type { i8, [10 x [20 x i32]], i8 }
			%struct.ST = type { i32, double, %struct.RT }*/

			NamedTypes["%struct.RT"] = "{ i8, [10 x [20 x i32]], i8 }";
			NamedTypes["%struct.ST"] = "{ i32, double, %struct.RT }";

			StructInfo inf = GetStructInfo(NamedTypes["%struct.ST"]);
		}

		// gets the offset of a type, and returns the type it has gotten
		static (string type, int offset) StaticTypeIndex(string type, int index)
		{
			if (type.EndsWith("*"))
			{
				string deref = TypeDeref(type);
				uint size = SizeOfAsInt(deref);
				return (deref, (int)size);
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
				uint stride = SizeOfAsInt(arry.subtype);

				return (subtype, (int)stride * index);
			}
			else
				throw new Exception($"Can't index type {type}");
		}

		static (string type, string operand) DynamicTypeIndex(MethodBuilder output, string type, string operand_type, string operand)
		{
			if (type.EndsWith("*"))
			{
				string deref = TypeDeref(type);
				uint size = SizeOfAsInt(deref);
				if (size == 8)
					return (deref, $"{ToOperand(output, operand_type, operand)}");
				else
					return (deref, $"{ToOperand(output, operand_type, operand)} * {size / 8}");
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
					return (subtype, $"{ToOperand(output, operand_type, operand)}"); // stride * index
				else
					return (subtype, $"{ToOperand(output, operand_type, operand)} * {stride / 8}"); // stride * index
			}
			else
				throw new Exception($"Can't index type {type}");
		}




		/// <summary>
		/// returns 
		/// </summary>
		/// <param name="type"></param>
		/// <returns>"ptr" or /[0-9]+/</returns>
		static string SizeOf(string type)
		{
			if (type == "void")
				return "0";
			if (type[type.Length - 1] == '*')
				return "ptr";

			if (type[0] == '%')
				type = NamedTypes[type];

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
			if (type[0] == 'i' || type[0] == 'f')
				return Regex.Match(type, "[0-9]+").Value;
			throw new NotImplementedException(type);
		}

		/// ptr info is lost during this
		static uint SizeOfAsInt(string type)
		{
			string sz = SizeOf(type);
			if (sz == "ptr")
				return Cpu.NativeSizeBits;
			return uint.Parse(sz);
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