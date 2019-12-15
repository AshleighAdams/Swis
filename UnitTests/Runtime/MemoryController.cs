using System;
using Swis;
using Xunit;
using FluentAssertions;

namespace UnitTests
{
	public class MemoryControllerTests
	{
		readonly byte[] Buffer = { 0x01, 0x00, 0x00, 0x00, 0x0a, 0x0b, 0xdd, 0xcc, 0xbb, 0xaa };

		[Theory]
		[InlineData(0u, 8u, 0x1)]
		[InlineData(0u, 32u, 0x1)]
		[InlineData(2u, 32u, 0x0b0a0000)]
		[InlineData(6u, 32u, 0xaabbccdd)]
		public void MemoryControllerRead(uint addr, uint bits, uint result)
		{
			{
				var controller = new PointerMemoryController(this.Buffer);
				controller[addr, bits].Should().Be(result);
			}

			{
				var controller = new ByteArrayMemoryController(this.Buffer);
				controller[addr, bits].Should().Be(result);
			}
		}

		[Theory]
		[InlineData(0u, 8u, 0x1, "\x01")]
		[InlineData(0u, 32u, 0x1, "\x01\x00\x00\x00")]
		[InlineData(0u, 32u, 0xaabbccdd, "\xdd\xcc\xbb\xaa")]
		[InlineData(2u, 32u, 0x0b0a0000, "\x00\x00\x0a\x0b")]
		public void MemoryControllerWrite(uint addr, uint bits, uint value, string result)
		{
			{
				var controller = new ByteArrayMemoryController(new byte[10]);
				controller[addr, bits] = value;

				for (int i = (int)addr, n = 0; i < bits / 8; i++, n++)
					controller.Memory[i].Should().Be((byte)result[n]);
			}

			{
				var controller = new PointerMemoryController(new byte[10]);
				controller[addr, bits] = value;

				for (int i = (int)addr, n = 0; i < bits / 8; i++, n++)
					controller.Memory[i].Should().Be((byte)result[n]);
			}
		}
	}
}
