using System;

namespace Swis
{
	// https://github.com/llvm-mirror/llvm/blob/master/include/llvm/IR/Instruction.def
	// https://llvm.org/docs/LangRef.html
	public static partial class LlvmIrCompiler
	{
		#region Misc
		[IrInstruction("trunc", "<operand:dst> = trunc <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Trunc(MethodBuilder output, dynamic args)
		{
			int tosz = int.Parse(args.dst_type_size);
			if (tosz % 8 != 0)
			{
				output.Emit(
					$"and " +
						$"{ToOperand(output, args.dst_type, args.dst)}, " +
						$"{ToOperand(output, args.src_type, args.src)}, " +
						$"{(1 << tosz) - 1} ; trunc to i{tosz}");
				return true;
			}
			output.Emit(
				$"mov " +
				$"{ToOperand(output, args.dst_type, args.dst)}, " +
				$"{ToOperand(output, args.src_type, args.src)} ; trunc {args.src_type} -> {args.dst_type}");
			return true;
		}

		[IrInstruction("sext", "<operand:dst> = sext <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Sext(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"sext " +
				$"{ToOperand(output, args.dst_type, args.dst)}, " +
				$"{ToOperand(output, args.src_type, args.src)}, " +
				$"{args.src_type_size}");
			return true;
		}

		[IrInstruction("zext", "<operand:dst> = zext <type:src_type> <operand:src> to <type:dst_type>")]
		private static bool Zext(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"zext " +
				$"{ToOperand(output, args.dst_type, args.dst)}, " +
				$"{ToOperand(output, args.src_type, args.src)}, " +
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
				$"{ToOperand(output, args.dst_type, args.dst, indirection: true)}, " +
				$"{ToOperand(output, args.src_type, args.src)}");
			return true;
		}

		[IrInstruction("load", "<register:dst> = load <type:dst_type>, <type:src_type> <operand:src>(, align <numeric:align>)?")]
		private static bool Load(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"mov " +
				$"{ToOperand(output, args.dst_type, args.dst)}, " +
				$"{ToOperand(output, args.src_type, args.src, indirection: true)}");
			return true;
		}
		#endregion

		#region Flow

		static string Targetify(MethodBuilder output, string type, string where)
		{
			if (string.IsNullOrWhiteSpace(type)) // is it a label?
				return $"${output.Id}_label_{where}";
			return ToOperand(output, type, where);
		}

		[IrInstruction("ret", "ret <type:type> <operand:what>")]
		private static bool Ret(MethodBuilder output, dynamic args)
		{
			output.Emit(
				$"mov " +
				$"{output.ConstantLocals["ret"]}, " +
				$"{ToOperand(output, args.type, args.what)}");
			output.Emit("ret");
			return true;
		}

		[IrInstruction("cmpbr", "cmpbr (?<cmptype>[a-z]) <keyword:method> <type:type> <operand:left>, <operand:right>, (label|<type:ontrue_type>) %<numeric:ontrue>, (label|<type:onfalse_type>) %<numeric:onfalse>$")]
		private static bool Cmpbr(MethodBuilder output, dynamic args)
		{
			string ircmp_to_cmp(string m)
			{
				switch (m)
				{
				case "i": return "";
				case "f": return "f";
				case "u": return "u";
				default: throw new NotImplementedException(m);
				}
			}
			string irmeth_to_asm_inverted(string m)
			{
				switch (m)
				{
				case "eq": return "ne";
				case "ne": return "e";
				case "slt": return "ge";
				case "ult": return "ge";
				case "sle": return "g";
				case "ule": return "g";
				case "sgt": return "le";
				case "ugt": return "le";
				case "sge": return "l";
				case "uge": return "l";
				default: throw new NotImplementedException(m);
				}
			}
			// NOTE: ontrue and onfalse are swapped for cleaner code
			string method = irmeth_to_asm_inverted(args.method);
			string postfix = ircmp_to_cmp(args.cmptype);

			if (args.right == "0" && (method == "ne" || method == "e") && (postfix == "" || postfix == "u"))
			{
				method = method == "ne" ? "nz" : "ez";
				string asm2 = $"j{method} {ToOperand(output, args.type, args.left)}, {Targetify(output, args.onfalse_type, args.onfalse)}";
				string asm3 = $"jmp {Targetify(output, args.ontrue_type, args.ontrue)}";
				output.Emit(asm2);
				output.Emit(asm3);
			}
			else
			{
				string asm1 = $"cmp{postfix} {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}";
				string asm2 = $"j{method} {Targetify(output, args.onfalse_type, args.onfalse)}";
				string asm3 = $"jmp {Targetify(output, args.ontrue_type, args.ontrue)}";

				output.Emit(asm1);
				output.Emit(asm2);
				output.Emit(asm3);
			}
			return true;
		}

		[IrInstruction("br", "br ((label|<type:uncond_type>) %<numeric:uncond>|<type:cond_type> <operand:cond>, (label|<type:ontrue_type>) %<numeric:ontrue>, (label|<type:onfalse_type>) %<numeric:onfalse>)$")]
		private static bool Br(MethodBuilder output, dynamic args)
		{
			if (!string.IsNullOrWhiteSpace(args.uncond))
				output.Emit($"jmp {Targetify(output, args.uncond_type, args.uncond)}");
			else if (!string.IsNullOrWhiteSpace(args.cond))
			{
				if (args.cond_type_size != "1")
					throw new Exception();

				//output.Emit($"jnz {ToOperand(output, args.cond_type, args.cond)}, {Targetify(output, args.ontrue_type, args.ontrue)}");
				//output.Emit($"jmp {Targetify(output, args.onfalse_type, args.onfalse)}");

				// inverting it generally allows us to optimize a jump out later
				output.Emit($"jez {ToOperand(output, args.cond_type, args.cond)}, {Targetify(output, args.onfalse_type, args.onfalse)}");
				output.Emit($"jmp {Targetify(output, args.ontrue_type, args.ontrue)}");
			}
			else
				throw new Exception();
			return true;
		}
		#endregion

		#region Transformative
		[IrInstruction("add", "<operand:dst> = add(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Add(MethodBuilder output, dynamic args)
		{
			output.Emit($"add {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("fadd", "<operand:dst> = fadd(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fadd(MethodBuilder output, dynamic args)
		{
			output.Emit($"addf {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("sub", "<operand:dst> = sub(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Sub(MethodBuilder output, dynamic args)
		{
			output.Emit($"sub {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("fsub", "<operand:dst> = fsub(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fsub(MethodBuilder output, dynamic args)
		{
			output.Emit($"subf {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("mul", "<operand:dst> = mul(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Mul(MethodBuilder output, dynamic args)
		{
			output.Emit($"mul {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("fmul", "<operand:dst> = fmul(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fmul(MethodBuilder output, dynamic args)
		{
			output.Emit($"mulf {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("udiv", "<operand:dst> = udiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Udiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"divu {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("sdiv", "<operand:dst> = sdiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Sdiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"div {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("fdiv", "<operand:dst> = fdiv(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Fdiv(MethodBuilder output, dynamic args)
		{
			output.Emit($"divf {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("urem", "<operand:dst> = urem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Urem(MethodBuilder output, dynamic args)
		{
			output.Emit($"modu {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("srem", "<operand:dst> = srem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Srem(MethodBuilder output, dynamic args)
		{
			output.Emit($"mod {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("frem", "<operand:dst> = frem(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Frem(MethodBuilder output, dynamic args)
		{
			output.Emit($"modf {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("shl", "<operand:dst> = shl(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Shl(MethodBuilder output, dynamic args)
		{
			output.Emit($"shl {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("lshr", "<operand:dst> = lshr(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Lshr(MethodBuilder output, dynamic args)
		{
			output.Emit($"shr {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("ashr", "<operand:dst> = ashr(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Ashr(MethodBuilder output, dynamic args)
		{
			output.Emit($"ashr {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("and", "<operand:dst> = and(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool And(MethodBuilder output, dynamic args)
		{
			output.Emit($"and {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("or", "<operand:dst> = or(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Or(MethodBuilder output, dynamic args)
		{
			output.Emit($"or {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}
		[IrInstruction("xor", "<operand:dst> = xor(?<mods>( <keyword>)*) <type:type> <operand:left>, <operand:right>")]
		private static bool Xor(MethodBuilder output, dynamic args)
		{
			output.Emit($"xor {ToOperand(output, args.type, args.dst)}, {ToOperand(output, args.type, args.left)}, {ToOperand(output, args.type, args.right)}");
			return true;
		}

		// atomicrmw's
		#endregion

	}
}