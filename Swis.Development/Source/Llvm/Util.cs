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
		static Dictionary<string, string> NamedPatternToRegex = new Dictionary<string, string>();

		static void Setup()
		{
			if (_IsSetup)
				return;
			_IsSetup = true;
			
			NamedPatternToRegex["parentheses"] = PatternCompile(@"\((?<inside>(?:[^\(\)]|(?<__unique__>\()|(?<-__unique__>\)))+(?(__unique__)(?!)))\)");
			NamedPatternToRegex["braces"]      = PatternCompile(@"\{(?<inside>(?:[^\{\}]|(?<__unique__>\{)|(?<-__unique__>\}))+(?(__unique__)(?!)))\}");
			NamedPatternToRegex["brackets"]    = PatternCompile(@"\[(?<inside>(?:[^\[\]]|(?<__unique__>\[)|(?<-__unique__>\]))+(?(__unique__)(?!)))\]");
			NamedPatternToRegex["angled"]      = PatternCompile(@"\<(?<inside>(?:[^\<\>]|(?<__unique__>\<)|(?<-__unique__>\>))+(?(__unique__)(?!)))\>");

			NamedPatternToRegex["keyword"] = PatternCompile(@"[a-z]+");
			NamedPatternToRegex["numeric"] = PatternCompile(@"[0-9]+");
			NamedPatternToRegex["array"] = PatternCompile(@"\[[0-9]+ x");
			NamedPatternToRegex["type"] = PatternCompile(@"([uif]<numeric:size>|void|half|float|double|fp128|x86_fp80|ppc_fp128|[a-zA-Z0-9_.]+( )*<parentheses>|<braces>|<brackets>|<angled>|\%[a-zA-Z0-9_.]+)\**");
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

				if (!IrInstructions.TryGetValue(attrib.Instruction, out var list))
					list = IrInstructions[attrib.Instruction] = new List<(string regex, MethodInfo func)>();

				list.Add((attrib.Pattern, func));
			}

		}

		static int uniqueid = 0;
		public static string PatternCompile(string pattern) // <> = sub-regex
		{
			pattern = pattern.Replace("__unique__", $"__unique__{uniqueid++}__");
			// test = "<register:abc>" -> "(?<abc>[%][0-9]+)"
			// finl = "<test:def>"     -> "(?<def>(?<def.abc>[%][0-9]+))"

			string ret = Regex.Replace(pattern, "[ ]+", @"\s+");

			ret = Regex.Replace(ret, @"(?<!\(\?)<(?<id>[a-z]+):(?<prefix>[a-zA-Z0-9_.-]+)>", delegate (Match m)
			{
				if (!NamedPatternToRegex.TryGetValue(m.Groups["id"].Value, out string sub_regex))
					throw new Exception($"unknown sub-pattern: {m.Groups["id"].Value}");
				string prefix = m.Groups["prefix"].Value;

				sub_regex = Regex.Replace(sub_regex, @"\(\?\<(?<id>[a-zA-Z0-9_.-]+)\>",
					delegate (Match subid)
					{
						string id = subid.Groups["id"].Value;
						if (id.Contains("__unique__"))
							return subid.Value;
						return $@"(?<{prefix}_{id}>";
					});

				sub_regex = sub_regex.Replace("__unique__", $"__unique__{prefix}__");
				//$@"(?<{prefix}_$1>");

				return $"(?<{prefix}>{sub_regex})";
			});

			ret = Regex.Replace(ret, @"(?<!\?)<(?<id>[a-z]+)>", delegate (Match m)
			{
				string sub_regex = NamedPatternToRegex[m.Groups["id"].Value];
				
				sub_regex = sub_regex.Replace("__unique__", $"__unique__{uniqueid++}__");
				return $"({sub_regex})";
			});

			return ret;
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
			public int Size;
			public (int offset, string type)[] Fields;
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

			int cur_size = 0;
			dynamic[] fields = stype.PatternMatches("<type:type>");

			List<(int offset, string type)> compfields = new List<(int offset, string type)>();
			foreach (dynamic field in fields)
			{
				compfields.Add((cur_size, field.type));

				int sz = SizeOfAsInt(field.type);

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
		static (string type, int offset) TypeIndex(string type, int index)
		{
			if (type.EndsWith("*"))
			{
				string deref = TypeDeref(type);
				int size = SizeOfAsInt(deref);
				return (deref, size);
			}

			if(type.StartsWith("%"))
				type = NamedTypes[type];

			bool aligned = type.StartsWith("<");
			if (aligned)
				type.Substring(1, type.Length - 2); // chop the <> off

			if (type.StartsWith("{"))
			{
				StructInfo si = GetStructInfo(type);
				throw new NotImplementedException();
			}
			else if (type.StartsWith("[")) // an array
			{
				throw new NotImplementedException();
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
				dynamic arry = type.PatternMatch(@"(?<sizes>(\[<numeric> x )*)<type:type>\]");
				dynamic[] sizes = ((string)arry.sizes).PatternMatches("<numeric:sz>");

				int bits = SizeOfAsInt(arry.type);
				foreach (dynamic size in sizes)
					bits *= int.Parse(size.sz);
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