using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Swis
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Caster
	{
		[FieldOffset(0)] public Int32  I32;
		[FieldOffset(0)] public UInt32 U32;
		[FieldOffset(0)] public Single F32;

		[FieldOffset(0)] public Byte ByteA;
		[FieldOffset(1)] public Byte ByteB;
		[FieldOffset(2)] public Byte ByteC;
		[FieldOffset(3)] public Byte ByteD;
	}
	
	public class Emulator
    {
		Register[] Registers = new Register[64];

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

		ref Register Flags
		{
			get { return ref this.Registers[(int)NamedRegister.Flags]; }
		}

		public byte[] Memory { get; set; }

		public Emulator()
		{
			this.Memory = null;
		}

		protected byte DecodeOpcode(int position)
		{
			return this.Memory[position];
		}
		
		protected ref Register DecodeRegister(int position)
		{
			byte reg = this.Memory[position];
			return ref this.Registers[reg];
		}

		protected void EncodeInt(int position, int value)
		{
			Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
			c.I32 = value;
			this.Memory[position + 0] = c.ByteA;
			this.Memory[position + 1] = c.ByteB;
			this.Memory[position + 2] = c.ByteC;
			this.Memory[position + 3] = c.ByteD;
		}

		protected int DecodeInt(int position)
		{
			Caster c; c.I32 = 0;
			c.ByteA = this.Memory[position + 0];
			c.ByteB = this.Memory[position + 1];
			c.ByteC = this.Memory[position + 2];
			c.ByteD = this.Memory[position + 3];
			return c.I32;
		}

		protected float DecodeFloat(int position)
		{
			Caster c; c.F32 = 0;
			c.ByteA = this.Memory[position + 0];
			c.ByteB = this.Memory[position + 1];
			c.ByteC = this.Memory[position + 2];
			c.ByteD = this.Memory[position + 3];
			return c.F32;
		}

		public int Clock(int count = 1)
		{
			ref Register ip = ref this.InstructionPointer;
			ref Register sp = ref this.StackPointer;
			ref Register cp = ref this.CallstackPointer;
			ref Register flags = ref this.Flags;

			if ((flags.I32 & (int)NamedRegister.FlagsHalted) != 0)
				return 0;

			for (int i = 0; i < count; i++)
			{
				int pos = ip.I32;
				Opcode op = (Opcode)this.Memory[pos];

				switch (op)
				{
				#region MISC
				case Opcode.Nop: ip.I32 += 1;
					break;
				case Opcode.InterruptR:
				case Opcode.InterruptV:
				case Opcode.TrapR:
				case Opcode.TrapV:
				case Opcode.Halt: ip.I32 += 1;
					{
						flags.I32 |= (int)NamedRegister.FlagsHalted;
						break;
					}
				case Opcode.Reset:
					throw new NotImplementedException();
				#endregion
				
				#region MEMORY
				case Opcode.LoadRR:
					ip.I32 += 1 + 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);
						ref Register a = ref this.DecodeRegister(pos + 2);
						r.I32 = this.DecodeInt(a.I32);
						break;
					}
				case Opcode.LoadRV:
					ip.I32 += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);
						int a = this.DecodeInt(pos + 2);
						r.I32 = this.DecodeInt(a);
						break;
					}
				case Opcode.StoreRR:
					ip.I32 += 1 + 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1);
						ref Register r = ref this.DecodeRegister(pos + 2);
						this.EncodeInt(a.I32, r.I32);
						break;
					}
				case Opcode.StoreVR:
					ip.I32 += 1 + 4 + 1;
					{
						int a = this.DecodeInt(pos + 1);
						ref Register r = ref this.DecodeRegister(pos + 1 + 4);
						this.EncodeInt(a, r.I32);
						break;
					}
				case Opcode.MoveRR:
					ip.I32 += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1);
						ref Register src = ref this.DecodeRegister(pos + 2);
						dst.I32 = src.I32;
						break;
					}
				case Opcode.MoveRV:
					ip.I32 += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);
						int a = this.DecodeInt(pos + 2);
						r.I32 = a;
						break;
					}
				case Opcode.PushR:
					ip.I32 += 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);

						this.EncodeInt(sp.I32, r.I32);
						sp.I32 += Register.DataSize;
						break;
					}
				case Opcode.PopR:
					ip.I32 += 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);

						sp.I32 -= Register.DataSize;
						r.I32 = this.DecodeInt(sp.I32);
						break;
					}
					#endregion

				#region FLOW
				case Opcode.CallR: ip.I32 += 1 + 1;
					int call_addr = 0;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);
						call_addr = r.I32;
						goto do_call;
					}
				case Opcode.CallV: ip.I32 += 1 + 4;
					{
						call_addr = this.DecodeInt(pos + 1);
						goto do_call;
					}
				do_call:
					{
						this.EncodeInt(cp.I32, ip.I32); // push our return address
						cp.I32 += Register.DataSize; // increase the call stack pointer

						ip.I32 = call_addr; // start exectuing there
						break;
					}
				
				case Opcode.Return: ip.I32 += 1;
					{
						cp.I32 -= Register.DataSize; // pop the stack
						int ret_addr = this.DecodeInt(cp.I32); // read the value

						ip.I32 = ret_addr; // return execution back
						break;
					}

				case Opcode.JumpV: ip.I32 += 1 + 4;
					{
						int to = this.DecodeInt(pos + 1);
						ip.I32 = to;
						break;
					}
						
				case Opcode.JumpR: ip.I32 += 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1);
						ip.I32 = r.I32;
						break;
					}

				case Opcode.CompareRR: ip.I32 += 1 + 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1);
						ref Register b = ref this.DecodeRegister(pos + 2);

						int iflags = flags.I32;
						iflags &= ~(int)(NamedRegister.FlagsEqual | NamedRegister.FlagsLess | NamedRegister.FlagsGreater);

						float ia = a.I32;
						float ib = b.I32;

						bool gt = ia > ib;
						bool lt = ia < ib;
						bool eq = ia == ib;

						iflags = !eq ? iflags : iflags | (int)NamedRegister.FlagsEqual;
						iflags = !lt ? iflags : iflags | (int)NamedRegister.FlagsLess;
						iflags = !gt ? iflags : iflags | (int)NamedRegister.FlagsGreater;

						flags.I32 = iflags;
						break;
					}
				case Opcode.CompareFloatRR: ip.I32 += 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1);
						ref Register b = ref this.DecodeRegister(pos + 1);

						int iflags = flags.I32;
						iflags &= ~(int)(NamedRegister.FlagsEqual | NamedRegister.FlagsLess | NamedRegister.FlagsGreater);

						float fa = a.F32;
						float fb = b.F32;

						bool gt = fa > fb;
						bool lt = fa < fb;
						bool eq = fa == fb;

						iflags = !eq ? iflags : iflags | (int)NamedRegister.FlagsEqual;
						iflags = !lt ? iflags : iflags | (int)NamedRegister.FlagsLess;
						iflags = !gt ? iflags : iflags | (int)NamedRegister.FlagsGreater;

						flags.I32 = iflags;
						break;
					}
				case Opcode.JumpEqualR: ip.I32 += 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1);
						int jump_to = this.DecodeInt(a.I32);
						if ((flags.I32 & (int)NamedRegister.FlagsEqual) != 0)
							ip.I32 = jump_to;
						break;
					}
				case Opcode.JumpEqualV: ip.I32 += 1 + 4;
					{
						int jump_to = this.DecodeInt(pos + 1);
						if ((flags.I32 & (int)NamedRegister.FlagsEqual) != 0)
							ip.I32 = jump_to;
						break;
					}
				#endregion

				#region Transformative
				case Opcode.AddRRR:
					ip.I32 += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1);
						ref Register left = ref this.DecodeRegister(pos + 2);
						ref Register right = ref this.DecodeRegister(pos + 3);
						dst.I32 = left.I32 + right.I32;
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
				this.Registers[i].U32 = 0;
		}

		public void Interrupt(int code)
		{
		}
    }
}
