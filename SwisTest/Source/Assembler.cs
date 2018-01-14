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
			{ "bp", NamedRegister.BasePointer },
			{ "flags", NamedRegister.Flags },
			{ "pm", NamedRegister.ProtectedMode },
			{ "pi", NamedRegister.ProtectedInterrupt },

			{ "ga", NamedRegister.GeneralA },
			{ "gb", NamedRegister.GeneralB },
			{ "gc", NamedRegister.GeneralC },
			{ "gd", NamedRegister.GeneralD },
			{ "ge", NamedRegister.GeneralE },
			{ "gf", NamedRegister.GeneralF },

			{ "ta", NamedRegister.TempA },
			{ "tb", NamedRegister.TempB },
			{ "tc", NamedRegister.TempC },
			{ "td", NamedRegister.TempD },
			{ "te", NamedRegister.TempE },
			{ "tf", NamedRegister.TempF },
		};

		static Dictionary<string, Opcode> OpcodeMap = new Dictionary<string, Opcode>()
		{
			{ "nop", Opcode.Nop },
			{ "intR", Opcode.InterruptR },
			{ "trapR", Opcode.TrapR },
			{ "halt", Opcode.Halt },
			{ "reset", Opcode.Reset },
			{ "inRR", Opcode.InRR },
			{ "outRR", Opcode.OutRR },

			{ "loadRR", Opcode.LoadRR },
			{ "loadRRR", Opcode.LoadRRR },
			{ "storeRR", Opcode.StoreRR },
			{ "storeRRR", Opcode.StoreRRR },
			{ "movRR", Opcode.MoveRR },
			{ "pushR", Opcode.PushR },
			{ "popR", Opcode.PopR },
			{ "callR", Opcode.CallR },
			{ "ret", Opcode.Return },
			{ "jmpR", Opcode.JumpR },
			{ "cmpRR", Opcode.CompareRR },
			{ "cmpfRR", Opcode.CompareFloatRR },
			{ "jeR", Opcode.JumpEqualR },
			{ "jneR", Opcode.JumpNotEqualR },
			{ "jlR", Opcode.JumpLessR },
			{ "jgR", Opcode.JumpGreaterR },
			{ "jleR", Opcode.JumpLessR },
			{ "jgeR", Opcode.JumpGreaterEqualR },
			{ "juoR", Opcode.JumpUnderOverFlowR },

			{ "addRRR", Opcode.AddRRR },
			{ "addfRRR", Opcode.AddFloatRRR },
			{ "subRRR", Opcode.SubtractRRR },
			{ "subfRRR", Opcode.SubtractRRR },
			{ "mulRRR", Opcode.MultiplyRRR },
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
			{ "notRR", Opcode.NotRR },
			{ "sqrtRR", Opcode.SqrtRR },
			{ "logRRR", Opcode.LogRRR },
			{ "sinRR", Opcode.SinRR },
			{ "cosRR", Opcode.CosRR },
			{ "tanRR", Opcode.TanRR },
			{ "asinRR", Opcode.AsinRR },
			{ "acosRR", Opcode.AcosRR },
			{ "atanRR", Opcode.AtanRR },
			{ "atan2RRR", Opcode.Atan2RRR },
			{ "powRRR", Opcode.PowRRR },
		};

		static char[] _Offset_chars = new char[] { '+', '-' };
		public static (byte[] binary, Dictionary<string, int> labels) Assemble(string asm)
		{
			string[] lines = asm.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			List<byte> bin = new List<byte>();

			Dictionary<string, int> found_placeholders = new Dictionary<string, int>();
			List<(string name, int pos, int defined_line)> placeholders = new List<(string, int, int)>();
			
			for (int i = 0; i < lines.Length; i++)
			{
				int read_nybble(char c)
				{
					switch (char.ToLowerInvariant(c))
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

				string line = lines[i].Trim();

				int comment = line.IndexOf(";");
				if (comment >= 0)
					line = line.Substring(0, comment);

				string[] words = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

				if (words.Length == 0)
					continue;

				string first = words[0].Trim();
				if (first.EndsWith(':'))
				{
					if (!first.StartsWith("$"))
						throw new Exception($"{i}: expected $");
					string lbl = first.Substring(1, first.Length - 2);
					found_placeholders[lbl] = bin.Count;
					continue;
				}
				else if (first == ".data")
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
					else if (type == "string")
					{
						for (int n = 1; n < value.Length - 1; n++)
						{
							char ltr = value[n];
							if (ltr == '"' || ltr == '\'')
								throw new Exception($"{i}: data string can't contain a (single)quote");

							if (ltr == '\\')
							{
								if (value[n + 1] == 'x')
								{
									char a = value[n + 2];
									char b = value[n + 3];
									n += 3;
									bin.Add((byte)((read_nybble(a) << 4) | (read_nybble(b) << 0)));
									continue;
								}

								throw new Exception($"{i}: data string error");
							}
							bin.Add((byte)ltr);
						}
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
					instr += 'R';

					// encode the operand
					{
						uint regid = 0;
						uint size = 8;
						uint indirection_size = 0;
						uint constant = 0;
						int offset = 0;

						string const_placeholder = null;
						string offset_placeholder = null;
						
						if (arg.EndsWith("]"))
						{
							string strindrsz = "";
							if (arg.StartsWith("ptr"))
							{
								arg = arg.Substring("ptr".Length);
								while (arg[0] >= '0' && arg[0] <= '9')
								{
									strindrsz += arg[0];
									arg = arg.Substring(1);
								}
								arg = arg.Trim();
							}
							if (strindrsz == "")
								indirection_size = Register.NativeSize * 8;
							else
								indirection_size = uint.Parse(strindrsz);

							switch (indirection_size)
							{
							default: throw new Exception("invalid indirection size");
							case 32:
							case 16:
							case 8:
								break;
							}

							if (arg.StartsWith("[") != arg.EndsWith("]"))
								throw new Exception("Indirection is conflicting");

							arg = arg.TrimStart('[').TrimEnd(']');
						}

						int offsetsignat = arg.IndexOfAny(_Offset_chars);
						int offset_sign = 0;
						string regstr = null;
						string offsetstr = null;

						if (offsetsignat == -1)
							regstr = arg;
						else
						{
							offset_sign = arg[offsetsignat] == '+' ? 1 : -1;
							regstr = arg.Substring(0, offsetsignat).Trim();
							offsetstr = arg.Substring(offsetsignat + 1).Trim();
						}

						// figure out what regstr is
						{
							if ((regstr[0] >= '0' && regstr[0] <= '9') || regstr[0] == '-')
							{
								if (regstr.EndsWith("f"))
								{
									Caster c;
									c.U32 = 0;
									c.F32 = float.Parse(regstr);
									constant = c.U32;
									size = 32;
								}
								else if (regstr.EndsWith("u"))
								{
									Caster c;
									c.I32 = 0;
									c.U32 = uint.Parse(regstr);
									constant = c.U32;
									size = 32;
								}
								else
								{
									constant = (uint)int.Parse(regstr);
									size = 32;
								}
							}
							else if (regstr[0] == '$')
							{
								const_placeholder = regstr.Substring(1);
								size = 32;
							}
							else // has to be a register
							{
								regstr = regstr.ToLowerInvariant();

								string strsize = "";
								while (regstr[regstr.Length - 1] >= '0' && regstr[regstr.Length - 1] <= '9')
								{
									strsize = regstr[regstr.Length - 1] + strsize;
									regstr = regstr.Substring(0, regstr.Length - 1);
								}

								size = !string.IsNullOrWhiteSpace(strsize) ? uint.Parse(strsize) : Register.NativeSize * 8;

								if (!RegisterMap.TryGetValue(regstr, out var reg))
									throw new Exception($"{i}: unknown register {regstr}");
								regid = (uint)reg;
							}
						}

						// figure out what offsetstr is
						if (offsetstr != null)
						{
							if ((offsetstr[0] >= '0' && offsetstr[0] <= '9') || offsetstr[0] == '-')
								offset = int.Parse(offsetstr) * offset_sign;
							else if (offsetstr[0] == '$')
								offset_placeholder = offsetstr.Substring(1);
							else
								throw new Exception("offset invalid value");
						}

						// serialize it, and remember placeholder positions
						{
							int enc_size = (int)(Math.Log(size, 2) - 3);
							int enc_indirection_size = indirection_size > 0 ? (int)(Math.Log(indirection_size, 2) - (3 - 1)) : 0;

							{
								byte a = (byte)((regid << 3) | (enc_size << 0));
								instr_bin.Add(a);
							}

							{
								byte b = 0b0000_0000;

								// flags
								b |= (byte)((enc_indirection_size << 5) & 0b1110_0000);

								if (offset != 0 || offset_placeholder != null)
									b |= 0b0001_0000;

								instr_bin.Add(b);
							}

							if (regid == 0)
							{
								// write the const, or the const placeholder
								Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
								c.U32 = constant;

								if (const_placeholder != null)
								{
									// check size == 32
									int ph_pos = bin.Count + instr_bin.Count;
									placeholders.Add((const_placeholder, ph_pos, i));
								}

								switch (size)
								{
								default: throw new Exception("");
								case 32:
									instr_bin.Add(c.ByteA);
									instr_bin.Add(c.ByteB);
									instr_bin.Add(c.ByteC);
									instr_bin.Add(c.ByteD);
									break;
								case 16:
									instr_bin.Add(c.ByteA);
									instr_bin.Add(c.ByteB);
									break;
								case 8:
									instr_bin.Add(c.ByteA);
									break;
								}
							}

							if (offset != 0 || offset_placeholder != null)
							{
								Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
								c.I32 = offset;

								if (offset_placeholder != null)
								{
									int ph_pos = bin.Count + instr_bin.Count;
									placeholders.Add((offset_placeholder, ph_pos, i));
								}

								instr_bin.Add(c.ByteA);
								instr_bin.Add(c.ByteB);
								instr_bin.Add(c.ByteC);
								instr_bin.Add(c.ByteD);
							}
						}
					}

					// old
					{
						/*
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

							string strsize = "";
							while (arg[arg.Length - 1] >= '0' && arg[arg.Length - 1] <= '9')
							{
								strsize = arg[arg.Length - 1] + strsize;
								arg = arg.Substring(0, arg.Length - 1);
							}
							int size = !string.IsNullOrWhiteSpace(strsize) ? int.Parse(strsize) : Register.NativeSize * 8;

							if (!RegisterMap.TryGetValue(arg, out var reg))
								throw new Exception($"{i}: unknown register {arg}");

							int regid = ((int)reg << 2) | (int)(Math.Log(size, 2) - 3);
							instr_bin.Add((byte)regid);
						}
						*/
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
					throw new Exception($"{ph.defined_line}: could not find the label {ph.name}");
				Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
				c.I32 = foundpos;
				bin[ph.pos + 0] = c.ByteA;
				bin[ph.pos + 1] = c.ByteB;
				bin[ph.pos + 2] = c.ByteC;
				bin[ph.pos + 3] = c.ByteD;
			}

			return (bin.ToArray(), found_placeholders);
		}
	}
}