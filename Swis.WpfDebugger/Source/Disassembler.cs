using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swis.WpfDebugger
{
	public class DebugDisassembler
	{
		ByteArrayMemoryController _Mem = new ByteArrayMemoryController(new byte[0]);

		class Instruction
		{
			public string Asm;
			public byte[] Bin;
			public (Opcode opcode, Operand[] operands) Decoded;
		}
		
		public DebugData DbgGuessed; // used to guess at the disassembled code
		Dictionary<uint, Instruction> Instructions;
		Dictionary<uint, string> Labeled;
		public uint[] Registers;

		(string str, (Opcode opcode, Operand[] operands) rep) Disassemble(byte[] data, out byte[] decoded)
		{
			string ret;
			uint ip = 0;
			this._Mem.Memory = data;

			(Opcode opcode, Operand[] operands) = this._Mem.DisassembleInstruction(ref ip);
			ret = opcode.Disassemble();
			string pre = " ";
			foreach (var operand in operands)
			{
				ret = $"{ret}{pre}{operand.Disassemble(this.DbgGuessed)}";
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
			this.Instructions = new Dictionary<uint, Instruction>();
			this.DbgGuessed = new DebugData()
			{
				Labels = new Dictionary<string, uint>(),
			};
			this.Labeled = new Dictionary<uint, string>();
		}

		uint last_ip = 0, expected_ip = 0, max_ip = 0;
		bool avoid_label = true;
		public void Clock(uint ip, byte[] instruction)
		{
			if (ip != this.expected_ip && !this.Labeled.ContainsKey(ip) && !this.avoid_label)
			{
				string lbl = $"$indirect_0x{ip:X}";
				this.Labeled[ip] = lbl;
				this.DbgGuessed.Labels[lbl] = ip;

				// re-decompile all of our existing instructions
				foreach (var kv in this.Instructions)
					(kv.Value.Asm, kv.Value.Decoded) = this.Disassemble(kv.Value.Bin, out kv.Value.Bin);
			}

			Instruction ni = this.Instructions[ip] = new Instruction();
			(string asm, (Opcode opcode, Operand[] operands)) = this.Disassemble(instruction, out ni.Bin);
			ni.Asm = asm;

			{ // build the ptr to asm up
			}

			this.max_ip = Math.Max(this.max_ip, ip);
			this.expected_ip = ip + (uint)ni.Bin.Length;

			for (uint i = 1; i < ni.Bin.Length; i++)
				if (this.Instructions.Remove(ip + i))
				{
					Console.Error.WriteLine("warning: instruction changed: either the instruction pointer is unsynchronized or the machine code has been altered.");
				}

			//avoid_label = false;
			try
			{
				this.avoid_label = true;
				
				// label the destination
				if (opcode == Opcode.CallR || opcode >= Opcode.JumpR && opcode <= Opcode.JumpNotZeroRR)
				{
					int index = 0;
					if (opcode >= Opcode.JumpZeroRR && opcode <= Opcode.JumpNotZeroRR)
						index = 1;

					Operand dest = operands[index];
					if (dest.Indirect)
					{
						this.avoid_label = false;
					}
					else if(!this.Labeled.ContainsKey(dest.Value))
					{
						string prefix;
						if (opcode == Opcode.CallR)
							prefix = "$function";
						else if (dest.Value < ip)
							prefix = $"\t$loop";
						else
							prefix = $"\t$end";

						string lbl = $"{prefix}_0x{dest.Value:X}";
						this.Labeled[dest.Value] = lbl;
						this.DbgGuessed.Labels[lbl.Trim()] = dest.Value;

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
				this.DbgGuessed.PtrToAsm = 
					this.DbgGuessed.PtrToAsm ?? new Dictionary<uint, (string file, int from, int to, DebugData.AsmPtrType type)>();

			for (uint i = 0; i <= this.max_ip; i++)
			{
				bool first = true;
				while (i <= this.max_ip)
				{
					if (this.Instructions.TryGetValue(i, out var inst))
					{
						if (first)
						{
							if (i != 0)
								sb.AppendLine("\t; ...");
							first = false;
						}

						if (this.Labeled.TryGetValue(i, out var lbl))
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
