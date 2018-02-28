using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Swis
{
	public class StreamDebugger : ExternalDebugger
	{
		protected NetworkStream Stream;
		protected DebugData Dbg;
		protected bool Flush;

		bool Paused = false;
		bool First = true;
		ConcurrentDictionary<uint, bool> Breakpoints = new ConcurrentDictionary<uint, bool>();
		uint? RunUntil = null;
		uint? BasePtrSmaller = null;
		bool Step = false;

		WeakReference<Cpu> Cpu = null; // for halt and reset

		StreamReader _Reader;
		ConcurrentQueue<string> ReadQueue = new ConcurrentQueue<string>();
		ConcurrentQueue<byte[]> WriteQueue = new ConcurrentQueue<byte[]>();
		void WriteLine(string line)
		{
			line += "\r\n";
			byte[] data = Encoding.ASCII.GetBytes(line);
			
			this.WriteQueue.Enqueue(data);
		}
		void _IOThread()
		{
			while (this.Stream.CanRead || this.Stream.CanWrite)
			{
				if (this.Stream.DataAvailable && this.ReadQueue.Count < 16)
				{
					string line = this._Reader.ReadLine();

					if (line.StartsWith("break "))
					{
						this.Breakpoints.Clear();
						string[] split = line.Split(' ');
						for (int i = 1; i < split.Length; i++)
							if (uint.TryParse(split[i], out uint bp))
								this.Breakpoints[bp] = true;
					}
					else if (line == "pause")
					{
						if (!this.Paused)
							this.Step = true;
					}
					else if (line == "halt")
					{
						if (this.Cpu.TryGetTarget(out var cpu))
							cpu.Registers[(int)NamedRegister.Flag] |= (uint)FlagsRegisterFlags.Halted;
						this.Paused = false;
						this.First = true;
					}
					else if (line == "reset")
					{
						if (this.Cpu.TryGetTarget(out var cpu))
							cpu.Reset();
						this.Paused = false;
						this.First = true;
					}
					else if (this.Paused) // ignore step-over, step into, and continue unless we're paused
						this.ReadQueue.Enqueue(line);
				}
				else if (this.WriteQueue.TryDequeue(out byte[] tosend))
				{
					this.Stream.WriteTimeout = 1;
					this.Stream.Write(tosend, 0, tosend.Length);

					if (this.Flush)
						this.Stream.Flush();
				}
				else
					System.Threading.Thread.Sleep(33);
			}
		}

		public StreamDebugger(NetworkStream str, DebugData dbg = null, bool flush = false)
		{
			this.Stream = str;
			this.Dbg = dbg;
			this.Flush = flush;

			this._Reader = new StreamReader(str);
			new System.Threading.Thread(this._IOThread).Start();
		}

		uint[] _LastValues;
		
		public override bool Clock(Cpu cpu)
		{
			if (this.Cpu == null)
				this.Cpu = new WeakReference<Cpu>(cpu);

			// if we're paused, wait for instruction
			bool step = false;
			if (this.Paused)
			{
				if (!this.ReadQueue.TryDequeue(out string line))
					return false;

				if (line == "continue")
					this.Paused = false;
				else if (line == "step-into")
				{
					this.Paused = false;
					this.Step = true;
					return true;
				}
				else if (line == "step-over")
				{
					uint eip = cpu.InstructionPointer;
					(Opcode op, _) = cpu.Memory.DisassembleInstruction(ref eip, null);

					if (op == Opcode.CallR)
					{
						this.RunUntil = eip;
						this.Paused = false;
					}
					else // nothing to step over, so step next
					{
						this.Paused = false;
						this.Step = true;
						return true;
					}
				}
				else if (line == "step-out")
				{
					this.BasePtrSmaller = cpu.BasePointer;
					this.Paused = false;
				}
				else
					return false;
			}

			// if we're not paused, check for breakpoints
			if (!this.Paused)
			{
				uint eip = cpu.InstructionPointer;

				if (this.First)
				{
					this.First = false;
					this.Paused = true;
				}
				else if (this.Step)
				{
					this.Step = false;
					this.Paused = true;
				}
				else if (this.RunUntil != null && this.RunUntil.Value == eip)
				{
					this.BasePtrSmaller = null;
					this.RunUntil = null;
					this.Paused = true;
				}
				else if (this.BasePtrSmaller != null && cpu.BasePointer < this.BasePtrSmaller.Value)
				{
					this.BasePtrSmaller = null;
					this.RunUntil = null;
					this.Paused = true;
				}
				else if (this.Breakpoints.TryGetValue(eip, out var _))
				{
					this.BasePtrSmaller = null;
					this.RunUntil = null;
					this.Paused = true;
				}
			}

			// if we stepped-into or hit a breakpoint, send debug info, then hold
			if (this.Paused)
			{
				MemoryController memory = cpu.Memory;


				StringBuilder sb = new StringBuilder();
				uint ip;

				var registers = cpu.Registers;
				// write the registers
				{
					if (this._LastValues == null)
						this._LastValues = new uint[registers.Length];

					string pre = "";
					for (int i = 0; i < registers.Length; i++)
						if (this._LastValues[i] != registers[i])
						{
							this._LastValues[i] = registers[i];
							sb.Append($"{pre}{i}: 0x{registers[i].ToString("X").ToLower()}");
							pre = " ";
						}

					this.WriteLine($"{sb}");
					sb.Clear();
				}
				// write the instruction
				{
					uint original_ip = cpu.InstructionPointer;
					ip = original_ip;
					(Opcode op, Operand[] args) = memory.DisassembleInstruction(ref ip, registers);

					// write the hex rep
					{
						string pre = "";
						for (uint i = original_ip; i < ip; i++, pre = " ")
							sb.Append($"{pre}{memory[i].ToString("X2").ToLowerInvariant()}");
					}

					this.WriteLine($"{sb}");
				}
				return step;
			}

			return true;
		}
	}

	[Serializable]
	public class DebugData
	{
		[Serializable]
		public enum AsmPtrType
		{
			None = 0,
			Instruction = 1,
			Operand,
			DataString,
			DataSigned,
			DataUnsigned,
			DataFloat,
			DataHex,
			DataPadding,
		}

		// what the code here corrosponds to, in absolute position.
		// type =
		public Dictionary<uint, (string file, int from, int to, AsmPtrType type)> PtrToAsm;
		public Dictionary<uint, (string file, int from, int to)> AsmToSrc;

		// for asm disasm
		public Dictionary<string, uint> Labels;

		public static string Serialize(DebugData data)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
		}

		public static DebugData Deserialize(string str)
		{
			return (DebugData)Newtonsoft.Json.JsonConvert.DeserializeObject<DebugData>(str);
		}
	}
}