using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		public static string Compile(string code)
		{
			LlvmIrCompiler.Setup();

			StringBuilder all = new StringBuilder();

			TranslationUnit unit = new TranslationUnit();
			unit.OptimizationLevel = 2;


			// https://llvm.org/docs/LangRef.html#global-variables
			dynamic[] globals = code.PatternMatches("<global:id> =" +
				"( <linkage>)?" +
				"( <preemptionspecifier>)?" +
				"( <visibility>)?" +
				"( <dllstorageclass>)?" +
				"( <threadlocal>)?" +
				"( (unnamed_addr|local_unnamed_addr))?" +
				//"( <externallyinitialized>)?" +
				" (?<globconst>global|constant) <type:type>( (?<init><const>|c<string>))?" +
				"(, section <string:section>)?" +
				"(, comdat\\s*(\\(\\$<keyword:comname>\\))?)?" +
				"(, align <numeric:align>)?" +
				"", IrPatterns);
			foreach (dynamic global in globals)
			{
				if (global.align != "" && global.align != "1")
					all.AppendLine($".align {global.align}");
				all.AppendLine($"${global.id}:");

				if (global.init != "")
				{
					switch (global.type)
					{
					case "i64": all.AppendLine($"\t.data int64 {global.init}"); break;
					case "i32": all.AppendLine($"\t.data int32 {global.init}"); break;
					case "i16": all.AppendLine($"\t.data int16 {global.init}"); break;
					case "i8": all.AppendLine($"\t.data int8 {global.init}"); break;
					case "i1": all.AppendLine($"\t.data int8 {global.init}"); break;

					case "u64": all.AppendLine($"\t.data uint64 {global.init}"); break;
					case "u32": all.AppendLine($"\t.data uint32 {global.init}"); break;
					case "u16": all.AppendLine($"\t.data uint16 {global.init}"); break;
					case "u8": all.AppendLine($"\t.data uint8 {global.init}"); break;
					case "u1": all.AppendLine($"\t.data uint8 {global.init}"); break;

					case "float": all.AppendLine($"\t.data float {global.init}"); break;
					case string x when x.StartsWith("[") && x.EndsWith("i8]") && global.init.StartsWith("c\""):
						string str = global.init;
						
						str = Regex.Replace(str, @"\\([a-zA-Z0-9]{2})", (Match m) => $@"\x{m.Groups[1].Value.ToLowerInvariant()}"); // @"\x$1"
						str = str.Replace(@"\""", @"\x22");

						all.AppendLine($"\t.data ascii {str.Substring(1)}");
						break;
					}
				}
				else
				{
					// just pad out the size so we don't have to put a default value that we need to interpret
					all.AppendLine($"\t.data pad {unit.SizeOfAsInt(global.type)}");
				}
			}
			all.AppendLine();

			string test = all.ToString();

			dynamic[] funcs = code.PatternMatches($@"define <type:ret_type> <ident:id>( )*<parentheses:args> #<numeric:attrib> <braces:body>", IrPatterns);
			foreach (dynamic func in funcs)
			//var funcs = Regex.Matches(code, func_regex);
			//foreach (Match func in funcs)
			{
				MethodBuilder builder = new MethodBuilder(unit)
				{
					Code = func.body_inside,
					Id = func.id,
				};

				builder.Emit($"${func.id}:");
				builder.EmitPrefix = "\t";

				BuildMethodLocals(builder, func.ret_type, func.args_inside/*, optimize_args: false*/);
				ReplacePhis(builder);
				SimplifyCompareBranches(builder);

				string[] lines = builder.Code.Split('\n');
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line))
						continue;

					dynamic match;

					if ((match = line.PatternMatch(@"^\s*(<operand> = )?<keyword:op>", IrPatterns)) != null)
					{
						if (IrInstructions.TryGetValue(match.op, out List<(string pattern, MethodInfo func)> list))
						{
							bool good = false;
							foreach (var var in list)
							{
								dynamic args = line.PatternMatch(var.pattern, IrPatterns);

								if(args != null)
									if((bool)var.func.Invoke(null, new object[] { builder, args }))
									{
										good = true;
										break;
									}
							}

							if (good == false)
								throw new Exception($"{match.op} is malformed: {line.Trim()}");
						}
						else
						{
							//builder.Emit($"; unknown instrunction \"{match.op}\"");
							builder.Emit($";{line.Trim()}");
						}
						continue;
					}
					else if ((match = line.PatternMatch(@"[<]label[>]:<numeric:id>:", IrPatterns)) != null)
					{
						builder.Emit("");
						builder.Emit($"${builder.Id}_label_{match.id}:");
					}
					else
						throw new Exception();
				}

				if(unit.OptimizationLevel >= 1)
					RemoveNopJumps(builder);
				if (unit.OptimizationLevel >= 2)
					OptimizeMovs(builder);
				AllocateRegisters(builder);

				all.AppendLine(builder.Assembly.ToString());
			}

			string bootloader =
$@"mov {unit.StackPointer}, $stack
mov {unit.BasePointer}, {unit.StackPointer}
add {unit.StackPointer}, {unit.StackPointer}, 12 ; int(int, char*) ; add this just in case it's defined with the prototypes
call $@main
sub {unit.StackPointer}, {unit.StackPointer}, 12
halt

.align 4
$stack:
	.data pad 1024

";

			return bootloader + all.ToString();
		}
	}
}