using System;
using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.Nop)]
		private Expression Nop(ref uint ip, ref bool sequential)
		{
			return null;
		}

		[CpuInstruction(Opcode.InterruptR)]
		private Expression InterruptR(ref uint ip, ref bool sequential)
		{
			Operand @int = this.Memory.DecodeOperand(ref ip, null);
			Expression intexp = this.ReadOperandExpression(ref @int);

			Expression<Action<uint>> cpuinterrupt = intcode => this.Interrupt(intcode);
			sequential = true;
			return Expression.Invoke(cpuinterrupt, intexp);
		}

		[CpuInstruction(Opcode.InterruptReturn)]
		private Expression InterruptReturn(ref uint ip, ref bool sequential)
		{
			Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
			Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);
			Expression epm = this.ReadWriteRegisterExpression(NamedRegister.ProtectedMode);
			Expression epi = this.ReadWriteRegisterExpression(NamedRegister.ProtectedInterrupt);

			Expression esp_ptr = this.PointerExpression(esp, Cpu.NativeSizeBits);

			sequential = true;
			return Expression.Block(
				// clear mode
				Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
				// set mode to enabled
				Expression.OrAssign(epi, Expression.Constant(0b0000_0000__0000_0000__0000_0001__0000_0000u)),
				// mov sp, bp
				Expression.Assign(esp, ebp),
				// pop pm
				Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				Expression.Assign(epm, esp_ptr),
				// pop bp
				Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				Expression.Assign(ebp, esp_ptr),
				// pop ip
				Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
				Expression.Assign(eip, esp_ptr)
			);
		}

		[CpuInstruction(Opcode.SetInterrupt)]
		private Expression SetInterrupt(ref uint ip, ref bool sequential)
		{
			Expression epi = this.ReadWriteRegisterExpression(NamedRegister.ProtectedInterrupt);
			ref uint pi = ref this.Registers[(int)NamedRegister.ProtectedInterrupt];
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
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand src = this.Memory.DecodeOperand(ref ip, null);
			Operand bit = this.Memory.DecodeOperand(ref ip, null);

			Expression srcexp = this.ReadOperandExpression(ref src);
			Expression bitexp = this.ReadOperandExpression(ref bit);

			return this.WriteOperandExpression(ref dst, ref sequential, SignExtendExpression(srcexp, bitexp));
		}

		[CpuInstruction(Opcode.ZeroExtendRRR)]
		private Expression ZeroExtendRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand src = this.Memory.DecodeOperand(ref ip, null);
			Operand bit = this.Memory.DecodeOperand(ref ip, null);

			Expression srcexp = this.ReadOperandExpression(ref src);
			Expression bitexp = this.ReadOperandExpression(ref bit);

			var valbits = Expression.Variable(typeof(uint), "valbits");

			sequential = dst.WriteAffectsFlow;
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
				this.WriteOperandExpression(ref dst, ref sequential,
					Expression.And(
						srcexp,
						valbits
					)
				)
			);
		}

		[CpuInstruction(Opcode.Halt)]
		private Expression Halt(ref uint ip, ref bool sequential)
		{
			var pmregister = this.ReadWriteRegisterExpression(NamedRegister.ProtectedMode);
			sequential = true;
			return Expression.OrAssign(pmregister, Expression.Constant((uint)ProtectedModeRegisterFlags.Halted));
		}

		[CpuInstruction(Opcode.InRR)]
		private Expression InRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand line = this.Memory.DecodeOperand(ref ip, null);

			Expression lineexp = this.ReadOperandExpression(ref line);

			Expression<Func<uint, uint>> readline = lineval => this.LineRead((UInt16)lineval);

			sequential = dst.WriteAffectsFlow;
			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Invoke(readline, lineexp));
		}

		[CpuInstruction(Opcode.OutRR)]
		private Expression OutRR(ref uint ip, ref bool sequential)
		{
			Operand line = this.Memory.DecodeOperand(ref ip, null);
			Operand lttr = this.Memory.DecodeOperand(ref ip, null);

			Expression lineexp = this.ReadOperandExpression(ref line);
			Expression lttrexp = this.ReadOperandExpression(ref lttr);

			Expression<Action<uint, uint>> writeline = (lineval, charval) => this.LineWrite((UInt16)lineval, (byte)charval);

			return Expression.Invoke(writeline, lineexp, lttrexp);
		}

		//[CpuInstruction(Opcode.______)]
		//private Expression ________(ref uint ip, ref bool sequential)
		//{
		//}
	}
}
#pragma warning restore IDE0051 // Remove unused private members