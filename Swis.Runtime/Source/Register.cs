using System;
using System.Runtime.InteropServices;

namespace Swis
{
	[Flags]
	public enum FlagsRegisterFlags
	{
		TrapMask  = 0b00000000_00000000_00000000_00000011,
		Equal     = 0b00000000_00000000_00000000_00000100,
		Less      = 0b00000000_00000000_00000000_00001000,
		Greater   = 0b00000000_00000000_00000000_00010000,
		Overflow  = 0b00000000_00000000_00000000_00100000,
		Underflow = 0b00000000_00000000_00000000_01000000,
		Halted    = 0b00000000_00000000_00000000_10000000,
	}

	public enum NamedRegister
	{
		TimeStampCounter = 0,
		InstructionPointer = 1,
		StackPointer = 2,
		BasePointer = 4,
		Flags = 5,

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

		// 0bnnnnnn_ss
		// sss = 2 bits, of the 2^(s+3) size, up to 128 bits
		// b, s, i, l: gai

		GeneralA = 16, GeneralB = 17, GeneralC = 18, GeneralD = 19, GeneralE = 20, GeneralF = 21,
		//GeneralG = 38, GeneralH = 39, GeneralI = 40, GeneralJ = 41, GeneralK = 42, GeneralL = 43,

		TempA = 24, TempB = 25, TempC = 26, TempD = 27, TempE = 28, TempF = 29,
		//TempG = 54, TempH = 55, TempI = 56, TempJ = 57, TempK = 58, TempL = 59
	}
}