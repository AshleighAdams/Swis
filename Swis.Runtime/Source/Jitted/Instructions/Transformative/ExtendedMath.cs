using System;
using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.SqrtFloatRR)]
		private Expression SqrtFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> sqrt = (val) => (float)Math.Sqrt(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(sqrt, leftexp));
		}

		[CpuInstruction(Opcode.LogFloatRRR)]
		private Expression LogFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			Expression<Func<float, float, float>> log = (val, @base) => (float)Math.Log(val, @base);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(log, leftexp, rightexp));
		}

		[CpuInstruction(Opcode.SinFloatRR)]
		private Expression SinFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> sin = (val) => (float)Math.Sin(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(sin, leftexp));
		}

		[CpuInstruction(Opcode.CosFloatRR)]
		private Expression CosFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> cos = (val) => (float)Math.Cos(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(cos, leftexp));
		}

		[CpuInstruction(Opcode.TanFloatRR)]
		private Expression TanFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> tan = (val) => (float)Math.Tan(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(tan, leftexp));
		}

		[CpuInstruction(Opcode.AsinFloatRR)]
		private Expression AsinFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> asin = (val) => (float)Math.Asin(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(asin, leftexp));
		}

		[CpuInstruction(Opcode.AcosFloatRR)]
		private Expression AcosFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> acos = (val) => (float)Math.Acos(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(acos, leftexp));
		}

		[CpuInstruction(Opcode.AtanFloatRR)]
		private Expression AtanFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);

			Expression<Func<float, float>> atan = (val) => (float)Math.Atan(val);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(atan, leftexp));
		}

		[CpuInstruction(Opcode.Atan2FloatRRR)]
		private Expression Atan2FloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			Expression<Func<float, float, float>> atan2 = (l, r) => (float)Math.Log(l, r);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(atan2, leftexp, rightexp));
		}

		[CpuInstruction(Opcode.PowFloatRRR)]
		private Expression PowFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand left = Memory.DecodeOperand(ref ip, null);
			Operand right = Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression<float>(ref left);
			Expression rightexp = this.ReadOperandExpression<float>(ref right);

			Expression<Func<float, float, float>> pow = (l, r) => (float)Math.Pow(l, r);
			return this.WriteOperandExpression<float>(ref dst, ref sequential,
				Expression.Invoke(pow, leftexp, rightexp));
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members