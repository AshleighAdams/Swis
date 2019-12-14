using System.Linq.Expressions;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		[CpuInstruction(Opcode.Nop)]
		private Expression Nop(ref uint ip, ref bool sequential_not_gauranteed)
		{
			return null;
		}
	}
}