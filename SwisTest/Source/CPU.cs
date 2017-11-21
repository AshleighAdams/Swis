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
			{ return (this.Flags.NativeInt & (int)NamedRegister.FlagsHalted) != 0; }
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
		
		protected ref Register DecodeRegister(int position, out int size)
		{
			int reg = this.Memory[position];

			size = Register.Pow(2, reg & 0b11);
			reg = reg >> 2;

			return ref this.Registers[reg];
		}

		protected void EncodeInt(int position, int size, int value)
		{
			Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
			c.I32 = value;

			switch (size)
			{
			case 4:
				this.Memory[position + 3] = c.ByteD;
				goto case 3;
			case 3:
				this.Memory[position + 2] = c.ByteC;
				goto case 2;
			case 2:
				this.Memory[position + 1] = c.ByteB;
				goto case 1;
			case 1:
				this.Memory[position + 0] = c.ByteA;
				break;
			default:
				throw new Exception("invalid read size");
			}
		}

		protected int DecodeInt(int position, int size)
		{
			Caster c; c.I32 = 0;
			switch (size)
			{
			case 4:
				c.ByteD = this.Memory[position + 3];
				goto case 3;
			case 3:
				c.ByteC = this.Memory[position + 2];
				goto case 2;
			case 2:
				c.ByteB = this.Memory[position + 1];
				goto case 1;
			case 1:
				c.ByteA = this.Memory[position + 0];
				break;
			default:
				throw new Exception("invalid read size");
			}
			return c.I32;
		}

		protected float DecodeFloat(int position, int size)
		{
			Caster c; c.F32 = 0;
			switch (size)
			{
			case 4:
				c.ByteD = this.Memory[position + 3];
				goto case 3;
			case 3:
				c.ByteC = this.Memory[position + 2];
				goto case 2;
			case 2:
				c.ByteB = this.Memory[position + 1];
				goto case 1;
			case 1:
				c.ByteA = this.Memory[position + 0];
				break;
			default:
				throw new Exception("invalid read size");
			}
			return c.F32;
		}

		public int Clock(int count = 1)
		{
			ref Register ip = ref this.InstructionPointer;
			ref Register sp = ref this.StackPointer;
			ref Register cp = ref this.CallstackPointer;
			ref Register bp = ref this.BasePointer;
			ref Register flags = ref this.Flags;

			if ((flags.NativeInt & (int)NamedRegister.FlagsHalted) != 0)
				return 0;

			for (int i = 0; i < count; i++)
			{
				int pos = ip.NativeInt;
				Opcode op = (Opcode)this.Memory[pos];

				switch (op)
				{
				#region MISC
				case Opcode.Nop: ip.NativeInt += 1;
					break;
				case Opcode.InterruptR:
				case Opcode.InterruptV:
				case Opcode.TrapR:
				case Opcode.TrapV:
				case Opcode.Halt: ip.NativeInt += 1;
					{
						flags.NativeInt |= (int)NamedRegister.FlagsHalted;
						break;
					}
				case Opcode.Reset:
					throw new NotImplementedException();
				case Opcode.InRR: ip.NativeInt += 1 + 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						ref Register l = ref this.DecodeRegister(pos + 2, out int lsz);

						int line = l.GetInteger(lsz);
						r.SetInteger(rsz, line); // todo: write actual value

						break;
					}
				case Opcode.InRV: ip.NativeInt += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						int l = this.DecodeInt(pos + 2, Register.NativeSize);

						r.SetInteger(rsz, l);
						break;
					}
				case Opcode.OutRR: ip.NativeInt += 1 + 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						ref Register l = ref this.DecodeRegister(pos + 2, out int lsz);
						
						int ascii = r.GetInteger(1); // 1 byte, not reg size
						int line = l.GetInteger(lsz);

						Console.Write((char)ascii);
						break;
					}
				case Opcode.OutRV: ip.NativeInt += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						int l = this.DecodeInt(pos + 2, Register.NativeSize);
						
						int ascii = r.GetInteger(1); // 1 byte, not reg size
						int line = l;

						Console.Write((char)ascii);

						break;
					}
				#endregion

				#region MEMORY
				case Opcode.LoadRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						ref Register a = ref this.DecodeRegister(pos + 2, out int asz);
						r.SetInteger(rsz, this.DecodeInt(a.GetInteger(asz), rsz));
						break;
					}
				case Opcode.LoadRV:
					ip.NativeInt += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						int a = this.DecodeInt(pos + 2, Register.NativeSize);
						r.SetInteger(rsz, this.DecodeInt(a, rsz));
						break;
					}
				case Opcode.StoreRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						ref Register a = ref this.DecodeRegister(pos + 2, out int asz);
						this.EncodeInt(a.GetInteger(asz), rsz, r.GetInteger(rsz));
						break;
					}
				case Opcode.StoreVR:
					ip.NativeInt += 1 + 4 + 1;
					{
						int a = this.DecodeInt(pos + 1, Register.NativeSize);
						ref Register r = ref this.DecodeRegister(pos + 1 + 4, out int rsz);
						this.EncodeInt(a, rsz, r.GetInteger(rsz));
						break;
					}
				case Opcode.MoveRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register src = ref this.DecodeRegister(pos + 2, out int srcsz);
						dst.SetInteger(dstsz, src.GetInteger(srcsz));
						break;
					}
				case Opcode.MoveRV:
					ip.NativeInt += 1 + 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						int v = this.DecodeInt(pos + 2, Register.NativeSize);
						r.SetInteger(rsz, v);
						break;
					}
				case Opcode.PushR:
					ip.NativeInt += 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);

						this.EncodeInt(sp.NativeInt, rsz, r.GetInteger(rsz));
						sp.NativeInt += rsz;
						break;
					}
				case Opcode.PopR:
					ip.NativeInt += 1 + 1;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);

						sp.NativeInt -= rsz;
						r.SetInteger(rsz, this.DecodeInt(sp.NativeInt, rsz));
						break;
					}
					#endregion

				#region FLOW
				case Opcode.CallR: ip.NativeInt += 1 + 1;
					int call_addr = 0;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						call_addr = r.GetInteger(rsz);
						goto do_call;
					}
				case Opcode.CallV: ip.NativeInt += 1 + 4;
					{
						call_addr = this.DecodeInt(pos + 1, Register.NativeSize);
						goto do_call;
					}
				do_call:
					{
						this.EncodeInt(cp.NativeInt, Register.NativeSize, ip.NativeInt); // push our return address
						cp.NativeInt += Register.NativeSize; // increase the call stack pointer

						this.EncodeInt(cp.NativeInt, Register.NativeSize, bp.NativeInt);
						cp.NativeInt += Register.NativeSize;

						ip.NativeInt = call_addr; // start exectuing there
						break;
					}
				
				case Opcode.Return: ip.NativeInt += 1;
					{
						cp.NativeInt -= Register.NativeSize; // do the same as above in reverse
						bp.NativeInt = this.DecodeInt(cp.NativeInt, Register.NativeSize); // reset the base ptr

						cp.NativeInt -= Register.NativeSize; // pop the stack
						int ret_addr = this.DecodeInt(cp.NativeInt, Register.NativeSize); // read the value
						ip.NativeInt = ret_addr; // return execution back
						break;
					}

				case Opcode.JumpV: ip.NativeInt += 1 + 4;
					{
						int to = this.DecodeInt(pos + 1, Register.NativeSize);
						ip.NativeInt = to;
						break;
					}
						
				case Opcode.JumpR: ip.NativeInt += 1 + 4;
					{
						ref Register r = ref this.DecodeRegister(pos + 1, out int rsz);
						ip.NativeInt = r.GetInteger(rsz);
						break;
					}

				case Opcode.CompareRR: ip.NativeInt += 1 + 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1, out int asz);
						ref Register b = ref this.DecodeRegister(pos + 2, out int bsz);

						int iflags = flags.NativeInt;
						iflags &= ~(int)(NamedRegister.FlagsEqual | NamedRegister.FlagsLess | NamedRegister.FlagsGreater);

						float ia = a.GetInteger(asz);
						float ib = b.GetInteger(bsz);

						bool gt = ia > ib;
						bool lt = ia < ib;
						bool eq = ia == ib;

						iflags = !eq ? iflags : iflags | (int)NamedRegister.FlagsEqual;
						iflags = !lt ? iflags : iflags | (int)NamedRegister.FlagsLess;
						iflags = !gt ? iflags : iflags | (int)NamedRegister.FlagsGreater;

						flags.NativeInt = iflags;
						break;
					}
				case Opcode.CompareFloatRR: ip.NativeInt += 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1, out int asz);
						ref Register b = ref this.DecodeRegister(pos + 1, out int bsz);

						int iflags = flags.NativeInt;
						iflags &= ~(int)(NamedRegister.FlagsEqual | NamedRegister.FlagsLess | NamedRegister.FlagsGreater);

						float fa = a.GetFloat(asz);
						float fb = b.GetFloat(bsz);

						bool gt = fa > fb;
						bool lt = fa < fb;
						bool eq = fa == fb;

						iflags = !eq ? iflags : iflags | (int)NamedRegister.FlagsEqual;
						iflags = !lt ? iflags : iflags | (int)NamedRegister.FlagsLess;
						iflags = !gt ? iflags : iflags | (int)NamedRegister.FlagsGreater;

						flags.NativeInt = iflags;
						break;
					}
				case Opcode.JumpEqualR: ip.NativeInt += 1 + 1;
					{
						ref Register a = ref this.DecodeRegister(pos + 1, out int asz);
						int jump_to = a.GetInteger(asz);
						if ((flags.NativeInt & (int)NamedRegister.FlagsEqual) != 0)
							ip.NativeInt = jump_to;
						break;
					}
				case Opcode.JumpEqualV: ip.NativeInt += 1 + 4;
					{
						int jump_to = this.DecodeInt(pos + 1, Register.NativeSize);
						if ((flags.NativeInt & (int)NamedRegister.FlagsEqual) != 0)
							ip.NativeInt = jump_to;
						break;
					}
				#endregion

				#region Transformative
				case Opcode.AddRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) + right.GetInteger(rightsz));
						break;
					}
				case Opcode.AddFloatRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, left.GetFloat(leftsz) + right.GetFloat(rightsz));
						break;
					}
				case Opcode.SubtractRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) - right.GetInteger(rightsz));
						break;
					}
				case Opcode.SubtractFloatRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, left.GetFloat(leftsz) - right.GetFloat(rightsz));
						break;
					}
				case Opcode.MultiplyRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) * right.GetInteger(rightsz));
						break;
					}
				case Opcode.MultiplyUnsignedRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetUnsigned(dstsz, left.GetUnsigned(leftsz) * right.GetUnsigned(rightsz));
						break;
					}
				case Opcode.MultiplyFloatRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, left.GetFloat(leftsz) * right.GetFloat(rightsz));
						break;
					}
				case Opcode.DivideRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) / right.GetInteger(rightsz));
						break;
					}
				case Opcode.DivideUnsignedRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetUnsigned(dstsz, left.GetUnsigned(leftsz) / right.GetUnsigned(rightsz));
						break;
					}
				case Opcode.DivideFloatRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, left.GetFloat(leftsz) / right.GetFloat(rightsz));
						break;
					}
				case Opcode.ModulusRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) % right.GetInteger(rightsz));
						break;
					}
				case Opcode.ModulusFloatRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, left.GetFloat(leftsz) % right.GetFloat(rightsz));
						break;
					}
				case Opcode.ShiftLeftRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) << right.GetInteger(rightsz));
						break;
					}
				case Opcode.ShiftRightRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) >> right.GetInteger(rightsz));
						break;
					}
				case Opcode.ArithmaticShiftRightRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						throw new NotImplementedException();
					}
				case Opcode.OrRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) | right.GetInteger(rightsz));
						break;
					}
				case Opcode.ExclusiveOrRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) ^ right.GetInteger(rightsz));
						break;
					}
				case Opcode.NotOrRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						//dst.I32 = left.I32 | (~right.I32);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) | (~right.GetInteger(rightsz)));
						break;
					}
				case Opcode.AndRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) & right.GetInteger(rightsz));
						break;
					}
				case Opcode.NotAndRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						//dst.I32 = left.I32 & (~right.I32);
						dst.SetInteger(dstsz, left.GetInteger(leftsz) & (~right.GetInteger(rightsz)));
						break;
					}
				case Opcode.NotRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetInteger(dstsz, ~left.GetInteger(leftsz));
						break;
					}
				case Opcode.SqrtRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Sqrt(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.LogRRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, (float)Math.Log(left.GetFloat(leftsz), right.GetFloat(rightsz)));
						break;
					}
				case Opcode.SinRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Sin(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.CosRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Cos(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.TanRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Tan(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.AsinRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Asin(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.AcosRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Acos(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.AtanRR:
					ip.NativeInt += 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						dst.SetFloat(dstsz, (float)Math.Atan(left.GetFloat(leftsz)));
						break;
					}
				case Opcode.Atan2RRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, (float)Math.Atan2(left.GetFloat(leftsz), right.GetFloat(rightsz)));
						break;
					}
				case Opcode.PowRRR:
					ip.NativeInt += 1 + 1 + 1 + 1;
					{
						ref Register dst = ref this.DecodeRegister(pos + 1, out int dstsz);
						ref Register left = ref this.DecodeRegister(pos + 2, out int leftsz);
						ref Register right = ref this.DecodeRegister(pos + 3, out int rightsz);
						dst.SetFloat(dstsz, (float)Math.Pow(left.GetFloat(leftsz), right.GetFloat(rightsz)));
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
