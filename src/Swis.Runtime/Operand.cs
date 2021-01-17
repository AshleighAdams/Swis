using System;
using System.Diagnostics.CodeAnalysis;

namespace Swis
{

	public struct Operand
	{
		//public Emulator Owner;
		[NotNull]
		public IMemoryController Memory;
		[NotNull]
		public uint[]? Registers;

		public sbyte RegIdA, RegIdB, RegIdC, RegIdD;
		public byte SizeA, SizeB, SizeC, SizeD;
		public uint ConstA, ConstB, ConstC, ConstD;

		public byte IndirectionSize;
		public byte AddressingMode;
		public byte Segment;

		public bool WriteAffectsFlow
		{
			get => AddressingMode == 0 && RegIdA == (int)NamedRegister.InstructionPointer;
		}

		public bool Indirect
		{
			get
			{
				return IndirectionSize != 0;
			}
		}

		public uint ValueSize // the effective size
		{
			get
			{
				if (Indirect)
					return IndirectionSize;
				return AddressingMode switch
				{
					0 => SizeA,
					1 => (uint)ICpu.NativeSizeBits,
					2 => (uint)ICpu.NativeSizeBits,
					3 => (uint)ICpu.NativeSizeBits,
					_ => throw new NotImplementedException(),
				};
			}
		}

		// TODO: somehow only provide this for a normal Cpu, mmmaybe move to inside InterpretedCpu
		private UInt32 InsideValue
		{
			get
			{
				uint[] regs = Registers ?? throw new Exception("Reigsters is null");

				uint part_value(sbyte regid, byte size, uint @const, bool signed = false)
				{
					if (regid == -1)
					{
						return signed ?
							Util.SignExtend(@const, size) :
							@const;
					}
					else
					{
						var stripped = (uint)(regs[regid] & (((ulong)1u << size) - 1)); //-V3106
						return signed ?
							Util.SignExtend(stripped, size) :
							stripped;
					}
				}

				return AddressingMode switch
				{
					// a
					0 => part_value(RegIdA, SizeA, ConstA),
					// a + b
					1 => 
						part_value(RegIdA, SizeA, ConstA)
						+
						part_value(RegIdB, SizeB, ConstB),
					// c * d
					2 => (uint)(
							(int)part_value(RegIdC, SizeC, ConstC, true)
							*
							(int)part_value(RegIdD, SizeD, ConstD, true)
						),
					// a + b + (c * d)
					3 =>
						part_value(RegIdA, SizeA, ConstA)
						+
						part_value(RegIdB, SizeB, ConstB)
						+
						(uint)(
							(int)part_value(RegIdC, SizeC, ConstC, true)
							*
							(int)part_value(RegIdD, SizeD, ConstD, true)
						),
					_ => throw new NotImplementedException(),
				};
			}
		}

		public override string ToString()
		{
			static string do_part(sbyte regid, byte size, uint @const)
			{
				if (regid > 0)
				{
					NamedRegister r = (NamedRegister)regid;

					if (regid >= (int)NamedRegister.A)
					{
						return size switch
						{
							8  => $"{r}l",
							16 => $"{r}x",
							32 => $"e{r}x",
							64 => $"r{r}x",
							_  => $"{r}sz{size}",
						};
					}
					else
					{
						return size switch
						{
							8  => $"{r}l",
							16 => $"{r}",
							32 => $"e{r}",
							64 => $"r{r}",
							_  => $"{r}sz{size}",
						};
					}
				}
				else
				{
					return $"{(int)@const}";
				}
			}

			string @base = AddressingMode switch
			{
				0 => $"{do_part(RegIdA, SizeA, ConstA)}",
				1 => $"{do_part(RegIdA, SizeA, ConstA)} + {do_part(RegIdB, SizeB, ConstB)}",
				2 => $"{do_part(RegIdC, SizeC, ConstC)} * {do_part(RegIdD, SizeD, ConstD)}",
				3 => $"{do_part(RegIdA, SizeA, ConstA)} + {do_part(RegIdB, SizeB, ConstB)}" + $" + {do_part(RegIdC, SizeC, ConstC)} * {do_part(RegIdD, SizeD, ConstD)}",
				_ => "???",
			};
			if (Indirect)
			{
				if (IndirectionSize == ICpu.NativeSizeBits)
					@base = $"[{@base}]";
				else
					@base = $"ptr{IndirectionSize} [{@base}]";
			}

			return @base;
		}

		public UInt32 Value
		{
			get
			{
				// get the inside value
				UInt32 inside = InsideValue;

				// indirection
				{
					if (!Indirect)
						return inside;

					return Memory[inside, IndirectionSize];
				}
			}
			set
			{
				// either indirection or address_mode == 0
				// cap it to the register memory size:
				value = (uint)((ulong)value & ((1ul << (int)SizeA) - 1));

				if (!Indirect)
				{
					// change the register
					if (AddressingMode != 0)
					{
						// nonsensical, halt
						throw new Exception("TODO: can't write to a computed value, doesn't make sense");
					}

					if (Registers is null) // TODO: move outside of this struct, to InterpretedCpu
						throw new Exception("Registers null");
					Registers[RegIdA] = value;
				}
				else
				{
					uint memloc = InsideValue;
					Memory[memloc, IndirectionSize] = value;
				}
			}
		}

		public Int32 Signed
		{
			get
			{
				Caster c = new Caster
				{
					U32 = Value
				};

				return ValueSize switch
				{
					32 => c.I32,
					16 => c.I16A,
					8  => c.I8A,
					_  => throw new Exception("invalid size"),
				};
			}
			set
			{
				Caster c = new Caster();

				switch (ValueSize)
				{
					default: throw new Exception("invalid size");
					case 32:
						c.I32 = value; break;
					case 16:
						c.I16A = (Int16)value; break;
					case 8:
						c.I8A = (SByte)value; break;
				}

				Value = c.U32;
			}
		}

		public Single Float
		{
			get
			{
				Caster c = new Caster
				{
					U32 = Value
				};
				return c.F32;
			}
			set
			{
				Caster c = new Caster
				{
					F32 = value
				};
				Value = c.U32;
			}
		}

	}

	public static class CpuExtensions
	{
		public static Opcode DecodeOpcode(this IMemoryController memory, ref uint ip)
		{
			var ret = (Opcode)memory[ip];
			ip++;
			return ret;
		}

		public static Operand DecodeOperand(this IMemoryController memory, ref uint ip, uint[]? registers)
		{
			byte master = memory[ip + 0];
			ip += 1;

			byte indirection_size = (byte)((master & 0b1110_0000u) >> 5);
			byte addressing_mode = (byte)((master & 0b0001_1000u) >> 3);
			byte segment = (byte)((master & 0b0000_0111u) >> 0);

			indirection_size = indirection_size switch
			{
				0 => 0,
				1 => 8,
				2 => 16,
				3 => 32,
				//4 => 64,
				_ => throw new Exception(),
			};

			sbyte rida, ridb, ridc, ridd;
			byte sza, szb, szc, szd;
			uint cona, conb, conc, cond;

			void decode_part(out sbyte regid, out byte size, out uint @const, ref uint ipinside)
			{
				byte control = memory[ipinside + 0];
				ipinside += 1;

				if ((control & 0b1000_0000u) != 0) // is it a constant?
				{
					uint extra_bytes = ((control & 0b0110_0000u) >> 5);
					switch (extra_bytes)
					{
						case 0: goto default;
						case 1: goto default;
						case 2: goto default;
						case 3: extra_bytes = 4; break;
						default: break;
					}

					uint total;
					if (extra_bytes != 4)
					{
						uint extra_bits = extra_bytes * 8;
						total = (control & 0b0001_1111u);
						if (extra_bytes > 0)
						{
							total |= (memory[ipinside, extra_bits] << 5);
							ipinside += extra_bytes;
						}
						total = Util.SignExtend(total, 5 + extra_bits);
					}
					else
					{
						total = memory[ipinside, 32];
						ipinside += 4;
					}
					regid = -1;
					size = 32; // todo: maybe
					@const = total;
					return;
				}
				else
				{
					@const = 0;
					regid = (sbyte)((control & 0b0111_1100u) >> 2);
					uint szid = ((control & 0b0000_0011u) >> 0);
					size = szid switch
					{
						0 => 8,
						1 => 16,
						2 => 32,
						//3 => 64,
						_ => throw new Exception(),
					};
					return;
				}
			}

			switch (addressing_mode)
			{
				case 0: // a
					decode_part(out rida, out sza, out cona, ref ip);
					ridb = ridc = ridd = -1;
					szb = szc = szd = 0;
					conb = conc = cond = 0;
					break;
				case 1: // a + b
					decode_part(out rida, out sza, out cona, ref ip);
					decode_part(out ridb, out szb, out conb, ref ip);
					ridc = ridd = -1;
					szc = szd = 0;
					conc = cond = 0;
					break;
				case 2: // c * d
					decode_part(out ridc, out szc, out conc, ref ip);
					decode_part(out ridd, out szd, out cond, ref ip);
					rida = ridb = -1;
					sza = szb = 0;
					cona = conb = 0;
					break;
				case 3: // a + b + c * d
					decode_part(out rida, out sza, out cona, ref ip);
					decode_part(out ridb, out szb, out conb, ref ip);
					decode_part(out ridc, out szc, out conc, ref ip);
					decode_part(out ridd, out szd, out cond, ref ip);
					break;
				default:
					throw new Exception();
			}

			return new Operand
			{
				Memory = memory,
				Registers = registers,
				RegIdA = rida,
				RegIdB = ridb,
				RegIdC = ridc,
				RegIdD = ridd,
				ConstA = cona,
				ConstB = conb,
				ConstC = conc,
				ConstD = cond,
				SizeA = sza,
				SizeB = szb,
				SizeC = szc,
				SizeD = szd,
				IndirectionSize = indirection_size,
				Segment = segment,
				AddressingMode = addressing_mode,
			};
		}
	}
}