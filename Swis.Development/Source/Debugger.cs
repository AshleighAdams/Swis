using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Swis
{
	public class RemoteDebugger : ExternalDebugger
	{
		protected NetworkStream Stream;
		protected DebugData? Dbg;
		protected bool Flush;
		private bool Paused = false;
		private bool First = true;
		private ConcurrentDictionary<uint, bool> Breakpoints = new ConcurrentDictionary<uint, bool>();
		private uint? IpEquals = null;
		private uint? BasePtrSmaller = null;
		private uint? BasePtrEquals = null;
		private uint? StackBottom = null;
		private bool Step = false;
		private WeakReference<Cpu> Cpu = new WeakReference<Cpu>(null!); // for halt and reset

		private StreamReader _Reader;
		private ConcurrentQueue<string> ReadQueue = new ConcurrentQueue<string>();
		private ConcurrentQueue<byte[]> WriteQueue = new ConcurrentQueue<byte[]>();

		private void WriteLine(string line)
		{
			line += "\r\n";
			byte[] data = Encoding.ASCII.GetBytes(line);

			WriteQueue.Enqueue(data);
		}

		private void IOThread() // TODO: make this async
		{
			while (Stream.CanRead || Stream.CanWrite)
			{
				bool idle = true;

				if (Cpu is null)
					break;

				if (Stream.DataAvailable && ReadQueue.Count < 16)
				{
					idle = false;
					string line = _Reader.ReadLine();

					if (line.StartsWith("break "))
					{
						Breakpoints.Clear();
						string[] split = line.Split(' ');
						for (int i = 1; i < split.Length; i++)
							if (uint.TryParse(split[i], out uint bp))
								Breakpoints[bp] = true;
					}
					else if (line == "pause")
					{
						if (!Paused)
							Step = true;
					}
					else if (line == "halt")
					{
						if (Cpu.TryGetTarget(out var cpu))
							cpu.ProtectedMode |= (uint)ProtectedModeRegisterFlags.Halted;
						Paused = false;
						First = true;
					}
					else if (line == "reset")
					{
						if (Cpu.TryGetTarget(out var cpu))
							cpu.Reset();
						Paused = false;
						First = true;
					}
					else if (Paused) // ignore step-over, step into, and continue unless we're paused
						ReadQueue.Enqueue(line);
				}

				if (WriteQueue.TryDequeue(out byte[] tosend))
				{
					idle = false;
					Stream.WriteTimeout = 10;
					Stream.Write(tosend, 0, tosend.Length);

					if (Flush)
						Stream.Flush();
				}

				if (idle)
					System.Threading.Thread.Sleep(33);
			}
		}

		public RemoteDebugger(NetworkStream str, DebugData? dbg = null, bool flush = true)
		{
			Stream = str;
			Dbg = dbg;
			Flush = flush;

			_Reader = new StreamReader(str);
			new System.Threading.Thread(this.IOThread).Start();
		}

		private uint[]? _LastValues;
		private byte[]? _LastStack;
		private const uint max_inst_len = 64; // so we can inspect the current instruction without needing to depend on Swis.Development
		public override bool Clock(Cpu cpu)
		{
			if (Cpu == null)
				Cpu = new WeakReference<Cpu>(cpu);
			if (StackBottom == null && cpu.StackPointer != 0)
				StackBottom = cpu.StackPointer;

			// if we're paused, wait for instruction
			bool step = false;
			if (Paused)
			{
				if (!ReadQueue.TryDequeue(out string line))
					return false;

				if (line == "continue")
					Paused = false;
				else if (line == "step-into")
				{
					Paused = false;
					Step = true;
					return true;
				}
				else if (line == "step-over")
				{
					// this is basically run until bp <= currentbp

					BasePtrSmaller = cpu.BasePointer;
					BasePtrEquals = cpu.BasePointer;
					Paused = false;
					return true; // execute self
				}
				else if (line == "step-out")
				{
					BasePtrSmaller = cpu.BasePointer;
					Paused = false;
				}
				else
					return false;
			}

			// if we're not paused, check for breakpoints
			if (!Paused)
			{
				uint eip = cpu.InstructionPointer;

				if (First)
				{
					First = false;
					Paused = true;
				}
				else if (Step)
				{
					Step = false;
					Paused = true;
				}
				else if (
					(Breakpoints.ContainsKey(eip)) ||
					(IpEquals != null && IpEquals.Value == eip) ||
					(BasePtrEquals != null && cpu.BasePointer == BasePtrEquals.Value) ||
					(BasePtrSmaller != null && cpu.BasePointer < BasePtrSmaller.Value))
				{
					IpEquals = BasePtrEquals = BasePtrSmaller = null;
					Paused = true;
				}
			}

			// if we stepped-into or hit a breakpoint, send debug info, then hold
			if (Paused)
			{
				IMemoryController memory = cpu.Memory;

				StringBuilder sb = new StringBuilder();

				var registers = cpu.Registers;
				// write the registers
				{
					if (_LastValues == null)
						_LastValues = new uint[registers.Count];

					string pre = "";
					for (int i = 0; i < registers.Count; i++)
						if (_LastValues[i] != registers[i])
						{
							_LastValues[i] = registers[i];
							sb.Append($"{pre}{i}: 0x{registers[i].ToString("X").ToLower()}");
							pre = " ";
						}

					this.WriteLine($"{sb}");
					sb.Clear();
				}
				// write the stack
				{
					if (StackBottom == null)
						this.WriteLine(""); // stack not yet set up
					else
					{
						if (_LastStack == null)
							_LastStack = new byte[128];

						uint sbtm = StackBottom.Value;
						uint sptr = cpu.StackPointer - sbtm;

						if (sptr >= _LastStack.Length)
						{
							int newsz = _LastStack.Length * 2;
							while (newsz <= sptr)
								newsz *= 2;
							byte[] newls = new byte[newsz];
							Buffer.BlockCopy(_LastStack, 0, newls, 0, _LastStack.Length);
							for (int i = _LastStack.Length; i < newls.Length; i++)
								newls[i] = 0;
							_LastStack = newls;
						}

						int index = -1;
						int length = 0;

						for (int i = 0; i < sptr; i++)
						{
							byte mem = cpu.Memory[sbtm + (uint)i];
							if (_LastStack[i] != mem)
							{
								if (index == -1)
									index = i;
								length = (i - index) + 1;
								_LastStack[i] = mem;
							}
						}

						if (length != 0)
						{
							string base64 = Convert.ToBase64String(_LastStack, index, length, Base64FormattingOptions.None);
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
#pragma warning disable CA2235 // Mark all non-serializable fields
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
	// TODO: this?
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
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning restore CA2235 // Mark all non-serializable fields
}