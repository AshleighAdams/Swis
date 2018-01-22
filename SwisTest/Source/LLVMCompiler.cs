using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

// https://godbolt.org/

namespace Swis
{
	// https://github.com/llvm-mirror/llvm/blob/master/include/llvm/IR/Instruction.def
	public static class LLVMCompiler
	{
		#region Test code

		/*
			
		*/

		public static string TestIR = @"
define void @_Z7reversePci(i8* %str, i32 %length) #0 {
  %str.addr = alloca i8*, align 4
  %length.addr = alloca i32, align 4
  %start = alloca i32, align 4
  %end = alloca i32, align 4
  %tmp = alloca i8, align 1
  store i8* %str, i8** %str.addr, align 4
  store i32 %length, i32* %length.addr, align 4
  store i32 0, i32* %start, align 4
  %1 = load i32, i32* %length.addr, align 4
  %sub = sub nsw i32 %1, 1
  store i32 %sub, i32* %end, align 4
  br label %2

; <label>:2:                                      ; preds = %5, %0
  %3 = load i32, i32* %start, align 4
  %4 = load i32, i32* %end, align 4
  %cmp = icmp slt i32 %3, %4
  br i1 %cmp, label %5, label %19

; <label>:5:                                      ; preds = %2
  %6 = load i8*, i8** %str.addr, align 4
  %7 = load i32, i32* %start, align 4
  %add.ptr = getelementptr inbounds i8, i8* %6, i32 %7
  %8 = load i8, i8* %add.ptr, align 1
  store i8 %8, i8* %tmp, align 1
  %9 = load i8*, i8** %str.addr, align 4
  %10 = load i32, i32* %end, align 4
  %add.ptr1 = getelementptr inbounds i8, i8* %9, i32 %10
  %11 = load i8, i8* %add.ptr1, align 1
  %12 = load i8*, i8** %str.addr, align 4
  %13 = load i32, i32* %start, align 4
  %add.ptr2 = getelementptr inbounds i8, i8* %12, i32 %13
  store i8 %11, i8* %add.ptr2, align 1
  %14 = load i8, i8* %tmp, align 1
  %15 = load i8*, i8** %str.addr, align 4
  %16 = load i32, i32* %end, align 4
  %add.ptr3 = getelementptr inbounds i8, i8* %15, i32 %16
  store i8 %14, i8* %add.ptr3, align 1
  %17 = load i32, i32* %start, align 4
  %inc = add nsw i32 %17, 1
  store i32 %inc, i32* %start, align 4
  %18 = load i32, i32* %end, align 4
  %dec = add nsw i32 %18, -1
  store i32 %dec, i32* %end, align 4
  br label %2

; <label>:19:                                     ; preds = %2
  ret void
}";
		#endregion

		public static string Compile(string code)
		{
			int ptr_size = 4;
			//scan the code and figure out:
			//	what values are stack offsets and can be hard coded
			//	what values are negative stack offsets (arguments)
			//attempt to reuse any temporary variables

			string type_regex = @"(?<type_attr>(signext )*)?(?<type>((?<type_letter>u|i|f)(?<type_size>8|16|32)|void)(?<ptr>\*+)?)";
			string const_regex = @"(?<const>-?[0-9]+)";
			string ident_regex = @"(?<id>[%@][-a-zA-Z$._][-a-zA-Z$._0-9]*)";
			string namedlocal_regex = @"(?<id>[%][-a-zA-Z$._][-a-zA-Z$._0-9]*)";
			string temp_regex = @"(?<id>[%][[0-9]+)";
			string local_regex = $"({namedlocal_regex}|{temp_regex})";
			string operand_regex = $"(?<operand>{const_regex}|{local_regex})";
			string global_regex = @"(?<id>[@][-a-zA-Z$._][-a-zA-Z$._0-9]*)";

			string arglist_regex = @"(?<arglist>[%\-a-zA-Z$._0-9\*, ]*)";

			string func_regex = $@"define {type_regex} {ident_regex}\({arglist_regex}\) #(?<attrib>[0-9]+) \{{(?<body>(?:[^{{}}]|(?<counter>\{{)|(?<-counter>\}}))+(?(counter)(?!)))\}}";

			StringBuilder sb = new StringBuilder();
			void emit_asm(string operation)
			{
				sb.AppendLine(operation);
			}
			
			// first let's find our arguments and body
			var funcs = Regex.Matches(code, func_regex);
			foreach (Match func in funcs)
			{
				string return_type = func.Groups["type"].Value;
				string func_name = func.Groups["id"].Value;
				string arglist = func.Groups["arglist"].Value;
				string body = func.Groups["body"].Value;

				int ret_size_bytes = 0;
				if(return_type != "void")
					ret_size_bytes = int.Parse(func.Groups["type_size"].Value) / 8;

				if (return_type.EndsWith('*'))
					ret_size_bytes = ptr_size;

				Console.WriteLine($"{return_type} {func_name}({arglist})");

				int stack_variable_size = 0; // how much we need to increase the stack ptr by, args and rets are before bp
				var constant_locals = new Dictionary<string, string>();

				string size_of(string type)
				{
					if (type.EndsWith('*'))
						return "ptr";
					return Regex.Match(type, "[0-9]+").Value;
				}

				string type_deref(string type)
				{
					if (type.EndsWith('*'))
						return type.Substring(0, type.Length - 1);
					throw new Exception("deref non ptr");
				}

				// i16*, %value.addr, true = ptr16 [%value.addr:ptr]
				// i16**, %value.addr, true = ptr [%value.addr:ptr]
				// i16**, %5, true = ptr [%5:ptr]
				// i16*, %5, false = %5:ptr
				// i16, %5, false = %5:16
				string to_operand(string type, string operand, bool indirection = false)
				{
					string part = "";

					string size = size_of(type);

					if (operand[0] == '@')
						part = $"${operand}";
					if (operand[0] != '%')
						part = $"{operand}"; // constants are always 32bits
											 // is it a calculated const size?
					else if (constant_locals.TryGetValue(operand, out var cl))
						part = cl;
					else // use a register
						part = $"{operand}:{size}";

					if (indirection)
					{
						string size_to = size_of(type_deref(type));
						part = $"ptr{size_to} [{part}]";
					}

					return part;
				}
				
				int bp_offset = 0;
				{ // assign the basepointer offsets to the argument locals
					string arg_regex = $@"{type_regex} (?<attr>(signext ))?{namedlocal_regex}";
					MatchCollection args = Regex.Matches(arglist, arg_regex);
					
					// we want to go in reverse
					for (int i = args.Count; i-- > 0;)
					{
						Match m = args[i];
						string type = m.Groups["type"].Value;
						string arg_size_bits_str = size_of(type);// m.Groups["type_size"].Value;
						int arg_size_bytes = arg_size_bits_str == "ptr" ? 4 : int.Parse(arg_size_bits_str) / 8;
						
						string arg_name = m.Groups["id"].Value;
						
						bp_offset -= arg_size_bytes;

						// these have brackets due to them being on the stack, and this using indirection
						// llvm assumes argument variables are passed as registers
						constant_locals[arg_name] = to_operand(type + "*", $"bp - {-bp_offset}", indirection: true);
						Console.WriteLine($"\t {arg_name} = bp - {-bp_offset}");
					}
				}

				{ // assign the ret offset
					bp_offset -= ret_size_bytes;
					constant_locals["ret"] = to_operand(return_type + "*", $"bp - {-bp_offset}", indirection: true);
					// $"[bp - {-bp_offset}]";
					Console.WriteLine($"\t ret = bp - {-bp_offset}");
				}

				{ // now do alloca's
					bp_offset = 0; // each alloca grows this instead now, starting from zero

					string alloca_regex = $@"{namedlocal_regex} = alloca {type_regex}, align (?<align>[0-9]+)";
					MatchCollection allocas = Regex.Matches(body, alloca_regex);

					foreach (Match alloca in allocas)
					{
						int align = int.Parse(alloca.Groups["align"].Value);
						string name = alloca.Groups["id"].Value;

						// align bp to the alignment
						bp_offset += Math.Abs((-bp_offset) % align);

						// assign it a constant offset
						constant_locals[name] = $"bp + {bp_offset}";
						Console.WriteLine($"\t {name} = bp + {bp_offset}");

						// increase the bp offset by the size
						string varsizestr = size_of(alloca.Groups["type"].Value); // int.Parse(alloca.Groups["type_size"].Value) / 8;
						int varsize = varsizestr == "ptr" ? ptr_size : int.Parse(varsizestr) / 8;
						bp_offset += varsize;
					}

					// remove the useless alloca's from the body now we have their offsets, doing it backward so we don't mess any offsets up
					for (int i = allocas.Count; i-- > 0;)
						body = body.Remove(allocas[i].Index, allocas[i].Length);
					
				}

				// getelementptrs
				
				// emit the function lable
				emit_asm($"${func_name}:");
				// emit the asm to stack-alloc these values:
				emit_asm($"\tadd sp, sp, {bp_offset}; alloca"); // we don't need to shrink this, it is done automatically on ret


				{ // translate the IR instructions into ASM instructions, but using virtual registers for now
					string[] lines = body.Split('\n');

					for(int line_number = 0; line_number < lines.Length; line_number++)
					//foreach (string line in lines)
					{
						string line = lines[line_number];
						string next = line_number + 1 >= lines.Length ? "" : lines[line_number + 1];
						//emit_asm($"\t; {line}");

						Match m, m2;

						if (string.IsNullOrWhiteSpace(line))
							continue; // transmit the whitespace
									  // memory
						else if ((m = Regex.Match(line, $"store (?<src>{type_regex} {operand_regex}), (?<dst>{type_regex} {operand_regex}), align (?<align>[0-9]+)")).Success)
						{
							// store i32 %value, i32* %value.addr, align 4
							// mov ptr32 [%value.addr], value

							string src = m.Groups["src"].Value;
							string dst = m.Groups["dst"].Value;
							string aln = m.Groups["align"].Value;

							Match srcm = Regex.Match(src, $"{type_regex} {operand_regex}");
							Match dstm = Regex.Match(dst, $"{type_regex} {operand_regex}");

							string src_tp = srcm.Groups["type"].Value;
							string src_op = srcm.Groups["operand"].Value;

							string dst_tp = dstm.Groups["type"].Value;
							string dst_op = dstm.Groups["operand"].Value;

							emit_asm($"\tmov {to_operand(dst_tp, dst_op, indirection: true)}, {to_operand(src_tp, src_op)}");
						}
						else if ((m = Regex.Match(line, $"(?<dst_loc>{operand_regex}) = load (?<dst_type>{type_regex}), (?<src>{type_regex} {operand_regex}), align (?<align>[0-9]+)")).Success)
						{
							string src = m.Groups["src"].Value;
							Match srcm = Regex.Match(src, $"{type_regex} {operand_regex}");

							string src_tp = srcm.Groups["type"].Value;
							string src_op = srcm.Groups["operand"].Value;

							string dst_tp = m.Groups["dst_type"].Value;
							string dst_op = m.Groups["dst_loc"].Value;

							emit_asm($"\tmov {to_operand(dst_tp, dst_op)}, {to_operand(src_tp, src_op, indirection: true)}");
						}
						// flow
						// <label>:3:
						else if ((m = Regex.Match(line, $"ret {type_regex} {operand_regex}")).Success)
						{
							string rettp = m.Groups["type"].Value;
							string retop = m.Groups["operand"].Value;
							emit_asm($"\tmov {constant_locals["ret"]}, {to_operand(rettp, retop)}");
							emit_asm($"\tret");
						}
						else if ((m = Regex.Match(line, "ret void")).Success)
						{
							emit_asm("\tret");
						}
						else if ((m = Regex.Match(line, @"<label>:(?<id>[0-9]+):")).Success)
						{
							string id = m.Groups["id"].Value;
							emit_asm($"\t${func_name}_label_{id}:");
						}
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
						// transformative
						else if ((m = Regex.Match(line, $"(?<dst_op>{operand_regex}) = sext (?<src_tp>{type_regex}) (?<src_op>{operand_regex}) to (?<dst_tp>{type_regex})")).Success)
						{
							string src_tp = m.Groups["src_tp"].Value;
							string src_op = m.Groups["src_op"].Value;
							string dst_tp = m.Groups["dst_tp"].Value;
							string dst_op = m.Groups["dst_op"].Value;
							emit_asm($"\tmov {to_operand(dst_tp, dst_op)}, {to_operand(src_tp, src_op)} ; sigex");
						}
						else if ((m = Regex.Match(line, $"(?<dst_op>{operand_regex}) = trunc (?<src_tp>{type_regex}) (?<src_op>{operand_regex}) to (?<dst_tp>{type_regex})")).Success)
						{
							string src_tp = m.Groups["src_tp"].Value;
							string src_op = m.Groups["src_op"].Value;
							string dst_tp = m.Groups["dst_tp"].Value;
							string dst_op = m.Groups["dst_op"].Value;
							emit_asm($"\tmov {to_operand(dst_tp, dst_op)}, {to_operand(src_tp, src_op)} ; trunc");
						}
						else if ((m = Regex.Match(line, $"(?<dst_op>{operand_regex}) = (?<action>(add|sub|mul|div))( nsw| nsu)* (?<tp>{type_regex}) (?<left_op>{operand_regex}), (?<right_op>{operand_regex})")).Success)
						{
							string act = m.Groups["action"].Value;
							string tp = m.Groups["tp"].Value;
							string dst_op = m.Groups["dst_op"].Value;
							string left_op = m.Groups["left_op"].Value;
							string right_op = m.Groups["right_op"].Value;
							emit_asm($"\t{act} {to_operand(tp, dst_op)}, {to_operand(tp, left_op)}, {to_operand(tp, right_op)}");
						}
						else
						{
							emit_asm($"\t;{line.Trim()}");
							//throw new Exception($"could not match: {line}");
						}
					}
				}

				// TODO: remove unnec jumps
				//Console.WriteLine(body);
				
				// the return value position, llvm-ir supports only 1 return value, but 
			}

			return sb.ToString();
		}
	}
}