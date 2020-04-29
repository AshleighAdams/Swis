using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		public static string CompileCpp(string code)
		{
			string tmp1 = Path.GetTempFileName();
			File.WriteAllText(tmp1, code);

			string tmp2 = Path.GetTempFileName();

			string pathv = Environment.GetEnvironmentVariable("PATH");
			ProcessStartInfo info
				= new ProcessStartInfo("clang++", $"-cc1 -x c++ -triple=i386 -S \"{tmp1}\" -emit-llvm -o \"{tmp2}\"");
			//info.UseShellExecute = true;
			
			Process p = Process.Start(info);
			p.WaitForExit();
			
			if (p.ExitCode != 0)
				throw new Exception("clang failed");

			return File.ReadAllText(tmp2);
		}

		public static string Compile(string code)
		{
			LlvmIrCompiler.Setup();

			StringBuilder all = new StringBuilder();

			TranslationUnit unit = new TranslationUnit();
			unit.OptimizationLevel = 2;

			//%struct.test = type { i32, i32 }
			dynamic[] namedtypes = code.PatternMatches("(?<name>%[a-zA-Z_.0-9]+) = type <type:type>", IrPatterns);
			foreach (dynamic nt in namedtypes)
				unit.NamedTypes[nt.name] = nt.type;

			/*
			NamedTypes*/

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
			
			dynamic[] funcs = code.PatternMatches(
				@"define( <linkage>)?( <preemptionspecifier>)?( <visibility>)?( <DLLStorageClass>)?" +
				@"( <callingconvention>)?( <retattributes>)*" +
				@" <type:ret_type> <ident:id>\s*<parentheses:args>" +
				@"( unnamed_addr| local_unnamed_addr)?( <functionattributes>| #<numeric:attrib>)*( section <string>)?" +
				@"( comdat( (\([^\)]\)))?)?( align <numeric>)?( gc)?( prefix <type> <const>)?" +
				@"( prologue <type> <const>)?( personality <type> <const>)? (!<alphanumeric> !<numeric>)*\s*<braces:body>", IrPatterns);
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
				
				ExpandConstants(builder);
				BuildMethodLocals(builder, func.ret_type, func.args_inside/*, optimize_args: false*/);
				ReplacePhis(builder);
				SimplifyCompareBranches(builder);

				string[] lines = builder.Code.Split('\n');
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line))
						continue;

					dynamic? match;

					if ((match = line.PatternMatch(@"^\s*(<operand> = )?<keyword:op>", IrPatterns)) != null)
					{
						if (IrInstructions.TryGetValue(match.op, out List<(Regex pattern, MethodInfo func)> list))
						{
							bool good = false;
							foreach (var var in list)
							{
								dynamic? args = line.PatternMatch(var.pattern, IrPatterns);

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
							throw new Exception($"Unknown instruction \"{match.op}\"");
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

			string bootloader = $"mov {unit.StackPointer}, $stack\n" +
			                    $"mov {unit.BasePointer}, {unit.StackPointer}\n" +
			                    $"add {unit.StackPointer}, {unit.StackPointer}, 12 ; int(int, char*) ; add this just in case it's defined with the prototypes\n" +
			                    $"call $@main\n" +
			                    $"sub {unit.StackPointer}, {unit.StackPointer}, 12\n" +
			                    $"halt\n\n";
			string postcode =   $"\n.align 4\n" +
			                    $"$stack:\n" +
			                    $"	.data pad 1024\n";
			

			return bootloader + all.ToString() + LlvmIntrinsics + postcode;
		}

		const string LlvmIntrinsics = @"

$@llvm.memcpy.p0i8.p0i8.i32:
        .src func ""void llvm.memcpy.p0i8.p0i8.i32(i8* dst, i8* src, i32 len, i32 align, i8 isvolatile)""
        .src local ""i"" ""i32"" 0 4
        .src local ""isvolatile"" ""i8"" -9 1
        .src local ""align"" ""i32"" -13 4
        .src local ""len"" ""i32"" -17 4
        .src local ""src"" ""i8*"" -21 4
        .src local ""dst"" ""i8*"" -25 4

		add esp, esp, 4 ; alloca
		mov ptr32 [ebp + 0], 0

        $@llvm.memcpy.p0i8.p0i8.i32_loop:
        cmp ptr32 [ebp + 0], ptr32 [ebp - 17]
		jge $@llvm.memcpy.p0i8.p0i8.i32_end

        mov eax, ptr32 [ebp - 21]
		mov ebx, ptr32 [ebp - 25]
		mov edx, ptr32 [ebp + 0]

		mov ptr8 [ebx + edx], ptr8 [eax + edx]

        add ptr32 [ebp + 0], ptr32 [ebp + 0], 1
		jmp $@llvm.memcpy.p0i8.p0i8.i32_loop
        $@llvm.memcpy.p0i8.p0i8.i32_end:
        ret

";
	}
}