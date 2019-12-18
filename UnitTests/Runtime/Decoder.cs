using System;
using Swis;
using Xunit;
using FluentAssertions;

namespace UnitTests
{
	public class DecoderTests
	{
		[Theory]
		[InlineData(new byte[] { (byte)Opcode.Nop }, Opcode.Nop, 1u)]
		[InlineData(new byte[] { (byte)Opcode.MoveRR }, Opcode.MoveRR, 1u)]
		[InlineData(new byte[] { (byte)Opcode.ExtendR }, Opcode.ExtendR, 1u)]
		public void DecodeOpcode(byte[] input, Opcode result, uint decode_size)
		{
			uint ip = 0u;
			var memctrlr = new ByteArrayMemoryController(input);
			
			memctrlr.DecodeOpcode(ref ip).Should().Be(result);
			ip.Should().Be(decode_size);
		}

		const byte MasterIndirectionSize0       = 0b000_00_000;
		const byte MasterIndirectionSize8       = 0b001_00_000;
		const byte MasterIndirectionSize16      = 0b010_00_000;
		const byte MasterIndirectionSize32      = 0b011_00_000;
		const byte MasterIndirectionSize64      = 0b100_00_000;
		const byte MasterIndirectionSizeUnused1 = 0b101_00_000;
		const byte MasterIndirectionSizeUnused2 = 0b110_00_000;
		const byte MasterIndirectionSizeUnused3 = 0b111_00_000;

		const byte MasterAddressingModeA    = 0b000_00_000;
		const byte MasterAddressingModeAB   = 0b000_01_000;
		const byte MasterAddressingModeCD   = 0b000_10_000;
		const byte MasterAddressingModeABCD = 0b000_11_000;
		
		const byte MasterSegmentNone = 0b000_00_000;
		const byte MasterSegmentSS   = 0b000_00_001;
		const byte MasterSegmentCD   = 0b000_00_010;
		const byte MasterSegmentDS   = 0b000_00_011;
		const byte MasterSegmentES   = 0b000_00_100;
		const byte MasterSegmentFS   = 0b000_00_101;
		const byte MasterSegmentGS   = 0b000_00_110;
		const byte MasterSegmentXS   = 0b000_00_111;

		const int ControlARegIdShift  = 2; // 0b011111_00
		const int ControlARegIdSize8  = 0b000000_00;
		const int ControlARegIdSize16 = 0b000000_01;
		const int ControlARegIdSize32 = 0b000000_10;
		const int ControlARegIdSize64 = 0b000000_11;

		[Theory]
		[InlineData(
			new byte[] { 
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.InstructionPointer << ControlARegIdShift | ControlARegIdSize32 },
			2u, "e" + nameof(NamedRegister.InstructionPointer) + "")]
		public void DecodeOperand(byte[] input, uint decode_size, string expected_asm)
		{
			uint ip = 0u;
			var memctrlr = new ByteArrayMemoryController(input);
			
			memctrlr.DecodeOperand(ref ip, null).ToString().Should().Be(expected_asm);
			ip.Should().Be(decode_size);
		}
	}
}
