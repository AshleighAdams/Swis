using System;

namespace Swis
{
	public enum Opcode
	{
		// misc
		Nop = 0, NoOperation = 0,
		InterruptR = 1, InterruptV = 2,
		TrapR = 3, TrapV = 4,
		Halt = 5,
		Reset = 6,

		// memory
		LoadRR = 48, LoadRV = 49,
		StoreRR = 50, StoreVR = 51,
		MoveRR = 52,
		MoveRV = 53,
		PushR = 54,
		PopR = 55,

		// flow
		CallR = 96, CallV = 97,
		Return = 98,
		JumpR = 99, JumpV = 100,
		CompareRR = 101,
		CompareFloatRR = 102,
		JumpEqualR = 103, JumpEqualV = 104,
		JumpNotEqualR = 105, JumpNotEqualV = 106,
		JumpLessR = 107, JumpLessV = 108,
		JumpGreaterR = 109, JumpGreaterV = 110,
		JumpLessEqualR = 111, JumpLessEqualV = 112,
		JumpGreaterEqualR = 113, JumpGreaterEqualV = 114,
		JumpUnderOverFlowR = 115, JumpUnderOverFlowV = 116,

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
		SqrtRR = 165,
		LogRR = 166,
		SinRR = 167,
		CosRR = 168,
		TanRR = 169,
		AsinRR = 170,
		AcosRR = 171,
		AtanRR = 172,
		Atan2RRR = 173,
		PowRRR = 174
	}
}