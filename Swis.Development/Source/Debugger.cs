using System;
using System.Collections.Generic;
using System.Text;

namespace Swis
{
	public class StreamDebugger : ExternalDebugger
	{
		protected System.IO.TextWriter Stream;
		protected DebugData Dbg;
		protected Dictionary<int, string> ReverseLabels = new Dictionary<int, string>();

		public StreamDebugger(System.IO.TextWriter stream, DebugData dbg)
		{
			this.Stream = stream;
			this.Dbg = dbg;

			if(dbg != null && dbg.Labels != null)
				foreach (var kv in dbg.Labels)
					this.ReverseLabels[kv.Value] = kv.Key;
		}

		uint[] _LastValues;
		bool ShowRegisters = false;

		uint? RunUntil = null;

		public override bool Clock(Cpu cpu)
		{
			MemoryController memory = cpu.Memory;

			if (this.RunUntil != null)
			{
				if (this.RunUntil.Value != cpu.InstructionPointer)
					return true;
				else
					this.RunUntil = null;
			}
			StringBuilder sb = new StringBuilder();
			uint ip;

			var registers = cpu.Registers;

			{ // write the registers
				if (this._LastValues == null)
					this._LastValues = new uint[registers.Length];

				for (int i = 0; i < registers.Length; i++)
				{
					// this is the time stamp counter, so don't show it, it is implicit
					if (i == 0 && this._LastValues[i] + 1 == registers[i])
						this._LastValues[i] = cpu.Registers[i];
					else if (this._LastValues[i] != registers[i])
					{
						this._LastValues[i] = registers[i];
						NamedRegister r = (NamedRegister)i;

						if (this.ReverseLabels.TryGetValue((int)registers[i], out string lbl))
							sb.Append($" {r.Disassemble()} = {lbl}");
						else
						{
							string lblnear = null;
							int lblnearloc = 0;
							for (int n = (int)registers[i]; n-- > 0;)
							{
								if (this.ReverseLabels.TryGetValue(n, out lblnear))
								{
									lblnearloc = n;
									break;
								}
							}

							if(lblnear == null)
								sb.Append($" {r.Disassemble()} = 0x{registers[i]:X}");
							else
								sb.Append($" {r.Disassemble()} = {lblnear} + 0x{registers[i] - lblnearloc:X}");
						}
					}
				}
				if(this.ShowRegisters)
					Console.WriteLine($"registers:{sb}");
				sb.Clear();
			}
			{ // write the instruction
				uint original_ip = cpu.InstructionPointer;
				ip = original_ip;
				(Opcode op, Operand[] args) = memory.DisassembleInstruction(ref ip, registers);
				
				sb.Append(op.Disassemble());

				if (args == null)
					sb.Append(" ???");
				else
				{
					string argprefix = " ";
					foreach (var arg in args)
					{
						sb.Append(argprefix);
						sb.Append(arg.Disassemble(this.Dbg));
						//sb.Append($" /*{arg.Signed}*/");
						argprefix = ", ";
					}
					
					argprefix = " ; operand values: ";
					foreach (var arg in args)
					{
						sb.Append(argprefix);
						sb.Append($"{arg.Signed}");
						argprefix = ", ";
					}
				}
				
				Console.WriteLine($"{Convert.ToString(original_ip, 16).ToLowerInvariant()}:\t{sb}");
			}

			//string rl = Console.ReadLine();
			//if (rl == "o")
			//	this.RunUntil = ip;
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
		public Dictionary<int, (string file, int from, int to, AsmPtrType type)> PtrToAsm;
		public Dictionary<int, (string file, int from, int to)> AsmToSrc;

		// for asm disasm
		public Dictionary<string, int> Labels;

		public static string Serialize(DebugData data)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
		}

		public DebugData Deserialize(string str)
		{
			return (DebugData)Newtonsoft.Json.JsonConvert.DeserializeObject(str);
		}
	}
}