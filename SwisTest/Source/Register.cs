using System;
using System.Runtime.InteropServices;

namespace Swis
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Register
	{
		public static int Pow(int a, int b)
		{
			int result = 1;
			for (int i = 0; i < b; i++)
				result *= a;
			return result;
		}
		public static uint Pow(uint a, uint b)
		{
			uint result = 1;
			for (uint i = 0; i < b; i++)
				result *= a;
			return result;
		}

		[FieldOffset(0)] public Int32  NativeInt;
		[FieldOffset(0)] public UInt32 NativeUInt;
		[FieldOffset(0)] public Single NativeFloat;
		public const int NativeSize = 4;

		public int GetInteger(int size)
		{
			int bitmask = Pow(2, size * 8) - 1;
			return this.NativeInt & bitmask;
		}

		public void SetInteger(int size, int value)
		{
			int bitmask = Pow(2, size * 8) - 1;
			this.NativeInt = value & bitmask; //  | (this.NativeUInt & ~bitmask); keep the higher bits? we're not
		}

		public uint GetUnsigned(int size)
		{
			uint bitmask = Pow(2, (uint)size * 8) - 1;
			return this.NativeUInt & bitmask;
		}

		public void SetUnsigned(int size, uint value)
		{
			uint bitmask = Pow(2, Pow(2, (uint)size + 3)) - 1;
			this.NativeUInt = value & bitmask;
		}

		public float GetFloat(int size)
		{
			if (size != 4)
				throw new Exception("invalid register for float");
			return this.NativeFloat;
		}

		public void SetFloat(int size, float value)
		{
			if (size != 4)
				throw new Exception("invalid register for float");
			this.NativeFloat = value;
		}
	}

	public enum NamedRegister
	{
		Invalid = 0,
		InstructionPointer = 1,
		StackPointer = 2,
		CallstackPointer = 3,
		BasePointer = 4,


		Flags = 5,
		FlagsTrapMask   = 0b00000000_00000000_00000000_00000011,
		FlagsEqual      = 0b00000000_00000000_00000000_00000100,
		FlagsLess       = 0b00000000_00000000_00000000_00001000,
		FlagsGreater    = 0b00000000_00000000_00000000_00010000,
		FlagsOverflow   = 0b00000000_00000000_00000000_00100000,
		FlagsUnderflow  = 0b00000000_00000000_00000000_01000000,
		FlagsHalted     = 0b00000000_00000000_00000000_10000000,

		// these can only be touched in ring0
		ProtectedMode = 6, // mode flags: int1 realmode, int2 ring
		ProtectedInterrupt = 7, // interrupt handler address

		// 0bnnnnnn_ss
		// sss = 2 bits, of the 2^(s+3) size, up to 128 bits
		// b, s, i, l: gai
		
		GeneralA = 32, GeneralB = 33, GeneralC = 34, GeneralD = 35, GeneralE = 36, GeneralF = 37,
		//GeneralG = 38, GeneralH = 39, GeneralI = 40, GeneralJ = 41, GeneralK = 42, GeneralL = 43,

		TempA = 48, TempB = 49, TempC = 50, TempD = 51, TempE = 52, TempF = 53,
		//TempG = 54, TempH = 55, TempI = 56, TempJ = 57, TempK = 58, TempL = 59
	}
}