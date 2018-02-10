using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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

	public abstract class ExternalDebugger
	{
		public abstract bool Clock(Cpu cpu, MemoryController memory);
	}
	
	public partial class Cpu
    {
		public static uint NativeSizeBits = 32;
		public static uint NativeSizeBytes = NativeSizeBits / 8;
		
		public uint[] Registers = new uint[32];

		public Cpu()
		{
			this.Memory = null;
		}
		
		

		public int Clock(int count = 1)
		{
			// todo: maybe make these as following if quicker: ref this.Registers[(int)NamedRegister.regname];
			ref uint tsc = ref this.TimeStampCounter;
			ref uint ip = ref this.InstructionPointer;
			ref uint sp = ref this.StackPointer;
			ref uint bp = ref this.BasePointer;
			ref uint flags = ref this.Flags;

			if (this.Halted)
				return 0;
			
			for (int i = 0; i < count; i++)
			{
				if (this.Debugger != null)
					if (!this.Debugger.Clock(this, this.Memory))
						break;

				Opcode op = this.Memory.DecodeOpcode(ref ip);
				
				switch (op)
				{
#region Misc
				case Opcode.Nop:
					break;

				case Opcode.SignExtendRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand bit = this.Memory.DecodeOperand(ref ip, this.Registers);
						
						dst.Value = Util.SignExtend(src.Value, bit.Value);
						break;
					}
				case Opcode.ZeroExtendRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand bit = this.Memory.DecodeOperand(ref ip, this.Registers);

						uint frombits = bit.Value;
						if (frombits < 1 || frombits >= 32)
							throw new Exception();

						uint valbits = (1u << (int)frombits) - 1;

						src.Value = dst.Value & valbits;
						break;
					}
				case Opcode.Halt:
					flags |= (uint)FlagsRegisterFlags.Halted;
					count = 0; // so we break from the loop
					break;
				case Opcode.InRR:
					{
						Operand lttr = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand line = this.Memory.DecodeOperand(ref ip, this.Registers);

						lttr.Value = (uint)Console.Read();
						break;
					}
				case Opcode.OutRR:
					{
						Operand line = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand lttr = this.Memory.DecodeOperand(ref ip, this.Registers);

						Console.Write((char)lttr.Value);
						break;
					}
#endregion
#region Memory
				case Opcode.MoveRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand src = this.Memory.DecodeOperand(ref ip, this.Registers);
						dst.Value = src.Value;
						break;
					}
				case Opcode.PushR:
					{
						Operand src = this.Memory.DecodeOperand(ref ip, this.Registers);

						Operand ptr = this.CreatePointer(sp, src.ValueSize);
						sp += src.ValueSize / 8;

						ptr.Value = src.Value;
						break;
					}
				case Opcode.PopR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);

						sp -= dst.ValueSize / 8;
						Operand ptr = this.CreatePointer(sp, dst.ValueSize);

						dst.Value = ptr.Value;
						break;
					}
#endregion
#region Flow
				case Opcode.CallR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						
						// push ip ; the retaddr
						{
							Operand sp_ptr = this.CreatePointer(sp, Cpu.NativeSizeBits);
							sp_ptr.Value = ip;
							sp += Cpu.NativeSizeBytes;
						}

						// push bp
						{
							Operand sp_ptr = this.CreatePointer(sp, Cpu.NativeSizeBits);
							sp_ptr.Value = bp;
							sp += Cpu.NativeSizeBytes;
						}

						// mov bp, sp
						{
							bp = sp;
						}

						// jmp loc
						{
							ip = loc.Value;
						}
						
						break;
					}
				case Opcode.Return:
					{
						// the reverse of call

						// mov sp, bp
						{
							sp = bp;
						}

						// pop bp
						{
							sp -= Cpu.NativeSizeBytes;
							Operand sp_ptr = this.CreatePointer(sp, Cpu.NativeSizeBits);
							bp = sp_ptr.Value;
						}

						// pop ip ; equiv to:  pop $1 jmp $1
						{
							sp -= Cpu.NativeSizeBytes;
							Operand sp_ptr = this.CreatePointer(sp, Cpu.NativeSizeBits);
							ip = sp_ptr.Value;
						}
						
						break;
					}
				case Opcode.JumpR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						ip = loc.Value;
						break;
					}
				case Opcode.CompareRR:
					{
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						Int32 l = left.Signed;
						Int32 r = right.Signed;

						var iflags = (FlagsRegisterFlags)this.Flags;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (l > r)
							iflags |= FlagsRegisterFlags.Greater;
						if (l < r)
							iflags |= FlagsRegisterFlags.Less;
						if (l == r)
							iflags |= FlagsRegisterFlags.Equal;

						flags = (uint)iflags;
						break;
					}
				case Opcode.CompareFloatRRR:
					{
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand ordered = this.Memory.DecodeOperand(ref ip, this.Registers);

						float l = left.Float;
						float r = right.Float;

						var iflags = (FlagsRegisterFlags)this.Flags;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (l > r)
							iflags |= FlagsRegisterFlags.Greater;
						if (l < r)
							iflags |= FlagsRegisterFlags.Less;
						if (l == r)
							iflags |= FlagsRegisterFlags.Equal;

						flags = (uint)iflags;
						break;
					}
				case Opcode.CompareUnsignedRR:
					{
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						UInt32 l = left.Value;
						UInt32 r = right.Value;

						var iflags = (FlagsRegisterFlags)this.Flags;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (l > r)
							iflags |= FlagsRegisterFlags.Greater;
						if (l < r)
							iflags |= FlagsRegisterFlags.Less;
						if (l == r)
							iflags |= FlagsRegisterFlags.Equal;

						flags = (uint)iflags;
						break;
					}
				case Opcode.JumpEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						if (((FlagsRegisterFlags)this.Flags).HasFlag(FlagsRegisterFlags.Equal))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpNotEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						if (!((FlagsRegisterFlags)this.Flags).HasFlag(FlagsRegisterFlags.Equal))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpGreaterR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						if (((FlagsRegisterFlags)this.Flags).HasFlag(FlagsRegisterFlags.Greater))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpGreaterEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						var flgs = (FlagsRegisterFlags)this.Flags;
						if (flgs.HasFlag(FlagsRegisterFlags.Greater) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpLessR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						if (((FlagsRegisterFlags)this.Flags).HasFlag(FlagsRegisterFlags.Less))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpLessEqualR:
					{
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);
						var flgs = (FlagsRegisterFlags)this.Flags;
						if (flgs.HasFlag(FlagsRegisterFlags.Less) || flgs.HasFlag(FlagsRegisterFlags.Equal))
							ip = loc.Value;
						break;
					}
				case Opcode.JumpZeroRR:
					{
						Operand cond = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);

						if (cond.Value == 0)
							ip = loc.Value;
						break;
					}
				case Opcode.JumpNotZeroRR:
					{
						Operand cond = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand loc = this.Memory.DecodeOperand(ref ip, this.Registers);

						if (cond.Value != 0)
							ip = loc.Value;
						break;
					}
#endregion
#region Transformative
				case Opcode.AddRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						// with two's compliment, adding does not need to be sign-aware
						dst.Value = left.Value + right.Value;
						break;
					}
				case Opcode.AddFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = left.Float + right.Float;
						break;
					}
				case Opcode.SubtractRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value - right.Value;
						break;
					}
				case Opcode.SubtractFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = left.Float - right.Float;
						break;
					}
				case Opcode.MultiplyRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Signed = left.Signed * right.Signed;
						break;
					}
				case Opcode.MultiplyUnsignedRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value * right.Value;
						break;
					}
				case Opcode.MultiplyFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = left.Float * right.Float;
						break;
					}
				case Opcode.DivideRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Signed = left.Signed / right.Signed;
						break;
					}
				case Opcode.DivideUnsignedRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value / right.Value;
						break;
					}
				case Opcode.DivideFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = left.Float / right.Float;
						break;
					}
				case Opcode.ModulusRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value % right.Value;
						break;
					}
				case Opcode.ModulusFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = left.Float % right.Float;
						break;
					}
				case Opcode.ModulusUnsignedRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value % right.Value;
						break;
					}
				case Opcode.ShiftLeftRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value << (int)right.Value;
						break;
					}
				case Opcode.ShiftRightRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value >> (int)right.Value;
						break;
					}
				case Opcode.ArithmaticShiftRightRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						throw new NotImplementedException();
					}
				case Opcode.OrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value | right.Value;
						break;
					}
				case Opcode.ExclusiveOrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value ^ right.Value;
						break;
					}
				case Opcode.NotOrRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value | (~right.Value);
						break;
					}
				case Opcode.AndRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value & right.Value;
						break;
					}
				case Opcode.NotAndRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = left.Value & (~right.Value);
						break;
					}
				case Opcode.NotRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Value = ~left.Value;
						break;
					}
				case Opcode.SqrtFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Sqrt(left.Float);
						break;
					}
				case Opcode.LogFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Log(left.Float, right.Float);
						break;
					}
				case Opcode.SinFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Sin(left.Float);
						break;
					}
				case Opcode.CosFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Cos(left.Float);
						break;
					}
				case Opcode.TanFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Tan(left.Float);
						break;
					}
				case Opcode.AsinFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Asin(left.Float);
						break;
					}
				case Opcode.AcosFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Acos(left.Float);
						break;
					}
				case Opcode.AtanFloatRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Atan(left.Float);
						break;
					}
				case Opcode.Atan2FloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Atan2(left.Float, right.Float);
						break;
					}
				case Opcode.PowFloatRRR:
					{
						Operand dst = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand left = this.Memory.DecodeOperand(ref ip, this.Registers);
						Operand right = this.Memory.DecodeOperand(ref ip, this.Registers);

						dst.Float = (float)Math.Pow(left.Float, right.Float);
						break;
					}
#endregion
				default:
					throw new NotImplementedException(); // todo: make it interrupt
				}

				tsc++;
			}

			return count;
		}

		public void Reset()
		{
			for (int i = 0; i < this.Registers.Length; i++)
				this.Registers[i] = 0;
		}

		public void Interrupt(int code)
		{
			throw new NotImplementedException();
		}
		
	}
}
