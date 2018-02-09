namespace Swis
{
	public partial class Cpu
	{

		//ref Register StackRegister = null;
		public ref uint TimeStampCounter
		{
			get { return ref this.Registers[(int)NamedRegister.TimeStampCounter]; }
		}

		public ref uint InstructionPointer
		{
			get { return ref this.Registers[(int)NamedRegister.InstructionPointer]; }
		}

		public ref uint StackPointer
		{
			get { return ref this.Registers[(int)NamedRegister.StackPointer]; }
		}

		public ref uint BasePointer
		{
			get { return ref this.Registers[(int)NamedRegister.BasePointer]; }
		}

		public ref uint Flags
		{
			get { return ref this.Registers[(int)NamedRegister.Flags]; }
		}

		public bool Halted
		{
			get
			{ return ((FlagsRegisterFlags)this.Flags).HasFlag(FlagsRegisterFlags.Halted); }
		}

		public MemoryController Memory;

		public ExternalDebugger Debugger { get; set; }

		protected Operand CreatePointer(uint address, uint size)
		{
			return new Operand
			{
				Memory = this.Memory,
				Registers = null,
				RegIdA = -1,
				SizeA = (byte)Cpu.NativeSizeBits, // size of the const (address), not the size of the value at the address
				ConstA = address,
				IndirectionSize = (byte)size,
			};
		}

	}
}