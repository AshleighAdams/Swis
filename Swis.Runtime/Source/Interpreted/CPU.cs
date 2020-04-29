﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// https://github.com/wiremod/Miscellaneous/blob/master/CPU%20%26%20GPU/zcpudocs/zcpudoc.pdf

namespace Swis
{
	[StructLayout(LayoutKind.Explicit)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct Caster
#pragma warning restore CA1815 // Override equals and operator equals on value types
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
		public abstract bool Clock(Cpu cpu);
	}

	public interface IReadWriteList<T>
	{
		T this[int index] { get; set; }
		int Count { get; }
	}

	public interface ICpu
	{
		IReadWriteList<uint> Registers { get; }
		int Clock(int cycles = 1);
		void Interrupt(uint code);
		void Reset();

		ref uint TimeStampCounter { get; }
		ref uint InstructionPointer { get; }
		ref uint StackPointer { get; }
		ref uint BasePointer { get; }
		ref uint Flags { get; }
		ref uint ProtectedMode { get; }
		ref uint ProtectedInterrupt { get; }

		bool Halted { get; }

		bool HandleInterrupts(IProducerConsumerCollection<uint> interrupt_queue);
	}

	public abstract class Cpu : ICpu
	{
		public static uint NativeSizeBits = 32;
		public static uint NativeSizeBytes = NativeSizeBits / 8;

		public virtual ExternalDebugger? Debugger { get; set; }
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		// TODO: use dependency injection to provide this, fixes nullable issues
		public virtual IMemoryController Memory { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

		// todo: replace with interface by DI
		public virtual Action<UInt16, byte> LineWrite { get; set; } = delegate (UInt16 _, byte unused) { };
		public virtual Func<UInt16, byte> LineRead { get; set; } = delegate (UInt16 _) { return 0; };
		public abstract IReadWriteList<uint> Registers { get; }
		
		public abstract int Clock(int clocks = 1);
		public abstract void Interrupt(uint code);
		public abstract void Reset();

		public abstract ref uint TimeStampCounter { get; }
		public abstract ref uint InstructionPointer { get; }
		public abstract ref uint StackPointer { get; }
		public abstract ref uint BasePointer { get; }
		public abstract ref uint Flags { get; }
		public abstract ref uint ProtectedMode { get; }
		public abstract ref uint ProtectedInterrupt { get; }

		public virtual bool Halted { get { return (ProtectedMode & (uint)ProtectedModeRegisterFlags.Halted) != 0; } }

		public virtual bool HandleInterrupts(IProducerConsumerCollection<uint> interrupt_queue)
		{
			if (interrupt_queue.Count == 0)
				return false;

			ref uint sp = ref StackPointer;
			ref uint bp = ref BasePointer;
			ref uint ip = ref InstructionPointer;
			ref uint pm = ref ProtectedMode;
			ref uint pi = ref ProtectedInterrupt;
			uint mode = (pi & 0b0000_0000__0000_0000__0000_0011__0000_0000u) >> 8;

			switch (mode)
			{
				case 0b00: /*disabled silent*/
					while (interrupt_queue.TryTake(out _)) { } // clear the remaining
					return false;
				case 0b10: return false; /*queued*/
				case 0b11: pm |= (uint)ProtectedModeRegisterFlags.Halted; return true; /*disabled halt*/
				case 0b01:
				default: /*consume one*/
					if (!interrupt_queue.TryTake(out uint @int))
						return false;

					uint ivt = (pi & 0b0000_0000__0000_0000__0000_0000__1111_1111u) << 8;
					uint ivtn = @int > 255 ? 255 : @int;
					uint addr = Memory[ivt + ivtn * Cpu.NativeSizeBytes, Cpu.NativeSizeBits];

					// simulate call to the interrupt address if it's enabled
					if (addr != 0)
					{
						pi &= ~0b0000_0000__0000_0000__0000_0011__0000_0000u; // clear mode
						pi |= 0b0000_0000__0000_0000__0000_0010__0000_0000u; // set mode to queue

						// push ip
						Memory[sp, Cpu.NativeSizeBits] = ip;
						sp += Cpu.NativeSizeBytes;
						// push bp
						Memory[sp, Cpu.NativeSizeBits] = bp;
						sp += Cpu.NativeSizeBytes;
						// push flags
						Memory[sp, Cpu.NativeSizeBits] = Flags;
						sp += Cpu.NativeSizeBytes;
						// mov bp, sp
						bp = sp;
						// jmp loc
						ip = addr;

						if (ivtn == 255) // extended interrupt, push the interrupt code
						{
							// push @int
							Memory[sp, Cpu.NativeSizeBits] = @int;
							sp += Cpu.NativeSizeBytes;
						}
						return false;
					}
					else
					{
						if (@int == (uint)Interrupts.DoubleFault)
						{
							pm |= (uint)ProtectedModeRegisterFlags.Halted;
							return true;
						}
						else
						{
							this.Interrupt((uint)Interrupts.DoubleFault);
							return this.HandleInterrupts(interrupt_queue);
						}
					}
			}
		}


	}

	public sealed partial class InterpretedCpu : Cpu, ICpu
	{
		private struct ListWrapper<T> : IReadWriteList<T>
		{
			private IList<T> Source { get; }
			public ListWrapper(IList<T> source)
			{
				Source = source;
			}

			T IReadWriteList<T>.this[int index] { get => Source[index]; set => Source[index] = value; }
			int IReadWriteList<T>.Count { get => Source.Count; }
		}

		public uint[] InternalRegisters = new uint[(int)NamedRegister.L + 1];
		public override IReadWriteList<uint> Registers { get => new ListWrapper<uint>(InternalRegisters); }
		
		public InterpretedCpu(IMemoryController memory)
		{
			Memory = memory;
		}

		public override int Clock(int count = 1)
		{
			// todo: maybe make these as following if quicker: ref this.Registers[(int)NamedRegister.regname];
			ref uint tsc = ref TimeStampCounter;
			ref uint ip = ref InstructionPointer;
			ref uint sp = ref StackPointer;
			ref uint bp = ref BasePointer;
			ref uint flags = ref Flags;

			if (Halted)
				return 0;

			for (int i = 0; i < count; i++)
			{
				if (this.HandleInterrupts(InterruptQueue))
					return i;

				if (Debugger != null)
					if (!Debugger.Clock(this))
						break;

				Opcode op = Memory.DecodeOpcode(ref ip);

				switch (op)
				{
					#region Misc
					case Opcode.Nop:
						break;
					case Opcode.InterruptR:
						Operand @int = Memory.DecodeOperand(ref ip, InternalRegisters);
						this.Interrupt(@int.Value);
						count = 0;
						break;
					case Opcode.InterruptReturn:
						{
							ref uint pi = ref InternalRegisters[(int)NamedRegister.ProtectedInterrupt];
							pi &= ~0b0000_0000__0000_0000__0000_0011__0000_0000u; // clear mode
							pi |= 0b0000_0000__0000_0000__0000_0001__0000_0000u; // set mode to enabled

							//if(realmode)
							{
								// mov sp, bp
								sp = bp;
								// pop flags
								sp -= Cpu.NativeSizeBytes;
								flags = Memory[sp, Cpu.NativeSizeBits];
								// pop bp
								sp -= Cpu.NativeSizeBytes;
								bp = Memory[sp, Cpu.NativeSizeBits];
								// pop ip
								sp -= Cpu.NativeSizeBytes;
								ip = Memory[sp, Cpu.NativeSizeBits];
							}
							break;
						}
					case Opcode.SetInterrupt:
						{
							ref uint pi = ref InternalRegisters[(int)NamedRegister.ProtectedInterrupt];
							pi &= ~0b0000_0000__0000_0000__0000_0011__0000_0000u; // clear mode
							pi |= 0b0000_0000__0000_0000__0000_0001__0000_0000u; // set mode to enabled
							break;
						}
					case Opcode.ClearInterrupt:
						{
							ref uint pi = ref InternalRegisters[(int)NamedRegister.ProtectedInterrupt];
							pi &= ~0b0000_0000__0000_0000__0000_0011__0000_0000u; // clear mode
							pi |= 0b0000_0000__0000_0000__0000_0010__0000_0000u; // set mode to queue
							break;
						}
					case Opcode.SignExtendRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand src = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand bit = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = Util.SignExtend(src.Value, bit.Value);
							break;
						}
					case Opcode.ZeroExtendRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand src = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand bit = Memory.DecodeOperand(ref ip, InternalRegisters);

							uint frombits = bit.Value;
							if (frombits < 1 || frombits >= 32)
								throw new Exception();

							uint valbits = (1u << (int)frombits) - 1;

							dst.Value = src.Value & valbits;
							break;
						}
					case Opcode.Halt:
						ProtectedMode |= (uint)ProtectedModeRegisterFlags.Halted;
						count = 0; // so we break from the loop
						break;
					case Opcode.InRR:
						{
							Operand lttr = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand line = Memory.DecodeOperand(ref ip, InternalRegisters);

							lttr.Value = (uint)LineRead((UInt16)line.Value);
							break;
						}
					case Opcode.OutRR:
						{
							Operand line = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand lttr = Memory.DecodeOperand(ref ip, InternalRegisters);

							LineWrite((UInt16)line.Value, (byte)lttr.Value);
							break;
						}
					#endregion
					#region Memory
					case Opcode.MoveRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand src = Memory.DecodeOperand(ref ip, InternalRegisters);
							dst.Value = src.Value;
							break;
						}
					case Opcode.PushR:
						{
							Operand src = Memory.DecodeOperand(ref ip, InternalRegisters);

							Memory[sp, src.ValueSize] = src.Value;
							sp += src.ValueSize / 8;
							break;
						}
					case Opcode.PopR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);

							sp -= dst.ValueSize / 8;
							dst.Value = Memory[sp, dst.ValueSize];
							break;
						}
					#endregion
					#region Flow
					case Opcode.CallR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);

							// push ip
							Memory[sp, Cpu.NativeSizeBits] = ip;
							sp += Cpu.NativeSizeBytes;
							// push bp
							Memory[sp, Cpu.NativeSizeBits] = bp;
							sp += Cpu.NativeSizeBytes;
							// mov bp, sp
							bp = sp;
							// jmp loc
							ip = loc.Value;
							break;
						}
					case Opcode.Return:
						{
							// the reverse of call

							// mov sp, bp
							sp = bp;
							// pop bp
							sp -= Cpu.NativeSizeBytes;
							bp = Memory[sp, Cpu.NativeSizeBits];
							// pop ip
							sp -= Cpu.NativeSizeBytes;
							ip = Memory[sp, Cpu.NativeSizeBits];

							break;
						}
					case Opcode.JumpR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							ip = loc.Value;
							break;
						}
					case Opcode.CompareRR:
						{
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							Int32 l = left.Signed;
							Int32 r = right.Signed;

							var iflags = (FlagsRegisterFlags)Flags;
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
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand ordered = Memory.DecodeOperand(ref ip, InternalRegisters);

							float l = left.Float;
							float r = right.Float;

							var iflags = (FlagsRegisterFlags)Flags;
							iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

							if (l > r)
								iflags |= FlagsRegisterFlags.Greater;
							if (l < r)
								iflags |= FlagsRegisterFlags.Less;
							if (l == r) //-V3024
								iflags |= FlagsRegisterFlags.Equal;

							flags = (uint)iflags;
							break;
						}
					case Opcode.CompareUnsignedRR:
						{
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							UInt32 l = left.Value;
							UInt32 r = right.Value;

							var iflags = (FlagsRegisterFlags)Flags;
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
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							if (((FlagsRegisterFlags)Flags).HasFlag(FlagsRegisterFlags.Equal))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpNotEqualR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							if (!((FlagsRegisterFlags)Flags).HasFlag(FlagsRegisterFlags.Equal))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpGreaterR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							if (((FlagsRegisterFlags)Flags).HasFlag(FlagsRegisterFlags.Greater))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpGreaterEqualR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							var flgs = (FlagsRegisterFlags)Flags;
							if (flgs.HasFlag(FlagsRegisterFlags.Greater) || flgs.HasFlag(FlagsRegisterFlags.Equal))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpLessR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							if (((FlagsRegisterFlags)Flags).HasFlag(FlagsRegisterFlags.Less))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpLessEqualR:
						{
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);
							var flgs = (FlagsRegisterFlags)Flags;
							if (flgs.HasFlag(FlagsRegisterFlags.Less) || flgs.HasFlag(FlagsRegisterFlags.Equal))
								ip = loc.Value;
							break;
						}
					case Opcode.JumpZeroRR:
						{
							Operand cond = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);

							if (cond.Value == 0)
								ip = loc.Value;
							break;
						}
					case Opcode.JumpNotZeroRR:
						{
							Operand cond = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand loc = Memory.DecodeOperand(ref ip, InternalRegisters);

							if (cond.Value != 0)
								ip = loc.Value;
							break;
						}
					#endregion
					#region Transformative
					case Opcode.AddRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							// with two's compliment, adding does not need to be sign-aware
							dst.Value = left.Value + right.Value;
							break;
						}
					case Opcode.AddFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = left.Float + right.Float;
							break;
						}
					case Opcode.SubtractRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value - right.Value;
							break;
						}
					case Opcode.SubtractFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = left.Float - right.Float;
							break;
						}
					case Opcode.MultiplyRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Signed = left.Signed * right.Signed;
							break;
						}
					case Opcode.MultiplyUnsignedRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value * right.Value;
							break;
						}
					case Opcode.MultiplyFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = left.Float * right.Float;
							break;
						}
					case Opcode.DivideRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Signed = left.Signed / right.Signed;
							break;
						}
					case Opcode.DivideUnsignedRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value / right.Value;
							break;
						}
					case Opcode.DivideFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = left.Float / right.Float;
							break;
						}
					case Opcode.ModulusRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value % right.Value;
							break;
						}
					case Opcode.ModulusUnsignedRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value % right.Value;
							break;
						}
					case Opcode.ModulusFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = left.Float % right.Float;
							break;
						}
					case Opcode.ShiftLeftRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value << (int)right.Value;
							break;
						}
					case Opcode.ShiftRightRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value >> (int)right.Value;
							break;
						}
					case Opcode.ArithmaticShiftRightRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Signed = left.Signed >> (int)right.Value; // TODO: check this
							throw new NotImplementedException();
						}
					case Opcode.OrRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value | right.Value;
							break;
						}
					case Opcode.ExclusiveOrRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value ^ right.Value;
							break;
						}
					case Opcode.NotOrRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value | (~right.Value);
							break;
						}
					case Opcode.AndRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value & right.Value;
							break;
						}
					case Opcode.NotAndRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = left.Value & (~right.Value);
							break;
						}
					case Opcode.NotRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Value = ~left.Value;
							break;
						}
					case Opcode.SqrtFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Sqrt(left.Float);
							break;
						}
					case Opcode.LogFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Log(left.Float, right.Float);
							break;
						}
					case Opcode.SinFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Sin(left.Float);
							break;
						}
					case Opcode.CosFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Cos(left.Float);
							break;
						}
					case Opcode.TanFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Tan(left.Float);
							break;
						}
					case Opcode.AsinFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Asin(left.Float);
							break;
						}
					case Opcode.AcosFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Acos(left.Float);
							break;
						}
					case Opcode.AtanFloatRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Atan(left.Float);
							break;
						}
					case Opcode.Atan2FloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

							dst.Float = (float)Math.Atan2(left.Float, right.Float);
							break;
						}
					case Opcode.PowFloatRRR:
						{
							Operand dst = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand left = Memory.DecodeOperand(ref ip, InternalRegisters);
							Operand right = Memory.DecodeOperand(ref ip, InternalRegisters);

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

		public override void Reset()
		{
			for (int i = 0; i < InternalRegisters.Length; i++)
				InternalRegisters[i] = 0;
		}
	}
}
