namespace Swis
{
	public enum Interrupts : uint
	{
		Wake = 0,
		DoubleFault = 1,
		NonMaskable = 2,
		Breakpoint = 3,
		InvalidOpcode = 4,
		GeneralProtectionFault = 5,
		PageFault = 6,
		SegmentFault = 7,
		DivideByZero = 8,
		Overflow = 9,
		StackException = 10,

		InputBase = 251
	}
}
