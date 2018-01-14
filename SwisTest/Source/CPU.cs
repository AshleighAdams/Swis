using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
		//public abstract byte this[int x] { get; set; }

		
	}

	public class DirectMemoryController : MemoryController
	{
		byte[] Memory;
		public DirectMemoryController(byte[] memory)
		{
			this.Memory = memory;
		}
		public override byte this[uint x]
		{
			get { return this.Memory[x]; }
			set { this.Memory[x] = value; }
		}
		//public override byte this[int x]
		//{
		//	get { return this.Memory[x]; }
		//	set { this.Memory[x] = value; }
		//}
	}

	public class Emulator
    {
		protected struct Operand
		{
			public Emulator Owner;

			public uint RegisterID; // if this is 0, it is a const of Size bits after the flags
			public UInt32 Constant; // if not using a register, use this
			public uint Size; // the size of the register
			public bool Indirect; // mov 1, blah: mem[1] = blah; mov [1], blah: mem[mem[1]] = blah
			public uint IndirectionSize;
			public int Offset; // mov ga, bp + 4; ga = bp.int + 4; // mov ga, [bp + 4]; ga = mem[bp.int + 4]

			public uint ValueSize // the effective size
			{
				get
				{
					return this.Indirect ? this.IndirectionSize : this.Size;
				}
			}

			public UInt32 Value
			{
				get
				{
					// get the immidiate value
					UInt32 immidiate;
					{
						if (this.RegisterID != 0)
							immidiate = this.Owner.Registers[this.RegisterID].NativeUInt;
						else
							immidiate = this.Constant;
					}

					// offset
					{
						if (this.Offset != 0)
							immidiate = (UInt32)(immidiate + this.Offset);
					}

					// indirection
					{
						if (!this.Indirect)
							return immidiate;

						Caster c; c.U32 = 0;
						switch (this.IndirectionSize)
						{
						default: throw new Exception("TODO: register with invalid indirection size");
						case 32:
							c.ByteD = this.Owner.Memory[immidiate + 3];
							c.ByteC = this.Owner.Memory[immidiate + 2];
							goto case 16;
						case 16:
							c.ByteB = this.Owner.Memory[immidiate + 1];
							goto case 8;
						case 8:
							c.ByteA = this.Owner.Memory[immidiate + 0];
							break;
						}

						return c.U32;
					}
				}
				set
				{
					// cap it to the register memory size:
					value = (uint)((ulong)value & ((1ul << (int)this.Size) - 1));

					if (!this.Indirect)
					{
						// change the register
						if (this.RegisterID == 0 || this.Offset != 0)
						{
							// nonsensical, halt
							throw new Exception("TODO: can't write to an immidiate value, doesn't make sense");
						}

						this.Owner.Registers[this.RegisterID].NativeUInt = value;
					}
					else
					{
						uint memloc = this.RegisterID == 0 ?
							this.Constant :
							this.Owner.Registers[this.RegisterID].NativeUInt;

						if (this.Offset != 0)
							memloc = (uint)(memloc + this.Offset);

						Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
						c.U32 = value;

						switch (this.IndirectionSize) // maybe don't fall thru, so we go forward thru data?
						{
						default: throw new Exception("TODO: invalid register size");
						case 32:
							this.Owner.Memory[memloc + 3] = c.ByteD;
							this.Owner.Memory[memloc + 2] = c.ByteC;
							goto case 16;
						case 16:
							this.Owner.Memory[memloc + 1] = c.ByteB;
							goto case 8;
						case 8:
							this.Owner.Memory[memloc + 0] = c.ByteA;
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

		ref Register InstructionPointer
		{
			get { return ref this.Registers[(int)NamedRegister.InstructionPointer]; }
		}

		ref Register StackPointer
		{
			get { return ref this.Registers[(int)NamedRegister.StackPointer]; }
		}

		ref Register CallstackPointer
		{
			get { return ref this.Registers[(int)NamedRegister.CallstackPointer]; }
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

		public Emulator()
		{
			this.Memory = null;
		}

		protected Opcode DecodeOpcode(ref uint ip)
		{
			Opcode ret = (Opcode)this.Memory[ip];
			ip += 1;
			return ret;
		}

		protected Operand DecodeOperand(ref uint ip)
		{
			byte a = this.Memory[ip + 0];
			byte b = this.Memory[ip + 1];
			ip += 2;

			uint regid = (uint)((a & 0b11111000) >> 3);
			uint size  = (uint)((a & 0b00000011) >> 0);

			size = Register.Pow(2, size); // to bytes

			uint constant = 0;
			if (regid == 0)
			{
				Caster c; c.U32 = 0;

				switch (size)
				{
				default: throw new Exception("invalid register const size");
				case 32 / 8:
					c.ByteD = this.Memory[ip + 3];
					c.ByteC = this.Memory[ip + 2];
					goto case 16 / 8;
				case 16 / 8:
					c.ByteB = this.Memory[ip + 1];
					goto case 8 / 8;
				case 8 / 8:
					c.ByteA = this.Memory[ip + 0];
					break;
				}

				ip += size;
				constant = c.U32;
			}

			uint indirection_size = (uint)((b & 0b1110_0000) >> 5);
			bool indirect = indirection_size != 0;
			if (indirect)
				indirection_size = Register.Pow(2, indirection_size - 1);

			int offset = 0;
			if ((b & 0b0001_0000) != 0)
			{
				Caster c; c.I32 = 0;

				c.ByteA = this.Memory[ip + 0];
				c.ByteB = this.Memory[ip + 1];
				c.ByteC = this.Memory[ip + 2];
				c.ByteD = this.Memory[ip + 3];

				ip += 4;
				offset = c.I32;
			}

			return new Operand
			{
				Owner = this,
				RegisterID = regid,
				Size = size << 3,
				Constant = constant,
				Indirect = indirect,
				IndirectionSize = indirection_size << 3,
				Offset = offset,
			};
		}

		protected Operand CreatePointer(uint address, uint size)
		{
			return new Operand
			{
				Owner = this,
				Size = Register.NativeSize * 8, // size of the const (address), not the size of the value at the address
				Constant = address, // memory location when in indirect mode
				Indirect = true,
				IndirectionSize = size,
			};
		}
		
		public int Clock(int count = 1)
		{
			ref Register ip = ref this.InstructionPointer;
			ref Register sp = ref this.StackPointer;
			ref Register cp = ref this.CallstackPointer;
			ref Register bp = ref this.BasePointer;
			ref Register flags = ref this.Flags;

			if (this.Halted)
				return 0;

			for (int i = 0; i < count; i++)
			{
				Opcode op = this.DecodeOpcode(ref ip.NativeUInt);
				
				switch (op)
				{
				#region Misc
				case Opcode.Nop:
					break;
				case Opcode.Halt:
					flags.NativeUInt |= (uint)FlagsRegisterFlags.Halted;
					break;
				case Opcode.InRR:
					{
						Operand lttr = this.DecodeOperand(ref ip.NativeUInt);
						Operand line = this.DecodeOperand(ref ip.NativeUInt);

						lttr.Value = (uint)Console.Read();
						break;
					}
				case Opcode.OutRR:
					{
						Operand line = this.DecodeOperand(ref ip.NativeUInt);
						Operand lttr = this.DecodeOperand(ref ip.NativeUInt);

						Console.Write((char)lttr.Value);
						break;
					}
				#endregion
				#region Memory
				case Opcode.MoveRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand src = this.DecodeOperand(ref ip.NativeUInt);
						dst.Value = src.Value;
						break;
					}
				case Opcode.PushR:
					{
						Operand src = this.DecodeOperand(ref ip.NativeUInt);

						Operand ptr = this.CreatePointer(sp.NativeUInt, src.ValueSize);
						sp.NativeUInt += src.ValueSize / 8;

						ptr.Value = src.Value;
						break;
					}
				case Opcode.PopR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);

						sp.NativeUInt -= dst.ValueSize / 8;
						Operand ptr = this.CreatePointer(sp.NativeUInt, dst.ValueSize);

						dst.Value = ptr.Value;
						break;
					}
				#endregion
				#region Flow
				case Opcode.CallR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);

						Operand ret_ptr = this.CreatePointer(cp.NativeUInt, Register.NativeSize * 8);
						cp.NativeUInt += Register.NativeSize;

						Operand base_ptr = this.CreatePointer(cp.NativeUInt, Register.NativeSize * 8);
						cp.NativeUInt += Register.NativeSize;

						ret_ptr.Value = ip.NativeUInt;
						base_ptr.Value = bp.NativeUInt;

						ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.Return:
					{
						cp.NativeUInt -= Register.NativeSize;
						var base_ptr = this.CreatePointer(cp.NativeUInt, Register.NativeSize * 8);

						cp.NativeUInt -= Register.NativeSize;
						var ret_ptr = this.CreatePointer(cp.NativeUInt, Register.NativeSize * 8);

						ip.NativeUInt = ret_ptr.Value;
						bp.NativeUInt = base_ptr.Value;
						break;
					}
				case Opcode.JumpR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.CompareRR:
					{
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

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
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

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
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpNotEqualR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						if (!((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpGreaterR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Greater))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpGreaterEqualR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						var flgs = (FlagsRegisterFlags)this.Flags.NativeUInt;
						if (flgs.HasFlag(FlagsRegisterFlags.Greater) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpLessR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						if (((FlagsRegisterFlags)this.Flags.NativeUInt).HasFlag(FlagsRegisterFlags.Less))
							ip.NativeUInt = loc.Value;
						break;
					}
				case Opcode.JumpLessEqualR:
					{
						Operand loc = this.DecodeOperand(ref ip.NativeUInt);
						var flgs = (FlagsRegisterFlags)this.Flags.NativeUInt;
						if (flgs.HasFlag(FlagsRegisterFlags.Less) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip.NativeUInt = loc.Value;
						break;
					}
				#endregion
				#region Transformative
				case Opcode.AddRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						// with two's compliment, adding does not need to be sign-aware
						dst.Value = left.Value + right.Value;
						break;
					}
				case Opcode.AddFloatRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = left.Float + right.Float;
						break;
					}
				case Opcode.SubtractRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value - right.Value;
						break;
					}
				case Opcode.SubtractFloatRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = left.Float - right.Float;
						break;
					}
				case Opcode.MultiplyRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Signed = left.Signed * right.Signed;
						break;
					}
				case Opcode.MultiplyUnsignedRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value * right.Value;
						break;
					}
				case Opcode.MultiplyFloatRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = left.Float * right.Float;
						break;
					}
				case Opcode.DivideRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Signed = left.Signed / right.Signed;
						break;
					}
				case Opcode.DivideUnsignedRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value / right.Value;
						break;
					}
				case Opcode.DivideFloatRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = left.Float / right.Float;
						break;
					}
				case Opcode.ModulusRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value % right.Value;
						break;
					}
				case Opcode.ModulusFloatRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = left.Float % right.Float;
						break;
					}
				case Opcode.ShiftLeftRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value << (int)right.Value;
						break;
					}
				case Opcode.ShiftRightRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value >> (int)right.Value;
						break;
					}
				case Opcode.ArithmaticShiftRightRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						throw new NotImplementedException();
					}
				case Opcode.OrRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value | right.Value;
						break;
					}
				case Opcode.ExclusiveOrRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value ^ right.Value;
						break;
					}
				case Opcode.NotOrRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value | (~right.Value);
						break;
					}
				case Opcode.AndRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value & right.Value;
						break;
					}
				case Opcode.NotAndRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = left.Value & (~right.Value);
						break;
					}
				case Opcode.NotRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Value = ~left.Value;
						break;
					}
				case Opcode.SqrtRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Sqrt(left.Float);
						break;
					}
				case Opcode.LogRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Log(left.Float, right.Float);
						break;
					}
				case Opcode.SinRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Sin(left.Float);
						break;
					}
				case Opcode.CosRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Cos(left.Float);
						break;
					}
				case Opcode.TanRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Tan(left.Float);
						break;
					}
				case Opcode.AsinRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Asin(left.Float);
						break;
					}
				case Opcode.AcosRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Acos(left.Float);
						break;
					}
				case Opcode.AtanRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Atan(left.Float);
						break;
					}
				case Opcode.Atan2RRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Atan2(left.Float, right.Float);
						break;
					}
				case Opcode.PowRRR:
					{
						Operand dst = this.DecodeOperand(ref ip.NativeUInt);
						Operand left = this.DecodeOperand(ref ip.NativeUInt);
						Operand right = this.DecodeOperand(ref ip.NativeUInt);

						dst.Float = (float)Math.Pow(left.Float, right.Float);
						break;
					}
				#endregion
				default:
					throw new NotImplementedException(); // todo: make it interrupt
				}
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
		}
    }
}
