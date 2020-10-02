using System;

namespace Swis
{
	public abstract class IExternalDebugger
	{
		public abstract bool Clock(CpuBase cpu);
	}

	public interface IReadWriteList<T>
	{
		T this[int index] { get; set; }
		int Count { get; }
	}

	public interface ILineIO
	{
		byte ReadLineValue(UInt16 line);
		void WriteLineValue(UInt16 line, byte value);
	}

	public interface ICpu
	{
		public static uint NativeSizeBits = 32;
		public static uint NativeSizeBytes = NativeSizeBits / 8;

		IReadWriteList<uint> Registers { get; }
		IMemoryController Memory { get; set; }
		ILineIO LineIO { get; set; }
		int Clock(int cycles = 1);
		void Interrupt(uint code);
		void Reset();

		ref uint TimeStampCounter { get; }
		ref uint InstructionPointer { get; }
		ref uint StackPointer { get; }
		ref uint BasePointer { get; }
		ref uint Flags { get; }
		ref uint ProtectedMode { get; }
		ref uint ProtectedInterrupt { get; }

		bool Halted { get; }
	}
}