﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
	public static class Assembler
	{
		public static Dictionary<string, NamedRegister> RegisterMap = new Dictionary<string, NamedRegister>()
		{
			{ "tsc", NamedRegister.TimeStampCounter },
			{ "ip", NamedRegister.InstructionPointer },
			{ "sp", NamedRegister.StackPointer },
			{ "bp", NamedRegister.BasePointer },
			{ "flag", NamedRegister.Flag },
			{ "pm", NamedRegister.ProtectedMode },
			{ "pi", NamedRegister.ProtectedInterrupt },

			{ "ss", NamedRegister.StackSegment },
			{ "cs", NamedRegister.CodeSegment },
			{ "ds", NamedRegister.DataSegment },
			{ "es", NamedRegister.ExtraSegment },
			{ "fs", NamedRegister.FSegment },
			{ "gs", NamedRegister.GSegment },
			{ "xs", NamedRegister.XtraSegment },

			{ "a", NamedRegister.A },
			{ "b", NamedRegister.B },
			{ "c", NamedRegister.C },
			{ "d", NamedRegister.D },
			{ "e", NamedRegister.E },
			{ "f", NamedRegister.F },
			{ "g", NamedRegister.G },
			{ "h", NamedRegister.H },
			{ "i", NamedRegister.I },
			{ "j", NamedRegister.J },
			{ "k", NamedRegister.K },
			{ "l", NamedRegister.L },
		};

		private static (NamedRegister reg, uint size) ParseRegister(string reg)
		{
			uint size;

			if (reg.EndsWith("s") && reg.Length == 2)
			{ // segment
				size = 32;
			}
			else if (reg.StartsWith("r"))
			{
				size = 64;
				if (reg.EndsWith("x"))
					reg = reg[1..^1];
				else
					reg = reg[1..];
			}
			else if (reg.StartsWith("e"))
			{
				size = 32;
				if (reg.EndsWith("x"))
					reg = reg[1..^1];
				else
					reg = reg[1..];
			}
			else if (reg.EndsWith("x"))
			{
				size = 16;
				reg = reg[0..^1];
			}
			else if (reg.EndsWith("l"))
			{
				size = 8;
				reg = reg[0..^1];
			}
			else throw new Exception(reg);

			return (RegisterMap[reg], size);
		}

		public static Dictionary<string, Opcode> OpcodeMap = new Dictionary<string, Opcode>()
		{
			{ "nop", Opcode.Nop },
			{ "intR", Opcode.InterruptR },
			{ "iret", Opcode.InterruptReturn },
			{ "sti", Opcode.SetInterrupt },
			{ "cli", Opcode.ClearInterrupt },
			{ "sextRRR", Opcode.SignExtendRRR },
			{ "zextRRR", Opcode.ZeroExtendRRR },
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
			{ "cmpfRRR", Opcode.CompareFloatRRR },
			{ "cmpuRR", Opcode.CompareUnsignedRR },
			{ "jeR", Opcode.JumpEqualR },
			{ "jneR", Opcode.JumpNotEqualR },
			{ "jlR", Opcode.JumpLessR },
			{ "jgR", Opcode.JumpGreaterR },
			{ "jleR", Opcode.JumpLessEqualR },
			{ "jgeR", Opcode.JumpGreaterEqualR },
			{ "juoR", Opcode.JumpUnderOverflowR },
			{ "jzRR", Opcode.JumpZeroRR },
			{ "jnzRR", Opcode.JumpNotZeroRR},

			{ "addRRR", Opcode.AddRRR },
			{ "addfRRR", Opcode.AddFloatRRR },
			{ "subRRR", Opcode.SubtractRRR },
			{ "subfRRR", Opcode.SubtractFloatRRR },
			{ "mulRRR", Opcode.MultiplyRRR },
			{ "muluRRR", Opcode.MultiplyUnsignedRRR },
			{ "mulfRRR", Opcode.MultiplyFloatRRR },
			{ "divRRR", Opcode.DivideRRR },
			{ "divuRRR", Opcode.DivideUnsignedRRR },
			{ "divfRRR", Opcode.DivideFloatRRR },
			{ "modRRR", Opcode.ModulusRRR },
			{ "modfRRR", Opcode.ModulusFloatRRR },
			{ "moduRRR", Opcode.ModulusUnsignedRRR },
			{ "shlRRR", Opcode.ShiftLeftRRR },
			{ "shrRRR", Opcode.ShiftRightRRR },
			{ "ashrRRR", Opcode.ArithmaticShiftRightRRR },
			{ "orRRR", Opcode.OrRRR },
			{ "xorRRR", Opcode.ExclusiveOrRRR },
			{ "norRRR", Opcode.NotOrRRR },
			{ "andRRR", Opcode.AndRRR },
			{ "nandRRR", Opcode.NotAndRRR },
			{ "notRR", Opcode.NotRR },
			{ "sqrtfRR", Opcode.SqrtFloatRR },
			{ "logfRRR", Opcode.LogFloatRRR },
			{ "sinfRR", Opcode.SinFloatRR },
			{ "cosfRR", Opcode.CosFloatRR },
			{ "tanfRR", Opcode.TanFloatRR },
			{ "asinfRR", Opcode.AsinFloatRR },
			{ "acosfRR", Opcode.AcosFloatRR },
			{ "atanfRR", Opcode.AtanFloatRR },
			{ "atan2fRRR", Opcode.Atan2FloatRRR },
			{ "powfRRR", Opcode.PowFloatRRR },
		};
		private static char[] _Offset_chars = new char[] { '+', '-' };
		public static (byte[] binary, DebugData dbg) Assemble(string asm)
		{
			Dictionary<string, string> named_patterns = new Dictionary<string, string>();
			Dictionary<string, string> named_patterns_cache = new Dictionary<string, string>();
			string pattern_compile_optional_whitespace(string x)
			{
				if (named_patterns_cache.TryGetValue(x, out string ret))
					return ret;
				return named_patterns_cache[x] = LlvmUtil.PatternCompile(x.Replace(" ", @"\s*"), named_patterns);
			}

			named_patterns["register"] = pattern_compile_optional_whitespace(@"(?<name>[a-zA-Z]+)");
			named_patterns["constant"] = pattern_compile_optional_whitespace(@"0x[a-fA-F0-9]+|\-?[0-9\.]+f|\-?[0-9]+|\$[a-zA-Z0-9._@]+");
			named_patterns["rc"] = pattern_compile_optional_whitespace(@"<register>|<constant>");

			named_patterns["a"] = pattern_compile_optional_whitespace("<rc:a>");
			named_patterns["b"] = pattern_compile_optional_whitespace("<rc:a> [+] <rc:b>");
			named_patterns["c"] = pattern_compile_optional_whitespace("<rc:c> [*] <rc:d>");
			named_patterns["d"] = pattern_compile_optional_whitespace(
				"<rc:a> [+] <rc:b> [+] <rc:c> [*] <rc:d>|" + // a + b + c * d
				"<rc:a> [+] <rc:c> [*] <rc:d> [+] <rc:b>|" + // a + c * d + b
				"<rc:c> [*] <rc:d> [+] <rc:a> [+] <rc:b>");  // c * d + a + b
															 // these must be transformed into the right d form
			named_patterns["d1"] = pattern_compile_optional_whitespace("<rc:a> [+] <rc:b> [+] <rc:c>"); // a + b + c * 1
			named_patterns["d2"] = pattern_compile_optional_whitespace( // to:  // a + 0 + c * d
				"<rc:a> [+] <rc:c> [*] <rc:d>|" + // from: a + c * d
				"<rc:c> [*] <rc:d> [+] <rc:a>");  // from: c * d + a

			named_patterns["forms"] = pattern_compile_optional_whitespace("<a:a>|<b:b>|<c:c>|<d:d>|<d1:d1>|<d2:d2>");

			//named_patterns["___"] = Util.PatternCompile(___, named_patterns);

			DebugData dbg = new DebugData
			{
				AssemblySource = asm,
				AssemblySourceFile = false,
				PtrToAsm = new Dictionary<uint, (string file, int from, int to, DebugData.AsmPtrType type)>(),
				AsmToSrc = new Dictionary<uint, (string file, int from, int to)>(),
				Labels = new Dictionary<string, uint>(),
				SourceFunctions = new Dictionary<string, (uint loc, List<(string local, int bp_offset, uint size, string typehint)> locals)>()
			};

			string[] lines = asm.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			List<byte> bin = new List<byte>();

			Dictionary<string, uint> found_placeholders = dbg.Labels;
			List<(string name, int pos, int defined_line)> placeholders = new List<(string, int, int)>();

			int asm_pos = 0;
			int linenum = 0;
			StringBuilder sb = new StringBuilder();

			bool eof()
			{
				return asm_pos >= asm.Length;
			}

			(string? line, int pos) read_line()
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
				return char.ToLowerInvariant(c) switch
				{
					'0' => 0,
					'1' => 1,
					'2' => 2,
					'3' => 3,
					'4' => 4,
					'5' => 5,
					'6' => 6,
					'7' => 7,
					'8' => 8,
					'9' => 9,
					'a' => 10,
					'b' => 11,
					'c' => 12,
					'd' => 13,
					'e' => 14,
					'f' => 15,
					_ => throw new Exception($"{linenum}: data hex has an invalid nybble"),
				};
			}

			string? source_function = null;

			while (true)
			{
				(string? line, int pos) = read_line();
				if (line == null)
					break;

				Match m;

				if (string.IsNullOrWhiteSpace(line))
					continue;
				else if ((m = line.Match(@"^\s*(\$[^\s]+) \s* :")).Success)
				{
					found_placeholders[m.Groups[1].Value] = (uint)bin.Count;
				}
				//bp_offset += Math.Abs((-bp_offset) % align);
				else if ((m = line.Match(@"^\s*\.align \s+ (\d+)")).Success)
				{
					int align = int.Parse(m.Groups[1].Value);
					if (bin.Count % align != 0)
					{
						int bytes = align - (bin.Count % align);
						bin.AddRange(new byte[bytes]);
					}
				}
				else if ((m = line.Match(@"^\s*\.src loc \s+ (\d+) \s+ (\d+) \s+ (.+) \s* $")).Success)
				{
					int locpos = int.Parse(m.Groups[1].Value);
					int locto = int.Parse(m.Groups[2].Value);
					string locfile = m.Groups[3].Value;

					dbg.AsmToSrc.Add((uint)bin.Count, (locfile, locpos, locto));
				}
				else if ((m = line.Match(@"^\s*\.src \s+ func \s+ ""([^""]+)""\s*$")).Success)
				{
					string funcname = m.Groups[1].Value;

					source_function = funcname;
					dbg.SourceFunctions[funcname] = ((uint)bin.Count, new List<(string, int, uint, string)>());
				}
				else if ((m = line.Match(@"^\s*\.src \s+ local \s+ ""([^""]+)"" \s+ ""([^""]+)"" \s+ ([0-9\-]+) \s+ ([0-9]+)\s*$")).Success)
				{
					if (source_function == null || !dbg.SourceFunctions.ContainsKey(source_function))
						throw new Exception(".src local requires a preceding .src func");
					string localname = m.Groups[1].Value;
					string typehint = m.Groups[2].Value;
					int bp_offset = int.Parse(m.Groups[3].Value);
					uint size = uint.Parse(m.Groups[4].Value);

					dbg.SourceFunctions[source_function].locals.Add((localname, bp_offset, size, typehint));
				}
				else if ((m = line.Match(@"^\s*\.src")).Success)
				{
					throw new Exception("invalid src directive.");
				}
				else if ((m = line.Match(@"^\s*\.data \s+ ([A-Za-z][A-Za-z0-9]*) \s+ (.+)")).Success)
				{
					string type = m.Groups[1].Value;
					string value = m.Groups[2].Value;

					int from = pos;
					int to = pos + line.Length;

					var dbginfo = ("[string]", from, to, (DebugData.AsmPtrType.None));
					uint binpos = (uint)bin.Count;

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

						case "int32":
						case "i32":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.I32 = Int32.Parse(value);
							goto four_bytes;
						case "int16":
						case "i16":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.I32 = Int16.Parse(value);
							goto two_bytes;
						case "int8":
						case "i8":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.I32 = SByte.Parse(value);
							goto one_byte;

						case "uint32":
						case "u32":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.U32 = UInt32.Parse(value);
							goto four_bytes;
						case "uint16":
						case "u16":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.U32 = UInt16.Parse(value);
							goto two_bytes;
						case "uint8":
						case "u8":
							dbginfo.Item4 = DebugData.AsmPtrType.DataSigned;
							c.U32 = Byte.Parse(value);
							goto one_byte;

						case "float":
							dbginfo.Item4 = DebugData.AsmPtrType.DataFloat;
							c.F32 = float.Parse(value);
							goto four_bytes;
						case "ascii":
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

						one_byte:
							bin.Add(c.ByteA);
							break;
						two_bytes:
							bin.Add(c.ByteA);
							bin.Add(c.ByteB);
							break;
						four_bytes:
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
				else if ((m = line.Match(@"([A-z]+) (\s+ (([^,]+,?)+) )?")).Success)
				{
					string op = m.Groups[1].Value;
					int op_pos = pos + m.Groups[1].Index;
					int op_to = op_pos + m.Groups[1].Length;
					string args = m.Groups[3].Value;
					int args_pos = pos + m.Groups[3].Index;
					MatchCollection mc = args.Matches("[^,]+");

					if (mc.Count == 1 && string.IsNullOrWhiteSpace(mc[0].Value)) // HACK
						mc = "".Matches("nope");

					string instr = $"{op.ToLowerInvariant()}{"R".Times(mc.Count)}";
					if (!OpcodeMap.TryGetValue(instr, out var opcode))
						throw new Exception($"{linenum}: unknown opcode {op} that takes {mc.Count} operands.");

					dbg.PtrToAsm[(uint)bin.Count] = ("[string]", op_pos, op_to, DebugData.AsmPtrType.Instruction);
					bin.Add((byte)opcode);

					foreach (Match opm in mc)
					{
						string oa = opm.Value;
						int oa_pos = args_pos + opm.Index;
						int oa_to = oa_pos + opm.Length;

						// convert - x into + -x

						oa = Regex.Replace(oa, @"-(\s*)\-([0-9])", "+$1$2");
						oa = Regex.Replace(oa, @"(?<!^\s*)\-(\s*)([0-9])", "+$1-$2");

						dbg.PtrToAsm[(uint)bin.Count] = ("[string]", oa_pos, oa_to, DebugData.AsmPtrType.Operand);

						// encode the operand
						{
							//(uint regid, uint regsz, uint @const, uint constsz, string constplaceholder)[] parts = new 
							//(uint regid, uint regsz, uint @const, uint constsz, string constplaceholder)[4];
							uint regid_a = 0, regid_b = 0, regid_c = 0, regid_d = 0;
							uint regsz_a = 0, regsz_b = 0, regsz_c = 0, regsz_d = 0;
							uint const_a = 0, const_b = 0, const_c = 0, const_d = 0;
							uint constsz_a = 0, constsz_b = 0, constsz_c = 0, constsz_d = 0;
							string? const_placeholder_a = null, const_placeholder_b = null, const_placeholder_c = null, const_placeholder_d = null;

							uint addressing_mode = 0;
							uint indirection_size = 0;
							uint segmentid = 0;

							#region Parse operand
							{
								string operanducmp = @"^ ((<alphanumeric:segment> :)? <forms:form> |(?<indirection>(ptr<numeric:indirection_size>)? (<alphanumeric:segment> :)? \[) <forms:form> \]) $";

								string operandpttn = pattern_compile_optional_whitespace(operanducmp);
								dynamic? match = oa.PatternMatch(operandpttn, named_patterns);
								if (match == null)
									throw new Exception($"Failed to parse operand: \"{oa}\"");

								if (match.indirection != "")
								{
									if (match.indirection_size == "")
										indirection_size = (uint)ICpu.NativeSizeBits;
									else
									{
										indirection_size = match.indirection_size switch
										{
											"8"  => 8,
											"16" => 16,
											"32" => 32,
											//"64" => 64,
											_ => throw new Exception($"invalid indirection size {match.indirection_size}"),
										};
									}

									uint first = (uint)NamedRegister.StackSegment - 1;
									segmentid = ((string)match.segment).ToLowerInvariant() switch
									{
										"" => 0,
										"ss" => (uint)NamedRegister.StackSegment - first,
										"cs" => (uint)NamedRegister.CodeSegment - first,
										"ds" => (uint)NamedRegister.DataSegment - first,
										"es" => (uint)NamedRegister.ExtraSegment - first,
										"fs" => (uint)NamedRegister.FSegment - first,
										"gs" => (uint)NamedRegister.GSegment - first,
										"xs" => (uint)NamedRegister.XtraSegment - first,
										_ => throw new Exception($"unknown segment: {match.segment}"),
									};
								}

								static void read_operand(string input, out uint regid, out uint regsz, out uint @const, out uint constsz, out string? const_placeholder)
								{
									regid = 0;
									regsz = 0;
									@const = 0;
									constsz = 0;
									const_placeholder = null;

									Caster c; c.U32 = 0;

									if (input.StartsWith("0x"))
									{
										throw new NotImplementedException();
										//return;
									}
									else if (char.IsDigit(input[0]) && input.EndsWith("f"))
									{
										c.F32 = float.Parse(input);
										@const = c.U32;
										constsz = 32;
									}
									else if (input[0] == '-' || char.IsDigit(input[0]))
									{
										c.I32 = int.Parse(input);
										constsz = c.I32 >= 0
											? (uint)Math.Ceiling(Math.Log(c.I32 + 1, 2) + 1)
											: (uint)Math.Ceiling(Math.Log(-c.I32, 2) + 1);
										@const = c.U32;
										return;
									}
									else if (input[0] == '$')
									{
										const_placeholder = input;
										constsz = 32;
										return;
									}
									else
									{
										// must be a register

										var reginf = ParseRegister(input);
										regsz = reginf.size;
										regid = (uint)reginf.reg;
										/*
										string reg_only = Regex.Replace(input, @"\d+", "");
										string size_only = Regex.Match(input, @"\d+").Value;

										if (!RegisterMap.TryGetValue(reg_only.ToLowerInvariant(), out var reg))
											throw new Exception($"{linenum}: unknown register {input}");
										regid = (uint)reg;

										if (size_only == "")
											regsz = (uint)Cpu.NativeSizeBits;
										else
											regsz = uint.Parse(size_only);
										*/
									}
								}

								if (match.form_a != "")
								{
									addressing_mode = 0;
									read_operand(match.form_a_a, out regid_a, out regsz_a, out const_a, out constsz_a, out const_placeholder_a);
								}
								else if (match.form_b != "")
								{
									addressing_mode = 1;
									read_operand(match.form_b_a, out regid_a, out regsz_a, out const_a, out constsz_a, out const_placeholder_a);
									read_operand(match.form_b_b, out regid_b, out regsz_b, out const_b, out constsz_b, out const_placeholder_b);
								}
								else if (match.form_c != "")
								{
									addressing_mode = 2;
									read_operand(match.form_c_c, out regid_c, out regsz_c, out const_c, out constsz_c, out const_placeholder_c);
									read_operand(match.form_c_d, out regid_d, out regsz_d, out const_d, out constsz_d, out const_placeholder_d);
								}
								else if (match.form_d != "")
								{
									addressing_mode = 3;
									read_operand(match.form_d_a, out regid_a, out regsz_a, out const_a, out constsz_a, out const_placeholder_a);
									read_operand(match.form_d_b, out regid_b, out regsz_b, out const_b, out constsz_b, out const_placeholder_b);
									read_operand(match.form_d_c, out regid_c, out regsz_c, out const_c, out constsz_c, out const_placeholder_c);
									read_operand(match.form_d_d, out regid_d, out regsz_d, out const_d, out constsz_d, out const_placeholder_d);
								}
								else if (match.form_d1 != "")
								{
									addressing_mode = 3;
									read_operand(match.form_d1_a, out regid_a, out regsz_a, out const_a, out constsz_a, out const_placeholder_a);
									read_operand(match.form_d1_b, out regid_b, out regsz_b, out const_b, out constsz_b, out const_placeholder_b);
									read_operand(match.form_d1_c, out regid_c, out regsz_c, out const_c, out constsz_c, out const_placeholder_c);
									read_operand("1", out regid_d, out regsz_d, out const_d, out constsz_d, out const_placeholder_d);
								}
								else if (match.form_d2 != "")
								{
									addressing_mode = 3;
									read_operand(match.form_d2_a, out regid_a, out regsz_a, out const_a, out constsz_a, out const_placeholder_a);
									read_operand("0", out regid_b, out regsz_b, out const_b, out constsz_b, out const_placeholder_b);
									read_operand(match.form_d2_c, out regid_c, out regsz_c, out const_c, out constsz_c, out const_placeholder_c);
									read_operand(match.form_d2_d, out regid_d, out regsz_d, out const_d, out constsz_d, out const_placeholder_d);
								}
							}
							#endregion

							#region Serialize operand

							uint enc_indir_size = indirection_size > 0u ? (uint)(Math.Log(indirection_size, 2) - (3 - 1)) : 0u;

							byte master = (byte)(0
								| ((enc_indir_size & 0b111) << 5)
								| ((addressing_mode & 0b11) << 3)
								| ((segmentid & 0b111) << 0)
							);
							bin.Add(master);

							void seralize_operand_rcs(uint regid, uint regsz, uint @const, uint constsz, string? const_placeholder)
							{
								if (regsz == 0)
								{
									uint value5bits;
									uint extra = 0;
									byte extraa = 0, extrab = 0, extrac = 0, extrad = 0;

									if (constsz <= 5 + 2 * 8) // can we store it in <= 2.5 bytes
									{
										value5bits = @const & 0b1_1111;
										@const = @const >> 5;

										if (constsz > 5 + 0 * 8)
										{
											extra = 1;
											extraa = (byte)(@const & 0b1111_1111);
											@const = @const >> 8;
										}

										if (constsz > 5 + 1 * 8)
										{
											extra = 2;
											extrab = (byte)(@const & 0b1111_1111);
											@const = @const >> 8;
										}
									}
									else
									{
										value5bits = 0;
										extra = 4;
										extraa = (byte)(@const & 0b1111_1111);
										@const = @const >> 8;
										extrab = (byte)(@const & 0b1111_1111);
										@const = @const >> 8;
										extrac = (byte)(@const & 0b1111_1111);
										@const = @const >> 8;
										extrad = (byte)(@const & 0b1111_1111);
										@const = @const >> 8;
									}

									uint enc_xtr = extra switch
									{
										0 => 0,
										1 => 1,
										2 => 2,
										4 => 3,
										_ => throw new Exception(extra.ToString()),
									};

									byte constbyte = (byte)(0
										| ((1 & 0b1) << 7) // is_constant
										| ((enc_xtr & 0b11) << 5) // extra_bytes
										| ((value5bits) << 0) // value
									);
									bin.Add(constbyte);

									if (const_placeholder != null) // remember the placeholder pos
										placeholders.Add((const_placeholder, bin.Count, linenum));

									if (extra >= 1)
										bin.Add(extraa);
									if (extra >= 2)
										bin.Add(extrab);
									if (extra >= 3)
										bin.Add(extrac);
									if (extra >= 4)
										bin.Add(extrad);
								}
								else
								{
									uint enc_rsz = (uint)(Math.Log(regsz, 2) - 3);
									byte registerbyte = (byte)(0
										| ((0 & 0b1) << 7) // is_constant
										| ((regid & 0b11111) << 2)
										| ((enc_rsz & 0b11) << 0)
									);
									bin.Add(registerbyte);
								}
							}

							switch (addressing_mode)
							{
								case 0:
									seralize_operand_rcs(regid_a, regsz_a, const_a, constsz_a, const_placeholder_a);
									break;
								case 1:
									seralize_operand_rcs(regid_a, regsz_a, const_a, constsz_a, const_placeholder_a);
									seralize_operand_rcs(regid_b, regsz_b, const_b, constsz_b, const_placeholder_b);
									break;
								case 2:
									seralize_operand_rcs(regid_c, regsz_c, const_c, constsz_c, const_placeholder_c);
									seralize_operand_rcs(regid_d, regsz_d, const_d, constsz_d, const_placeholder_d);
									break;
								case 3:
									seralize_operand_rcs(regid_a, regsz_a, const_a, constsz_a, const_placeholder_a);
									seralize_operand_rcs(regid_b, regsz_b, const_b, constsz_b, const_placeholder_b);
									seralize_operand_rcs(regid_c, regsz_c, const_c, constsz_c, const_placeholder_c);
									seralize_operand_rcs(regid_d, regsz_d, const_d, constsz_d, const_placeholder_d);
									break;
								default: throw new Exception($"bad addressing mode {addressing_mode}");
							}

							#region OLD
							/*
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
							*/
							#endregion
							#endregion
						}
					}
				}
				else
				{
					throw new Exception($"failed to interpret: {line}");
				}
			}

			// now do the placeholders
			foreach (var ph in placeholders)
			{
				if (!found_placeholders.TryGetValue(ph.name, out uint foundpos))
					throw new Exception($"{ph.defined_line}: could not find the label {ph.name}");
				Caster c; c.ByteA = c.ByteB = c.ByteC = c.ByteD = 0;
				c.U32 = foundpos;
				bin[ph.pos + 0] = c.ByteA;
				bin[ph.pos + 1] = c.ByteB;
				bin[ph.pos + 2] = c.ByteC;
				bin[ph.pos + 3] = c.ByteD;
			}

			return (bin.ToArray(), dbg);
		}
	}
}