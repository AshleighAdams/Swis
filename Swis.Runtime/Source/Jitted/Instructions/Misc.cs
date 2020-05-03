using System;
using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
namespace Swis
{
	public sealed partial class JittedCpu
	{
		[CpuInstruction(Opcode.Nop)]
		private Expression Nop(ref uint ip, ref bool sequential)
		{
			return Expression.Block();
		}

		[CpuInstruction(Opcode.InterruptR)]
		private Expression InterruptR(ref uint ip, ref bool sequential)
		{
			Operand @int = Memory.DecodeOperand(ref ip, null);
			Expression intexp = this.ReadOperandExpression<uint>(ref @int);

			return this.RaiseInterruptExpression(intexp, ref sequential);
		}

		[CpuInstruction(Opcode.InterruptReturn)]
		private Expression InterruptReturn(ref uint ip, ref bool sequential)
		{
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
			Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);
			Expression epm = this.ReadWriteRegisterExpression(NamedRegister.ProtectedMode);
			Expression epi = this.ReadWriteRegisterExpression(NamedRegister.ProtectedInterrupt);

			Expression esp_ptr = this.PointerExpression(esp, ICpu.NativeSizeBits);

			sequential = false;
			return Expression.Block(
				// clear mode
				Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
				// set mode to enabled
				Expression.OrAssign(epi, Expression.Constant(0b0000_0000__0000_0000__0000_0001__0000_0000u)),
				// mov sp, bp
				Expression.Assign(esp, ebp),
				// pop pm
				Expression.SubtractAssign(esp, Expression.Constant(ICpu.NativeSizeBytes)),
				Expression.Assign(epm, esp_ptr),
				// pop bp
				Expression.SubtractAssign(esp, Expression.Constant(ICpu.NativeSizeBytes)),
				Expression.Assign(ebp, esp_ptr),
				// pop ip
				Expression.SubtractAssign(esp, Expression.Constant(ICpu.NativeSizeBytes)),
				Expression.Assign(eip, esp_ptr)
			);
		}

		[CpuInstruction(Opcode.SetInterrupt)]
		private Expression SetInterrupt(ref uint ip, ref bool sequential)
		{
			Expression epi = this.ReadWriteRegisterExpression(NamedRegister.ProtectedInterrupt);
			return Expression.Block(
				// clear mode
				Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
				// set mode to enabled
				Expression.OrAssign(epi, Expression.Constant(0b0000_0000__0000_0000__0000_0001__0000_0000u))
			);
		}

		[CpuInstruction(Opcode.ClearInterrupt)]
		private Expression ClearInterrupt(ref uint ip, ref bool sequential)
		{
			Expression epi = this.ReadWriteRegisterExpression(NamedRegister.ProtectedInterrupt);
			return Expression.Block(
				// clear mode
				Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
				// set mode to queue
				Expression.OrAssign(epi, Expression.Constant(0b0000_0000__0000_0000__0000_0010__0000_0000u))
			);
		}

		[CpuInstruction(Opcode.SignExtendRRR)]
		private Expression SignExtendRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand src = Memory.DecodeOperand(ref ip, null);
			Operand bit = Memory.DecodeOperand(ref ip, null);

			Expression srcexp = this.ReadOperandExpression<uint>(ref src);
			Expression bitexp = this.ReadOperandExpression<uint>(ref bit);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential, SignExtendExpression(srcexp, bitexp));
		}

		[CpuInstruction(Opcode.ZeroExtendRRR)]
		private Expression ZeroExtendRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand src = Memory.DecodeOperand(ref ip, null);
			Operand bit = Memory.DecodeOperand(ref ip, null);

			Expression srcexp = this.ReadOperandExpression<uint>(ref src);
			Expression bitexp = this.ReadOperandExpression<uint>(ref bit);

			var valbits = Expression.Variable(typeof(uint), "valbits");

			return Expression.Block(
				new ParameterExpression[] { valbits },
				// uint valbits = (1u << (int)frombits) - 1;
				Expression.Assign(valbits,
					Expression.Subtract(
						Expression.LeftShift(
							Expression.Constant(1u), bitexp
						),
						Expression.Constant(1u)
					)
				),
				// dst.Value = src.Value & valbits;
				this.WriteOperandExpression<uint>(ref dst, ref sequential,
					Expression.And(srcexp, valbits))
			);
		}

		[CpuInstruction(Opcode.Halt)]
		private Expression Halt(ref uint ip, ref bool sequential)
		{
			var pmregister = this.ReadWriteRegisterExpression(NamedRegister.ProtectedMode);
			sequential = false;
			return Expression.OrAssign(pmregister, Expression.Constant((uint)ProtectedModeRegisterFlags.Halted));
		}

		[CpuInstruction(Opcode.InRR)]
		private Expression InRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand line = Memory.DecodeOperand(ref ip, null);

			Expression lineexp = this.ReadOperandExpression<uint>(ref line);

			Expression<Func<uint, uint>> readline = lineval => LineIO.ReadLineValue((UInt16)lineval);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Invoke(readline, lineexp));
		}

		[CpuInstruction(Opcode.OutRR)]
		private Expression OutRR(ref uint ip, ref bool sequential)
		{
			Operand line = Memory.DecodeOperand(ref ip, null);
			Operand lttr = Memory.DecodeOperand(ref ip, null);

			Expression lineexp = this.ReadOperandExpression<uint>(ref line);
			Expression lttrexp = this.ReadOperandExpression<uint>(ref lttr);

			Expression<Action<uint, uint>> writeline = (lineval, charval) => LineIO.WriteLineValue((UInt16)lineval, (byte)charval);

			return Expression.Invoke(writeline, lineexp, lttrexp);
		}

		[CpuInstruction(Opcode.ExtendR)]
		private Expression ExtendR(ref uint ip, ref bool sequential)
		{
			Operand instr = Memory.DecodeOperand(ref ip, null);

			Expression instrexp = this.ReadOperandExpression<uint>(ref instr);

			return this.RaiseInterruptExpression(Interrupts.InvalidOpcode, ref sequential);
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0060 // Remove unused parameter