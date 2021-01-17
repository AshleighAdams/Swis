using FluentAssertions;
using Swis;
using Xunit;

namespace UnitTests
{
	public class PowTests
	{
		[Theory]
		[InlineData(1, 0, 1)]
		[InlineData(-1, 0, -1)]
		[InlineData(-2, 0, -1)]
		[InlineData(1, 1, 1)]
		[InlineData(2, 2, 4)]
		[InlineData(-2, 2, 4)]
		[InlineData(10, 2, 100)]
		[InlineData(-5, 3, -125)]
		[InlineData(0, 0, 1)]
		public void PowInt(int value, int power, int expected)
		{
			Util.Pow(value, power).Should().Be(expected);
		}

		[Theory]
		[InlineData(1u, 0u, 1u)]
		[InlineData(0u, 0u, 1u)]
		[InlineData(1u, 1u, 1u)]
		[InlineData(2u, 2u, 4u)]
		[InlineData(10u, 2u, 100u)]
		public void PowUInt(uint value, uint power, uint expected)
		{
			Util.Pow(value, power).Should().Be(expected);
		}
	}
}
