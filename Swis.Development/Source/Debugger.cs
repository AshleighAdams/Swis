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
		uint? IpEquals = null;
		uint? BasePtrSmaller = null;
		uint? BasePtrEquals = null;
		uint? StackBottom = null;
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
		byte[] _LastStack;

		const uint max_inst_len = 64; // so we can inspect the current instruction without needing to depend on Swis.Development
		public override bool Clock(Cpu cpu)
		{
			if (this.Cpu == null)
				this.Cpu = new WeakReference<Cpu>(cpu);
			if (this.StackBottom == null && cpu.StackPointer != 0)
				this.StackBottom = cpu.StackPointer;

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
					// this is basically run until bp <= currentbp

					this.BasePtrSmaller = cpu.BasePointer;
					this.BasePtrEquals = cpu.BasePointer;
					this.Paused = false;
					return true; // execute self

					/*
					uint eip = cpu.InstructionPointer;
					(Opcode op, _) = cpu.Memory.DisassembleInstruction(ref eip, null);

					if (op == Opcode.CallR)
					{
						this.IpEquals = eip;
						this.Paused = false;
					}
					else // nothing to step over, so step next
					{
						this.Paused = false;
						this.Step = true;
						return true;
					}
					*/
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
				else if (
					(this.Breakpoints.ContainsKey(eip)) ||
					(this.IpEquals != null && this.IpEquals.Value == eip) ||
					(this.BasePtrEquals != null && cpu.BasePointer == this.BasePtrEquals.Value) ||
					(this.BasePtrSmaller != null && cpu.BasePointer < this.BasePtrSmaller.Value))
				{
					this.IpEquals = this.BasePtrEquals = this.BasePtrSmaller = null;
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
				// write the stack
				{
					if (this.StackBottom == null)
						this.WriteLine(""); // stack not yet set up
					else
					{
						if (this._LastStack == null)
							this._LastStack = new byte[128];

						uint sbtm = this.StackBottom.Value;
						uint sptr = cpu.StackPointer - sbtm;

						if (sptr >= this._LastStack.Length)
						{
							int newsz = this._LastStack.Length * 2;
							while (newsz <= sptr)
								newsz *= 2;
							byte[] newls = new byte[newsz];
							Buffer.BlockCopy(this._LastStack, 0, newls, 0, this._LastStack.Length);
							for (int i = this._LastStack.Length; i < newls.Length; i++)
								newls[i] = 0;
							this._LastStack = newls;
						}

						int index = -1;
						int length = 0;

						for (int i = 0; i < sptr; i++)
						{
							byte mem = cpu.Memory[sbtm + (uint)i];
							if (this._LastStack[i] != mem)
							{
								if (index == -1)
									index = i;
								length = (i - index) + 1;
								this._LastStack[i] = mem;
							}
						}

						if (length != 0)
						{
							string base64 = Convert.ToBase64String(this._LastStack, index, length, Base64FormattingOptions.None);
							this.WriteLine($"{sbtm}+{index}: {base64}");
						}
						else
							this.WriteLine($"=");
					}
				}
				
				// write the instruction
				{
					uint len = Math.Min(64, memory.Length - cpu.InstructionPointer);
					ReadOnlySpan<byte> span = memory.Span(cpu.InstructionPointer, len);

					// TODO: use the span
					byte[] arr = new byte[span.Length];
					for (int i = 0; i < span.Length; i++)
						arr[i] = span[i];

					string base64 = Convert.ToBase64String(arr, Base64FormattingOptions.None);
					
					this.WriteLine($"{base64}");
				}
				return step;
			}

			return true;
		}
	}

	[Serializable]
	public class DebugData
	{
		public bool AssemblySourceFile;
		public string AssemblySource;

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

		// what the code here corresponds to, in absolute position.
		// type =
		public Dictionary<uint, (string file, int from, int to, AsmPtrType type)> PtrToAsm;
		public Dictionary<uint, (string file, int from, int to)> AsmToSrc;

		// for asm disasm
		public Dictionary<string, uint> Labels;
		public Dictionary<string, (uint loc, List<(string local, int bp_offset, uint size, string typehint)> locals)> SourceFunctions;

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