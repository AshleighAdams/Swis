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
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> sqrt = (val) => (float)Math.Sqrt(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(sqrt,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.LogFloatRRR)]
		private Expression LogFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			Expression<Func<float, float, float>> log = (val, @base) => (float)Math.Log(val, @base);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(log,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.SinFloatRR)]
		private Expression SinFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> sin = (val) => (float)Math.Sin(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(sin,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.CosFloatRR)]
		private Expression CosFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> cos = (val) => (float)Math.Cos(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(cos,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.TanFloatRR)]
		private Expression TanFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> tan = (val) => (float)Math.Tan(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(tan,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.AsinFloatRR)]
		private Expression AsinFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> asin = (val) => (float)Math.Asin(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(asin,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.AcosFloatRR)]
		private Expression AcosFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> acos = (val) => (float)Math.Acos(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(acos,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.AtanFloatRR)]
		private Expression AtanFloatRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);

			Expression<Func<float, float>> atan = (val) => (float)Math.Atan(val);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(atan,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.Atan2FloatRRR)]
		private Expression Atan2FloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			Expression<Func<float, float, float>> atan2 = (l, r) => (float)Math.Log(l, r);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(atan2,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}

		[CpuInstruction(Opcode.PowFloatRRR)]
		private Expression PowFloatRRR(ref uint ip, ref bool sequential)
		{
			Operand dst = this.Memory.DecodeOperand(ref ip, null);
			Operand left = this.Memory.DecodeOperand(ref ip, null);
			Operand right = this.Memory.DecodeOperand(ref ip, null);

			Expression leftexp = this.ReadOperandExpression(ref left);
			Expression rightexp = this.ReadOperandExpression(ref right);

			Expression<Func<float, float, float>> pow = (l, r) => (float)Math.Pow(l, r);
			return this.WriteOperandExpression(ref dst, ref sequential,
				Expression.Invoke(ReinterpretCast<float, uint>.Expression,
					Expression.Invoke(pow,
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
						Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
					)
				)
			);
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members