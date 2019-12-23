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
		// special registers
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.InstructionPointer << ControlARegIdShift | ControlARegIdSize32,
			},
			"e" + nameof(NamedRegister.InstructionPointer) + "")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.BasePointer << ControlARegIdShift | ControlARegIdSize32,
			},
			"e" + nameof(NamedRegister.BasePointer) + "")]

		// standard registers and sizes
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
			},
			"e" + nameof(NamedRegister.A) + "x")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize16,
			},
			"" + nameof(NamedRegister.B) + "x")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.C << ControlARegIdShift | ControlARegIdSize8,
			},
			"" + nameof(NamedRegister.C) + "l")]

		// addressing modes
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeAB | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize32,
			},
			"e" + nameof(NamedRegister.A) + "x + " + "e" + nameof(NamedRegister.B) + "x")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeAB | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize16,
			},
			"e" + nameof(NamedRegister.A) + "x + " + nameof(NamedRegister.B) + "x")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeCD | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize16,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize32,
			},
			"" + nameof(NamedRegister.A) + "x * e" + nameof(NamedRegister.B) + "x")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize0 | MasterAddressingModeABCD | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.C << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.D << ControlARegIdShift | ControlARegIdSize32,
			},
			"e" + nameof(NamedRegister.A) + "x + e" + nameof(NamedRegister.B) + "x + e" + nameof(NamedRegister.C) + "x * e" + nameof(NamedRegister.D) + "x")]

		// indirection
		[InlineData(
			new byte[] {
				MasterIndirectionSize8 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
			},
			"ptr8 [e" + nameof(NamedRegister.A) + "x]")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize16 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
			},
			"ptr16 [e" + nameof(NamedRegister.A) + "x]")]
		[InlineData(
			new byte[] {
				MasterIndirectionSize32 | MasterAddressingModeA | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
			},
			"[e" + nameof(NamedRegister.A) + "x]")]

		// segments
		// TODO: unit tests for segments

		//combined
		[InlineData(
			new byte[] {
				MasterIndirectionSize16 | MasterAddressingModeABCD | MasterSegmentNone,
				(int)NamedRegister.A << ControlARegIdShift | ControlARegIdSize32,
				(int)NamedRegister.B << ControlARegIdShift | ControlARegIdSize16,
				(int)NamedRegister.C << ControlARegIdShift | ControlARegIdSize8,
				(int)NamedRegister.D << ControlARegIdShift | ControlARegIdSize32,
			},
			"ptr16 [e" + nameof(NamedRegister.A) + "x + " + nameof(NamedRegister.B) + "x + " + nameof(NamedRegister.C) + "l * e" + nameof(NamedRegister.D) + "x]")]

		public void DecodeOperand(byte[] input, string expected_asm)
		{
			uint ip = 0u;
			var memctrlr = new ByteArrayMemoryController(input);
			
			memctrlr.DecodeOperand(ref ip, null).ToString().Should().Be(expected_asm);
			ip.Should().Be((uint)input.Length);
		}
	}
}
