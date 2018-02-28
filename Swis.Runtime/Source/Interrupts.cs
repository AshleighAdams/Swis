using System;
using System.Collections.Generic;
using System.Text;

namespace Swis
{
	public enum Interrupts : uint
	{
		Wake = 0,
		DoubleFault = 1,
		Trap = 2,
		InputBase = 251
	}
}
