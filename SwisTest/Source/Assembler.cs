using System;
using System.Collections.Generic;

namespace Swis
{
	public static class Assembler
	{
		static Dictionary<string, NamedRegister> RegisterMap = new Dictionary<string, NamedRegister>()
		{
			{ "ip", NamedRegister.InstructionPointer },
			{ "sp", NamedRegister.StackPointer },
			{ "cp", NamedRegister.CallstackPointer },
			{ "flags", NamedRegister.Flags },
			{ "pm", NamedRegister.ProtectedMode },
			{ "pi", NamedRegister.ProtectedInterrupt },

			{ "ga", NamedRegister.GeneralA },
			{ "gb", NamedRegister.GeneralB },
			{ "gc", NamedRegister.GeneralC },
			{ "gd", NamedRegister.GeneralD },
			{ "ge", NamedRegister.GeneralE },
			{ "gf", NamedRegister.GeneralF },
			{ "gg", NamedRegister.GeneralG },
			{ "gh", NamedRegister.GeneralH },
			{ "gi", NamedRegister.GeneralI },
			{ "gj", NamedRegister.GeneralJ },
			{ "gk", NamedRegister.GeneralK },
			{ "gl", NamedRegister.GeneralL },

			{ "ta", NamedRegister.TempA },
			{ "tb", NamedRegister.TempB },
			{ "tc", NamedRegister.TempC },
			{ "td", NamedRegister.TempD },
			{ "te", NamedRegister.TempE },
			{ "tf", NamedRegister.TempF },
			{ "tg", NamedRegister.TempG },
			{ "th", NamedRegister.TempH },
			{ "ti", NamedRegister.TempI },
			{ "tj", NamedRegister.TempJ },
			{ "tk", NamedRegister.TempK },
			{ "tl", NamedRegister.TempL },
		};

		static Dictionary<string, Opcode> OpcodeMap = new Dictionary<string, Opcode>()
		{
			{ "nop", Opcode.Nop },
			{ "intR", Opcode.InterruptR },
			{ "intV", Opcode.InterruptV },
			{ "trapR", Opcode.TrapR },
			{ "trapV", Opcode.TrapV },
			{ "halt", Opcode.Halt },
			{ "reset", Opcode.Reset },

			{ "loadRR", Opcode.LoadRR },
			{ "loadRV", Opcode.LoadRV },
			{ "storeRR", Opcode.StoreRR },
			{ "storeVR", Opcode.StoreVR },
			{ "movRR", Opcode.MoveRR },
			{ "movRV", Opcode.MoveRV },
			{ "pushR", Opcode.PushR },
			{ "popR", Opcode.PopR },
			{ "callR", Opcode.CallR },
			{ "callV", Opcode.CallV },
			{ "ret", Opcode.Return },
			{ "jmpR", Opcode.JumpR },
			{ "jmpV", Opcode.JumpV },
			{ "cmpRR", Opcode.CompareRR },
			{ "cmpfRR", Opcode.CompareFloatRR },
			{ "jeR", Opcode.JumpEqualR },
			{ "jeV", Opcode.JumpEqualV },
			{ "jneR", Opcode.JumpNotEqualR },
			{ "jneV", Opcode.JumpNotEqualV },
			{ "jlR", Opcode.JumpLessR },
			{ "jlV", Opcode.JumpLessV },
			{ "jgR", Opcode.JumpGreaterR },
			{ "jgV", Opcode.JumpGreaterV },
			{ "jleR", Opcode.JumpLessR },
			{ "jleV", Opcode.JumpLessV },
			{ "jgeR", Opcode.JumpGreaterEqualR },
			{ "jgeV", Opcode.JumpGreaterEqualV },
			{ "juoR", Opcode.JumpUnderOverFlowR },
			{ "juoV", Opcode.JumpUnderOverFlowV },

			{ "addRRR", Opcode.AddRRR },
			{ "addfRRR", Opcode.AddFloatRRR },
			{ "subRRR", Opcode.SubtractRRR },
			{ "subfRRR", Opcode.SubtractRRR },
			{ "mulRRR", Opcode.MuliplyRRR },
			{ "muluRRR", Opcode.MultiplyUnsignedRRR },
			{ "mulfRRR", Opcode.MultiplyFloatRRR },
			{ "divRRR", Opcode.DivideRRR },
			{ "divuRRR", Opcode.DivideUnsignedRRR },
			{ "divfRRR", Opcode.DivideFloatRRR },
			{ "modRRR", Opcode.ModulusRRR },
			{ "modfRRR", Opcode.ModulusFloatRRR },
			{ "shlRRR", Opcode.ShiftRightRRR },
			{ "shrRRR", Opcode.ShiftLeftRRR },
			{ "ashrRRR", Opcode.ArithmaticShiftRightRRR },
			{ "orRRR", Opcode.OrRRR },
			{ "xorRRR", Opcode.ExclusiveOrRRR },
			{ "norRRR", Opcode.NotOrRRR },
			{ "andRRR", Opcode.AndRRR },
			{ "nandRRR", Opcode.NotAndRRR },
			{ "notRRR", Opcode.NotRRR },
			{ "sqrtRR", Opcode.SqrtRR },
			{ "logRR", Opcode.LogRR },
			{ "sinRR", Opcode.SinRR },
			{ "cosRR", Opcode.CosRR },
			{ "tanRR", Opcode.TanRR },
			{ "asinRR", Opcode.AsinRR },
			{ "acosRR", Opcode.AcosRR },
			{ "atanRR", Opcode.AtanRR },
			{ "atan2RRR", Opcode.Atan2RR },
			{ "powRRR", Opcode.PowRRR },
		};

		public static byte[] Assemble(string asm)
		{
			string[] lines = asm.Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
			List<byte> bin = new List<byte>();

			Dictionary<string, int> found_placeholders = new Dictionary<string, int>();
			List<(string name, int pos, int defined_line)> placeholders = new List<(string, int, int)>();

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();

				int comment = line.IndexOf("//");
				if (comment >= 0)
					line = line.Substring(0, comment);

				string[] words = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

				if (words.Length == 0)
					continue;

				string first = words[0].Trim();
				if (first.EndsWith(':'))
				{
					string lbl = first.Substring(0, first.Length - 1);
					found_placeholders[lbl] = bin.Count;
					continue;
				}
				else if (first == "data")
				{
					string[] split = words[1].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
					string type = split[0].ToLowerInvariant().Trim();
					string value = split[1].Trim();

					if (type == "pad")
					{
						if (!int.TryParse(value, out int padbytes))
							throw new Exception($"{i}: data pad parse error");
						for (int n = 0; n < padbytes; n++)
							bin.Add(0);
					}
					else if (type == "hex")
					{
						if (value.Length % 2 != 0)
							throw new Exception($"{i}: data hex missing nybble");
						
						int read_nybble(char c)
						{
							switch (c)
							{
							case '0': return 0;
							case '1': return 1;
							case '2': return 2;
							case '3': return 3;
							case '4': return 4;
							case '5': return 5;
							case '6': return 6;
							case '7': return 7;
							case '8': return 8;
							case '9': return 9;
							case 'a': return 10;
							case 'b': return 11;
							case 'c': return 12;
							case 'd': return 13;
							case 'e': return 14;
							case 'f': return 15;
							default: throw new Exception($"{i}: data hex has an invalid nybble");
							}
						}

						string lower = value.ToLowerInvariant();
						for (int n = 0; n < value.Length; n += 2)
						{
							byte b = (byte)((read_nybble(lower[n + 0]) << 4) | (read_nybble(lower[n + 1]) << 0));
							bin.Add(b);
						}
					}
					else if (type == "int")
					{
						if (!int.TryParse(value, out int val))
							throw new Exception($"{i}: data int parse error");
						Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
						c.I32 = val;
						bin.Add(c.ByteA);
						bin.Add(c.ByteB);
						bin.Add(c.ByteC);
						bin.Add(c.ByteD);
					}
					else if (type == "uint")
					{
						if (!uint.TryParse(value, out uint val))
							throw new Exception($"{i}: data int parse error");
						Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
						c.U32 = val;
						bin.Add(c.ByteA);
						bin.Add(c.ByteB);
						bin.Add(c.ByteC);
						bin.Add(c.ByteD);
					}
					else if (type == "float")
					{
						if (!float.TryParse(value, out float val))
							throw new Exception($"{i}: data int parse error");
						Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
						c.F32 = val;
						bin.Add(c.ByteA);
						bin.Add(c.ByteB);
						bin.Add(c.ByteC);
						bin.Add(c.ByteD);
					}
					else
						throw new Exception($"{i}: unknown data type {type}");

					continue;
				}

				string instr = first.ToLowerInvariant();
				string[] args = words.Length == 1 ? new string[0] : words[1].Split(',');

				List<byte> instr_bin = new List<byte>() { (byte)Opcode.NoOperation };

				for (int n = 0; n < args.Length; n++)
				{
					string arg = args[n].Trim();

					if (arg[0] >= '0' && arg[0] <= '9')
					{
						instr += 'V';
						if (arg.EndsWith("f"))
						{
							Caster c;
							c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
							c.F32 = float.Parse(arg);
							instr_bin.Add(c.ByteA);
							instr_bin.Add(c.ByteB);
							instr_bin.Add(c.ByteC);
							instr_bin.Add(c.ByteD);
						}
						if (arg.EndsWith("u"))
						{
							Caster c;
							c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
							c.U32 = uint.Parse(arg);
							instr_bin.Add(c.ByteA);
							instr_bin.Add(c.ByteB);
							instr_bin.Add(c.ByteC);
							instr_bin.Add(c.ByteD);
						}
						else
						{
							Caster c;
							c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
							c.I32 = int.Parse(arg);
							instr_bin.Add(c.ByteA);
							instr_bin.Add(c.ByteB);
							instr_bin.Add(c.ByteC);
							instr_bin.Add(c.ByteD);
						}
						// it's a value
					}
					else if (arg.StartsWith("$"))
					{
						instr += 'V';
						int ph_pos = bin.Count + instr_bin.Count;
						placeholders.Add((arg.Substring(1), ph_pos, i));
						instr_bin.Add(0);
						instr_bin.Add(0);
						instr_bin.Add(0);
						instr_bin.Add(0);
					}
					else // a register
					{
						instr += "R";
						arg = arg.ToLowerInvariant();
						if (!RegisterMap.TryGetValue(arg, out var reg))
							throw new Exception($"{i}: unknown register {arg}");
						instr_bin.Add((byte)reg);
					}
				}

				if (!OpcodeMap.TryGetValue(instr, out var op))
					throw new Exception($"{i}: unknown opcode ${instr}");
				instr_bin[0] = (byte)op;

				bin.AddRange(instr_bin);
			}

			// now do the placeholders
			foreach (var ph in placeholders)
			{
				if (!found_placeholders.TryGetValue(ph.name, out int foundpos))
					throw new Exception($"{ph.defined_line}: could not find the label ${ph.name}");
				Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
				c.I32 = foundpos;
				bin[ph.pos + 0] = c.ByteA;
				bin[ph.pos + 1] = c.ByteB;
				bin[ph.pos + 2] = c.ByteC;
				bin[ph.pos + 3] = c.ByteD;
			}

			return bin.ToArray();
		}
	}
}