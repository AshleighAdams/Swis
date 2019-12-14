using System;
using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.ShiftLeftRRR)]
		private Expression ShiftLeftRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.LeftShift(leftexp, rightexp)
			);
		}

		[CpuInstruction(Opcode.ShiftRightRRR)]
		private Expression ShiftRightRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.RightShift(leftexp, rightexp)
			);
		}

		[CpuInstruction(Opcode.ArithmaticShiftRightRRR)]
		private Expression ArithmaticShiftRightRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<int>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<int>(ref dst, ref sequential,
				Expression.RightShift(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.OrRRR)]
		private Expression OrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Or(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ExclusiveOrRRR)]
		private Expression ExclusiveOrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.ExclusiveOr(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.NotOrRRR)]
		private Expression NotOrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Or(leftexp, Expression.Not(rightexp)));
		}

		[CpuInstruction(Opcode.AndRRR)]
		private Expression AndRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.And(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.NotAndRRR)]
		private Expression NotAndRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.And(leftexp, Expression.Not(rightexp)));
		}

		[CpuInstruction(Opcode.NotRR)]
		private Expression NotRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential, Expression.Not(leftexp));
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members