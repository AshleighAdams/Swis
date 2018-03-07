using System;

namespace Swis
{

	public struct Operand
	{
		//public Emulator Owner;
		public MemoryController Memory;
		public uint[] Registers;
		
		public sbyte RegIdA, RegIdB, RegIdC, RegIdD;
		public byte SizeA, SizeB, SizeC, SizeD;
		public uint ConstA, ConstB, ConstC, ConstD;

		public byte IndirectionSize;
		public byte AddressingMode;
		public byte Segment;

		public bool Indirect
		{
			get
			{
				return this.IndirectionSize != 0;
			}
		}

		public uint ValueSize // the effective size
		{
			get
			{
				if (this.Indirect)
					return this.IndirectionSize;
				switch (this.AddressingMode)
				{
				case 0: return this.SizeA;
				case 1: return (uint)Cpu.NativeSizeBits;
				case 2: return (uint)Cpu.NativeSizeBits;
				case 3: return (uint)Cpu.NativeSizeBits;
				default: throw new NotImplementedException();
				}
			}
		}

		UInt32 InsideValue
		{
			get
			{
				uint[] regs = this.Registers;

				uint part_value(sbyte regid, byte size, uint @const)
				{
					if (regid == -1)
						return @const;
					else
						// maybe sign extend
						return (uint)(regs[regid] & (((ulong)1u << size) - 1)); //-V3106
				}

				uint total = 0;
				switch (this.AddressingMode)
				{
				case 0:
					return part_value(this.RegIdA, this.SizeA, this.ConstA);
				case 1:
					return part_value(this.RegIdA, this.SizeA, this.ConstA)
						+ part_value(this.RegIdB, this.SizeB, this.ConstB);
				case 2:
					return total;
				case 3:
					total = part_value(this.RegIdA, this.SizeA, this.ConstA);
					total += part_value(this.RegIdB, this.SizeB, this.ConstB);
					
					return total + (uint)((int)part_value(this.RegIdC, this.SizeC, this.ConstC)
						* (int)part_value(this.RegIdD, this.SizeD, this.ConstD));
				default: throw new NotImplementedException();
				}
			}
		}

		public override string ToString()
		{
			string @base;

			string do_part(sbyte regid, byte size, uint @const)
			{
				if (regid > 0)
				{
					NamedRegister r = (NamedRegister)regid;

					if (regid >= (int)NamedRegister.A)
					{
						switch (size)
						{
						case 8:
							return $"{r}l";
						case 16:
							return $"{r}x";
						case 32:
							return $"e{r}x";
						case 64:
							return $"r{r}x";
						default: return $"{r}sz{size}";
						}
					}
					else
					{
						switch (size)
						{
						case 8:
							return $"{r}l";
						case 16:
							return $"{r}";
						case 32:
							return $"e{r}";
						case 64:
							return $"r{r}";
						default: return $"{r}sz{size}";
						}
					}
				}
				else
				{
					return $"{(int)@const}";
				}
			}

			switch (this.AddressingMode)
			{
			case 0:
				@base = $"{do_part(this.RegIdA, this.SizeA, this.ConstA)}";
				break;
			case 1:
				@base = $"{do_part(this.RegIdA, this.SizeA, this.ConstA)} + {do_part(this.RegIdB, this.SizeB, this.ConstB)}";
				break;
			case 2:
				@base = $"{do_part(this.RegIdC, this.SizeC, this.ConstC)} * {do_part(this.RegIdD, this.SizeD, this.ConstD)}";
				break;
			case 3:
				@base = $"{do_part(this.RegIdA, this.SizeA, this.ConstA)} + {do_part(this.RegIdB, this.SizeB, this.ConstB)}" +
					$" + {do_part(this.RegIdC, this.SizeC, this.ConstC)} * {do_part(this.RegIdD, this.SizeD, this.ConstD)}";
				break;
			default:
				@base = "???";
				break;
			}

			if (this.Indirect)
			{
				if (this.IndirectionSize == Cpu.NativeSizeBits)
					@base = $"[{@base}]";
				else
					@base = $"ptr{this.IndirectionSize} [{@base}]";
			}

			return @base;
		}

		public UInt32 Value
		{
			get
			{
				// get the inside value
				UInt32 inside = this.InsideValue;

				// indirection
				{
					if (!this.Indirect)
						return inside;

					return this.Memory[inside, this.IndirectionSize];
				}
			}
			set
			{
				// either indirection or address_mode == 0
				// cap it to the register memory size:
				value = (uint)((ulong)value & ((1ul << (int)this.SizeA) - 1));

				if (!this.Indirect)
				{
					// change the register
					if (this.AddressingMode != 0)
					{
						// nonsensical, halt
						throw new Exception("TODO: can't write to a computed value, doesn't make sense");
					}

					this.Registers[this.RegIdA] = value;
				}
				else
				{
					uint memloc = this.InsideValue;
					this.Memory[memloc, this.IndirectionSize] = value;
				}
			}
		}

		public Int32 Signed
		{
			get
			{
				Caster c = new Caster
				{
					U32 = this.Value
				};

				switch (this.ValueSize)
				{
				default: throw new Exception("invalid size");
				case 32:
					return c.I32;
				case 16:
					return c.I16A;
				case 8:
					return c.I8A;
				}
			}
			set
			{
				Caster c = new Caster();

				switch (this.ValueSize)
				{
				default: throw new Exception("invalid size");
				case 32:
					c.I32 = value; break;
				case 16:
					c.I16A = (Int16)value; break;
				case 8:
					c.I8A = (SByte)value; break;
				}

				this.Value = c.U32;
			}
		}

		public Single Float
		{
			get
			{
				Caster c = new Caster
				{
					U32 = this.Value
				};
				return c.F32;
			}
			set
			{
				Caster c = new Caster
				{
					F32 = value
				};
				this.Value = c.U32;
			}
		}

	}

	public static class CpuExtensions
	{
		public static Opcode DecodeOpcode(this MemoryController memory, ref uint ip)
		{
			var ret = (Opcode)memory[ip];
			ip++;
			return ret;
		}

		public static Operand DecodeOperand(this MemoryController memory, ref uint ip, uint[] registers)
		{
			byte master = memory[ip + 0];
			ip += 1;

			byte indirection_size = (byte)((master & 0b1110_0000u) >> 5);
			byte addressing_mode = (byte)((master & 0b0001_1000u) >> 3);
			byte segment = (byte)((master & 0b0000_0111u) >> 0);

			switch (indirection_size)
			{
			case 0: break;
			case 1: indirection_size = 8; break;
			case 2: indirection_size = 16; break;
			case 3: indirection_size = 32; break;
			//case 4: indirection_size = 64; break;
			default: throw new Exception();
			}

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
					switch (szid)
					{
					case 0: size = 8; break;
					case 1: size = 16; break;
					case 2: size = 32; break;
					//case 3: size = 64; break;
					default: throw new Exception();
					}
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
				ConstDSigned = dsign,
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