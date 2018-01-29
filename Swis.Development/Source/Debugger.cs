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

		Register[] _LastValues;
		public override bool Clock(Cpu cpu, MemoryController memory, Register[] registers)
		{
			StringBuilder sb = new StringBuilder();

			{ // write the registers
				if (this._LastValues == null)
					this._LastValues = new Register[registers.Length];

				for (int i = 0; i < registers.Length; i++)
				{
					if (this._LastValues[i].NativeUInt != registers[i].NativeUInt)
					{
						this._LastValues[i].NativeUInt = registers[i].NativeUInt;
						NamedRegister r = (NamedRegister)i;

						if (this.ReverseLabels.TryGetValue((int)registers[i].NativeUInt, out string lbl))
							sb.Append($" {r.Disassemble()} = {lbl}");
						else
						{
							string lblnear = null;
							int lblnearloc = 0;
							for (int n = (int)registers[i].NativeUInt; n-- > 0;)
							{
								if (this.ReverseLabels.TryGetValue(n, out lblnear))
								{
									lblnearloc = n;
									break;
								}
							}

							if(lblnear == null)
								sb.Append($" {r.Disassemble()} = 0x{registers[i].NativeUInt:X}");
							else
								sb.Append($" {r.Disassemble()} = {lblnear} + 0x{registers[i].NativeUInt - lblnearloc:X}");
						}
					}
				}
				Console.WriteLine($"registers:{sb}");
				sb.Clear();
			}
			{ // write the instruction
				uint original_ip = registers[(int)NamedRegister.InstructionPointer].NativeUInt;
				uint ip = original_ip;
				(Opcode op, Cpu.Operand[] args) = memory.DisassembleInstruction(ref ip);


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
						argprefix = ", ";
					}
				}

				string const_len(string input, int length, bool truncate)
				{
					if (input.Length > length)
					{
						if (!truncate)
							length = input.Length;
						else
							input = input.Substring(length - 3) + "...";
					}
					char[] final = new char[length];
					int free_chars = length - input.Length;
					int i;
					for (i = 0; i < free_chars; i++)
						final[i] = ' ';
					for (; i < length; i++)
						final[i] = input[i - free_chars];
					return new string(final);
				}

				Console.WriteLine($"op: {sb}");
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