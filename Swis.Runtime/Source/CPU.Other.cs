namespace Swis
{
	public sealed partial class InterpretedCpu
	{

		//ref Register StackRegister = null;
		public override ref uint TimeStampCounter
		{
			get { return ref this.Registers[(int)NamedRegister.TimeStampCounter]; }
		}

		public override ref uint InstructionPointer
		{
			get { return ref this.Registers[(int)NamedRegister.InstructionPointer]; }
		}

		public override ref uint StackPointer
		{
			get { return ref this.Registers[(int)NamedRegister.StackPointer]; }
		}

		public override ref uint BasePointer
		{
			get { return ref this.Registers[(int)NamedRegister.BasePointer]; }
		}

		public override ref uint Flags
		{
			get { return ref this.Registers[(int)NamedRegister.Flag]; }
		}
	}
}