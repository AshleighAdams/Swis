using System;

namespace Swis
{
	public enum Opcode
	{
		// misc
		Nop = 0, NoOperation = 0,
		InterruptR = 1,
		SignExtendRRR = 10,
		ZeroExtendRRR = 11,
		TrapR = 3,
		Halt = 5,
		Reset = 6,
		InRR = 7,
		OutRR = 9,

		// memory
		LoadRR = 48, LoadRRR = 50,
		StoreRR = 51, StoreRRR = 53,
		MoveRR = 54,
		PushR = 56,
		PopR = 57,

		// flow
		CallR = 96,
		Return = 98,
		JumpR = 99,
		CompareRR = 101,
		CompareFloatRR = 102,
		JumpEqualR = 103,
		JumpNotEqualR = 105,
		JumpLessR = 107,
		JumpGreaterR = 109,
		JumpLessEqualR = 111,
		JumpGreaterEqualR = 113,
		JumpUnderOverFlowR = 115,

		// transformative
		AddRRR = 144,
		AddFloatRRR = 145,
		SubtractRRR = 146,
		SubtractFloatRRR = 147,
		MultiplyRRR = 148,
		MultiplyUnsignedRRR = 149,
		MultiplyFloatRRR = 150,
		DivideRRR = 151,
		DivideUnsignedRRR = 152,
		DivideFloatRRR = 153,
		ModulusRRR = 154,
		ModulusFloatRRR = 155,
		ShiftLeftRRR = 156,
		ShiftRightRRR = 157,
		ArithmaticShiftRightRRR = 158,
		OrRRR = 159,
		ExclusiveOrRRR = 160,
		NotOrRRR = 161,
		AndRRR = 162,
		NotAndRRR = 163,
		NotRR = 164,
		SqrtFloatRR = 165,
		LogFloatRRR = 166,
		SinFloatRR = 167,
		CosFloatRR = 168,
		TanFloatRR = 169,
		AsinFloatRR = 170,
		AcosFloatRR = 171,
		AtanFloatRR = 172,
		Atan2FloatRRR = 173,
		PowFloatRRR = 174
	}
}