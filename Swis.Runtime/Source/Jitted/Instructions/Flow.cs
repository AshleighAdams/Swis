﻿using System;
using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.CallR)]
		private Expression CallR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);

			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
			Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);

			Expression esp_ptr = this.PointerExpression(esp, Cpu.NativeSizeBits);

			sequential = false;
			return Expression.Block(
				// push ip ; the retaddr
				Expression.Assign(esp_ptr, eip),
				Expression.AddAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				// push bp
				Expression.Assign(esp_ptr, ebp),
				Expression.AddAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				// mov bp, sp
				Expression.Assign(ebp, esp),
				// jmp loc
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.Return)]
		private Expression Return(ref uint ip, ref bool sequential)
		{
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
			Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);

			Expression esp_ptr = this.PointerExpression(esp, Cpu.NativeSizeBits);

			sequential = false;
			return Expression.Block(
				// mov sp, bp
				Expression.Assign(esp, ebp),
				// pop bp
				Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				Expression.Assign(ebp, esp_ptr),
				// pop ip
				Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				Expression.Assign(eip, esp_ptr)
			);

		}

		[CpuInstruction(Opcode.JumpR)]
		private Expression JumpR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);

			sequential = false;
			return Expression.Assign(this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer), locexp);
		}

		[CpuInstruction(Opcode.CompareRR)]
		private Expression CompareRR(ref uint ip, ref bool sequential)
		{
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpressionSigned(ref left);
			Expression rightexp = this.ReadOperandExpressionSigned(ref right);

			Action<int, int> comparer = (ileft, iright) =>
			{
				var iflags = (FlagsRegisterFlags)this.Reg5;
				iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

				if (ileft > iright) //-V3022
					iflags |= FlagsRegisterFlags.Greater;
				if (ileft < iright) //-V3022
					iflags |= FlagsRegisterFlags.Less;
				if (ileft == iright) //-V3022
					iflags |= FlagsRegisterFlags.Equal;

				this.Reg5 = (uint)iflags;
			};

			Expression<Action<int, int>> comparerexp = (l, r) => comparer(l, r);

			return Expression.Invoke(comparerexp, leftexp, rightexp);
		}

		[CpuInstruction(Opcode.CompareFloatRRR)]
		private Expression CompareFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);
			Operand ordered = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);
			Expression orderedexp = this.ReadOperandExpression(ref ordered);

			Action<uint, uint, uint> comparer = (uleft, uright, uordered) =>
			{
				Caster c; c.F32 = 0;
				c.U32 = uleft;
				float fleft = c.F32;
				c.U32 = uright;
				float fright = c.F32;

				var iflags = (FlagsRegisterFlags)this.Reg5;
				iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

				if (fleft > fright)
					iflags |= FlagsRegisterFlags.Greater;
				if (fleft < fright)
					iflags |= FlagsRegisterFlags.Less;
				if (fleft == fright) //-V3024
					iflags |= FlagsRegisterFlags.Equal;

				this.Reg5 = (uint)iflags;
			};

			Expression<Action<uint, uint, uint>> comparerexp = (l, r, o) => comparer(l, r, o);

			return Expression.Invoke(comparerexp, leftexp, rightexp, orderedexp);
		}

		[CpuInstruction(Opcode.CompareUnsignedRR)]
		private Expression CompareUnsignedRR(ref uint ip, ref bool sequential)
		{
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			Action<uint, uint> comparer = (uleft, uright) =>
			{
				var iflags = (FlagsRegisterFlags)this.Reg5;
				iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

				if (uleft > uright)
					iflags |= FlagsRegisterFlags.Greater;
				if (uleft < uright)
					iflags |= FlagsRegisterFlags.Less;
				if (uleft == uright)
					iflags |= FlagsRegisterFlags.Equal;

				this.Reg5 = (uint)iflags;
			};

			Expression<Action<uint, uint>> comparerexp = (l, r) => comparer(l, r);

			return Expression.Invoke(comparerexp, leftexp, rightexp);
		}

		[CpuInstruction(Opcode.JumpEqualR)]
		private Expression JumpEqualR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(
					Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Equal)),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpNotEqualR)]
		private Expression JumpNotEqualR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			//	if (uleft > uright)
			//		iflags |= FlagsRegisterFlags.Greater;
			//	if (uleft < uright)
			//		iflags |= FlagsRegisterFlags.Less;
			//	if (uleft == uright)
			//		iflags |= FlagsRegisterFlags.Equal;
			//	this.Reg5 = (uint)iflags;

			sequential = false;
			return Expression.IfThen(
				Expression.Equal(
					Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Equal)),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpGreaterR)]
		private Expression JumpGreaterR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(
					Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Greater)),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpGreaterEqualR)]
		private Expression JumpGreaterEqualR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(
					Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Greater | FlagsRegisterFlags.Equal))),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpLessR)]
		private Expression JumpLessR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(
					Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Less)),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpLessEqualR)]
		private Expression JumpLessEqualR(ref uint ip, ref bool sequential)
		{
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(
					Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Less | FlagsRegisterFlags.Equal))),
					Expression.Constant(0u)
				),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpZeroRR)]
		private Expression JumpZeroRR(ref uint ip, ref bool sequential)
		{
			Operand cnd = this.Memory.DecodeOperand(ref ip, null);
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression cndexp = this.ReadOperandExpression(ref cnd);
			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.Equal(cndexp, Expression.Constant(0u)),
				Expression.Assign(eip, locexp)
			);
		}

		[CpuInstruction(Opcode.JumpNotZeroRR)]
		private Expression JumpNotZeroRR(ref uint ip, ref bool sequential)
		{
			Operand cnd = this.Memory.DecodeOperand(ref ip, null);
			Operand loc = this.Memory.DecodeOperand(ref ip, null);

			Expression cndexp = this.ReadOperandExpression(ref cnd);
			Expression locexp = this.ReadOperandExpression(ref loc);
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

			sequential = false;
			return Expression.IfThen(
				Expression.NotEqual(cndexp, Expression.Constant(0u)),
				Expression.Assign(eip, locexp)
			);
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members