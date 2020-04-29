using System;

namespace Swis
{

	public sealed partial class JittedCpu
	{
		private class JitCacheInvalidator : IMemoryController
		{
			public IMemoryController Parent { get; }

			private JittedCpu Cpu;

			public JitCacheInvalidator(JittedCpu cpu, IMemoryController parent)
			{
				Parent = parent;
				Cpu = cpu;
			}

			public uint Length { get => Parent.Length; }

			public byte this[uint x]
			{
				get { return Parent[x]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= Cpu.JitCacheFirst && x <= Cpu.JitCacheLast)
						Cpu.ClearJitCache();
					Parent[x] = value;
				}
			}
			public uint this[uint x, uint bits]
			{
				get { return Parent[x, bits]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= Cpu.JitCacheFirst && x <= Cpu.JitCacheLast)
						Cpu.ClearJitCache();
					Parent[x, bits] = value;
				}
			}

			public Span<byte> Span(uint x, uint length)
			{
				return Parent.Span(x, length);
			}
		}
	}
}