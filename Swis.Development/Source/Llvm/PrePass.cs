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
			int bp_offset = -(int)Cpu.NativeSizeBytes * 2; // -4 is base ptr, and -8 is ret addr

			{ // args
				dynamic[] args = argslist
					.PatternMatches("<type:type>( <keyword:mods>)* <namedlocal:name>", IrPatterns);
				output.Arguments = new(string, string)[args.Length];

				for (int i = args.Length; i-- > 0;)
				{
					dynamic arg = args[i];
					output.Arguments[i] = (arg.name, arg.type);

					bp_offset -= (int)output.Unit.SizeOfAsIntBytes(arg.type);

					output.ConstantLocals[arg.name] = output.ToOperand(arg.type + "*", $"{output.Unit.BasePointer} - {-bp_offset}", indirection: true);
					output.Emit($"; params: {arg.name} = {output.Unit.BasePointer} - {-bp_offset}");
					
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

						output.ConstantLocals[arg.name + ".addr"] = output.ToOperand(arg.type, $"{output.Unit.BasePointer} - {-bp_offset}");
					}
				}
			}

			{ // ret
				uint ret_bytes = output.Unit.SizeOfAsIntBytes(return_type);

				if (ret_bytes > 0)
				{
					bp_offset -= (int)ret_bytes;
					output.ConstantLocals["ret"] = output.ToOperand(return_type + "*", $"{output.Unit.BasePointer} - {-bp_offset}", indirection: true);
					output.Emit($"; return: ret = {output.Unit.BasePointer} - {-bp_offset}");
				}
			}

			bp_offset = 0;

			{ // locals (alloca-s)
				string alloca_regex = LlvmUtil.PatternCompile(@"<namedlocal:id> = alloca <type:type>(, align <numeric:align>)\s*", IrPatterns);

				output.Code = Regex.Replace(output.Code, alloca_regex, delegate (Match alloca)
				{
					int align = int.Parse(alloca.Groups["align"].Value);
					string name = alloca.Groups["id"].Value;

					// align bp to the alignment
					if (bp_offset % align != 0)
						bp_offset += align - (bp_offset % align);

					// assign it a constant offset
					output.ConstantLocals[name] = $"{output.Unit.BasePointer} + {bp_offset}";
					output.Emit($"; locals: {name} = {output.Unit.BasePointer} + {bp_offset}");

					// increase the bp offset by the size
					bp_offset += (int)output.Unit.SizeOfAsIntBytes(alloca.Groups["type"].Value);

					return "";
				});
			}

			if(bp_offset > 0)
				output.Emit($"add {output.Unit.StackPointer}, {output.Unit.StackPointer}, {bp_offset} ; alloca");
		}
		
		static void ExpandConstants(MethodBuilder output)
		{
			// https://llvm.org/docs/LangRef.html#constant-expressions

			//convert things like:
			// call void @puts(i8* getelementptr inbounds ([14 x i8], [14 x i8]* @.str, i32 0, i32 0))
			//into
			// %__argconst1__ = i8* getelementptr inbounds ([14 x i8], [14 x i8]* @.str, i32 0, i32 0)
			// call void @puts(i8* %__argconst1__)

			//string startswith = @"(?<=[\(\,]|(^|\n)\s*<keyword>)\s*"; // within keyword
			string startswith = @"(?<=[\(\,a-z])\s+"; // within keyword
			string rx = LlvmUtil.PatternCompile($@"{startswith}<constexp:exp>", IrPatterns);

			while (true)
			{
				Match m = output.Code.Match(rx);
				if (!m.Success)
					break;

				string type = m.Groups["exp_type"].Value;
				string op = m.Groups["exp_op"].Value;
				string args = m.Groups["exp_args_inside"].Value;
				string reg = output.CreateSSARegister("constexp");

				//exp = exp.Substring(1, exp.Length - 2); // remove the ()s

				string new_arg = $" {type} {reg}";
				output.Code = output.Code.Remove(m.Index, m.Length);
				output.Code = output.Code.Insert(m.Index, new_arg);

				int i;
				for (i = m.Index; i --> 0;)
					if (output.Code[i] == '\n')
						break;

				output.Code = output.Code.Insert(i + 1, $"  {reg} = {op} {args}\n");
			}
			
		}

		static void SimplifyCompareBranches(MethodBuilder output)
		{
			//%cmp = icmp eq i32 %1, 0
			//br i1 %cmp, label %2, label %8

			// ->

			//cmpbr i eq i32 %1, 0, label %2, label %8

			string rx = LlvmUtil.PatternCompile(
				@"<operand:dst> = (?<cmptype>i|u|f)?cmp <keyword:method> <type:type> <operand:left>, <operand:right>" +
				@"\s*\n\s*" +
				@"br <type:cond_type> <operand:cond>, (?<ontrue>(label|<type>) %<numeric>), (?<onfalse>(label|<type:onfalse_type>) %<numeric:onfalse>)", IrPatterns
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

			string phi_regexs = LlvmUtil.PatternCompile(@"<local:dst> = phi <type:type> (?<sources>\[ <local>, %<numeric> \](, \[ <local>, %<numeric> \])*)", IrPatterns);
			MatchCollection phis = Regex.Matches(output.Code, phi_regexs);

			foreach (Match phi in phis)
			{
				string dst = phi.Groups["dst"].Value;
				dynamic[] preds = phi.Groups["sources"].Value.PatternMatches(@"\[ <local:src>, %<numeric:pred> \]", IrPatterns);
				//MatchCollection preds = Regex.Matches(phi.Groups["sources"].Value, $@"{prid_regex}");

				foreach (dynamic pred in preds)
					output.Code = output.Code.Replace($"{pred.src} =", $"{dst} =");

				output.Code = output.Code.Replace(phi.Value, "");
			}
		}
	}
}