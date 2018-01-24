using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
	public static class Assembler
	{
		static Dictionary<string, NamedRegister> RegisterMap = new Dictionary<string, NamedRegister>()
		{
			{ "ip", NamedRegister.InstructionPointer },
			{ "sp", NamedRegister.StackPointer },
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
		public static (byte[] binary, DebugData dbg) Assemble(string asm)
		{
			DebugData dbg = new DebugData();
			dbg.PtrToAsm = new Dictionary<int, (string file, int from, int to, DebugData.AsmPtrType type)>();
			dbg.AsmToSrc = new Dictionary<int, (string file, int from, int to)>();
			dbg.Labels = new Dictionary<string, int>();
			
			string[] lines = asm.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			List<byte> bin = new List<byte>();

			Dictionary<string, int> found_placeholders = dbg.Labels;
			List<(string name, int pos, int defined_line)> placeholders = new List<(string, int, int)>();

			int asm_pos = 0;
			int linenum = 0;
			StringBuilder sb = new StringBuilder();

			bool eof()
			{
				return asm_pos >= asm.Length;
			}

			(string line, int pos) read_line()
			{
				sb.Clear();

				if (eof())
					return (null, -1);

				int basepos = asm_pos;
				bool in_comment = false;
				while (!eof())
				{
					char x = asm[asm_pos];
					asm_pos++;

					if (x == '\n')
						break;
					else if (x == ';')
						in_comment = true;
					else if (!in_comment)
						sb.Append(x);
				}

				linenum++;
				return (sb.ToString(), basepos);
			}

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
				default: throw new Exception($"{linenum}: data hex has an invalid nybble");
				}
			}

			while (true)
			{
				(string line, int pos) = read_line();
				if (line == null)
					break;

				Match m;

				if (string.IsNullOrWhiteSpace(line))
					continue;
				else if ((m = line.Match(@"^\s*(\$[^\s]+) \s* :")).Success)
				{
					found_placeholders[m.Groups[1].Value] = bin.Count;
				}
				//bp_offset += Math.Abs((-bp_offset) % align);
				else if ((m = line.Match(@"^\s*\.align \s+ (\d+)")).Success)
				{
					int align = int.Parse(m.Groups[1].Value);
					int bytes = align - (bin.Count % align);
					bin.AddRange(new byte[bytes]);
				}
				else if ((m = line.Match(@"^\s*\.data \s+ ([A-z]+) \s+ (.+)")).Success)
				{
					string type = m.Groups[1].Value;
					string value = m.Groups[2].Value;

					int from = pos;
					int to = pos + line.Length;

					var dbginfo = ("[string]", from, to, (DebugData.AsmPtrType.None));
					int binpos = bin.Count;

					Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
					switch (type)
					{
					case "pad":
						dbginfo.Item4 = DebugData.AsmPtrType.DataPadding;
						for (int i = 0, count = int.Parse(value); i < count; i++)
							bin.Add(0);
						break;
					case "hex":
						dbginfo.Item4 = DebugData.AsmPtrType.DataHex;
						value = value.Trim().ToLowerInvariant();
						for (int n = 0; n < value.Length; n += 2)
							bin.Add((byte)((read_nybble(value[n + 0]) << 4) | (read_nybble(value[n + 1]) << 0)));
						break;
					case "int":
						dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
						c.I32 = int.Parse(value);
						goto raw_bytes;
					case "uint":
						dbginfo.Item4 = DebugData.AsmPtrType.DataUnsigned;
						c.U32 = uint.Parse(value);
						goto raw_bytes;
					case "float":
						dbginfo.Item4 = DebugData.AsmPtrType.DataFloat;
						c.F32 = float.Parse(value);
						goto raw_bytes;
					case "string":
						dbginfo.Item4 = DebugData.AsmPtrType.DataString;
						value = value.Trim();
						for (int n = 1; n < value.Length - 1; n++)
						{
							char ltr = value[n];
							if (ltr == '"' || ltr == '\'')
								throw new Exception($"{linenum}: data string can't contain a (single) quote");
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
								throw new Exception($"{linenum}: data string error");
							}
							bin.Add((byte)ltr);
						}
						break;

						raw_bytes:
						bin.Add(c.ByteA);
						bin.Add(c.ByteB);
						bin.Add(c.ByteC);
						bin.Add(c.ByteD);
						break;

					default:
						throw new Exception($"{linenum}: unknown data type: {type}");
					}

					dbg.PtrToAsm[binpos] = dbginfo;
					//dbg.PtrToAsm[bin.Count] = ("[string]", oa_pos, oa_to, DebugData.AsmPtrType.Operand);
				}
				else if ((m = line.Match(@"([A-z]+) \s+ ( ([^,]+,?)* )")).Success)
				{
					string op = m.Groups[1].Value;
					int op_pos = pos + m.Groups[1].Index;
					int op_to = op_pos + m.Groups[1].Length;
					string args = m.Groups[2].Value;
					int args_pos = pos + m.Groups[2].Index;
					MatchCollection mc = args.Matches("[^,]+");

					string instr = $"{op.ToLowerInvariant()}{"R".Times(mc.Count)}";
					if (!OpcodeMap.TryGetValue(instr, out var opcode))
						throw new Exception($"{linenum}: unknown opcode {op} that takes {mc.Count} operands.");

					dbg.PtrToAsm[bin.Count] = ("[string]", op_pos, op_to, DebugData.AsmPtrType.Instruction);
					bin.Add((byte)opcode);

					foreach (Match opm in mc)
					{
						string oa = opm.Value;
						int oa_pos = args_pos + opm.Index;
						int oa_to = oa_pos + opm.Length;

						dbg.PtrToAsm[bin.Count] = ("[string]", oa_pos, oa_to, DebugData.AsmPtrType.Operand);

						// encode the operand
						{
							uint regid = 0;
							uint size = 8;
							uint indirection_size = 0;
							uint constant = 0;
							int offset = 0;
							string const_placeholder = null;
							string offset_placeholder = null;

							#region Parse operand
							{
								Match oa_rx = oa.Match(@"^ \s* (?<ptr> (ptr(?<ptr_sz>8|16|32|64))? \s* \[)? \s* (?<base> (?!ptr) [a-zA-Z]+(?<base_sz>8|16|32|64)? | (?<constant> (?<uint>\d+) | (?<int>\-\d+) | (?<float>[-+]?\d+.\d+) ) | (?<label>\$[^\s\]]+) ) \s* ((?<sign>[+-]) \s* (?<offset>[0-9]+|(?<offset_label>\$[^\s\]]+)) )? \s* \]? \s* $");
								string oa_ptr = oa_rx.Groups["ptr"].Value;
								string oa_ptr_sz = oa_rx.Groups["ptr_sz"].Value;
								string oa_base = oa_rx.Groups["base"].Value;
								string oa_base_sz = oa_rx.Groups["base_sz"].Value;
								string oa_constant = oa_rx.Groups["constant"].Value;
								string oa_label = oa_rx.Groups["label"].Value;
								string oa_sign = oa_rx.Groups["sign"].Value;
								string oa_offset = oa_rx.Groups["offset"].Value;
								string oa_offset_label = oa_rx.Groups["offset_label"].Value;

								if (oa_ptr != "")
									indirection_size = oa_ptr_sz == "" ? Register.NativeSize * 8 : uint.Parse(oa_ptr_sz);

								if (oa_constant != "")
								{
									string struint = oa_rx.Groups["uint"].Value;
									string strint = oa_rx.Groups["int"].Value;
									string strfloat = oa_rx.Groups["float"].Value;

									Caster c; c.U32 = 0;
									if (struint != "")
										constant = uint.Parse(struint);
									else if (strint != "")
									{
										c.I32 = int.Parse(strint);
										constant = c.U32;
									}
									else if (strfloat != "")
									{
										c.F32 = float.Parse(strfloat);
										constant = c.U32;
									}

									constant = uint.Parse(oa_constant);
									size = Register.NativeSize * 8;
								}
								else if (oa_label != "")
								{
									constant = 1;
									size = Register.NativeSize * 8;
									const_placeholder = oa_label;
								}
								else
								{
									string reg_only = Regex.Replace(oa_base, @"\d+", "");
									if (!RegisterMap.TryGetValue(reg_only.ToLowerInvariant(), out var reg))
										throw new Exception($"{linenum}: unknown register {oa_base}");
									regid = (uint)reg;

									if (oa_base_sz == "")
										size = Register.NativeSize * 8;
									else
										size = uint.Parse(oa_base_sz);
								}

								if (oa_offset_label != "")
								{
									offset = 1;
									offset_placeholder = oa_offset_label;
								}
								if (oa_offset != "")
									offset = int.Parse($"{oa_sign}{oa_offset}");

							}
							#endregion

							#region Serialize operand
							{
								int enc_size = (int)(Math.Log(size, 2) - 3);
								int enc_indirection_size = indirection_size > 0 ? (int)(Math.Log(indirection_size, 2) - (3 - 1)) : 0;

								{
									byte a = (byte)((regid << 3) | (enc_size << 0));
									bin.Add(a);
								}

								{
									byte b = 0b0000_0000;

									// flags
									b |= (byte)((enc_indirection_size << 5) & 0b1110_0000);

									if (offset != 0 || offset_placeholder != null)
										b |= 0b0001_0000;

									bin.Add(b);
								}

								if (regid == 0)
								{
									// write the const, or the const placeholder
									Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
									c.U32 = constant;

									if (const_placeholder != null)
									{
										// check size == 32
										int ph_pos = bin.Count;
										placeholders.Add((const_placeholder, ph_pos, linenum));
									}

									switch (size)
									{
									default: throw new Exception("");
									case 32:
										bin.Add(c.ByteA);
										bin.Add(c.ByteB);
										bin.Add(c.ByteC);
										bin.Add(c.ByteD);
										break;
									case 16:
										bin.Add(c.ByteA);
										bin.Add(c.ByteB);
										break;
									case 8:
										bin.Add(c.ByteA);
										break;
									}
								}

								if (offset != 0 || offset_placeholder != null)
								{
									Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
									c.I32 = offset;

									if (offset_placeholder != null)
									{
										int ph_pos = bin.Count;
										placeholders.Add((offset_placeholder, ph_pos, linenum));
									}

									bin.Add(c.ByteA);
									bin.Add(c.ByteB);
									bin.Add(c.ByteC);
									bin.Add(c.ByteD);
								}
							}
							#endregion
						}
					}
				}
				else
				{
				}
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

			return (bin.ToArray(), dbg);
		}
	}
}