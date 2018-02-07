using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Operand = Swis.Cpu.Operand;

// https://github.com/wiremod/Miscellaneous/blob/master/CPU%20%26%20GPU/zcpudocs/zcpudoc.pdf

namespace Swis
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Caster
	{
		[FieldOffset(0)] public Int32 I32;
		[FieldOffset(0)] public Int16 I16A;
		[FieldOffset(2)] public Int16 I16B;
		[FieldOffset(0)] public SByte I8A;
		[FieldOffset(1)] public SByte I8B;
		[FieldOffset(2)] public SByte I8C;
		[FieldOffset(3)] public SByte I8D;


		[FieldOffset(0)] public UInt32 U32;
		[FieldOffset(0)] public UInt16 U16A;
		[FieldOffset(2)] public UInt16 U16B;
		[FieldOffset(0)] public Byte U8A;
		[FieldOffset(1)] public Byte U8B;
		[FieldOffset(2)] public Byte U8C;
		[FieldOffset(3)] public Byte U8D;

		[FieldOffset(0)] public Single F32;
		
		[FieldOffset(0)] public Byte ByteA;
		[FieldOffset(1)] public Byte ByteB;
		[FieldOffset(2)] public Byte ByteC;
		[FieldOffset(3)] public Byte ByteD;
	}

	public abstract class MemoryController
	{
		public abstract byte this[uint x] { get; set; }

		// bits MUST be either 8, 16, 32 (, or 64 if 64bit, which is it's not yet)
		// try to override this method, as doing so can get alignment speedup gains
		public virtual uint this[uint x, uint bits]
		{
			get
			{
				uint ret = 0;
				switch (bits)
				{
				case 32:
					ret |= ((uint)this[x + 3] << 24) | ((uint)this[x + 2] << 16);
					goto case 16;
				case 16:
					ret |= ((uint)this[x + 1]) << 8;
					goto case 8;
				case 8:
					ret |= ((uint)this[x]) << 0;
					return ret;
				default:
					throw new Exception();
				}
			}
			set
			{
			}
		}

		//public abstract byte this[int x] { get; set; }


		}

	public class DirectMemoryController : MemoryController
	{
		uint[] Memory;
		public DirectMemoryController(byte[] memory) // TODO: use Span<>
		{
			this.Memory = new uint[memory.Length / 4];
			for (uint i = 0; i < memory.Length; i++)
				this[i] = memory[i];
			//this.Memory = memory;
		}

		public override byte this[uint x]
		{
			get
			{
				return (byte)this[x, 8];
			}
			set
			{
				this[x, 8] = value;
				//this.Memory[x] = value;
			}
		}
		
		[StructLayout(LayoutKind.Explicit)]
		struct UnalignedHelper
		{
			[FieldOffset(0)] public UInt32 Lo;
			[FieldOffset(4)] public UInt32 Hi;

			// 32bit
			[FieldOffset(0)] public UInt32 U32_0;
			[FieldOffset(1)] public UInt32 U32_8;
			[FieldOffset(2)] public UInt32 U32_16;
			[FieldOffset(3)] public UInt32 U32_24;

			// 16bit
			[FieldOffset(0)] public UInt16 U16_0;
			[FieldOffset(1)] public UInt16 U16_8;
			[FieldOffset(2)] public UInt16 U16_16;
			[FieldOffset(3)] public UInt16 U16_24;

			[FieldOffset(0)] public Byte U8_0;
			[FieldOffset(1)] public Byte U8_8;
			[FieldOffset(2)] public Byte U8_16;
			[FieldOffset(3)] public Byte U8_24;
		}

		public override uint this[uint x, uint bits] // unaligned access
		{
			get
			{
				int missalign = (int)((x % 4) * 8);
				uint idx = x / sizeof(uint);

				// aligned is super easy
				if (missalign == 0 && bits == 32)
					return this.Memory[idx];

				UnalignedHelper helper = new UnalignedHelper();

				helper.Lo = this.Memory[idx + 0];
				if (bits + missalign > 32)
					helper.Hi = this.Memory[idx + 1];

				switch (bits)
				{
				case 8:
					switch (missalign)
					{
					case 0:  return helper.U8_0;
					case 8:  return helper.U8_8;
					case 16: return helper.U8_16;
					case 24: return helper.U8_24;
					default: throw new Exception();
					}
				case 16:
					switch (missalign)
					{
					case 0:  return helper.U16_0;
					case 8:  return helper.U16_8;
					case 16: return helper.U16_16;
					case 24: return helper.U16_24;
					default: throw new Exception();
					}
				case 32:
					switch (missalign)
					{
					// case 0:  return helper.U32_0; // special case above
					case 8:  return helper.U32_8;
					case 16: return helper.U32_16;
					case 24: return helper.U32_24;
					default: throw new Exception();
					}
				default: throw new Exception();
				}
			}
			set
			{
				int missalign = (int)((x % 4) * 8);
				uint idx = x / sizeof(uint);

				// aligned is super easy
				if (missalign == 0 && bits == 32)
				{
					this.Memory[idx] = value;
					return;
				}

				UnalignedHelper helper = new UnalignedHelper();

				helper.Lo = this.Memory[idx + 0];
				if (bits + missalign > 32)
					helper.Hi = this.Memory[idx + 1];

				switch (bits)
				{
				case 8:
					switch (missalign)
					{
					case  0: helper.U8_0 =  (Byte)value; break;
					case  8: helper.U8_8 =  (Byte)value; break;
					case 16: helper.U8_16 = (Byte)value; break;
					case 24: helper.U8_24 = (Byte)value; break;
					default: throw new Exception();
					} break;
				case 16:
					switch (missalign)
					{
					case  0: helper.U16_0 =  (UInt16)value; break;
					case  8: helper.U16_8 =  (UInt16)value; break;
					case 16: helper.U16_16 = (UInt16)value; break;
					case 24: helper.U16_24 = (UInt16)value; break;
					default: throw new Exception();
					} break;
				case 32:
					switch (missalign)
					{
					// case  0: helper.U32_0 =  value; break; // special case, fully in alignment
					case  8: helper.U32_8 =  value; break;
					case 16: helper.U32_16 = value; break;
					case 24: helper.U32_24 = value; break;
					default: throw new Exception();
					} break;
				default: throw new Exception();
				}

				// store any changes back
				this.Memory[idx + 0] = helper.Lo;
				if (bits + missalign > 32)
					this.Memory[idx + 1] = helper.Hi;
			}
		}
		/*
		// TODO: compare the performance of this compared to the one above, when the bux is fixed with it
		public override uint this[uint x, uint bits] // aligned access
		{
			// uses little endian
			// missalign  1 >= 8bits; 2 >= 16 bits; 3 >= 24bits; 4 >= 32bits
			// 8:         [3 2 1 _] [_ _ _ 4]
			// 16:        [2 1 _ _] [_ _ 4 3]
			// 24:        [1 _ _ _] [_ 4 3 2]
			get
			{
				int missalign = (int)((x % 4) * 8);
				uint idx = x / sizeof(uint);

				uint bitmask = (uint)(((long)1u << (int)bits) - 1);

				if (missalign == 0)
				{
					if (bits == 32) // fully aligned
						return this.Memory[idx];
					else
						return this.Memory[idx] & bitmask;
				}
				
				uint val = (this.Memory[idx + 0] >> missalign);

				if (bits + missalign > 32)
				{
					val |= (this.Memory[idx + 1] << (32 - missalign));
				}

				return val & bitmask;
			}
			set
			{
				int missalign = (int)((x % 4) * 8);
				uint idx = x / sizeof(uint);

				uint bitmask = (uint)(((long)1u << (int)bits) - 1);

				if (missalign == 0) // fully aligned
				{
					if (bits == 32) // preserve no bits
						this.Memory[idx] = value;
					else
					{
						uint valbits = bitmask;   // 0b00001111
						uint oldbits = ~valbits;  // 0b11110000

						uint old = this.Memory[idx] & oldbits;
						this.Memory[idx] = old | (value | valbits);
					}
					return;
				}
				
				uint lo_valmask = bitmask << missalign;
				uint lo_oldmask = ~lo_valmask;
				uint lo_newbits = (value << missalign) & lo_valmask;
				this.Memory[idx + 0] = (this.Memory[idx + 0] & lo_oldmask) | lo_newbits;

				if (bits + missalign > 32)
				{
					uint hi_valmask = bitmask >> (32 - missalign);
					uint hi_oldmask = ~hi_valmask;
					uint hi_newbits = (value >> (32 - missalign)) & hi_valmask;
					this.Memory[idx + 1] = (this.Memory[idx + 1] & hi_oldmask) | hi_newbits;
				}
			}
		}*/
	}

	public abstract class ExternalDebugger
	{
		public abstract bool Clock(Cpu cpu, MemoryController memory, Register[] registers);
	}

	public static class CpuExtensions
	{
		public static Opcode DecodeOpcode(this MemoryController memory, ref uint ip)
		{
			var ret = (Opcode)memory[ip];
			ip++;
			return ret;
		}

		public static Operand DecodeOperand(this MemoryController memory, ref uint ip, Register[] registers)
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
			bool dsign = false;

			bool decode_part(out sbyte regid, out byte size, out uint @const, ref uint ipinside) // returns true if signed
			{
				byte control = memory[ipinside + 0];
				ipinside += 1;

				if ((control & 0b1000_0000u) != 0) // is it a constant?
				{
					uint extra_bytes = ((control & 0b0110_0000u) >> 5);
					bool signed = (control & 0b0001_0000u) != 0;
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
						// todo: check endianness here
						total = (control & 0b0000_1111u);
						for (int i = 0; i < extra_bytes; i++)
						{
							uint constpart = memory[ipinside + 0];
							ipinside += 1;
							total |= constpart << (i * 8 + 4);
						}
						if (signed)
							total = Cpu.SignExtend(total, 4 + extra_bytes * 8);
					}
					else
					{
						uint a = memory[ipinside + 0];
						uint b = memory[ipinside + 1];
						uint c = memory[ipinside + 2];
						uint d = memory[ipinside + 3];
						total = 0
							| (a << 0)
							| (b << 8)
							| (c << 16)
							| (d << 24);
						ipinside += 4;
					}
					regid = -1;
					size = 32; // todo: maybe
					@const = total;
					return signed;
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
					return false;
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
				dsign = decode_part(out ridd, out szd, out cond, ref ip);
				rida = ridb = -1;
				sza = szb = 0;
				cona = conb = 0;
				break;
			case 3: // a + b + c * d
				decode_part(out rida, out sza, out cona, ref ip);
				decode_part(out ridb, out szb, out conb, ref ip);
				decode_part(out ridc, out szc, out conc, ref ip);
				dsign = decode_part(out ridd, out szd, out cond, ref ip);
				break;
			default:
				throw new Exception();
			}

			return new Operand
			{
				Memory = memory,
				Registers = registers,
				RegIdA = rida, RegIdB = ridb, RegIdC = ridc, RegIdD = ridd,
				ConstA = cona, ConstB = conb, ConstC = conc, ConstD = conc, ConstDSigned = dsign,
				SizeA = sza, SizeB = szb, SizeC = szc, SizeD = szd,
				IndirectionSize = indirection_size,
				Segment = segment,
				AddressingMode = addressing_mode,
			};
		}
	}

	public class Cpu
    {
		public static uint NativeSizeBits = 32;
		public static uint NativeSizeBytes = NativeSizeBits / 8;


		public struct Operand
		{
			//public Emulator Owner;
			public MemoryController Memory;
			public Register[] Registers;

			public bool ConstDSigned; // if to do smul for c*d
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
					default:  throw new NotImplementedException();
					}
				}
			}

			UInt32 InsideValue
			{
				get
				{
					Register[] regs = this.Registers;

					uint part_value(sbyte regid, byte size, uint @const)
					{
						if (regid == -1)
							return @const;
						else
							return (uint)(regs[regid].NativeUInt & (((ulong)1u << size) - 1));
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
						total =  part_value(this.RegIdA, this.SizeA, this.ConstA);
						total += part_value(this.RegIdB, this.SizeB, this.ConstB);
						if (this.ConstDSigned) // does this need the caster?
							return total + (uint)((int)part_value(this.RegIdC, this.SizeC, this.ConstC)
								* (int)part_value(this.RegIdD, this.SizeD, this.ConstD));
						else
							return total + part_value(this.RegIdC, this.SizeC, this.ConstC)
								* part_value(this.RegIdD, this.SizeD, this.ConstD);
					default: throw new NotImplementedException();
					}
				}
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

						Caster c; c.U32 = 0;
						switch (this.IndirectionSize)
						{
						default: throw new Exception("TODO: register with invalid indirection size");
						case 32:
							c.ByteD = this.Memory[inside + 3];
							c.ByteC = this.Memory[inside + 2];
							goto case 16;
						case 16:
							c.ByteB = this.Memory[inside + 1];
							goto case 8;
						case 8:
							c.ByteA = this.Memory[inside + 0];
							break;
						}

						return c.U32;
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

						this.Registers[this.RegIdA].NativeUInt = value;
					}
					else
					{
						uint memloc = this.InsideValue;

						Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
						c.U32 = value;

						switch (this.IndirectionSize) // maybe don't fall thru, so we go forward thru data?
						{
						default: throw new Exception("TODO: invalid register size");
						case 32:
							this.Memory[memloc + 3] = c.ByteD;
							this.Memory[memloc + 2] = c.ByteC;
							goto case 16;
						case 16:
							this.Memory[memloc + 1] = c.ByteB;
							goto case 8;
						case 8:
							this.Memory[memloc + 0] = c.ByteA;
							break;
						}
					}
				}
			}

			public Int32 Signed
			{
				get
				{
					Caster c = new Caster();
					c.U32 = this.Value;

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
					Caster c = new Caster();
					c.U32 = this.Value;
					return c.F32;
				}
				set
				{
					Caster c = new Caster();
					c.F32 = value;
					this.Value = c.U32;
				}
			}
			
		}
		
		Register[] Registers = new Register[32];

		//ref Register StackRegister = null;
		ref Register TimeStampCounter
		{
			get { return ref this.Registers[(int)NamedRegister.TickCount]; }
		}

		ref Register InstructionPointer
		{
			get { return ref this.Registers[(int)NamedRegister.InstructionPointer]; }
		}

		ref Register StackPointer
		{
			get { return ref this.Registers[(int)NamedRegister.StackPointer]; }
		}

		ref Register BasePointer
		{
			get { return ref this.Registers[(int)NamedRegister.BasePointer]; }
		}

		ref Register Flags
		{
			get { return ref this.Registers[(int)NamedRegister.Flags]; }
		}

		public bool Halted
		{
			get
			{ return ((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Halted); }
		}
		
		public MemoryController Memory;

		public ExternalDebugger Debugger { get; set; }

		public Cpu()
		{
			this.Memory = null;
		}
		
		protected Operand CreatePointer(uint address, uint size)
		{
			return new Operand
			{
				Memory = this.Memory,
				Registers = null,
				RegIdA = -1,
				SizeA = (byte)Cpu.NativeSizeBits, // size of the const (address), not the size of the value at the address
				ConstA = address,
				IndirectionSize = (byte)size,
			};
		}

		public static uint SignExtend(uint src, uint frombits)
		{
			if (frombits < 1 || frombits >= 32)
				throw new Exception();

			uint valbits = (1u << (int)frombits) - 1; // ext 4bits to 8bits = 00001111
			uint extbits = ~valbits;                  //                      11110000
			uint signbit = 1u << (int)(frombits - 1); //                      00001000

			uint srcval = src & valbits; // ensure no more bits are present after the sign extension
			uint sign = (signbit & srcval) >> ((int)frombits - 1);

			return srcval | (extbits * sign);
		}

		public int Clock(int count = 1)
		{
			ref Register tsc = ref this.TimeStampCounter;
			ref Register ip = ref this.InstructionPointer;
			ref Register sp = ref this.StackPointer;
			ref Register bp = ref this.BasePointer;
			ref Register flags = ref this.Flags;

			if (this.Halted)
				return 0;
			
			for (int i = 0; i < count; i++)
			{
				if (this.Debugger != null)
					if (!this.Debugger.Clock(this, this.Memory, this.Registers))
						break;

				Opcode op = this.Memory.DecodeOpcode(ref ip.NativeUInt);
				
				switch (op)
				{
				#region Misc
				case Opcode.Nop:
					break;

				case Opcode.SignExtendRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand bit = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						
						dst.Value = SignExtend(src.Value, bit.Value);
						break;
					}
				case Opcode.ZeroExtendRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand bit = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						uint frombits = bit.Value;
						if (frombits < 1 || frombits >= 32)
							throw new Exception();

						uint valbits = (1u << (int)frombits) - 1;

						src.Value = dst.Value & valbits;
						break;
					}
				case Opcode.Halt:
					flags.NativeUInt |= (uint)FlagsRegisterFlags.Halted;
					count = 0; // so we break from the loop
					break;
				case Opcode.InRR:
					{
						Operand lttr = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand line = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						lttr.Value = (uint)Console.Read();
						break;
					}
				case Opcode.OutRR:
					{
						Operand line = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand lttr = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						Console.Write((char)lttr.Value);
						break;
					}
				#endregion
				#region Memory
				case Opcode.MoveRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						dst.Value = src.Value;
						break;
					}
				case Opcode.PushR:
					{
						Operand src = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						Operand ptr = this.CreatePointer(sp.NativeUInt, src.ValueSize);
						sp.NativeUInt += src.ValueSize / 8;

						ptr.Value = src.Value;
						break;
					}
				case Opcode.PopR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						sp.NativeUInt -= dst.ValueSize / 8;
						Operand ptr = this.CreatePointer(sp.NativeUInt, dst.ValueSize);

						dst.Value = ptr.Value;
						break;
					}
				#endregion
				#region Flow
				case Opcode.CallR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						
						// push ip ; the retaddr
						{
							Operand sp_ptr = this.CreatePointer(sp.NativeUInt, Cpu.NativeSizeBits);
							sp_ptr.Value = ip.NativeUInt;
							sp.NativeUInt += Register.NativeSize;
						}

						// push bp
						{
							Operand sp_ptr = this.CreatePointer(sp.NativeUInt, Register.NativeSize * 8);
							sp_ptr.Value = bp.NativeUInt;
							sp.NativeUInt += Register.NativeSize;
						}

						// mov bp, sp
						{
							bp.NativeUInt = sp.NativeUInt;
						}

						// jmp loc
						{
							ip.NativeUInt = loc.Value;
						}
						
						break;
					}
				case Opcode.Return:
					{
						// the reverse of call

						// mov sp, bp
						{
							sp.NativeUInt = bp.NativeUInt;
						}

						// pop bp
						{
							sp.NativeUInt -= Register.NativeSize;
							Operand sp_ptr = this.CreatePointer(sp.NativeUInt, Register.NativeSize * 8);
							bp.NativeUInt = sp_ptr.Value;
						}

						// pop ip ; equiv to:  pop $1 jmp $1
						{
							sp.NativeUInt -= Register.NativeSize;
							Operand sp_ptr = this.CreatePointer(sp.NativeUInt, Register.NativeSize * 8);
							ip.NativeUInt = sp_ptr.Value;
						}
						
						break;
					}
				case Opcode.JumpR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.CompareRR:
					{
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						Int32 l = left.Signed;
						Int32 r = right.Signed;

						var iflags = (FlagsRegisterFlags)this.Flags.NativeUInt;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (l > r)
							iflags |= FlagsRegisterFlags.Greater;
						if (l < r)
							iflags |= FlagsRegisterFlags.Less;
						if (l == r)
							iflags |= FlagsRegisterFlags.Equal;

						flags.NativeUInt = (uint)iflags;
						break;
					}
				case Opcode.CompareFloatRR:
					{
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						float l = left.Float;
						float r = right.Float;

						var iflags = (FlagsRegisterFlags)this.Flags.NativeUInt;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (l > r)
							iflags |= FlagsRegisterFlags.Greater;
						if (l < r)
							iflags |= FlagsRegisterFlags.Less;
						if (l == r)
							iflags |= FlagsRegisterFlags.Equal;

						flags.NativeUInt = (uint)iflags;
						break;
					}
				case Opcode.JumpEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpNotEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						if (!((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpGreaterR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Greater))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpGreaterEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						var flgs = (FlagsRegisterFlags)this.Flags.NativeUInt;
						if (flgs.HasFlag(FlagsRegisterFlags.Greater) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpLessR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Less))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpLessEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						var flgs = (FlagsRegisterFlags)this.Flags.NativeUInt;
						if (flgs.HasFlag(FlagsRegisterFlags.Less) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				#endregion
				#region Transformative
				case Opcode.AddRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						// with two's compliment, adding does not need to be sign-aware
						dst.Value = left.Value + right.Value;
						break;
					}
				case Opcode.AddFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = left.Float + right.Float;
						break;
					}
				case Opcode.SubtractRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value - right.Value;
						break;
					}
				case Opcode.SubtractFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = left.Float - right.Float;
						break;
					}
				case Opcode.MultiplyRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Signed = left.Signed * right.Signed;
						break;
					}
				case Opcode.MultiplyUnsignedRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value * right.Value;
						break;
					}
				case Opcode.MultiplyFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = left.Float * right.Float;
						break;
					}
				case Opcode.DivideRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Signed = left.Signed / right.Signed;
						break;
					}
				case Opcode.DivideUnsignedRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value / right.Value;
						break;
					}
				case Opcode.DivideFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = left.Float / right.Float;
						break;
					}
				case Opcode.ModulusRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value % right.Value;
						break;
					}
				case Opcode.ModulusFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = left.Float % right.Float;
						break;
					}
				case Opcode.ShiftLeftRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value << (int)right.Value;
						break;
					}
				case Opcode.ShiftRightRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value >> (int)right.Value;
						break;
					}
				case Opcode.ArithmaticShiftRightRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						throw new NotImplementedException();
					}
				case Opcode.OrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value | right.Value;
						break;
					}
				case Opcode.ExclusiveOrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value ^ right.Value;
						break;
					}
				case Opcode.NotOrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value | (~right.Value);
						break;
					}
				case Opcode.AndRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value & right.Value;
						break;
					}
				case Opcode.NotAndRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = left.Value & (~right.Value);
						break;
					}
				case Opcode.NotRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Value = ~left.Value;
						break;
					}
				case Opcode.SqrtFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Sqrt(left.Float);
						break;
					}
				case Opcode.LogFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Log(left.Float, right.Float);
						break;
					}
				case Opcode.SinFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Sin(left.Float);
						break;
					}
				case Opcode.CosFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Cos(left.Float);
						break;
					}
				case Opcode.TanFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Tan(left.Float);
						break;
					}
				case Opcode.AsinFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Asin(left.Float);
						break;
					}
				case Opcode.AcosFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Acos(left.Float);
						break;
					}
				case Opcode.AtanFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Atan(left.Float);
						break;
					}
				case Opcode.Atan2FloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Atan2(left.Float, right.Float);
						break;
					}
				case Opcode.PowFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Pow(left.Float, right.Float);
						break;
					}
				#endregion
				default:
					throw new NotImplementedException(); // todo: make it interrupt
				}

				tsc.NativeUInt++;
			}

			return count;
		}

		public void Reset()
		{
			for (int i = 0; i < this.Registers.Length; i++)
				this.Registers[i].NativeUInt = 0;
		}

		public void Interrupt(int code)
		{
			throw new NotImplementedException();
		}
		
	}
}
