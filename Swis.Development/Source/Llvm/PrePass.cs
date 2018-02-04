using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		static void BuildMethodLocals(MethodBuilder output,
			string return_type, string argslist, bool optimize_args = true)
		{
			int bp_offset = -Cpu.NativeSizeBytes * 2; // -4 is base ptr, and -8 is ret addr

			{ // args
				dynamic[] args = argslist
					.PatternMatches("<type:type>( <keyword:mods>)* <namedlocal:name>");
				output.Arguments = new(string, string)[args.Length];

				for (int i = args.Length; i-- > 0;)
				{
					dynamic arg = args[i];
					output.Arguments[i] = (arg.name, arg.type);

					bp_offset -= SizeOfAsInt(arg.type) / 8;

					output.ConstantLocals[arg.name] = ToOperand(output, arg.type + "*", $"bp - {-bp_offset}", indirection: true);
					output.Emit($"; params: {arg.name} = bp - {-bp_offset}");
					
					// optimize a copy out:
					if (optimize_args)
					{
						string argalloc = $"{arg.name}.addr = alloca {arg.type}";
						string argstore = $"store {arg.type} {arg.name}, {arg.type}* {arg.name}.addr";

						var alloc = output.Code.IndexOf(argalloc);
						var store = output.Code.IndexOf(argstore);

						var allocend = output.Code.IndexOf('\n', alloc);
						var storeend = output.Code.IndexOf('\n', store);

						if (alloc == -1 || store == -1 || allocend == -1 || storeend == -1)
							throw new Exception();

						// remove it
						output.Code = output.Code.Remove(store, storeend - store);
						output.Code = output.Code.Remove(alloc, allocend - alloc);

						output.ConstantLocals[arg.name + ".addr"] = ToOperand(output, arg.type, $"bp - {-bp_offset}");
					}
				}
			}

			{ // ret
				int ret_bytes = SizeOfAsInt(return_type) / 8;

				if (ret_bytes > 0)
				{
					bp_offset -= SizeOfAsInt(return_type) / 8;
					output.ConstantLocals["ret"] = ToOperand(output, return_type + "*", $"bp - {-bp_offset}", indirection: true);
					output.Emit($"; return: ret = bp - {-bp_offset}");
				}
			}

			bp_offset = 0;

			{ // locals (alloca-s)
				string alloca_regex = PatternCompile(@"<namedlocal:id> = alloca <type:type>(, align <numeric:align>)\s*");

				output.Code = Regex.Replace(output.Code, alloca_regex, delegate (Match alloca)
				{
					int align = int.Parse(alloca.Groups["align"].Value);
					string name = alloca.Groups["id"].Value;

					// align bp to the alignment
					if (bp_offset % align != 0)
						bp_offset += align - (bp_offset % align);

					// assign it a constant offset
					output.ConstantLocals[name] = $"bp + {bp_offset}";
					output.Emit($"; locals: {name} = bp + {bp_offset}");

					// increase the bp offset by the size
					bp_offset += SizeOfAsInt(alloca.Groups["type"].Value) / 8;

					return "";
				});
			}

			output.Emit($"add sp, sp, {bp_offset} ; alloca");
		}

		/* // this needs to be implemented
		else if ((m = Regex.Match(line, $@"(?<dst>{operand_regex}) = (?<cmp_type>(i|u|f))cmp (?<cmp_method>(sgt|sge|sne|seq|slt|sle)) (?<tp>{type_regex}) (?<left>{operand_regex}), (?<right>{operand_regex})")).Success &&
			(m2 = Regex.Match(next, $@"br i1 {m.Groups["dst"].Value}, label %(?<true>[0-9]+), label %(?<false>[0-9]+)")).Success)
		{
			line_number++;

			string type = m.Groups["tp"].Value;
			string cmp_type = m.Groups["cmp_type"].Value;
			string cmp_method = m.Groups["cmp_method"].Value;
			string left = m.Groups["left"].Value;
			string right = m.Groups["right"].Value;

			string comparer = "";
			switch (cmp_type)
			{
			default: throw new NotImplementedException("unknown compare method " + cmp_method);
			case "i": comparer = "cmp"; break;
			case "f": comparer = "cmp"; break;
			case "u": comparer = "cmpf"; break;
			}

			// compare part
			emit_asm($"\t{comparer} {to_operand(type, left)}, {to_operand(type, right)}");

			string jmper = "";
			switch (cmp_method)
			{
			default: throw new NotImplementedException("unknown compare type " + cmp_method);
			case "sgt": jmper = "jg"; break;
			case "sge": jmper = "jge"; break;
			case "slt": jmper = "jl"; break;
			case "sle": jmper = "jle"; break;
			case "seq": jmper = "je"; break;
			case "sne": jmper = "jne"; break;
			}

			string on_true = $"${func_name}_label_{m2.Groups["true"].Value}";
			string on_false = $"${func_name}_label_{m2.Groups["false"].Value}";

			emit_asm($"\t{jmper} {on_true}");
			emit_asm($"\tjmp {on_false}");
		}
		else if ((m = Regex.Match(line, @"br label %(?<id>[0-9]+)")).Success)
		{
			string id = m.Groups["id"].Value;
			emit_asm($"\tjmp ${func_name}_label_{id}");
		}
		*/

		static void SimplifyCompareBranches(MethodBuilder output)
		{
			//%cmp = icmp eq i32 %1, 0
			//br i1 %cmp, label %2, label %8

			// ->

			//cmpbr i eq i32 %1, 0, label %2, label %8

			string rx = PatternCompile(
				@"<operand:dst> = (?<cmptype>i|u|f)?cmp <keyword:method> <type:type> <operand:left>, <operand:right>" +
				@"\s*\n\s*" +
				@"br <type:cond_type> <operand:cond>, (?<ontrue>(label|<type>) %<numeric>), (?<onfalse>(label|<type:onfalse_type>) %<numeric:onfalse>)"
			);

			output.Code = Regex.Replace(output.Code, rx,
				delegate (Match m) 
				{
					if (m.Groups["cond"].Value != m.Groups["dst"].Value) // only do it if it's the same jump
						return m.Value;

					bool used_before_or_after(string varname, string code, int from, int to)
					{
						int pos = code.IndexOf(varname);
						while (pos > 0)
						{
							if (pos < from | pos > to)
							{
								char c = code[pos + varname.Length]; // next char
								if (!char.IsLetterOrDigit(c))
									return true;
							}
							pos = code.IndexOf(varname, pos + varname.Length);
						}
						return false;
					}

					// if it's used anywhere else, don't
					if(used_before_or_after(m.Groups["dst"].Value, output.Code, m.Index, m.Index + m.Length))
						return m.Value;

					string cmptype = m.Groups["cmptype"].Value;
					string method = m.Groups["method"].Value;
					string type = m.Groups["type"].Value;
					string left = m.Groups["left"].Value;
					string right = m.Groups["right"].Value;
					string ontrue = m.Groups["ontrue"].Value;
					string onfalse = m.Groups["onfalse"].Value;
					return $"cmpbr {cmptype} {method} {type} {left}, {right}, {ontrue}, {onfalse}";
				});
		}

		static void ReplacePhis(MethodBuilder output)
		{
			//string prid_regex = $@"\[ (?<src>{local_regex}), %[0-9]+ \]";
			//string phi_regex = $@"(?<dst>{local_regex}) = phi {type_regex} (?<sources>{prid_regex}(, {prid_regex})+)";

			string phi_regexs = PatternCompile(@"<local:dst> = phi <type:type> (?<sources>\[ <local>, %<numeric> \](, \[ <local>, %<numeric> \])*)");
			MatchCollection phis = Regex.Matches(output.Code, phi_regexs);

			foreach (Match phi in phis)
			{
				string dst = phi.Groups["dst"].Value;
				dynamic[] preds = phi.Groups["sources"].Value.PatternMatches(@"\[ <local:src>, %<numeric:pred> \]");
				//MatchCollection preds = Regex.Matches(phi.Groups["sources"].Value, $@"{prid_regex}");

				foreach (dynamic pred in preds)
					output.Code = output.Code.Replace($"{pred.src} =", $"{dst} =");

				output.Code = output.Code.Replace(phi.Value, "");
			}
		}
	}
}