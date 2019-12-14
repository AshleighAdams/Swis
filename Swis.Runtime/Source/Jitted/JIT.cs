using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

using static Swis.JittedCpu.ReinterpretCastExpressionHelpers;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
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
			this.InitializeOpcodeTable();
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
						
						var jitinst = this.JitInstruction(simulated_ip);

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

			bool sequential = true;

			Expression exp = null;

			switch (op)
			{
				default:
					{
						if (op < 0 || op > Opcode.MaxEnum)
							throw new Exception();

						var func = OpcodeTable[(int)op];
						if (func == null)
							throw new Exception();

						exp = func(ref ip, ref sequential);
					} break;
			#region Flow
			case Opcode.CallR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);

					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
					Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
					Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);

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

					sequential = false;
					break;
				}
			case Opcode.Return:
				{
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
					Expression esp = this.ReadWriteRegisterExpression(NamedRegister.StackPointer);
					Expression ebp = this.ReadWriteRegisterExpression(NamedRegister.BasePointer);

					Expression esp_ptr = this.PointerExpression(esp, Cpu.NativeSizeBits);

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

					sequential = false;
					break;
				}
			case Opcode.JumpR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);

					exp = Expression.Assign(this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer), locexp);

					sequential = false;
					break;
				}
			case Opcode.CompareRR:
				{
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

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

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);
					Expression orderedexp = this.ReadOperandExpression(ref ordered);

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
					
					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);
					
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

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
					
					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Equal)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpNotEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

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

					sequential = false;
					break;
				}
			case Opcode.JumpGreaterR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Greater)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpGreaterEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Greater | FlagsRegisterFlags.Equal))),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpLessR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)FlagsRegisterFlags.Less)),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpLessEqualR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.NotEqual(
							Expression.And(eflag, Expression.Constant((uint)(FlagsRegisterFlags.Less | FlagsRegisterFlags.Equal))),
							Expression.Constant(0u)
						),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpZeroRR:
				{
					Operand cnd = this.Memory.DecodeOperand(ref ip, null);
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression cndexp = this.ReadOperandExpression(ref cnd);
					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.Equal(cndexp, Expression.Constant(0u)),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}
			case Opcode.JumpNotZeroRR:
				{
					Operand cnd = this.Memory.DecodeOperand(ref ip, null);
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression cndexp = this.ReadOperandExpression(ref cnd);
					Expression locexp = this.ReadOperandExpression(ref loc);
					Expression eflag = this.ReadWriteRegisterExpression(NamedRegister.Flag);
					Expression eip = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);

					exp = Expression.IfThen(
						Expression.NotEqual(cndexp, Expression.Constant(0u)),
						Expression.Assign(eip, locexp)
					);

					sequential = false;
					break;
				}

			#endregion
			#region Transformative
			case Opcode.AddRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);
					
					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Add(leftexp, rightexp));
					break;
				}
			case Opcode.AddFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, 
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Add(
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.SubtractRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Subtract(leftexp, rightexp));
					break;
				}
			case Opcode.SubtractFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Subtract(
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.MultiplyRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Multiply(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					break;
				}
			case Opcode.MultiplyUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Multiply(leftexp, rightexp));
					break;
				}
			case Opcode.MultiplyFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Multiply(
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.DivideRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Divide(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					break;
				}
			case Opcode.DivideUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Divide(leftexp, rightexp));
					break;
				}
			case Opcode.DivideFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Divide(
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.ModulusRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretInt32AsUInt32Expression,
							Expression.Modulo(
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, leftexp, Expression.Constant(left.ValueSize)),
								Expression.Invoke(ReinterpretUInt32AsInt32Expression, rightexp, Expression.Constant(right.ValueSize))
							)
						)
					);
					break;
				}
			case Opcode.ModulusUnsignedRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Modulo(leftexp, rightexp));
					break;
				}
			case Opcode.ModulusFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Modulo(
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.ShiftLeftRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.LeftShift(leftexp, Expression.Convert(rightexp, typeof(int))));
					break;
				}
			case Opcode.ShiftRightRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.RightShift(leftexp, Expression.Convert(rightexp, typeof(int))));
					break;
				}
			case Opcode.ArithmaticShiftRightRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, // TODO: check this
						Expression.Convert(
							Expression.RightShift(
								Expression.Convert(leftexp, typeof(int)), 
								Expression.Convert(rightexp, typeof(int))
							),
							typeof(int)
						)
					);
					throw new NotImplementedException();
				}
			case Opcode.OrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Or(leftexp, rightexp));
					break;
				}
			case Opcode.ExclusiveOrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.ExclusiveOr(leftexp, rightexp));
					break;
				}
			case Opcode.NotOrRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Or(leftexp, Expression.Not(rightexp)));
					break;
				}
			case Opcode.AndRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.And(leftexp, rightexp));
					break;
				}
			case Opcode.NotAndRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.And(leftexp, Expression.Not(rightexp)));
					break;
				}
			case Opcode.NotRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					exp = this.WriteOperandExpression(ref dst, ref sequential, Expression.Not(leftexp));
					break;
				}
			case Opcode.SqrtFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> sqrt = (val) => (float)Math.Sqrt(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(sqrt,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.LogFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					Expression<Func<float, float, float>> log = (val, @base) => (float)Math.Log(val, @base);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(log,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.SinFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> sin = (val) => (float)Math.Sin(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(sin,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.CosFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> cos = (val) => (float)Math.Cos(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(cos,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.TanFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> tan = (val) => (float)Math.Tan(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(tan,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.AsinFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> asin = (val) => (float)Math.Asin(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(asin,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.AcosFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> acos = (val) => (float)Math.Acos(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(acos,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.AtanFloatRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);

					Expression<Func<float, float>> atan = (val) => (float)Math.Atan(val);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(atan,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp)
							)
						)
					);
					break;
				}
			case Opcode.Atan2FloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					Expression<Func<float, float, float>> atan2 = (l, r) => (float)Math.Log(l, r);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(atan2,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			case Opcode.PowFloatRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression leftexp = this.ReadOperandExpression(ref left);
					Expression rightexp = this.ReadOperandExpression(ref right);

					Expression<Func<float, float, float>> pow = (l, r) => (float)Math.Pow(l, r);
					exp = this.WriteOperandExpression(ref dst, ref sequential,
						Expression.Invoke(ReinterpretCast<float, uint>.Expression,
							Expression.Invoke(pow,
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, leftexp),
								Expression.Invoke(ReinterpretCast<uint, float>.Expression, rightexp)
							)
						)
					);
					break;
				}
			#endregion
			}

			var ipreg = this.ReadWriteRegisterExpression(NamedRegister.InstructionPointer);
			Expression ip_inc = Expression.AddAssign(
				ipreg,
				Expression.Constant(ip - original_ip, typeof(uint))
			);

			// reg0++;
			var tscreg = this.ReadWriteRegisterExpression(NamedRegister.TimeStampCounter);
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
			
			return (exp, ip - original_ip, !sequential || this.Debugger != null /*TODO: why is this necessary? it shouldn't be*/);
		}

		ConcurrentQueue<uint> InterruptQueue = new ConcurrentQueue<uint>();
		public override void Interrupt(uint code)
		{
			this.InterruptQueue.Enqueue(code);
		}
	}
}