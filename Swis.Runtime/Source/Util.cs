using System;
using System.Diagnostics;
using ReinterpretMediumType = System.UInt32;
namespace Swis
{
	public static class Util
	{
		public static TDst ReinterpretCast<TSrc, TDst>(TSrc src)
				where TSrc : unmanaged
				where TDst : unmanaged
		{
			unsafe
			{
				Debug.Assert(sizeof(TSrc) <= sizeof(uint));
				Debug.Assert(sizeof(TDst) <= sizeof(uint));

				ReinterpretMediumType medium = *(ReinterpretMediumType*)(void*)&src;
				return *(TDst*)(void*)&medium;
			}
		}

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
			if (frombits <= 0 || frombits > Cpu.NativeSizeBits)
				throw new ArgumentOutOfRangeException(nameof(frombits));
			if (frombits == Cpu.NativeSizeBits)
				return src;

			uint valbits = (1u << (int)frombits) - 1; // ext 4bits to 8bits = 00001111
			uint extbits = ~valbits;                  //                      11110000
			uint signbit = 1u << (int)(frombits - 1); //                      00001000

			uint srcval = src & valbits; // ensure no more bits are present after the sign extension
			uint sign = (signbit & srcval) >> ((int)frombits - 1);

			return srcval | (extbits * sign);
		}


		public static string GetDebugView(this System.Linq.Expressions.Expression exp)
		{
			if (exp == null)
				return null;

			var propertyInfo = typeof(System.Linq.Expressions.Expression).GetProperty("DebugView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			return propertyInfo.GetValue(exp) as string;
		}
	}
}