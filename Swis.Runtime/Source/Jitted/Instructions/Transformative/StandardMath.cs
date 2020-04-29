using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.AddRRR)]
		private Expression AddRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Add(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.AddFloatRRR)]
		private Expression AddFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Add(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.SubtractRRR)]
		private Expression SubtractRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Subtract(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.SubtractFloatRRR)]
		private Expression SubtractFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Subtract(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.MultiplyRRR)]
		private Expression MultiplyRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);


			Expression leftexp = this.ReadOperandExpression<int>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<int>(ref dst, ref sequential,
				Expression.Multiply(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.MultiplyUnsignedRRR)]
		private Expression MultiplyUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Multiply(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.MultiplyFloatRRR)]
		private Expression MultiplyFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Multiply(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.DivideRRR)]
		private Expression DivideRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<int>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<int>(ref dst, ref sequential,
				Expression.Divide(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.DivideUnsignedRRR)]
		private Expression DivideUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Divide(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.DivideFloatRRR)]
		private Expression DivideFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Divide(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ModulusRRR)]
		private Expression ModulusRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<int>(ref left);
			Expression rightexp = this.ReadOperandExpression<int>(ref right);

			return this.WriteOperandExpression<int>(ref dst, ref sequential,
				Expression.Modulo(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ModulusUnsignedRRR)]
		private Expression ModulusUnsignedRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<uint>(ref left);
			Expression rightexp = this.ReadOperandExpression<uint>(ref right);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential,
				Expression.Modulo(leftexp, rightexp));
		}

		[CpuInstruction(Opcode.ModulusFloatRRR)]
		private Expression ModulusFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Modulo(leftexp, rightexp));
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members