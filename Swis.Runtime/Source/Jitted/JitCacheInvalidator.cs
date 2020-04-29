using System;

namespace Swis
{

	public sealed partial class JittedCpu
	{
		class JitCacheInvalidator : IMemoryController
		{
			public IMemoryController Parent { get; }
			JittedCpu Cpu;

			public JitCacheInvalidator(JittedCpu cpu, IMemoryController parent)
			{
				this.Parent = parent;
				this.Cpu = cpu;
			}

			public uint Length { get => this.Parent.Length; }

			public byte this[uint x]
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
			public uint this[uint x, uint bits]
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

			public Span<byte> Span(uint x, uint length)
			{
				return this.Parent.Span(x, length);
			}
		}
	}
}