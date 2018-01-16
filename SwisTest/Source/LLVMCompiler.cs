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
; Function Attrs: noinline nounwind
define signext i16 @_Z11alloca_testsPs(i16 signext %value, i16* %other) #0 {
  %value.addr = alloca i16, align 2
  %other.addr = alloca i16*, align 4
  %x = alloca i16, align 2
  store i16 %value, i16* %value.addr, align 2
  store i16* %other, i16** %other.addr, align 4
  store i16 5, i16* %x, align 2
  %1 = load i16, i16* %value.addr, align 2
  %conv = sext i16 %1 to i32
  %2 = load i16, i16* %x, align 2
  %conv1 = sext i16 %2 to i32
  %cmp = icmp sgt i32 %conv, %conv1
  br i1 %cmp, label %3, label %7

; <label>:3:                                      ; preds = %0
  %4 = load i16, i16* %value.addr, align 2
  %conv2 = sext i16 %4 to i32
  %5 = load i16*, i16** %other.addr, align 4
  %6 = load i16, i16* %5, align 2
  %conv3 = sext i16 %6 to i32
  %mul = mul nsw i32 %conv2, %conv3
  %conv4 = trunc i32 %mul to i16
  store i16 %conv4, i16* %x, align 2
  br label %7

; <label>:7:                                      ; preds = %3, %0
  %8 = load i16, i16* %x, align 2
  ret i16 %8
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
			string const_regex = @"(?<const>[0-9]+)";
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

				int ret_size_bytes = int.Parse(func.Groups["type_size"].Value) / 8;

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
						int varsize = int.Parse(alloca.Groups["type_size"].Value) / 8;
						string name = alloca.Groups["id"].Value;

						// align bp to the alignment
						bp_offset += Math.Abs((-bp_offset) % align);

						// assign it a constant offset
						constant_locals[name] = $"bp + {bp_offset}";
						Console.WriteLine($"\t {name} = bp + {bp_offset}");

						// increase the bp offset by the size
						bp_offset += varsize;
					}

					// remove the useless alloca's from the body now we have their offsets, doing it backward so we don't mess any offsets up
					for (int i = allocas.Count; i-- > 0;)
						body = body.Remove(allocas[i].Index, allocas[i].Length);
					
				}
				
				// emit the function lable
				emit_asm($"${func_name}:");
				// emit the asm to stack-alloc these values:
				emit_asm($"\tadd sp, sp, {bp_offset}; alloca"); // we don't need to shrink this, it is done automatically on ret


				{ // translate the IR instructions into ASM instructions, but using virtual registers for now
					string[] lines = body.Split('\n');
					foreach (string line in lines)
					{
						emit_asm($"\t; {line}");

						Match m;

						if (string.IsNullOrWhiteSpace(line))
							continue; // transmit the whitespace
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
						// <label>:3:
						else if ((m = Regex.Match(line, @"<label>:(?<id>[0-9]+):")).Success)
						{
							string id = m.Groups["id"].Value;
							emit_asm($"\t${func_name}_label_{id}:");
						}
						else if ((m = Regex.Match(line, @"br label %(?<id>[0-9]+)")).Success)
						{
							string id = m.Groups["id"].Value;
							emit_asm($"\tjmp ${func_name}_label_{id}");
						}
						else if ((m = Regex.Match(line, $"ret {type_regex} {operand_regex}")).Success)
						{
							string rettp = m.Groups["type"].Value;
							string retsz = m.Groups["type_size"].Value;
							string retop = m.Groups["operand"].Value;
							emit_asm($"\tmov {constant_locals["ret"]}, {to_operand(rettp, retop)}");
							emit_asm($"\tret");
						}
						else
						{
							emit_asm($"\tnop");
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