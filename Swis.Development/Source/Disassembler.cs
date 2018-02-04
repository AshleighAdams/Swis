using System;
using System.Collections.Generic;

using Operand = Swis.Cpu.Operand;

namespace Swis
{
	public static class Disassembler
	{
		public static string Disassemble(this Operand self, DebugData dbg)
		{
			string @base = "";

			if (self.RegisterID == 0)
			{
				uint @const = self.Constant;
				@base = @const.ToString();

				if (dbg != null)
					foreach (var lbl in dbg?.Labels)
					{
						if (lbl.Value == @const)
						{
							@base = lbl.Key;
							break;
						}
					}
			}
			else
			{
				NamedRegister r = (NamedRegister)self.RegisterID;
				@base = $"{r.Disassemble()}{(self.Size != Register.NativeSize * 8 ? self.Size.ToString() : "")}";
			}

			if (self.Offset != 0)
			{
				if (self.Offset > 0)
					@base = $"{@base} + {self.Offset}";
				else
					@base = $"{@base} - {-self.Offset}";
			}

			if (self.Indirect)
			{
				if (self.IndirectionSize == Register.NativeSize * 8)
					@base = $"[{@base}]";
				else
					@base = $"ptr{self.IndirectionSize} [{@base}]";
			}

			return @base;
		}

		public static Dictionary<NamedRegister, string> RegisterMapReverse = new Dictionary<NamedRegister, string>()
		{
			{ NamedRegister.InstructionPointer, "ip" },
			{ NamedRegister.StackPointer, "sp" },
			{ NamedRegister.BasePointer, "bp"},
			{ NamedRegister.Flags, "flags"},
			{ NamedRegister.ProtectedMode, "pm" },
			{ NamedRegister.ProtectedInterrupt, "pi" },

			{ NamedRegister.GeneralA, "ga" },
			{ NamedRegister.GeneralB, "gb" },
			{ NamedRegister.GeneralC, "gc" },
			{ NamedRegister.GeneralD, "gd" },
			{ NamedRegister.GeneralE, "ge" },
			{ NamedRegister.GeneralF, "gf" },

			{ NamedRegister.TempA, "ta" },
			{ NamedRegister.TempB, "tb" },
			{ NamedRegister.TempC, "tc" },
			{ NamedRegister.TempD, "td" },
			{ NamedRegister.TempE, "te" },
			{ NamedRegister.TempF, "tf" },
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
			{ Opcode.CompareFloatRR, "cmpf" },
			{ Opcode.JumpEqualR, "je" },
			{ Opcode.JumpNotEqualR, "jne" },
			{ Opcode.JumpLessR, "jl" },
			{ Opcode.JumpGreaterR, "jg" },
			{ Opcode.JumpLessEqualR, "jle" },
			{ Opcode.JumpGreaterEqualR, "jge" },
			{ Opcode.JumpUnderOverFlowR, "juo" },

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
			{ Opcode.ShiftRightRRR, "shl" },
			{ Opcode.ShiftLeftRRR, "shr" },
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
			{ Opcode.CompareFloatRR, 2 },
			{ Opcode.JumpEqualR, 1 },
			{ Opcode.JumpNotEqualR, 1 },
			{ Opcode.JumpLessR, 1 },
			{ Opcode.JumpGreaterR, 1 },
			{ Opcode.JumpLessEqualR, 1 },
			{ Opcode.JumpGreaterEqualR, 1 },
			{ Opcode.JumpUnderOverFlowR, 1 },

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

		public static (Opcode, Operand[]) DisassembleInstruction(this MemoryController mem, ref uint ip)
		{
			Opcode op = mem.DecodeOpcode(ref ip);
			if (!OpcodeLengths.TryGetValue(op, out int args))
				return (op, null);

			Operand[] operands = new Operand[args];
			for (int i = 0; i < operands.Length; i++)
				operands[i] = mem.DecodeOperand(ref ip, null);

			return (op, operands);
		}
	}
}