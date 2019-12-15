using System;
using Swis;
using Xunit;
using FluentAssertions;

namespace UnitTests
{
	public class SignExtendTests
	{
		[Theory]
		[InlineData(0x000000ffu, 8u, 0xffffffffu)]
		[InlineData(0x0000000fu, 8u, 0x0000000fu)]
		[InlineData(0x0000000fu, 4u, 0xffffffffu)]
		[InlineData(0x0000000fu, 5u, 0x0000000fu)]
		[InlineData(0b00000000000000000000000000000000u, 1u,  0b00000000000000000000000000000000u)]
		[InlineData(0b00000000000000000000000000000001u, 1u,  0b11111111111111111111111111111111u)]
		[InlineData(0b00000000000000000101010101010101u, 15u, 0b11111111111111111101010101010101u)]
		[InlineData(0b00000000000000000101010101010101u, 16u, 0b00000000000000000101010101010101u)]
		[InlineData(0b00000000000000000101010101010101u, 6u,  0b00000000000000000000000000010101u)]
		[InlineData(0b00000000000000000010101010101010u, 6u,  0b11111111111111111111111111101010u)]
		[InlineData(0b00000000000000000101010101010101u, 7u,  0b11111111111111111111111111010101u)]
		[InlineData(0b00000000000000000010101010101010u, 7u,  0b00000000000000000000000000101010u)]
		[InlineData(0xdeadbeefu, sizeof(uint) * 8u, 0xdeadbeefu)]
		public void CheckKnown(uint value, uint from_bits, uint expected)
		{
			Util.SignExtend(value, from_bits).Should().Be(expected);
		}

		[Theory]
		[InlineData(1u, 0u)]
		public void CheckThrows(uint value, uint from_bits)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => Util.SignExtend(value, from_bits));
		}
	}
}
