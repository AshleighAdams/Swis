using System;
using System.Runtime.InteropServices;

namespace Swis
{
	public interface IMemoryController
	{
		public abstract uint Length { get; }
		public abstract byte this[uint x] { get; set; }

		// bits MUST be either 8, 16, 32 (, or 64 if 64bit, which is it's not yet)
		// try to override this method, as doing so can get alignment speedup gains
		public abstract uint this[uint x, uint bits] { get; set; }

		public abstract Span<byte> Span(uint x, uint length);
	}

	// very slightly quicker, but memory is pinned and thus can't be moved.
	public unsafe class PointerMemoryController : IMemoryController
	{
		public byte* Ptr;
		public byte[] Memory;
		private int _Length;

		public uint Length { get => (uint)_Length; }

		private GCHandle MemoryHandle; // to pin it; call .Free() to unpin it
		public PointerMemoryController(byte[] memory) // TODO: use Span<>
		{
			Memory = new byte[memory.Length + 4];
			for (uint i = 0; i < memory.Length; i++)
				Memory[i] = memory[i];
			//this.Ptr = &this.Memory;
			_Length = memory.Length;
			MemoryHandle = GCHandle.Alloc(Memory, GCHandleType.Pinned);
			Ptr = (byte*)MemoryHandle.AddrOfPinnedObject().ToPointer();
			//Marshal.UnsafeAddrOfPinnedArrayElement(this.Memory, 0);
		}

		public byte this[uint x]
		{
			get
			{
				if (x >= _Length) throw new IndexOutOfRangeException();
				return *(Ptr + x);
			}
			set
			{
				if (x >= _Length) throw new IndexOutOfRangeException();
				*(Ptr + x) = value;
			}
		}

		public uint this[uint x, uint bits]
		{
			get
			{
				try
				{
					if (checked(x + bits / 8u) > (ulong)_Length)
						throw new IndexOutOfRangeException();
				}
				catch (OverflowException) { throw new IndexOutOfRangeException(); }

				return bits switch
				{
					8  => *(Byte*)(Ptr + x),
					16 => *(UInt16*)(Ptr + x),
					32 => *(UInt32*)(Ptr + x),
					_  => throw new Exception(),
				};
			}
			set
			{
				try
				{
					if (checked(x + bits / 8u) > (ulong)_Length)
						throw new IndexOutOfRangeException();
				}
				catch (OverflowException) { throw new IndexOutOfRangeException(); }

				switch (bits)
				{
					case 8: *(Byte*)(Ptr + x) = (byte)value; break;
					case 16: *(UInt16*)(Ptr + x) = (UInt16)value; break;
					case 32: *(UInt32*)(Ptr + x) = (UInt32)value; break;
					default: throw new Exception();
				}
			}
		}

		public Span<byte> Span(uint x, uint length)
		{
			return new Span<byte>(Memory, (int)x, (int)length);
		}
	}

	public class ByteArrayMemoryController : IMemoryController // 25% slower than the pointer version, but guaranteed safe
	{
		public byte[] Memory;
		public ByteArrayMemoryController(byte[] memory) // TODO: use Span<>
		{
			Memory = (byte[])memory.Clone();
		}

		public uint Length { get => (uint)Memory.Length; }

		public byte this[uint x]
		{
			get
			{
				return Memory[x];
			}
			set
			{
				Memory[x] = value;
			}
		}

		public uint this[uint x, uint bits]
		{
			get
			{
				Caster c; c.U32 = 0;
				switch (bits)
				{
					case 8:
						c.ByteA = Memory[x];
						break;
					case 16:
						c.ByteA = Memory[x + 0];
						c.ByteB = Memory[x + 1];
						break;
					case 32:
						c.ByteA = Memory[x + 0];
						c.ByteB = Memory[x + 1];
						c.ByteC = Memory[x + 2];
						c.ByteD = Memory[x + 3];
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
						Memory[x] = c.ByteA;
						break;
					case 16:
						Memory[x + 0] = c.ByteA;
						Memory[x + 1] = c.ByteB;
						break;
					case 32:
						Memory[x + 0] = c.ByteA;
						Memory[x + 1] = c.ByteB;
						Memory[x + 2] = c.ByteC;
						Memory[x + 3] = c.ByteD;
						break;
					default:
						throw new Exception();
				}
			}
		}

		public Span<byte> Span(uint x, uint length)
		{
			return new Span<byte>(Memory, (int)x, (int)length);
		}
	}
}