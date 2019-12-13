using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

using static Swis.JitExpressionHelpers;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		uint Reg0;
		#region Other Registers
		uint Reg1;
		uint Reg2;
		uint Reg3;
		uint Reg4;
		uint Reg5;
		uint Reg6;
		uint Reg7;
		uint Reg8;
		uint Reg9;
		uint Reg10;
		uint Reg11;
		uint Reg12;
		uint Reg13;
		uint Reg14;
		uint Reg15;
		uint Reg16;
		uint Reg17;
		uint Reg18;
		uint Reg19;
		uint Reg20;
		uint Reg21;
		uint Reg22;
		uint Reg23;
		uint Reg24;
		uint Reg25;
		uint Reg26;
		uint Reg27;
		uint Reg28;
		uint Reg29;
		uint Reg30;
		uint Reg31;

		public override uint[] Registers
		{
			get
			{
				return new uint[] {
					this.Reg0,  this.Reg1,  this.Reg2,  this.Reg3,  this.Reg4,  this.Reg5,  this.Reg6,  this.Reg7,  this.Reg8,  this.Reg9,
					this.Reg10, this.Reg11, this.Reg12, this.Reg13, this.Reg14, this.Reg15, this.Reg16, this.Reg17, this.Reg18, this.Reg19,
					this.Reg20, this.Reg21, this.Reg22, this.Reg23, this.Reg24, this.Reg25, this.Reg26, this.Reg27, this.Reg28, this.Reg29,
					this.Reg30, this.Reg31,
				};
			}
		}
		#endregion
		
		MemoryController _Memory;
		public override MemoryController Memory
		{
			get { return this._Memory; }
			set { this._Memory = new JitCacheInvalidator(this, value); }
		}

		public uint JitCostFactor = 100; // how much slower the first time code is JITed approx is, to prevent abuse
		Dictionary<uint, (Action λ, uint cycles)> JitCache;
		uint _JitBlockSize = 16;
		uint JitCacheFirst;
		uint JitCacheLast; // track the jitted bounds so as to clear JIT instructions
		
		public JittedCpu()
		{
			this.JitCache = new Dictionary<uint, (Action λ, uint cycles)>();
			this.ClearJitCache(); // sets up default values
		}

		public void ClearJitCache()
		{
			this.JitCacheFirst = uint.MaxValue;
			this.JitCacheLast = 0;
			this.JitCache.Clear();
		}

		public override ExternalDebugger Debugger
		{
			get { return base.Debugger; }
			set
			{
				// the old instructions need re-building to ensure they call the debugger
				this.ClearJitCache();
				base.Debugger = value;
			}
		}

		public uint JitBlockSize
		{
			get { return this._JitBlockSize; }
			set { this._JitBlockSize = value; this.ClearJitCache(); }
		}
		
		private long CycleBank = 0; // don't execute the next instruction block until we can afford it
		public override int Clock(int cycles = 1)
		{
			if (this.Halted)
				return 0;
			this.CycleBank += cycles;
			int executed = 0;

			while (this.CycleBank > 0)
			{
				if (this.HandleInterrupts(this.InterruptQueue))
					return executed;
				
				if (!this.JitCache.TryGetValue(this.Reg1, out (Action λ, uint cycles) instr))
				{
					uint block_length = 0;
					List<Expression> block_instructions = new List<Expression>();

					uint simulated_ip = this.Reg1;

					if (simulated_ip < this.JitCacheFirst)
						this.JitCacheFirst = simulated_ip;

					for (uint n = 0; n < this.JitBlockSize; n++)
					{
						if (simulated_ip >= this.Memory.Length)
							break;
						
						var jitinst = JitInstruction(simulated_ip);

						block_instructions.Add(jitinst.λ);
						block_length += jitinst.len;
						simulated_ip += jitinst.len;

						if (jitinst.sequential_not_gauranteed)
							break;
						if (this.JitCache.ContainsKey(simulated_ip)) // we have already jitted from this address, so use it
							break;
					}

					if (simulated_ip > this.JitCacheLast)
						this.JitCacheLast = simulated_ip;

					var λ = Expression.Lambda<Action>(Expression.Block(block_instructions));
					
					//Console.WriteLine($"JIT: [{this.Reg1}] = {λ.GetDebugView()}");

					instr = this.JitCache[this.Reg1] = (λ.Compile(), (uint)block_instructions.Count);

					// cost in cycles for jitting an instruction
					uint jitcost = (uint)block_instructions.Count * this.JitCostFactor;

					this.Reg0 += jitcost;
					this.CycleBank -= jitcost;
				}

				if (this.CycleBank >= instr.cycles)
				{
					instr.λ();
					this.CycleBank -= (int)instr.cycles;
					executed += (int)instr.cycles;
					if (this.Halted)
						return executed;
				}
				else
					break; // try again later
			}

			return executed;
		}
		
		(Expression λ, uint len, bool sequential_not_gauranteed) JitInstruction(uint location)
		{
			uint original_ip = location;
			uint ip = original_ip;
			Opcode op = this.Memory.DecodeOpcode(ref ip);

			bool sequential_not_gauranteed = false;

			Expression exp = null;

			switch (op)
			{
			#region Misc
			case Opcode.Nop:
				break;
			case Opcode.InterruptR:
				Operand @int = this.Memory.DecodeOperand(ref ip, null);
				Expression intexp = JitOperand(ref @int, true);

				Expression<Action<uint>> cpuinterrupt = intcode => this.Interrupt(intcode);
				exp = Expression.Invoke(cpuinterrupt, intexp);
				sequential_not_gauranteed = true;
				break;
			case Opcode.InterruptReturn:
				{
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, false);
					Expression esp = RegisterExpression(NamedRegister.StackPointer, Cpu.NativeSizeBits, false);
					Expression ebp = RegisterExpression(NamedRegister.BasePointer, Cpu.NativeSizeBits, false);
					Expression epm = RegisterExpression(NamedRegister.ProtectedMode, Cpu.NativeSizeBits, false);
					Expression epi = RegisterExpression(NamedRegister.ProtectedInterrupt, Cpu.NativeSizeBits, false);

					Expression esp_ptr = PointerExpression(esp, Cpu.NativeSizeBits);

					exp = Expression.Block(
						// clear mode
						Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
						// set mode to enabled
						Expression.OrAssign(epi,  Expression.Constant( 0b0000_0000__0000_0000__0000_0001__0000_0000u)),
						// mov sp, bp
						Expression.Assign(esp, ebp),
						// pop pm
						Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						Expression.Assign(epm, esp_ptr),
						// pop bp
						Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						Expression.Assign(ebp, esp_ptr),
						// pop ip
						Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						Expression.Assign(eip, esp_ptr)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.SetInterrupt:
				{
					Expression epi = RegisterExpression(NamedRegister.ProtectedInterrupt, Cpu.NativeSizeBits, false);
					ref uint pi = ref this.Registers[(int)NamedRegister.ProtectedInterrupt];
					exp = Expression.Block(
						// clear mode
						Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
						// set mode to enabled
						Expression.OrAssign(epi,  Expression.Constant( 0b0000_0000__0000_0000__0000_0001__0000_0000u))
					);
					break;
				}
			case Opcode.ClearInterrupt:
				{
					Expression epi = RegisterExpression(NamedRegister.ProtectedInterrupt, Cpu.NativeSizeBits, false);
					exp = Expression.Block(
						// clear mode
						Expression.AndAssign(epi, Expression.Constant(~0b0000_0000__0000_0000__0000_0011__0000_0000u)),
						// set mode to queue
						Expression.OrAssign(epi,  Expression.Constant( 0b0000_0000__0000_0000__0000_0010__0000_0000u))
					);
					break;
				}
			case Opcode.SignExtendRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);
					Operand bit = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);
					Expression bitexp = JitOperand(ref bit, false);
					
					
					var valbits = Expression.Variable(typeof(uint), "valbits");
					var extbits = Expression.Variable(typeof(uint), "extbits");
					var signbit = Expression.Variable(typeof(uint), "signbit");

					var srcval = Expression.Variable(typeof(uint), "srcval");
					var sign = Expression.Variable(typeof(uint), "sign");

					exp = Expression.Block(
						new ParameterExpression[] { valbits, extbits, signbit, srcval, sign },
						// uint valbits = (1u << (int)frombits) - 1;
						Expression.Assign(valbits,
							Expression.Subtract(
								Expression.LeftShift(
									Expression.Constant(1u),
									Expression.Convert(bitexp, typeof(int))
								),
								Expression.Constant(1u)
							)
						),
						// uint extbits = ~valbits;
						Expression.Assign(extbits,
							Expression.Not(valbits)
						),
						// uint signbit = 1u << (int)(frombits - 1);
						Expression.Assign(signbit,
							Expression.LeftShift(
								Expression.Constant(1u),
								Expression.Convert(
									Expression.Subtract(bitexp, Expression.Constant(1u)),
									typeof(int)
								)
							)
						),
						// uint srcval = src & valbits;
						Expression.Assign(srcval,
							Expression.And(srcexp, valbits)
						),
						// uint sign = (signbit & srcval) >> ((int)frombits - 1);
						Expression.Assign(sign,
							Expression.RightShift(
								Expression.And(signbit, srcval),
								Expression.Convert(
									Expression.Subtract(bitexp, Expression.Constant(1u)),
									typeof(int)
								)
							)
						),
						// dst = srcval | (extbits * sign);
						Expression.Assign(dstexp,
							Expression.Or(
								srcval,
								Expression.Multiply(extbits, sign)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ZeroExtendRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);
					Operand bit = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);
					Expression bitexp = JitOperand(ref bit, false);

					var valbits = Expression.Variable(typeof(uint), "valbits");
					
					exp = Expression.Block(
						new ParameterExpression[] { valbits },
						// uint valbits = (1u << (int)frombits) - 1;
						Expression.Assign(valbits,
							Expression.Subtract(
								Expression.LeftShift(
									Expression.Constant(1u), bitexp
								),
								Expression.Constant(1u)
							)
						),
						// dst.Value = src.Value & valbits;
						Expression.Assign(dstexp,
							Expression.And(
								srcexp,
								valbits
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.Halt:
				{
					var pmregister = this.RegisterExpression(NamedRegister.ProtectedMode, 32, false);
					exp = Expression.OrAssign(pmregister, Expression.Constant((uint)ProtectedModeRegisterFlags.Halted));
					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.InRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand line = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression lineexp = JitOperand(ref line, false);
					
					Expression<Func<uint, uint>> readline = lineval => this.LineRead((UInt16)lineval);
					exp = Expression.Assign(dstexp, Expression.Invoke(readline, lineexp));

					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.OutRR:
				{
					Operand line = this.Memory.DecodeOperand(ref ip, null);
					Operand lttr = this.Memory.DecodeOperand(ref ip, null);

					Expression lineexp = JitOperand(ref line, false);
					Expression lttrexp = JitOperand(ref lttr, false);

					Expression<Action<uint, uint>> writeline = (lineval, charval) => this.LineWrite((UInt16)lineval, (byte)charval);

					exp = Expression.Invoke(writeline, lineexp, lttrexp);
					break;
				}
			#endregion
			#region Memory
			case Opcode.MoveRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);

					exp = Expression.Assign(dstexp, srcexp);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.PushR:
				{
					Operand src = this.Memory.DecodeOperand(ref ip, null);
					Expression srcexp = JitOperand(ref src, false);

					Expression sp = RegisterExpression(NamedRegister.StackPointer, Cpu.NativeSizeBits, false);
					Expression ptr = PointerExpression(sp, src.ValueSize);

					exp = Expression.Block(
						Expression.Assign(ptr, srcexp),
						Expression.AddAssign(sp, Expression.Constant(src.ValueSize / 8u))
					);
					break;
				}
			case Opcode.PopR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);

					Expression esp = RegisterExpression(NamedRegister.StackPointer, Cpu.NativeSizeBits, false);
					Expression ptr = PointerExpression(esp, dst.ValueSize);

					exp = Expression.Block(
						Expression.SubtractAssign(esp, Expression.Constant(dst.ValueSize / 8u)),
						Expression.Assign(dstexp, ptr)
					);

					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			#endregion
			#region Flow
			case Opcode.CallR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);

					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, false);
					Expression esp = RegisterExpression(NamedRegister.StackPointer, Cpu.NativeSizeBits, false);
					Expression ebp = RegisterExpression(NamedRegister.BasePointer, Cpu.NativeSizeBits, false);

					Expression esp_ptr = PointerExpression(esp, Cpu.NativeSizeBits);

					exp = Expression.Block(
						// push ip ; the retaddr
						Expression.Assign(esp_ptr, eip),
						Expression.AddAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						// push bp
						Expression.Assign(esp_ptr, ebp),
						Expression.AddAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						// mov bp, sp
						Expression.Assign(ebp, esp),
						// jmp loc
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.Return:
				{
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, false);
					Expression esp = RegisterExpression(NamedRegister.StackPointer, Cpu.NativeSizeBits, false);
					Expression ebp = RegisterExpression(NamedRegister.BasePointer, Cpu.NativeSizeBits, false);

					Expression esp_ptr = PointerExpression(esp, Cpu.NativeSizeBits);

					exp = Expression.Block(
						// mov sp, bp
						Expression.Assign(esp, ebp),
						// pop bp
						Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						Expression.Assign(ebp, esp_ptr),
						// pop ip
						Expression.SubtractAssign(esp, Expression.Constant(Cpu.NativeSizeBytes)),
						Expression.Assign(eip, esp_ptr)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, true);

					exp = Expression.Assign(RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, false), locexp);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.CompareRR:
				{
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					Action<uint, uint> comparer = (uleft, uright) =>
					{
						Caster c; c.I32 = 0;
						c.U32 = uleft;
						int ileft = c.I32;
						c.U32 = uright;
						int iright = c.I32;

						var iflags = (FlagsRegisterFlags)this.Reg5;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (ileft > iright) //-V3022
							iflags |= FlagsRegisterFlags.Greater;
						if (ileft < iright) //-V3022
							iflags |= FlagsRegisterFlags.Less;
						if (ileft == iright) //-V3022
							iflags |= FlagsRegisterFlags.Equal;

						this.Reg5 = (uint)iflags;
					};

					Expression<Action<uint, uint>> comparerexp = (l, r) => comparer(l, r);

					exp = Expression.Invoke(comparerexp, leftexp, rightexp);

					break;
				}
			case Opcode.CompareFloatRRR:
				{
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);
					Operand ordered = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);
					Expression orderedexp = JitOperand(ref ordered, false);

					Action<uint, uint, uint> comparer = (uleft, uright, uordered) =>
					{
						Caster c; c.F32 = 0;
						c.U32 = uleft;
						float fleft = c.F32;
						c.U32 = uright;
						float fright = c.F32;

						var iflags = (FlagsRegisterFlags)this.Reg5;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (fleft > fright)
							iflags |= FlagsRegisterFlags.Greater;
						if (fleft < fright)
							iflags |= FlagsRegisterFlags.Less;
						if (fleft == fright) //-V3024
							iflags |= FlagsRegisterFlags.Equal;

						this.Reg5 = (uint)iflags;
					};

					Expression<Action<uint, uint, uint>> comparerexp = (l, r, o) => comparer(l, r, o);

					exp = Expression.Invoke(comparerexp, leftexp, rightexp, orderedexp);

					break;
				}
			case Opcode.CompareUnsignedRR:
				{
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);
					
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);
					
					Action<uint, uint> comparer = (uleft, uright) =>
					{
						var iflags = (FlagsRegisterFlags)this.Reg5;
						iflags &= ~(FlagsRegisterFlags.Equal | FlagsRegisterFlags.Less | FlagsRegisterFlags.Greater);

						if (uleft > uright)
							iflags |= FlagsRegisterFlags.Greater;
						if (uleft < uright)
							iflags |= FlagsRegisterFlags.Less;
						if (uleft == uright)
							iflags |= FlagsRegisterFlags.Equal;

						this.Reg5 = (uint)iflags;
					};

					Expression<Action<uint, uint>> comparerexp = (l, r) => comparer(l, r);

					exp = Expression.Invoke(comparerexp, leftexp, rightexp);

					break;
				}
			case Opcode.JumpEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);
					
					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Equal)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpNotEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					//	if (uleft > uright)
					//		iflags |= FlagsRegisterFlags.Greater;
					//	if (uleft < uright)
					//		iflags |= FlagsRegisterFlags.Less;
					//	if (uleft == uright)
					//		iflags |= FlagsRegisterFlags.Equal;
					//	this.Reg5 = (uint)iflags;

					exp = Expression.IfThen(
						Expression.Equal(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Equal)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpGreaterR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Greater)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpGreaterEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Greater | FlagsRegisterFlags.Equal))),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpLessR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Less)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpLessEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Less | FlagsRegisterFlags.Equal))),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpZeroRR:
				{
					Operand cnd = this.Memory.DecodeOperand(ref ip, null);
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression cndexp = JitOperand(ref cnd, false);
					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.Equal(cndexp, Expression.Constant(0u)),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}
			case Opcode.JumpNotZeroRR:
				{
					Operand cnd = this.Memory.DecodeOperand(ref ip, null);
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression cndexp = JitOperand(ref cnd, false);
					Expression locexp = JitOperand(ref loc, false);
					Expression eflag = RegisterExpression(NamedRegister.Flag, Cpu.NativeSizeBits, false);
					Expression eip = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, true);

					exp = Expression.IfThen(
						Expression.NotEqual(cndexp, Expression.Constant(0u)),
						Expression.Assign(eip, locexp)
					);

					sequential_not_gauranteed = true;
					break;
				}

			#endregion
			#region Transformative
			case Opcode.AddRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);
					
					exp = Expression.Assign(dstexp, Expression.Add(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.AddFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, 
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Add(
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.SubtractRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Subtract(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.SubtractFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Subtract(
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.MultiplyRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Multiply(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.MultiplyUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Multiply(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.MultiplyFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Multiply(
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.DivideRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Divide(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.DivideUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Divide(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.DivideFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Divide(
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ModulusRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Modulo(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ModulusUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Modulo(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ModulusFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Modulo(
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ShiftLeftRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.LeftShift(leftexp, Expression.Convert(rightexp, typeof(int))));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ShiftRightRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.RightShift(leftexp, Expression.Convert(rightexp, typeof(int))));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ArithmaticShiftRightRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, // TODO: check this
						Expression.Convert(
							Expression.RightShift(
								Expression.Convert(leftexp, typeof(int)), 
								Expression.Convert(rightexp, typeof(int))
							),
							typeof(int)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					throw new NotImplementedException();
				}
			case Opcode.OrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Or(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ExclusiveOrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.ExclusiveOr(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.NotOrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Or(leftexp, Expression.Not(rightexp)));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.AndRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.And(leftexp, rightexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.NotAndRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.And(leftexp, Expression.Not(rightexp)));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.NotRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					exp = Expression.Assign(dstexp, Expression.Not(leftexp));
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.SqrtFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> sqrt = (val) => (float)Math.Sqrt(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(sqrt,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.LogFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					Expression<Func<float, float, float>> log = (val, @base) => (float)Math.Log(val, @base);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(log,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.SinFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> sin = (val) => (float)Math.Sin(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(sin,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.CosFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> cos = (val) => (float)Math.Cos(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(cos,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.TanFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> tan = (val) => (float)Math.Tan(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(tan,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.AsinFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> asin = (val) => (float)Math.Asin(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(asin,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.AcosFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> acos = (val) => (float)Math.Acos(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(acos,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.AtanFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);

					Expression<Func<float, float>> atan = (val) => (float)Math.Atan(val);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(atan,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.Atan2FloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					Expression<Func<float, float, float>> atan2 = (l, r) => (float)Math.Log(l, r);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(atan2,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.PowFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					Expression<Func<float, float, float>> pow = (l, r) => (float)Math.Pow(l, r);
					exp = Expression.Assign(dstexp,
						Expression.Invoke(ReinterpretFloat32AsUInt32Expression,
							Expression.Invoke(pow,
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, leftexp),
								Expression.Invoke(ReinterpretUInt32AsFloat32Expression, rightexp)
							)
						)
					);
					sequential_not_gauranteed = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			#endregion
			default: throw new Exception($"JitCpu: instruction {op} not supported!");
			}

			var ipreg = RegisterExpression(NamedRegister.InstructionPointer, Cpu.NativeSizeBits, false);
			Expression ip_inc = Expression.AddAssign(
				ipreg,
				Expression.Constant(ip - original_ip, typeof(uint))
			);

			// reg0++;
			var tscreg = RegisterExpression(NamedRegister.TimeStampCounter, Cpu.NativeSizeBits, false);
			Expression tsc_inc = Expression.PostIncrementAssign(
				tscreg
			);
			
			if(exp != null)
				exp = Expression.Block(ip_inc, exp, tsc_inc);
			else // nop
				exp = Expression.Block(ip_inc, tsc_inc);

			if (this.Debugger != null)
			{
				Expression<Func<bool>> dbgclock = () => this.Debugger.Clock(this);
				exp = Expression.IfThen(
					Expression.Invoke(dbgclock),
					exp
				);
			}
			
			return (exp, ip - original_ip, sequential_not_gauranteed || this.Debugger != null /*TODO: why is this necessary? it shouldn't be*/);
		}

		ConcurrentQueue<uint> InterruptQueue = new ConcurrentQueue<uint>();
		public override void Interrupt(uint code)
		{
			this.InterruptQueue.Enqueue(code);
		}
	}
}