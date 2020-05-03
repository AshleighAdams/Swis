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

		public static int Pow(int x, int pow)
		{
			int sign = 1;
			if (x < 0)
			{
				if ((pow & 1) != 0 || pow == 0)
					sign = -1;
				x = -x;
			}

			int ret = 1;
			while (pow != 0)
			{
				if ((pow & 1) == 1)
					ret *= x;
				x *= x;
				pow >>= 1;
			}
			return ret * sign;
		}
		public static uint Pow(uint x, uint pow)
		{
			uint ret = 1;
			while (pow != 0)
			{
				if ((pow & 1) == 1)
					ret *= x;
				x *= x;
				pow >>= 1;
			}
			return ret;
		}

		public static uint SignExtend(uint src, uint frombits)
		{
			if (frombits <= 0 || frombits > ICpu.NativeSizeBits)
				throw new ArgumentOutOfRangeException(nameof(frombits));
			if (frombits == ICpu.NativeSizeBits)
				return src;

			uint valbits = (1u << (int)frombits) - 1; // ext 4bits to 8bits = 00001111
			uint extbits = ~valbits;                  //                      11110000
			uint signbit = 1u << (int)(frombits - 1); //                      00001000

			uint srcval = src & valbits; // ensure no more bits are present after the sign extension
			uint sign = (signbit & srcval) >> ((int)frombits - 1);

			return srcval | (extbits * sign);
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public static string GetDebugView(this System.Linq.Expressions.Expression exp)
		{
			var property_info = typeof(System.Linq.Expressions.Expression).GetProperty("DebugView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			return property_info.GetValue(exp) as string ?? throw new Exception("DebugView() returned null!");
		}

		public class NullLineIO : ILineIO
		{
			byte ILineIO.ReadLineValue(ushort line) => 0;
			void ILineIO.WriteLineValue(ushort line, byte value) { }
		}
	}
}