using System.Collections.Concurrent;

namespace Swis
{
	public sealed partial class InterpretedCpu
	{
		//ref Register StackRegister = null;
		public override ref uint TimeStampCounter
		{
			get { return ref InternalRegisters[(int)NamedRegister.TimeStampCounter]; }
		}

		public override ref uint InstructionPointer
		{
			get { return ref InternalRegisters[(int)NamedRegister.InstructionPointer]; }
		}

		public override ref uint StackPointer
		{
			get { return ref InternalRegisters[(int)NamedRegister.StackPointer]; }
		}

		public override ref uint BasePointer
		{
			get { return ref InternalRegisters[(int)NamedRegister.BasePointer]; }
		}

		public override ref uint Flags
		{
			get { return ref InternalRegisters[(int)NamedRegister.Flag]; }
		}

		public override ref uint ProtectedMode
		{
			get { return ref InternalRegisters[(int)NamedRegister.ProtectedMode]; }
		}

		public override ref uint ProtectedInterrupt
		{
			get { return ref InternalRegisters[(int)NamedRegister.ProtectedInterrupt]; }
		}

		public override void Interrupt(uint code)
		{
			InterruptQueue.Enqueue(code);
		}

		private ConcurrentQueue<uint> InterruptQueue = new ConcurrentQueue<uint>();
	}
}