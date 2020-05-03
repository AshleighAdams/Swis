using Swis;
using Xunit;

namespace UnitTests
{
	public class ReinterpretCastTests
	{
		[Theory]
		[InlineData((short)-1)]
		[InlineData((short)-123)]
		[InlineData((short)123)]
		[InlineData((short)0)]
		[InlineData(short.MaxValue)]
		[InlineData(short.MinValue)]
		public void ReinterpretCastShortInt(short value)
		{
			var result = Util.ReinterpretCast<short, int>(value);
			var original = Util.ReinterpretCast<int, short>(result);

			Assert.Equal(value, original);

			Caster c;
			c.I32 = 0; c.U32 = 0;
			c.I16A = value;
			// registers don't see endianness, but memory (where Caster is used) does
			uint caster_extended = Util.SignExtend(c.U32, 16);
			uint result_uint = Util.ReinterpretCast<int, uint>(result);

			Assert.Equal(caster_extended, result_uint);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(-123)]
		[InlineData(123)]
		[InlineData(0)]
		[InlineData(int.MaxValue)]
		[InlineData(int.MinValue)]
		public void ReinterpretCastIntUInt(int value)
		{
			var result = Util.ReinterpretCast<int, uint>(value);
			var original = Util.ReinterpretCast<uint, int>(result);

			Assert.Equal(value, original);

			Caster c; c.U32 = 0;
			c.I32 = value;

			Assert.Equal(c.U32, result);
		}

		[Theory]
		[InlineData(0.5f)]
		[InlineData(-1.0f)]
		[InlineData(-123.0f)]
		[InlineData(123.0f)]
		[InlineData(0.0f)]
		[InlineData(float.MaxValue)]
		[InlineData(float.MinValue)]
		public void ReinterpretCastFloatUInt(float value)
		{
			var result = Util.ReinterpretCast<float, uint>(value);
			var original = Util.ReinterpretCast<uint, float>(result);

			Assert.Equal(value, original);

			Caster c; c.U32 = 0;
			c.F32 = value;

			Assert.Equal(c.U32, result);
		}
	}
}
