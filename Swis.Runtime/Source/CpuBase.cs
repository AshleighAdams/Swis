using System.Collections.Concurrent;

namespace Swis
{
	public abstract class CpuBase : ICpu
	{
		public CpuBase(IMemoryController memory, ILineIO line_io)
		{
			Memory = memory;
			LineIO = line_io;
		}

		public virtual IExternalDebugger? Debugger { get; set; }
		public virtual IMemoryController Memory { get; set; }
		public virtual ILineIO LineIO { get; set; }
		public abstract IReadWriteList<uint> Registers { get; }

		public abstract int Clock(int clocks = 1);
		public abstract void Interrupt(uint code);
		public abstract void Reset();

		public abstract ref uint TimeStampCounter { get; }
		public abstract ref uint InstructionPointer { get; }
		public abstract ref uint StackPointer { get; }
		public abstract ref uint BasePointer { get; }
		public abstract ref uint Flags { get; }
		public abstract ref uint ProtectedMode { get; }
		public abstract ref uint ProtectedInterrupt { get; }

		public virtual bool Halted { get { return (ProtectedMode & (uint)ProtectedModeRegisterFlags.Halted) != 0; } }

		protected virtual bool HandleInterrupts(IProducerConsumerCollection<uint> interrupt_queue)
		{
			if (interrupt_queue.Count == 0)
				return false;

			ref uint sp = ref StackPointer;
			ref uint bp = ref BasePointer;
			ref uint ip = ref InstructionPointer;
			ref uint pm = ref ProtectedMode;
			ref uint pi = ref ProtectedInterrupt;
			uint mode = (pi & 0b0000_0000__0000_0000__0000_0011__0000_0000u) >> 8;

			switch (mode)
			{
				case 0b00: /*disabled silent*/
					while (interrupt_queue.TryTake(out _)) { } // clear the remaining
					return false;
				case 0b10: return false; /*queued*/
				case 0b11: pm |= (uint)ProtectedModeRegisterFlags.Halted; return true; /*disabled halt*/
				case 0b01:
				default: /*consume one*/
					if (!interrupt_queue.TryTake(out uint @int))
						return false;

					uint ivt = (pi & 0b0000_0000__0000_0000__0000_0000__1111_1111u) << 8;
					uint ivtn = @int > 255 ? 255 : @int;
					uint addr = Memory[ivt + ivtn * ICpu.NativeSizeBytes, ICpu.NativeSizeBits];

					// simulate call to the interrupt address if it's enabled
					if (addr != 0)
					{
						pi &= ~0b0000_0000__0000_0000__0000_0011__0000_0000u; // clear mode
						pi |= 0b0000_0000__0000_0000__0000_0010__0000_0000u; // set mode to queue

						// push ip
						Memory[sp, ICpu.NativeSizeBits] = ip;
						sp += ICpu.NativeSizeBytes;
						// push bp
						Memory[sp, ICpu.NativeSizeBits] = bp;
						sp += ICpu.NativeSizeBytes;
						// push flags
						Memory[sp, ICpu.NativeSizeBits] = Flags;
						sp += ICpu.NativeSizeBytes;
						// mov bp, sp
						bp = sp;
						// jmp loc
						ip = addr;

						if (ivtn == 255) // extended interrupt, push the interrupt code
						{
							// push @int
							Memory[sp, ICpu.NativeSizeBits] = @int;
							sp += ICpu.NativeSizeBytes;
						}
						return false;
					}
					else
					{
						if (@int == (uint)Interrupts.DoubleFault)
						{
							pm |= (uint)ProtectedModeRegisterFlags.Halted;
							return true;
						}
						else
						{
							this.Interrupt((uint)Interrupts.DoubleFault);
							return this.HandleInterrupts(interrupt_queue);
						}
					}
			}
		}
	}
}