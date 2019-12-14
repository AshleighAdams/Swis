using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.AddRRR)]
		private Expression AddRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Add(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.AddFloatRRR)]
		private Expression AddFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Add(
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.SubtractRRR)]
		private Expression SubtractRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential, Expression.Subtract(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.SubtractFloatRRR)]
		private Expression SubtractFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Subtract(
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.MultiplyRRR)]
		private Expression MultiplyRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);


			Expression leftexp = this.ReadOperandExpressionSigned(ref left);
			Expression rightexp = this.ReadOperandExpressionSigned(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Convert(
					Expression.Multiply(leftexp, rightexp),
					typeof(uint)
				)
			);
		}

		[CpuInstruction(Opcode.MultiplyUnsignedRRR)]
		private Expression MultiplyUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Multiply(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.MultiplyFloatRRR)]
		private Expression MultiplyFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Multiply(
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.DivideRRR)]
		private Expression DivideRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpressionSigned(ref left);
			Expression rightexp = this.ReadOperandExpressionSigned(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Convert(
					Expression.Divide(leftexp, rightexp),
					typeof(uint)
				)
			);
		}

		[CpuInstruction(Opcode.DivideUnsignedRRR)]
		private Expression DivideUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Divide(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.DivideFloatRRR)]
		private Expression DivideFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Divide(
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.ModulusRRR)]
		private Expression ModulusRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpressionSigned(ref left);
			Expression rightexp = this.ReadOperandExpressionSigned(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Convert(
					Expression.Modulo(leftexp, rightexp),
					typeof(uint)
				)
			);
		}

		[CpuInstruction(Opcode.ModulusUnsignedRRR)]
		private Expression ModulusUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Modulo(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ModulusFloatRRR)]
		private Expression ModulusFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Modulo(
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members