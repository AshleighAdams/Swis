using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Operand = Swis.Cpu.Operand;

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
			byte a = memory[ip + 0];
			byte b = memory[ip + 1];
			ip += 2;

			uint regid = (uint)((a & 0b11111000) >> 3);
			uint size = (uint)((a & 0b00000011) >> 0);

			size = Register.Pow(2, size); // to bytes

			uint constant = 0;
			if (regid == 0)
			{
				Caster c; c.U32 = 0;

				switch (size)
				{
				default: throw new Exception("invalid register const size");
				case 32 / 8:
					c.ByteD = memory[ip + 3];
					c.ByteC = memory[ip + 2];
					goto case 16 / 8;
				case 16 / 8:
					c.ByteB = memory[ip + 1];
					goto case 8 / 8;
				case 8 / 8:
					c.ByteA = memory[ip + 0];
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

				c.ByteA = memory[ip + 0];
				c.ByteB = memory[ip + 1];
				c.ByteC = memory[ip + 2];
				c.ByteD = memory[ip + 3];

				ip += 4;
				offset = c.I32;
			}

			return new Operand
			{
				Memory = memory,
				Registers = registers,

				RegisterID = regid,
				Size = size << 3,
				Constant = constant,
				Indirect = indirect,
				IndirectionSize = indirection_size << 3,
				Offset = offset,
			};
		}
	}

	public class Cpu
    {
		

		public struct Operand
		{
			//public Emulator Owner;
			public MemoryController Memory;
			public Register[] Registers;

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
							immidiate = this.Registers[this.RegisterID].NativeUInt;
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
							c.ByteD = this.Memory[immidiate + 3];
							c.ByteC = this.Memory[immidiate + 2];
							goto case 16;
						case 16:
							c.ByteB = this.Memory[immidiate + 1];
							goto case 8;
						case 8:
							c.ByteA = this.Memory[immidiate + 0];
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

						this.Registers[this.RegisterID].NativeUInt = value;
					}
					else
					{
						uint memloc = this.RegisterID == 0 ?
							this.Constant :
							this.Registers[this.RegisterID].NativeUInt;

						if (this.Offset != 0)
							memloc = (uint)(memloc + this.Offset);

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
							Operand sp_ptr = this.CreatePointer(sp.NativeUInt, Register.NativeSize * 8);
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
				case Opcode.SqrtRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Sqrt(left.Float);
						break;
					}
				case Opcode.LogRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Log(left.Float, right.Float);
						break;
					}
				case Opcode.SinRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Sin(left.Float);
						break;
					}
				case Opcode.CosRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Cos(left.Float);
						break;
					}
				case Opcode.TanRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Tan(left.Float);
						break;
					}
				case Opcode.AsinRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Asin(left.Float);
						break;
					}
				case Opcode.AcosRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Acos(left.Float);
						break;
					}
				case Opcode.AtanRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Atan(left.Float);
						break;
					}
				case Opcode.Atan2RRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip.NativeUInt, this.Registers);

						dst.Float = (float)Math.Atan2(left.Float, right.Float);
						break;
					}
				case Opcode.PowRRR:
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
