using System;
using System.Collections.Generic;
using System.Text;

namespace Swis
{
	// https://github.com/llvm-mirror/llvm/blob/master/include/llvm/IR/Instruction.def
	// https://llvm.org/docs/LangRef.html
	public static partial class LlvmIrCompiler
	{
		#region Misc
		[IrInstruction("bitcast", "<operand:dst> = bitcast <type:srctype> <operand:src> to <type:dsttype>")]
		private static bool Bitcast(MethodBuilder output, dynamic args)
		{
			string dst = args.dst;
			string dsttype = args.dsttype;
			string src = args.src;
			string srctype = args.srctype;

			output.Emit($"mov {output.ToOperand(dsttype, dst)}, {output.ToOperand(srctype, src)}; bitcast");
			return true;
		}

		[IrInstruction("ptrtoint", "<operand:dst> = ptrtoint <type:srctype> <operand:src> to <type:dsttype>")]
		private static bool Ptrtoint(MethodBuilder output, dynamic args)
		{
			string dst = args.dst;
			string dsttype = args.dsttype;
			string src = args.src;
			string srctype = args.srctype;

			output.Emit($"mov {output.ToOperand(dsttype, dst)}, {output.ToOperand(srctype, src)}; ptrtoint");
			return true;
		}

		[IrInstruction("inttoptr", "<operand:dst> = inttoptr <type:srctype> <operand:src> to <type:dsttype>")]
		private static bool Inttoptr(MethodBuilder output, dynamic args)
		{
			string dst = args.dst;
			string dsttype = args.dsttype;
			string src = args.src;
			string srctype = args.srctype;

			output.Emit($"mov {output.ToOperand(dsttype, dst)}, {output.ToOperand(srctype, src)}; inttoptr");
			return true;
		}

		// getelementptr is malformed: %arrayidx = getelementptr inbounds i8, i8* %3, i32 %4
		[IrInstruction("getelementptr", "<operand:dst> = getelementptr(?<inbounds> inbounds)? <type:basis>(?<indexers>(,( inrange)? <type> <operand>)+)")]
		private static bool Getelementptr(MethodBuilder output, dynamic args)
		{
			bool inbounds = args.inbounds != "";
			//string base_type = args.basis;
			string indexersstr = args.indexers;

			dynamic[] indexers = indexersstr.PatternMatches("(inrange )?<type:type> <operand:index>", IrPatterns);

			List<string> dynamic_offsets_operands = new List<string>();

			string base_type = indexers[0].type;
			string base_operand = indexers[0].index;

			string type = base_type;
			int static_offset = 0;

			for (int i = 1; i < indexers.Length; i++)
			{
				dynamic indexer = indexers[i];

				string operand = indexer.index;
				string operand_type = indexer.type;

				if (!char.IsDigit(operand[0]))
				{
					(string subtype, string genop) = output.Unit.DynamicTypeIndex(output, type, operand_type, operand);
					dynamic_offsets_operands.Add(genop);
					type = subtype;
				}
				else
				{
					int index = int.Parse(operand);
					(string subtype, int suboffset) = output.Unit.StaticTypeIndex(type, index);
					static_offset += suboffset;
					type = subtype;
				}
			}

			string destop = output.ToOperand("void*", args.dst);
			{
				string baseop;
				if (static_offset == 0)
					baseop = $"{output.ToOperand(base_type, base_operand)}";
				else
					baseop = $"{output.ToOperand(base_type, base_operand)} + {static_offset}";
				dynamic_offsets_operands.Insert(0, baseop);
			}

			// optimize out some add instructions by condensing them down into the operand form a + b + c * 1 if possible
			if (output.Unit.OptimizationLevel >= 1)
			{
				(int adds, int muls) simpleop(int index)
				{
					string op = dynamic_offsets_operands[index];
					return (op.PatternMatches(@"\+", IrPatterns).Length, op.PatternMatches(@"\*", IrPatterns).Length);
				}
				for (int i = 0; i < dynamic_offsets_operands.Count;)
				{
					var info = simpleop(i);
					if (info.adds >= 2 || info.muls >= 1)
					{
						i++;
						continue;
					}

					string condensed = dynamic_offsets_operands[i];
					while (i + 1 < dynamic_offsets_operands.Count)
					{
						var info2 = simpleop(i + 1);
						if (info.adds + info2.adds + 1 > 2 || info.muls + info2.muls > 1)
							break;

						info.adds += info2.adds + 1;
						info.muls += info2.muls;

						condensed = $"{condensed} + {dynamic_offsets_operands[i + 1]}";
						dynamic_offsets_operands.RemoveAt(i + 1);
					}
					dynamic_offsets_operands[i] = condensed;
					i++;
				}
			}

			if (dynamic_offsets_operands.Count == 1)
			{
				if (output.Unit.OptimizationLevel >= 1)
				{
					output.Emit($"; constexp getelementptr: {args.dst}");
					output.ConstantLocals[args.dst] = dynamic_offsets_operands[0];
				}
				else
				{
					output.Emit($"add {destop}, {dynamic_offsets_operands[0]}, 0 ; getelementptr");
				}
			}
			else
			{
				string baseop = dynamic_offsets_operands[0];
				for (int i = 1; i < dynamic_offsets_operands.Count; i++)
				{
					int remain = dynamic_offsets_operands.Count - i - 1;

					output.Emit($"add {destop}, {baseop}, {dynamic_offsets_operands[i]} ; getelementptr");
					baseop = destop;
				}
			}
			return true;
		}

		[IrInstruction("trunc", "<operand:dst> = trunc <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Trunc(MethodBuilder output, dynamic args)
		{
			int tosz = int.Parse(args.dst_type_size);
			if (tosz % 8 != 0)
			{
				output.Emit(
					$"and " +
						$"{output.ToOperand(args.dst_type, args.dst)}, " +
						$"{output.ToOperand(args.src_type, args.src)}, " +
						$"{(1 << tosz) - 1} ; trunc to i{tosz}");
			}
			else
			{
				output.Emit(
					$"mov " +
					$"{output.ToOperand(args.dst_type, args.dst)}, " +
					$"{output.ToOperand(args.src_type, args.src)} ; trunc {args.src_type} -> {args.dst_type}");
			}
			return true;
		}

		[IrInstruction("sext", "<operand:dst> = sext <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Sext(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"sext " +
				$"{output.ToOperand(args.dst_type, args.dst)}, " +
				$"{output.ToOperand(args.src_type, args.src)}, " +
				$"{args.src_type_size}");
			return true;
		}

		[IrInstruction("zext", "<operand:dst> = zext <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Zext(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"zext " +
				$"{output.ToOperand(args.dst_type, args.dst)}, " +
				$"{output.ToOperand(args.src_type, args.src)}, " +
				$"{args.src_type_size}");
			return true;
		}

		#endregion

		#region Memory
		[IrInstruction("store", "store <type:src_type> <operand:src>, <type:dst_type> <operand:dst>(, align <numeric:align>)?")]
		private static bool Store(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"mov " +
				$"{output.ToOperand(args.dst_type, args.dst, indirection: true)}, " +
				$"{output.ToOperand(args.src_type, args.src)}");
			return true;
		}

		[IrInstruction("load", "<operand:dst> = load <type:dst_type>, <type:src_type> <operand:src>(, align <numeric:align>)?")]
		private static bool Load(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"mov " +
				$"{output.ToOperand(args.dst_type, args.dst)}, " +
				$"{output.ToOperand(args.src_type, args.src, indirection: true)}");
			return true;
		}
		#endregion

		#region Flow

		private static string Targetify(MethodBuilder output, string type, string where)
		{
			if (string.IsNullOrWhiteSpace(type)) // is it a label?
				return $"${output.Id}_label_{where}";
			return output.ToOperand(type, where);
		}

		[IrInstruction("ret", "ret void")]
		private static bool RetVoid(MethodBuilder output, dynamic args)
		{
			output.Emit("ret");
			return true;
		}

		[IrInstruction("ret", "ret <type:type> <operand:what>")]
		private static bool Ret(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"mov " +
				$"{output.ConstantLocals["ret"]}, " +
				$"{output.ToOperand(args.type, args.what)}");
			output.Emit("ret");
			return true;
		}

		[IrInstruction("cmpbr", @"cmpbr (?<cmptype>[a-z]) <keyword:method> <type:type> <operand:left>, <operand:right>, (label|<type:ontrue_type>) %<numeric:ontrue>, (label|<type:onfalse_type>) %<numeric:onfalse>\s*$")]
		private static bool Cmpbr(MethodBuilder output, dynamic args)
		{
			(string postfix, string method, string thirdop) irfcmp_to_asm_inverted(string m)
			{
				switch (m)
				{
					case "oeq": return ("f", "ne", ", 1");
					case "ueq": return ("f", "ne", ", 0");
					case "one": return ("f", "e", ", 1");
					case "une": return ("f", "e", ", 0");
					case "olt": return ("f", "ge", ", 1");
					case "ult": return ("f", "ge", ", 0");
					case "ole": return ("f", "g", ", 1");
					case "ule": return ("f", "g", ", 0");
					case "ogt": return ("f", "le", ", 1");
					case "ugt": return ("f", "le", ", 0");
					case "oge": return ("f", "l", ", 1");
					case "uge": return ("f", "l", ", 0");
					default: throw new NotImplementedException(m);
				}
			}

			(string postfix, string method, string thirdop) iricmp_to_asm_inverted(string m)
			{
				switch (m)
				{
					case "eq": return ("u", "ne", "");
					case "ne": return ("u", "e", "");
					case "slt": return ("", "ge", "");
					case "ult": return ("u", "ge", "");
					case "sle": return ("", "g", "");
					case "ule": return ("u", "g", "");
					case "sgt": return ("", "le", "");
					case "ugt": return ("u", "le", "");
					case "sge": return ("", "l", "");
					case "uge": return ("u", "l", "");
					default: throw new NotImplementedException(m);
				}
			}

			// NOTE: ontrue and onfalse are swapped for cleaner code
			string postfix, method, third;
			if (args.cmptype == "f")
				(postfix, method, third) = irfcmp_to_asm_inverted(args.method);
			else if (args.cmptype == "i")
				(postfix, method, third) = iricmp_to_asm_inverted(args.method);
			else
				return false;

			// can we optimize it into a jz/jnz call?
			if (args.right == "0" && (method == "ne" || method == "e") && (postfix == "" || postfix == "u")) //-V3063
			{
				method = method == "ne" ? "nz" : "z";
				string asm2 = $"j{method} {output.ToOperand(args.type, args.left)}, {Targetify(output, args.onfalse_type, args.onfalse)}";
				string asm3 = $"jmp {Targetify(output, args.ontrue_type, args.ontrue)}";
				output.Emit(asm2);
				output.Emit(asm3);
			}
			else
			{
				string asm1 = $"cmp{postfix} {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}{third}";
				string asm2 = $"j{method} {Targetify(output, args.onfalse_type, args.onfalse)}";
				string asm3 = $"jmp {Targetify(output, args.ontrue_type, args.ontrue)}";

				output.Emit(asm1);
				output.Emit(asm2);
				output.Emit(asm3);
			}
			return true;
		}

		[IrInstruction("br", @"br (label|<type:uncond_type>) %<operand:uncond>\s*$")]
		private static bool BrUnconditional(MethodBuilder output, dynamic args)
		{
			output.Emit($"jmp {Targetify(output, args.uncond_type, args.uncond)}");
			return true;
		}
		[IrInstruction("br", @"br <type:cond_type> <operand:cond>, (label|<type:ontrue_type>) %<operand:ontrue>, (label|<type:onfalse_type>) %<operand:onfalse>\s*$")]
		private static bool BrConditional(MethodBuilder output, dynamic args)
		{
			if (args.cond_type_size != "1")
				throw new Exception();

			//output.Emit($"jnz {output.ToOperand(args.cond_type, args.cond)}, {Targetify(output, args.ontrue_type, args.ontrue)}");
			//output.Emit($"jmp {Targetify(output, args.onfalse_type, args.onfalse)}");

			// inverting it generally allows us to optimize a jump out later
			output.Emit($"jz {output.ToOperand(args.cond_type, args.cond)}, {Targetify(output, args.onfalse_type, args.onfalse)}");
			output.Emit($"jmp {Targetify(output, args.ontrue_type, args.ontrue)}");

			return true;
		}

		[IrInstruction("call", @"(<operand:dst> = )?call (<type:ret_type>) asm( sideeffect)? ""(?<asm>[^""]+)"", ""(?<flags>[^""]+)""\s*<parentheses:args>( #<numeric>)?, (?<rest>.+)$")]
		private static bool CallAsm(MethodBuilder output, dynamic match)
		{
			string asm = match.asm;
			string strargs = match.args_inside;

			dynamic[] args = strargs.PatternMatches("<type:type> <operand:operand>", IrPatterns);

			for (int i = 0; i < args.Length; i++)
			{
				asm = asm.Replace($"${i}", output.ToOperand(args[i].type, args[i].operand));
			}

			output.Emit(asm);
			return true;
		}

		// TODO: add all of these <result> = [tail | musttail | notail ] call [fast-math flags] [cconv] [ret attrs] <ty>|<fnty> <fnptrval>(<function args>) [fn attrs] [operand bundles]
		[IrInstruction("call", @"(<operand:dst> = )?call( <retattributes>)* (<type:ret_type>) (<operand:func>)\s*<parentheses:args>")]
		private static bool Call(MethodBuilder output, dynamic match)
		{
			string callargs = match.args_inside;
			string ret_type = match.ret_type;
			string dst = match.dst;

			dynamic[] args = callargs.PatternMatches("<type:type>( <paramattributes>)* <operand:src>", IrPatterns);

			uint ret_size = output.Unit.SizeOfAsIntBytes(ret_type);
			uint[] arg_sizes = new uint[args.Length];
			uint[] arg_sp = new uint[args.Length];
			uint total_size = ret_size;

			uint stack_offset = 0;
			for (int i = args.Length; i --> 0;)
			{
				dynamic argmatch = args[i];
				uint size = output.Unit.SizeOfAsIntBytes(argmatch.type);
				arg_sp[i] = stack_offset += size;
				total_size += arg_sizes[i] = size;
			}
			uint ret_sp_offset = stack_offset + ret_size;

			{
				StringBuilder comment = new StringBuilder();
				comment.Append($"; call {ret_type} {match.func}(");
				string pre = "";
				for (int i = 0; i < arg_sizes.Length; i++)
				{
					comment.Append($"{pre}{args[i].type}");
					pre = ", ";
				}
				comment.Append($")");
				output.Emit("");
				output.Emit($"{comment}");
			}

			if (total_size > 0)
				output.Emit($"add {output.Unit.StackPointer}, {output.Unit.StackPointer}, {total_size} ; allocate space for return and arguments");

			for (int i = 0; i < arg_sizes.Length; i++)
			{
				uint argsz = arg_sizes[i];

				switch (argsz * 8)
				{
					case 8:
					case 16:
					case 32:
						output.Emit($"mov ptr{argsz * 8} [{output.Unit.StackPointer} - {arg_sp[i]}], {output.ToOperand(args[i].type, args[i].src)} ; copy arg #{i + 1}");
						break;
					default:
						// it *must* be a pointer type
						uint arg_sp_offset_at = arg_sp[i];
						//while (argsz > 0)
						//{
						//	if (argsz > 32)
						//	{
						//		
						//		//output.Emit
						//	}
						//}
						throw new NotImplementedException();
						//break;
						// break it down bit by bit
				}
			}

			output.Emit($"call {output.ToOperand("void*", match.func)}");

			if (ret_size > 0)
			{
				if (ret_size > 32)
					throw new NotImplementedException();
				output.Emit($"mov {output.ToOperand(match.ret_type, match.dst)}, ptr{ret_size * 8} [{output.Unit.StackPointer} - {ret_sp_offset}] ; copy return");
			}

			if (total_size > 0)
				output.Emit($"sub {output.Unit.StackPointer}, {output.Unit.StackPointer}, {total_size} ; pop args and ret");
			output.Emit("");

			return true;
		}
		#endregion

		#region Transformative
		[IrInstruction("add", "<operand:dst> = add(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Add(MethodBuilder output, dynamic args)
		{
			output.Emit($"add {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("fadd", "<operand:dst> = fadd(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fadd(MethodBuilder output, dynamic args)
		{
			output.Emit($"addf {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("sub", "<operand:dst> = sub(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Sub(MethodBuilder output, dynamic args)
		{
			output.Emit($"sub {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("fsub", "<operand:dst> = fsub(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fsub(MethodBuilder output, dynamic args)
		{
			output.Emit($"subf {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("mul", "<operand:dst> = mul(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Mul(MethodBuilder output, dynamic args)
		{
			output.Emit($"mul {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("fmul", "<operand:dst> = fmul(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fmul(MethodBuilder output, dynamic args)
		{
			output.Emit($"mulf {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("udiv", "<operand:dst> = udiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Udiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"divu {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("sdiv", "<operand:dst> = sdiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Sdiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"div {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("fdiv", "<operand:dst> = fdiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fdiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"divf {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("urem", "<operand:dst> = urem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Urem(MethodBuilder output, dynamic args)
		{
			output.Emit($"modu {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("srem", "<operand:dst> = srem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Srem(MethodBuilder output, dynamic args)
		{
			output.Emit($"mod {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("frem", "<operand:dst> = frem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Frem(MethodBuilder output, dynamic args)
		{
			output.Emit($"modf {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("shl", "<operand:dst> = shl(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Shl(MethodBuilder output, dynamic args)
		{
			output.Emit($"shl {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("lshr", "<operand:dst> = lshr(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Lshr(MethodBuilder output, dynamic args)
		{
			output.Emit($"shr {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("ashr", "<operand:dst> = ashr(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Ashr(MethodBuilder output, dynamic args)
		{
			output.Emit($"ashr {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("and", "<operand:dst> = and(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool And(MethodBuilder output, dynamic args)
		{
			output.Emit($"and {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("or", "<operand:dst> = or(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Or(MethodBuilder output, dynamic args)
		{
			output.Emit($"or {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}
		[IrInstruction("xor", "<operand:dst> = xor(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Xor(MethodBuilder output, dynamic args)
		{
			output.Emit($"xor {output.ToOperand(args.type, args.dst)}, {output.ToOperand(args.type, args.left)}, {output.ToOperand(args.type, args.right)}");
			return true;
		}

		// atomicrmw's
		#endregion

	}
}