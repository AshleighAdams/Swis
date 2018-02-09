using System;

namespace Swis
{
	public static class Util
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

		public static uint SignExtend(uint src, uint frombits)
		{
			if (frombits < 1 || frombits >= 32)
				throw new Exception();

			uint valbits = (1u << (int)frombits) - 1; // ext 4bits to 8bits = 00001111
			uint extbits = ~valbits;                  //                      11110000
			uint signbit = 1u << (int)(frombits - 1); //                      00001000

			uint srcval = src & valbits; // ensure no more bits are present after the sign extension
			uint sign = (signbit & srcval) >> ((int)frombits - 1);

			return srcval | (extbits * sign);
		}
	}
}