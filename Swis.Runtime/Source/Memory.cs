using System;
using System.Runtime.InteropServices;

namespace Swis
{
	public abstract class MemoryController
	{
		public abstract byte this[uint x, bool y] { get; set; }

		// bits MUST be either 8, 16, 32 (, or 64 if 64bit, which is it's not yet)
		// try to override this method, as doing so can get alignment speedup gains
		public virtual uint this[uint x, uint bits]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		//public abstract byte this[int x] { get; set; }


	}

	public unsafe class PointerMemoryController : MemoryController
	{
		public byte* Ptr;
		public byte[] Memory;
		int Length;
		GCHandle MemoryHandle; // to pin it; call .Free() to unpin it
		public PointerMemoryController(byte[] memory) // TODO: use Span<>
		{
			this.Memory = new byte[memory.Length + 4];
			for (uint i = 0; i < memory.Length; i++)
				this.Memory[i] = memory[i];
			//this.Ptr = &this.Memory;
			this.Length = memory.Length;
			this.MemoryHandle = GCHandle.Alloc(this.Memory, GCHandleType.Pinned);
			this.Ptr = (byte*)this.MemoryHandle.AddrOfPinnedObject().ToPointer();
			//Marshal.UnsafeAddrOfPinnedArrayElement(this.Memory, 0);
		}

		public override byte this[uint x, bool y]
		{
			get
			{
				if (x > this.Length) throw new IndexOutOfRangeException();
				return *(this.Ptr + x);
			}
			set
			{
				if (x > this.Length) throw new IndexOutOfRangeException();
				*(this.Ptr + x) = value;
			}
		}

		public override uint this[uint x, uint bits] // unaligned access
		{
			get
			{
				if (x > this.Length) throw new IndexOutOfRangeException();
				switch (bits)
				{
				case 8: return *(Byte*)(this.Ptr + x);
				case 16: return *(UInt16*)(this.Ptr + x);
				case 32: return *(UInt32*)(this.Ptr + x);
				default: throw new Exception();
				}
			}
			set
			{
				if (x > Length) throw new IndexOutOfRangeException();
				switch (bits)
				{
				case 8: *(Byte*)(this.Ptr + x) = (byte)value; break;
				case 16: *(UInt16*)(this.Ptr + x) = (UInt16)value; break;
				case 32: *(UInt32*)(this.Ptr + x) = (UInt32)value; break;
				default: throw new Exception();
				}
			}
		}
	}

	public class IntArrayMemoryController : MemoryController // 25% slower than the pointer version, but garunteed safe
	{
		public uint[] Memory;
		public IntArrayMemoryController(byte[] memory) // TODO: use Span<>
		{
			this.Memory = new uint[memory.Length / 4 + 1]; // +1 for if it rounds down
			for (uint i = 0; i < memory.Length; i++)
				this[i, true] = memory[i];
			//this.Memory = memory;
		}

		public override byte this[uint x, bool y]
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
					case 0: return helper.U8_0;
					case 8: return helper.U8_8;
					case 16: return helper.U8_16;
					case 24: return helper.U8_24;
					default: throw new Exception();
					}
				case 16:
					switch (missalign)
					{
					case 0: return helper.U16_0;
					case 8: return helper.U16_8;
					case 16: return helper.U16_16;
					case 24: return helper.U16_24;
					default: throw new Exception();
					}
				case 32:
					switch (missalign)
					{
					// case 0:  return helper.U32_0; // special case above
					case 8: return helper.U32_8;
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
					case 0: helper.U8_0 = (Byte)value; break;
					case 8: helper.U8_8 = (Byte)value; break;
					case 16: helper.U8_16 = (Byte)value; break;
					case 24: helper.U8_24 = (Byte)value; break;
					default: throw new Exception();
					}
					break;
				case 16:
					switch (missalign)
					{
					case 0: helper.U16_0 = (UInt16)value; break;
					case 8: helper.U16_8 = (UInt16)value; break;
					case 16: helper.U16_16 = (UInt16)value; break;
					case 24: helper.U16_24 = (UInt16)value; break;
					default: throw new Exception();
					}
					break;
				case 32:
					switch (missalign)
					{
					// case  0: helper.U32_0 =  value; break; // special case, fully in alignment
					case 8: helper.U32_8 = value; break;
					case 16: helper.U32_16 = value; break;
					case 24: helper.U32_24 = value; break;
					default: throw new Exception();
					}
					break;
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
}