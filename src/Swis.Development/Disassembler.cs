﻿using System;
using System.Collections.Generic;

namespace Swis
{
	public static class Disassembler
	{
		// TODO: when c# supports it, make self a ref
		public static string Disassemble(this Operand self, DebugData dbg)
		{
			string do_part(sbyte regid, byte size, uint @const)
			{
				if (regid > 0)
				{
					NamedRegister r = (NamedRegister)regid;

					if (regid >= (int)NamedRegister.A)
					{
						return size switch
						{
							8  => $"{r.Disassemble()}l",
							16 => $"{r.Disassemble()}x",
							32 => $"e{r.Disassemble()}x",
							64 => $"r{r.Disassemble()}x",
							_  => $"{r.Disassemble()}sz{size}",
						};
					}
					else
					{
						return size switch
						{
							8  => $"{r.Disassemble()}l",
							16 => $"{r.Disassemble()}",
							32 => $"e{r.Disassemble()}",
							64 => $"r{r.Disassemble()}",
							_  => $"{r.Disassemble()}sz{size}",
						};
					}
				}
				else
				{
					if (dbg != null)
						foreach (var lbl in dbg.Labels)
							if (lbl.Value == @const)
								return lbl.Key;
					return $"{(int)@const}";
				}
			}

			string @base = self.AddressingMode switch
			{
				0 => $"{do_part(self.RegIdA, self.SizeA, self.ConstA)}",
				1 => $"{do_part(self.RegIdA, self.SizeA, self.ConstA)} + {do_part(self.RegIdB, self.SizeB, self.ConstB)}",
				2 => $"{do_part(self.RegIdC, self.SizeC, self.ConstC)} * {do_part(self.RegIdD, self.SizeD, self.ConstD)}",
				3 => $"{do_part(self.RegIdA, self.SizeA, self.ConstA)} + {do_part(self.RegIdB, self.SizeB, self.ConstB)}" + $" + {do_part(self.RegIdC, self.SizeC, self.ConstC)} * {do_part(self.RegIdD, self.SizeD, self.ConstD)}",
				_ => "???",
			};

			if (self.Indirect)
			{
				if (self.IndirectionSize == ICpu.NativeSizeBits)
					@base = $"[{@base}]";
				else
					@base = $"ptr{self.IndirectionSize} [{@base}]";
			}

			return @base;
		}

		public static Dictionary<NamedRegister, string> RegisterMapReverse = new Dictionary<NamedRegister, string>()
		{
			{ NamedRegister.TimeStampCounter, "tsc" },
			{ NamedRegister.InstructionPointer, "ip" },
			{ NamedRegister.StackPointer, "sp" },
			{ NamedRegister.BasePointer, "bp"},
			{ NamedRegister.Flag, "flag"},
			{ NamedRegister.ProtectedMode, "pm" },
			{ NamedRegister.ProtectedInterrupt, "pi" },

			{ NamedRegister.A, "a" },
			{ NamedRegister.B, "b" },
			{ NamedRegister.C, "c" },
			{ NamedRegister.D, "d" },
			{ NamedRegister.E, "e" },
			{ NamedRegister.F, "f" },
			{ NamedRegister.G, "g" },
			{ NamedRegister.H, "h" },
			{ NamedRegister.I, "i" },
			{ NamedRegister.J, "j" },
			{ NamedRegister.K, "k" },
			{ NamedRegister.L, "l" },
		};

		public static string Disassemble(this NamedRegister self)
		{
			if (RegisterMapReverse.TryGetValue(self, out string ret))
				return ret;
			return $"ukn{(int)self}reg";
		}

		public static Dictionary<Opcode, string> OpcodeMapReverse = new Dictionary<Opcode, string>()
		{
			{ Opcode.Nop, "nop" },
			{ Opcode.InterruptR, "int" },
			{ Opcode.InterruptReturn, "iret" },
			{ Opcode.SignExtendRRR, "sext" },
			{ Opcode.ZeroExtendRRR, "zext" },
			{ Opcode.TrapR, "trap" },
			{ Opcode.Halt, "halt" },
			{ Opcode.Reset, "reset" },
			{ Opcode.InRR, "in" },
			{ Opcode.OutRR, "out" },

			{ Opcode.LoadRR, "load" },
			{ Opcode.LoadRRR, "load" },
			{ Opcode.StoreRR, "store" },
			{ Opcode.StoreRRR, "store" },
			{ Opcode.MoveRR, "mov" },
			{ Opcode.PushR, "push" },
			{ Opcode.PopR, "pop" },
			{ Opcode.CallR, "call" },
			{ Opcode.Return, "ret" },
			{ Opcode.JumpR, "jmp" },
			{ Opcode.CompareRR, "cmp" },
			{ Opcode.CompareFloatRRR, "cmpf" },
			{ Opcode.CompareUnsignedRR, "cmpu" },
			{ Opcode.JumpEqualR, "je" },
			{ Opcode.JumpNotEqualR, "jne" },
			{ Opcode.JumpLessR, "jl" },
			{ Opcode.JumpGreaterR, "jg" },
			{ Opcode.JumpLessEqualR, "jle" },
			{ Opcode.JumpGreaterEqualR, "jge" },
			{ Opcode.JumpUnderOverflowR, "juo" },
			{ Opcode.JumpZeroRR, "jz" },
			{ Opcode.JumpNotZeroRR, "jnz" },

			{ Opcode.AddRRR, "add" },
			{ Opcode.AddFloatRRR, "addf" },
			{ Opcode.SubtractRRR, "sub" },
			{ Opcode.SubtractFloatRRR, "subf" },
			{ Opcode.MultiplyRRR, "mul" },
			{ Opcode.MultiplyUnsignedRRR, "mulu" },
			{ Opcode.MultiplyFloatRRR, "mulf" },
			{ Opcode.DivideRRR, "div" },
			{ Opcode.DivideUnsignedRRR, "divu" },
			{ Opcode.DivideFloatRRR, "divf" },
			{ Opcode.ModulusRRR, "mod" },
			{ Opcode.ModulusFloatRRR, "modf" },
			{ Opcode.ModulusUnsignedRRR, "modu" },
			{ Opcode.ShiftLeftRRR, "shl" },
			{ Opcode.ShiftRightRRR, "shr" },
			{ Opcode.ArithmaticShiftRightRRR, "ashr" },
			{ Opcode.OrRRR, "or" },
			{ Opcode.ExclusiveOrRRR, "xor" },
			{ Opcode.NotOrRRR, "nor" },
			{ Opcode.AndRRR, "and" },
			{ Opcode.NotAndRRR, "nand" },
			{ Opcode.NotRR, "not" },
			{ Opcode.SqrtFloatRR, "sqrtf" },
			{ Opcode.LogFloatRRR, "logf" },
			{ Opcode.SinFloatRR, "sinf" },
			{ Opcode.CosFloatRR, "cosf" },
			{ Opcode.TanFloatRR, "tanf" },
			{ Opcode.AsinFloatRR, "asinf" },
			{ Opcode.AcosFloatRR, "acosf" },
			{ Opcode.AtanFloatRR, "atanf" },
			{ Opcode.Atan2FloatRRR, "atan2f" },
			{ Opcode.PowFloatRRR, "powf" },
		};

		public static string Disassemble(this Opcode self)
		{
			if (OpcodeMapReverse.TryGetValue(self, out string ret))
				return ret;
			return $"ukn{(int)self}op";
		}

		public static Dictionary<Opcode, int> OpcodeLengths = new Dictionary<Opcode, int>()
		{
			{ Opcode.Nop, 0 },
			{ Opcode.InterruptR, 1 },
			{ Opcode.InterruptReturn, 0 },
			{ Opcode.SignExtendRRR, 3 },
			{ Opcode.ZeroExtendRRR, 3 },
			{ Opcode.TrapR, 1 },
			{ Opcode.Halt, 0 },
			{ Opcode.Reset, 0 },
			{ Opcode.InRR, 2 },
			{ Opcode.OutRR, 2 },

			{ Opcode.LoadRR, 2 },
			{ Opcode.LoadRRR, 2 },
			{ Opcode.StoreRR, 2 },
			{ Opcode.StoreRRR, 3 },
			{ Opcode.MoveRR, 2 },
			{ Opcode.PushR, 1 },
			{ Opcode.PopR, 1 },
			{ Opcode.CallR, 1 },
			{ Opcode.Return, 0 },
			{ Opcode.JumpR, 1 },
			{ Opcode.CompareRR, 2 },
			{ Opcode.CompareFloatRRR, 3 },
			{ Opcode.CompareUnsignedRR, 2 },
			{ Opcode.JumpEqualR, 1 },
			{ Opcode.JumpNotEqualR, 1 },
			{ Opcode.JumpLessR, 1 },
			{ Opcode.JumpGreaterR, 1 },
			{ Opcode.JumpLessEqualR, 1 },
			{ Opcode.JumpGreaterEqualR, 1 },
			{ Opcode.JumpUnderOverflowR, 1 },
			{ Opcode.JumpZeroRR, 2 },
			{ Opcode.JumpNotZeroRR, 2 },

			{ Opcode.AddRRR, 3 },
			{ Opcode.AddFloatRRR, 3 },
			{ Opcode.SubtractRRR, 3 },
			{ Opcode.SubtractFloatRRR, 3 },
			{ Opcode.MultiplyRRR, 3 },
			{ Opcode.MultiplyUnsignedRRR, 3 },
			{ Opcode.MultiplyFloatRRR, 3 },
			{ Opcode.DivideRRR, 3 },
			{ Opcode.DivideUnsignedRRR, 3 },
			{ Opcode.DivideFloatRRR, 3 },
			{ Opcode.ModulusRRR, 3 },
			{ Opcode.ModulusFloatRRR, 3 },
			{ Opcode.ModulusUnsignedRRR, 3 },
			{ Opcode.ShiftRightRRR, 3 },
			{ Opcode.ShiftLeftRRR, 3 },
			{ Opcode.ArithmaticShiftRightRRR, 3 },
			{ Opcode.OrRRR, 3 },
			{ Opcode.ExclusiveOrRRR, 3 },
			{ Opcode.NotOrRRR, 3 },
			{ Opcode.AndRRR, 3 },
			{ Opcode.NotAndRRR, 3 },
			{ Opcode.NotRR, 2 },
			{ Opcode.SqrtFloatRR, 2 },
			{ Opcode.LogFloatRRR, 2 },
			{ Opcode.SinFloatRR, 2 },
			{ Opcode.CosFloatRR, 2 },
			{ Opcode.TanFloatRR, 2 },
			{ Opcode.AsinFloatRR, 2 },
			{ Opcode.AcosFloatRR, 2 },
			{ Opcode.AtanFloatRR, 2 },
			{ Opcode.Atan2FloatRRR, 3 },
			{ Opcode.PowFloatRRR, 3 },
		};

		public static (Opcode, Operand[]) DisassembleInstruction(this IMemoryController mem, ref uint ip, uint[]? registers = null)
		{
			Opcode op = mem.DecodeOpcode(ref ip);
			if (!OpcodeLengths.TryGetValue(op, out int args))
				return (op, Array.Empty<Operand>());

			Operand[] operands = new Operand[args];
			for (int i = 0; i < operands.Length; i++)
				operands[i] = mem.DecodeOperand(ref ip, registers);

			return (op, operands);
		}
	}
}