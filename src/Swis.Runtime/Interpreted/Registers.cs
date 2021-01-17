using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Swis
{
	public sealed partial class InterpretedCpu
	{
		private struct ListWrapper<T> : IReadWriteList<T>
		{
			private IList<T> Source { get; }
			public ListWrapper(IList<T> source)
			{
				Source = source;
			}

			T IReadWriteList<T>.this[int index] { get => Source[index]; set => Source[index] = value; }
			int IReadWriteList<T>.Count { get => Source.Count; }
		}

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


		public uint[] InternalRegisters = new uint[(int)NamedRegister.L + 1];
		public override IReadWriteList<uint> Registers { get => new ListWrapper<uint>(InternalRegisters); }
	}
}