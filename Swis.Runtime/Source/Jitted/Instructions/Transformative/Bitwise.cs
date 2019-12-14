﻿using System;
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

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.LeftShift(leftexp, Expression.Convert(rightexp, typeof(int))));
		}

		[CpuInstruction(Opcode.ShiftRightRRR)]
		private Expression ShiftRightRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.RightShift(leftexp, Expression.Convert(rightexp, typeof(int))));
		}

		[CpuInstruction(Opcode.ArithmaticShiftRightRRR)]
		private Expression ArithmaticShiftRightRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

#if DEBUG
			// TODO: this needs to sign extend the number of bits
			throw new NotImplementedException();
#endif
			return this.WriteOperandExpression(ref dst, ref sequential, // TODO: check this
				Expression.Convert(
					Expression.RightShift(
						Expression.Convert(leftexp, typeof(int)),
						Expression.Convert(rightexp, typeof(int))
					),
					typeof(int)
				)
			);
		}

		[CpuInstruction(Opcode.OrRRR)]
		private Expression OrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Or(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ExclusiveOrRRR)]
		private Expression ExclusiveOrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.ExclusiveOr(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.NotOrRRR)]
		private Expression NotOrRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Or(leftexp, Expression.Not(rightexp)));
		}

		[CpuInstruction(Opcode.AndRRR)]
		private Expression AndRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.And(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.NotAndRRR)]
		private Expression NotAndRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.And(leftexp, Expression.Not(rightexp)));
		}

		[CpuInstruction(Opcode.NotRR)]
		private Expression NotRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Not(leftexp));
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members