using System;
using System.Runtime.InteropServices;

namespace Swis
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Register
	{
		public const int DataSize = 4;
		[FieldOffset(0)] public Int32 I32;
		[FieldOffset(0)] public UInt32 U32;
		[FieldOffset(0)] public Single F32;
	}

	public enum NamedRegister
	{
		Invalid = 0,
		InstructionPointer = 1,
		StackPointer = 2,
		CallstackPointer = 3,

		Flags = 4,
		FlagsTrapMask   = 0b00000000_00000000_00000000_00000011,
		FlagsEqual      = 0b00000000_00000000_00000000_00000100,
		FlagsLess       = 0b00000000_00000000_00000000_00001000,
		FlagsGreater    = 0b00000000_00000000_00000000_00010000,
		FlagsOverflow   = 0b00000000_00000000_00000000_00100000,
		FlagsUnderflow  = 0b00000000_00000000_00000000_01000000,
		FlagsHalted     = 0b00000000_00000000_00000000_10000000,

		// these can only be touched in ring0
		ProtectedMode = 7, // mode flags: int1 realmode, int2 ring
		ProtectedInterrupt = 8, // interrupt handler address

		GeneralA = 32, GeneralB = 33, GeneralC = 34, GeneralD = 35, GeneralE = 36, GeneralF = 37,
		GeneralG = 38, GeneralH = 39, GeneralI = 40, GeneralJ = 41, GeneralK = 42, GeneralL = 43,
		TempA = 48, TempB = 49, TempC = 50, TempD = 51, TempE = 52, TempF = 53,
		TempG = 54, TempH = 55, TempI = 56, TempJ = 57, TempK = 58, TempL = 59
	}
}