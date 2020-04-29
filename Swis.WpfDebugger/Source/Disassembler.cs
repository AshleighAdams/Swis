using System;
using System.Collections.Generic;
using System.Text;

namespace Swis.WpfDebugger
{
	public class DebugDisassembler
	{
		private ByteArrayMemoryController _Mem = new ByteArrayMemoryController(new byte[0]);

		private class Instruction
		{
			public string Asm;
			public byte[] Bin;
			public (Opcode opcode, Operand[] operands) Decoded;
		}

		public DebugData DbgGuessed; // used to guess at the disassembled code
		private Dictionary<uint, Instruction> Instructions;
		private Dictionary<uint, string> Labeled;
		public uint[] Registers;

		private (string str, (Opcode opcode, Operand[] operands) rep) Disassemble(byte[] data, out byte[] decoded)
		{
			string ret;
			uint ip = 0;
			_Mem.Memory = data;

			(Opcode opcode, Operand[] operands) = _Mem.DisassembleInstruction(ref ip);
			ret = opcode.Disassemble();
			string pre = " ";
			foreach (var operand in operands)
			{
				ret = $"{ret}{pre}{operand.Disassemble(DbgGuessed)}";
				pre = ", ";
			}

			if (ip == data.Length)
				decoded = data;
			else
			{
				decoded = new byte[ip];
				Buffer.BlockCopy(data, 0, decoded, 0, (int)ip);
			}

			return (ret, (opcode, operands));
		}

		public DebugDisassembler()
		{
			this.Reset();
		}

		public void Reset()
		{
			Instructions = new Dictionary<uint, Instruction>();
			DbgGuessed = new DebugData()
			{
				Labels = new Dictionary<string, uint>(),
			};
			Labeled = new Dictionary<uint, string>();
		}

		private uint last_ip = 0, expected_ip = 0, max_ip = 0;
		private bool avoid_label = true;
		public void Clock(uint ip, byte[] instruction)
		{
			if (ip != expected_ip && !Labeled.ContainsKey(ip) && !avoid_label)
			{
				string lbl = $"$indirect_0x{ip:X}";
				Labeled[ip] = lbl;
				DbgGuessed.Labels[lbl] = ip;

				// re-decompile all of our existing instructions
				foreach (var kv in Instructions)
					(kv.Value.Asm, kv.Value.Decoded) = this.Disassemble(kv.Value.Bin, out kv.Value.Bin);
			}

			Instruction ni = Instructions[ip] = new Instruction();
			(string asm, (Opcode opcode, Operand[] operands)) = this.Disassemble(instruction, out ni.Bin);
			ni.Asm = asm;

			{ // build the ptr to asm up
			}

			max_ip = Math.Max(max_ip, ip);
			expected_ip = ip + (uint)ni.Bin.Length;

			for (uint i = 1; i < ni.Bin.Length; i++)
				if (Instructions.Remove(ip + i))
				{
					Console.Error.WriteLine("warning: instruction changed: either the instruction pointer is unsynchronized or the machine code has been altered.");
				}

			//avoid_label = false;
			try
			{
				avoid_label = true;

				// label the destination
				if (opcode == Opcode.CallR || opcode >= Opcode.JumpR && opcode <= Opcode.JumpNotZeroRR)
				{
					int index = 0;
					if (opcode >= Opcode.JumpZeroRR && opcode <= Opcode.JumpNotZeroRR)
						index = 1;

					Operand dest = operands[index];
					if (dest.Indirect)
					{
						avoid_label = false;
					}
					else if (!Labeled.ContainsKey(dest.Value))
					{
						string prefix;
						if (opcode == Opcode.CallR)
							prefix = "$function";
						else if (dest.Value < ip)
							prefix = $"\t$loop";
						else
							prefix = $"\t$end";

						string lbl = $"{prefix}_0x{dest.Value:X}";
						Labeled[dest.Value] = lbl;
						DbgGuessed.Labels[lbl.Trim()] = dest.Value;

						// to update the new label found
						ni.Asm = this.Disassemble(ni.Bin, out ni.Bin).str;
					}
				}
			}
			finally
			{
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			var ptr_to_asm =
				DbgGuessed.PtrToAsm =
					DbgGuessed.PtrToAsm ?? new Dictionary<uint, (string file, int from, int to, DebugData.AsmPtrType type)>();

			for (uint i = 0; i <= max_ip; i++)
			{
				bool first = true;
				while (i <= max_ip)
				{
					if (Instructions.TryGetValue(i, out var inst))
					{
						if (first)
						{
							if (i != 0)
								sb.AppendLine("\t; ...");
							first = false;
						}

						if (Labeled.TryGetValue(i, out var lbl))
						{
							if (i == 0)
								sb.AppendLine();
							sb.Append(lbl);
							sb.AppendLine(":");
						}

						sb.Append('\t');

						ptr_to_asm[i] = (
							"",
							sb.Length,
							sb.Length + inst.Asm.Length,
							DebugData.AsmPtrType.Instruction); // we don't need to add operands separately

						sb.AppendLine(inst.Asm);
						i += (uint)inst.Bin.Length;
					}
					else
					{
						//if (!first)
						//	sb.AppendLine("\t; ...");
						break;
					}
				}
			}

			return sb.ToString();
		}
	}
}
