using System.Linq.Expressions;

#pragma warning disable IDE0051 // Remove unused private members
namespace Swis
{
	public sealed partial class JittedCpu : CpuBase
	{
		[CpuInstruction(Opcode.MoveRR)]
		private Expression MoveRR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);
			Operand src = Memory.DecodeOperand(ref ip, null);

			Expression srcexp = this.ReadOperandExpression<uint>(ref src);

			return this.WriteOperandExpression<uint>(ref dst, ref sequential, srcexp);
		}

		[CpuInstruction(Opcode.PushR)]
		private Expression PushR(ref uint ip, ref bool sequential)
		{
			Operand src = Memory.DecodeOperand(ref ip, null);
			Expression srcexp = this.ReadOperandExpression<uint>(ref src);

			Expression sp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ptr = this.PointerExpression(sp, src.ValueSize);

			return Expression.Block(
				Expression.Assign(ptr, srcexp),
				Expression.AddAssign(sp, Expression.Constant(src.ValueSize / 8u))
			);
		}

		[CpuInstruction(Opcode.PopR)]
		private Expression PopR(ref uint ip, ref bool sequential)
		{
			Operand dst = Memory.DecodeOperand(ref ip, null);

			Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
			Expression ptr = this.PointerExpression(esp, dst.ValueSize);

			return Expression.Block(
				Expression.SubtractAssign(esp, Expression.Constant(dst.ValueSize / 8u)),
				this.WriteOperandExpression<uint>(ref dst, ref sequential, ptr) // TODO: check this is uint
			);
		}
	}
}
#pragma warning restore IDE0051 // Remove unused private members