using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;


namespace Swis
{
	public sealed partial class JittedCpu : Cpu, ICpu
	{
		[NotNull] // TODO: remove this with DI
		private JitCacheInvalidator _Memory;
		public override IMemoryController Memory
		{
			get { return _Memory.Parent; }
			set { _Memory = new JitCacheInvalidator(this, value); }
		}

		public uint JitCostFactor = 100; // how much slower the first time code is JITed approx is, to prevent abuse
		private Dictionary<uint, (Action λ, uint cycles)> JitCache;
		private uint _JitBlockSize = 16;
		private uint JitCacheFirst;
		private uint JitCacheLast; // track the jitted bounds so as to clear JIT instructions

		public JittedCpu(IMemoryController memory)
		{
			Memory = memory;
			JitCache = new Dictionary<uint, (Action λ, uint cycles)>();
			this.ClearJitCache(); // sets up default values
			this.InitializeOpcodeTable();
		}

		public void ClearJitCache()
		{
			JitCacheFirst = uint.MaxValue;
			JitCacheLast = 0;
			JitCache.Clear();
		}

		public override ExternalDebugger? Debugger
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
			get { return _JitBlockSize; }
			set { _JitBlockSize = value; this.ClearJitCache(); }
		}

		private long CycleBank = 0; // don't execute the next instruction block until we can afford it
		public override int Clock(int cycles = 1)
		{
			if (Halted)
				return 0;
			CycleBank += cycles;
			int executed = 0;

			while (CycleBank > 0)
			{
				if (this.HandleInterrupts(InterruptQueue))
					return executed;

				if (!JitCache.TryGetValue(Reg1, out (Action λ, uint cycles) instr))
				{
					uint block_length = 0;
					List<Expression> block_instructions = new List<Expression>();

					uint simulated_ip = Reg1;

					if (simulated_ip < JitCacheFirst)
						JitCacheFirst = simulated_ip;

					for (uint n = 0; n < JitBlockSize; n++)
					{
						if (simulated_ip >= Memory.Length)
							break;

						var jitinst = this.JitInstruction(simulated_ip);

						block_instructions.Add(jitinst.λ);
						block_length += jitinst.len;
						simulated_ip += jitinst.len;

						if (jitinst.sequential_not_gauranteed)
							break;
						if (JitCache.ContainsKey(simulated_ip)) // we have already jitted from this address, so use it
							break;
					}

					if (simulated_ip > JitCacheLast)
						JitCacheLast = simulated_ip;

					var λ = Expression.Lambda<Action>(Expression.Block(block_instructions));

					//Console.WriteLine($"JIT: [{this.Reg1}] = {λ.GetDebugView()}");

					instr = JitCache[Reg1] = (λ.Compile(), (uint)block_instructions.Count);

					// cost in cycles for jitting an instruction
					uint jitcost = (uint)block_instructions.Count * JitCostFactor;

					Reg0 += jitcost;
					CycleBank -= jitcost;
				}

				if (CycleBank >= instr.cycles)
				{
					instr.λ();
					CycleBank -= (int)instr.cycles;
					executed += (int)instr.cycles;
					if (Halted)
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
			Opcode op = Memory.DecodeOpcode(ref ip);

			bool sequential = true;

			Expression exp;
			{
				var decoder = (op >= 0 || op < Opcode.MaxEnum) ? OpcodeDecodeTable[(int)op] : null;
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

			if (Debugger != null)
			{
				Expression<Func<bool>> dbgclock = () => Debugger.Clock(this);
				exp = Expression.IfThen(
					Expression.Invoke(dbgclock),
					exp
				);
			}

			return (exp, ip - original_ip, !sequential || Debugger != null /*TODO: why is this necessary? it shouldn't be*/);
		}

		private ConcurrentQueue<uint> InterruptQueue = new ConcurrentQueue<uint>();
		public override void Interrupt(uint code)
		{
			InterruptQueue.Enqueue(code);
		}
	}
}