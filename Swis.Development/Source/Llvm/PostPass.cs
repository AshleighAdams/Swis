using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		static void RemoveNopJumps(MethodBuilder output)
		{
			//jmp $x
			//$x:

			// ->

			//$x:

			string rx = LlvmUtil.PatternCompile(
				@"jmp (?<target>\$[a-zA-Z0-9@%#-_\.]+)" +
				@"(?<ws>\s*\n\s*)+" +
				@"(?<dest>\$[^:]+):", IrPatterns
			);

			string asm = output.Assembly.ToString();
			asm = Regex.Replace(asm, rx,
				delegate (Match m)
				{
					if (m.Groups["target"].Value == m.Groups["dest"].Value)
						return // comment out the jump
							   //$";jmp {m.Groups["dest"].Value}" +
							$"{m.Groups["ws"].Value}" +
							$"{m.Groups["dest"].Value}:";
					return m.Value;
				});
			output.Assembly.Clear();
			output.Assembly.Append(asm);
		}

		class VarInfo
		{
			public List<(int from, int length)> FoundAt = new List<(int, int)>();
			public int First = 0;
			public int Last = 0;
			public string AllocatedRegister = null;
			public string Size = "";
		}

		class RegInfo
		{
			public List<(int from, int to)> Used = new List<(int from, int to)>();
		}

		// shaves off about 30% of the instructions
		static void OptimizeMovs(MethodBuilder output)
		{
			// llvm instructions can't store/load and perform an operation together, so let's fix that
			// mov %28:32, ptr32 [ebp - 20]
			// mov %29:32, ptr32 [ebp - 12]
			// div %div:32, %28:32, %29:32
			// mov ptr32 [ebp - 20], %div:32

			// to

			// div ptr32 [ebp - 20], ptr32 [ebp - 20], ptr32 [ebp - 12]
			string asm = output.Assembly.ToString();

			string ssa = @"(?<![^\n][;])(?<varname>%[a-zA-Z._0-9]+):(?<size>ptr|1|8|16|32)";

			List<(string what, string with)> replacements = new List<(string what, string with)>();

			string simple_use = @"([a-z]|,)[ \t]{0}[ \t]*(,|;|\r|\n|$)";
			Dictionary<string, bool> have_simplified = new Dictionary<string, bool>();
			bool searchdown = true;

			MatchEvaluator tester = delegate (Match m)
			{
				string searchin = searchdown ? asm.Substring(m.Index) : asm.Substring(0, m.Index + m.Length);
				string varname = m.Groups["reg"].Value;
				MatchCollection simple = searchin.Matches(string.Format(simple_use, Regex.Escape(varname)));
				MatchCollection total = asm.Matches(Regex.Escape(varname)); // this searches everything still
				
				if (simple.Count == 2 && total.Count == 2 && !have_simplified.TryGetValue(varname, out var _))
				{
					// we can probably replace it, but first let's check we haven't used that data chunk else where,
					// ensuring that this being in a different order won't change the symantic meaning
					
					string data = m.Groups["data"].Value.Trim();

					int times_found;
					
					if (searchdown)
						searchin = searchin.Substring(0, simple[1].Index + simple[1].Length);
					else
						searchin = searchin.Substring(simple[0].Index);

					
					if (
						// if we use it right after, it's safe
						searchin.Matches("[\n]+").Count == 1 ||
						// otherwise, if we access this more than once, the data might have changed.
						// also make sure there was not a call or a jump between us, as they could have unknowable changes to that memory,
						// unless it is this value that we're jumping to
						(searchin.Matches(Regex.Escape(data)).Count == 1 && searchin.Matches($@"\n\s*(call|j[a-zA-Z]{{1,2}}) (?!{Regex.Escape(varname)})").Count == 0))
					{
						// woop woop, we can replace it
						replacements.Add((varname, data));
						have_simplified[varname] = true; // so we don't reduce this variable further accidentally
						return m.Groups["startwhitespace"].Value + m.Groups["endwhitespace"].Value;
					}
				}

				return m.Value;
			};

			// mov to op
			searchdown = true;
			asm = Regex.Replace(asm, $@"(?<startwhitespace>[ \t])*mov (?<reg>{ssa}), (?<data>[^,;]+)(?<endwhitespace>\s*;|\n)", tester);

			// op to move
			searchdown = false;
			asm = Regex.Replace(asm, $@"(?<startwhitespace>[ \t])*mov (?<data>[^,;]+), (?<reg>{ssa})", tester);

			//MatchCollection movs_to_op = "".Matches($@"mov (?<reg>{ssa}), (?<data>[^,;]+)\s*(;|\n)"); // the %28 and %29 above
			//MatchCollection op_to_mov = "".Matches($@"mov (?<data>[^,;]+), (?<reg>{ssa})"); // the %div above

			foreach ((string what, string with) in replacements)
			{
				asm = asm.Replace(what, with);
			}

			output.Assembly.Clear();
			output.Assembly.Append(asm);
		}

		static void AllocateRegisters(MethodBuilder output)
		{
			string asm = output.Assembly.ToString();

			string[] registers = output.Unit.Registers;

			Dictionary<string, RegInfo> regs = new Dictionary<string, RegInfo>();
			foreach (string r in registers) regs[r] = new RegInfo();

			bool overlap((int from, int to) a, (int from, int to) b)
			{
				return a.from <= b.to && b.from <= a.to;
			}

			bool register_free_between(string reg, (int from, int to) range)
			{
				RegInfo info = regs[reg];

				foreach ((int usedfrom, int usedto) used in info.Used)
				{
					if (overlap(range, used))
						return false;
				}
				return true;
			}

			string allocate_register_between((int from, int to) range)
			{
				foreach (string reg in registers)
				{
					if (register_free_between(reg, range))
					{
						RegInfo inf = regs[reg];
						inf.Used.Add(range);
						return reg;
					}
				}
				throw new Exception("couldn't allocate register! spilling not implemented");
			}

			string ssa_regex = @"(?<![^\n][;])(?<varname>%[a-zA-Z._0-9]+):(?<size>ptr|1|8|16|32)";
			StringBuilder ssa_inverse = new StringBuilder();

			// take only the operands, and reverse them, placing the destinations at the end.
			// this way we can have code like this:
			
			// %add0 = add 0, 1
			// $add1 = add %add0, 2

			// reuse the same register as such:

			// add ta32, 0, 1
			// add ta32, ta32, 2

			{
				string[] lines = asm.Split('\n');
				foreach (string line in lines)
				{
					MatchCollection matches = line.Matches(ssa_regex);
					for (int i = matches.Count; i --> 0;)
						ssa_inverse.Append($" {matches[i].Value} ");
					ssa_inverse.AppendLine();
				}
			}

			string only_ssa = ssa_inverse.ToString();

			Dictionary<string, VarInfo> vars = new Dictionary<string, VarInfo>();
			{
				MatchCollection matches = only_ssa.Matches(ssa_regex);

				foreach (Match match in matches)
				{
					if (!vars.TryGetValue(match.Value, out VarInfo val))
						val = vars[match.Value] = new VarInfo()
						{
							First = match.Index,
							Last = match.Index + match.Length,
							Size = match.Groups["size"].Value,
						};

					if (match.Index < val.First)
						val.First = match.Index;
					if (match.Index + match.Length > val.Last)
						val.Last = match.Index + match.Length;

					val.FoundAt.Add((match.Index, match.Length));
				}

				foreach (var kv in vars)
				{
					var inf = kv.Value;
					inf.AllocatedRegister = allocate_register_between((inf.First, inf.Last));
				}
			}

			foreach (var kv in vars)
			{
				var inf = kv.Value;

				if (!output.Unit.IntelSyntax)
				{
					string strsz;
					switch (inf.Size)
					{
					case "ptr": strsz = ""; break;
					case "1": strsz = "8"; break;
					default: strsz = inf.Size; break;
					}

					asm = asm.Replace(kv.Key, $"t{inf.AllocatedRegister}{strsz}");
				}
				else
				{
					string reg;
					string alc = inf.AllocatedRegister;
					switch (inf.Size)
					{
					case "8": reg = $"{alc}l"; break;
					case "16": reg = $"{alc}x"; break;
					case "32": reg = $"e{alc}x"; break;
					case "64": reg = $"r{alc}x"; break;

					case "ptr": goto case "32";
					case "1": goto case "8";
					default: throw new NotImplementedException();
					}

					asm = asm.Replace(kv.Key, reg);
				}
			}

			// also remove the indirection ptrptrs
			asm = Regex.Replace(asm, @"ptrptr\s*", "");

			output.Assembly.Clear();
			output.Assembly.Append(asm);
		}
	}

	//todo: convert:
	// cmp %1:32, 0
	// j(ne|e) $@_Z4itoaiPci_label_8
	// jmp...
	//to:
	// j(nz|ez) $@_Z4itoaiPci_label_8
	// jmp...
}