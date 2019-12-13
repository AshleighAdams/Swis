using System;

namespace Swis
{

	public sealed partial class JittedCpu
	{
		class JitCacheInvalidator : MemoryController
		{
			MemoryController Parent;
			JittedCpu Cpu;

			public JitCacheInvalidator(JittedCpu cpu, MemoryController parent)
			{
				this.Parent = parent;
				this.Cpu = cpu;
			}

			public override uint Length { get { return this.Parent.Length; } }

			public override byte this[uint x]
			{
				get { return this.Parent[x]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= this.Cpu.JitCacheFirst && x <= this.Cpu.JitCacheLast)
						this.Cpu.ClearJitCache();
					this.Parent[x] = value;
				}
			}
			public override uint this[uint x, uint bits]
			{
				get { return this.Parent[x, bits]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= this.Cpu.JitCacheFirst && x <= this.Cpu.JitCacheLast)
						this.Cpu.ClearJitCache();
					this.Parent[x, bits] = value;
				}
			}

			public override Span<byte> Span(uint x, uint length)
			{
				return this.Parent.Span(x, length);
			}
		}
	}
}