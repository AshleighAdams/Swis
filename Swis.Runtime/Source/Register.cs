using System;
using System.Runtime.InteropServices;

namespace Swis
{
	public enum FlagsRegisterFlags
	{
		Equal     = 0b00000000_00000000_00000000_00000100,
		Less      = 0b00000000_00000000_00000000_00001000,
		Greater   = 0b00000000_00000000_00000000_00010000,
		Overflow  = 0b00000000_00000000_00000000_00100000,
		Underflow = 0b00000000_00000000_00000000_01000000,
	}

	public enum ProtectedModeRegisterFlags : uint
	{
		TrapMask = 0b00000000_00000000_00000000_00000011u,
		Halted = 0b00000000_00000000_00000000_00000100u,
	}

	public enum NamedRegister : uint
	{
		TimeStampCounter = 0,
		InstructionPointer = 1,
		StackPointer = 2,
		BasePointer = 4,
		Flag = 5,

		// these can only be touched in ring0
		ProtectedMode = 6, // mode flags: int1 realmode, int2 ring
		ProtectedInterrupt = 7, // interrupt handler address

		StackSegment = 8,
		CodeSegment = 9,
		DataSegment = 10,
		ExtraSegment = 11,
		FSegment = 12,
		GSegment = 13,
		XtraSegment = 14,
		
		// general
		A = 20, B = 21, C = 22, D = 23, E = 24, F = 25,
		G = 26, H = 27, I = 28, J = 29, K = 30, L = 31,
	}
}