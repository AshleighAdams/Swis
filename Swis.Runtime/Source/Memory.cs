using System;
using System.Runtime.InteropServices;

namespace Swis
{
	public abstract class MemoryController
	{
		public abstract uint Length { get; }
		public abstract byte this[uint x] { get; set; }

		// bits MUST be either 8, 16, 32 (, or 64 if 64bit, which is it's not yet)
		// try to override this method, as doing so can get alignment speedup gains
		public abstract uint this[uint x, uint bits] { get; set; }

		public abstract Span<byte> Span(uint x, uint length);
	}

	// very slightly quicker, but memory is pinned and thus can't be moved.
	public unsafe class PointerMemoryController : MemoryController
	{
		public byte* Ptr;
		public byte[] Memory;
		int _Length;
		
		public override uint Length { get { return (uint)this._Length; } }

		GCHandle MemoryHandle; // to pin it; call .Free() to unpin it
		public PointerMemoryController(byte[] memory) // TODO: use Span<>
		{
			this.Memory = new byte[memory.Length + 4];
			for (uint i = 0; i < memory.Length; i++)
				this.Memory[i] = memory[i];
			//this.Ptr = &this.Memory;
			this._Length = memory.Length;
			this.MemoryHandle = GCHandle.Alloc(this.Memory, GCHandleType.Pinned);
			this.Ptr = (byte*)this.MemoryHandle.AddrOfPinnedObject().ToPointer();
			//Marshal.UnsafeAddrOfPinnedArrayElement(this.Memory, 0);
		}

		public override byte this[uint x]
		{
			get
			{
				if (x > this._Length) throw new IndexOutOfRangeException();
				return *(this.Ptr + x);
			}
			set
			{
				if (x > this._Length) throw new IndexOutOfRangeException();
				*(this.Ptr + x) = value;
			}
		}

		public override uint this[uint x, uint bits]
		{
			get
			{
				if (x > this._Length) throw new IndexOutOfRangeException();
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
				if (x > this._Length) throw new IndexOutOfRangeException();
				switch (bits)
				{
				case 8: *(Byte*)(this.Ptr + x) = (byte)value; break;
				case 16: *(UInt16*)(this.Ptr + x) = (UInt16)value; break;
				case 32: *(UInt32*)(this.Ptr + x) = (UInt32)value; break;
				default: throw new Exception();
				}
			}
		}

		public override Span<byte> Span(uint x, uint length)
		{
			return new Span<byte>(this.Memory, (int)x, (int)length);
		}
	}

	public class ByteArrayMemoryController : MemoryController // 25% slower than the pointer version, but garunteed safe
	{
		public byte[] Memory;
		public ByteArrayMemoryController(byte[] memory) // TODO: use Span<>
		{
			this.Memory = (byte[])memory.Clone();
		}

		public override uint Length { get { return (uint)this.Memory.Length; } }

		public override byte this[uint x]
		{
			get
			{
				return this.Memory[x];
			}
			set
			{
				this.Memory[x] = value;
			}
		}
		
		public override uint this[uint x, uint bits]
		{
			get
			{
				Caster c; c.U32 = 0;
				switch (bits)
				{
				case 8:
					c.ByteA = this.Memory[x];
					break;
				case 16:
					c.ByteA = this.Memory[x + 0];
					c.ByteB = this.Memory[x + 1];
					break;
				case 32:
					c.ByteA = this.Memory[x + 0];
					c.ByteB = this.Memory[x + 1];
					c.ByteC = this.Memory[x + 2];
					c.ByteD = this.Memory[x + 3];
					break;
				default:
					throw new Exception();
				}
				return c.U32;
			}
			set
			{
				Caster c = new Caster() { U32 = value };
				c.U32 = value;
				switch (bits)
				{
				case 8:
					this.Memory[x] = c.ByteA;
					break;
				case 16:
					this.Memory[x + 0] = c.ByteA;
					this.Memory[x + 1] = c.ByteB;
					break;
				case 32:
					this.Memory[x + 0] = c.ByteA;
					this.Memory[x + 1] = c.ByteB;
					this.Memory[x + 2] = c.ByteC;
					this.Memory[x + 3] = c.ByteD;
					break;
				default:
					throw new Exception();
				}
			}
		}

		public override Span<byte> Span(uint x, uint length)
		{
			return new Span<byte>(this.Memory, (int)x, (int)length);
		}
	}
}