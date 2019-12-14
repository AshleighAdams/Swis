using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		private MemoryController _Memory;
		public override MemoryController Memory
		{
			get { return this._Memory; }
			set { this._Memory = new JitCacheInvalidator(this, value); }
		}

		public uint JitCostFactor = 100; // how much slower the first time code is JITed approx is, to prevent abuse
		private Dictionary<uint, (Action λ, uint cycles)> JitCache;
		private uint _JitBlockSize = 16;
		private uint JitCacheFirst;
		private uint JitCacheLast; // track the jitted bounds so as to clear JIT instructions

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

		private (Expression λ, uint len, bool sequential_not_gauranteed) JitInstruction(uint location)
		{
			uint original_ip = location;
			uint ip = original_ip;
			Opcode op = this.Memory.DecodeOpcode(ref ip);

			bool sequential = true;

			Expression exp;
			{
				var decoder = (op >= 0 || op < Opcode.MaxEnum) ? this.OpcodeDecodeTable[(int)op] : null;
				if (decoder == null)
					exp = this.RaiseInterruptExpression(Interrupts.InvalidOpcode, ref sequential);
				else
					exp = decoder(ref ip, ref sequential);
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

			if (exp != null)
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

		private ConcurrentQueue<uint> InterruptQueue = new ConcurrentQueue<uint>();
		public override void Interrupt(uint code)
		{
			this.InterruptQueue.Enqueue(code);
		}
	}
}