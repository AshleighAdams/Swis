using ScintillaNET;
using ScintillaNET.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;
using SStyle = ScintillaNET.Style;

namespace Swis.WpfDebugger
{
	public class SymbolicButton : Button
	{
		public static readonly DependencyProperty SymbolicColorAProperty = DependencyProperty.Register("SymbolicColorA", typeof(Brush), typeof(SymbolicButton), new PropertyMetadata(Brushes.Crimson));
		public static readonly DependencyProperty SymbolicColorBProperty = DependencyProperty.Register("SymbolicColorB", typeof(Brush), typeof(SymbolicButton), new PropertyMetadata(Brushes.Cyan));
		public static readonly DependencyProperty SymbolicColorCProperty = DependencyProperty.Register("SymbolicColorC", typeof(Brush), typeof(SymbolicButton), new PropertyMetadata(Brushes.Green));

		public Brush SymbolicColorA
		{
			get { return (Brush)this.GetValue(SymbolicColorAProperty); }
			set { this.SetValue(SymbolicColorAProperty, value); }
		}
		public Brush SymbolicColorB
		{
			get { return (Brush)this.GetValue(SymbolicColorBProperty); }
			set { this.SetValue(SymbolicColorBProperty, value); }
		}
		public Brush SymbolicColorC
		{
			get { return (Brush)this.GetValue(SymbolicColorCProperty); }
			set { this.SetValue(SymbolicColorCProperty, value); }
		}

		public SymbolicButton() : base()
		{
			//this.BorderThickness = new Thickness(0);
			//this.Background = Brushes.Transparent;

		}
	}

	public partial class MainWindow : System.Windows.Window
	{
		private string VisualizeValue(uint stack_base, uint base_ptr, int bp_offset, string type)
		{
			int pos = (int)(base_ptr + bp_offset - stack_base);
			uint stack_ptr = Registers[(int)NamedRegister.StackPointer] - stack_base;

			if (pos > stack_ptr || pos < 0)
				return "<not in stack>";

			if (type.EndsWith("*"))
			{
				uint ptr = BitConverter.ToUInt32(StackData, pos);

				if (ptr > stack_base && ptr < stack_base + stack_ptr)
				{
					return $"0x{ptr.ToString("X").ToLowerInvariant()}: {this.VisualizeValue(stack_base, base_ptr, (int)(stack_base + bp_offset - pos), type.Substring(0, type.Length - 1))}";
				}
				else
					return $"0x{ptr.ToString("X").ToLowerInvariant()}";
			}

			//if (type == "char*" || type == "i8*")
			//{
			//	// see if the value falls on the stack
			//	StringBuilder ret = new StringBuilder();
			//	for (int i = pos; i < 128 && i < stack_ptr && this.StackData[i] != '\0'; i++)
			//		ret.Append((char)this.StackData[i]);
			//	return ret.ToString();
			//}

			else if (type == "int" || type == "int32" || type == "i32")
				return BitConverter.ToInt32(StackData, pos).ToString().ToLowerInvariant();
			else if (type == "uint" || type == "uint32" || type == "u32")
				return BitConverter.ToUInt32(StackData, pos).ToString().ToLowerInvariant();
			else if (type == "char" || type == "i8")
				return ((char)StackData[pos]).ToString();

			return "?";
		}

		private ScintillaWPF AssemblyEditor, DisassemblyEditor;
		private DebugData _Dbg = null;

		private DebugData DebugInfo
		{
			get
			{
				return _Dbg;
			}
			set
			{
				DocumentPane.Children.Clear();
				_Dbg = value;
				AssemblyEditor = this.CreateEditor("Assembly", _Dbg.AssemblySource, "asm");

				DocumentPane.Children.Add(new AvalonDock.Layout.LayoutDocument()
				{
					Title = "Disassembly",
					ToolTip = "Disassembly",
					Content = DisassemblyEditor,
					CanClose = false,
				});

				AsmToPtr = new Dictionary<int, uint>();
				foreach (var kv in value.PtrToAsm)
					AsmToPtr[kv.Value.from] = kv.Key;

				// update the asm
				this.Clock();
			}
		}

		private Dictionary<int, uint> AsmToPtr = null;
		private uint? StackBase = null;
		private byte[] StackData = new byte[128];
		private uint[] Registers = new uint[32];
		private List<uint> Breakpoints = new List<uint>();

		private void SendBreakpoints()
		{
			string s = "";
			foreach (uint bp in Breakpoints)
				s += $" {bp}";
			ConnectionWriter?.Invoke($"break {s}");
		}

		private bool Connected { get; set; }
		private bool Running { get; set; }
		public bool Autostepping { get; private set; }

		private void UpdateState()
		{
			bool running = Running || Autostepping;
			ContinueButton.IsEnabled = Connected && !running;
			PauseButton.IsEnabled = Connected && running;
			StopButton.IsEnabled = Connected;
			ResetButton.IsEnabled = Connected;
			StepInButton.IsEnabled = Connected && !running;
			StepOverButton.IsEnabled = Connected && !running;
			StepOutButton.IsEnabled = Connected && !running;

			StatusBar.Background
				= Connected
				? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xca, 0x51, 0x00))
				: new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x7a, 0xcc));

			if ((!Connected || Running == true) && AssemblyEditor != null)
			{
				// clear the current position
				AssemblyEditor.IndicatorCurrent = INDICATOR_AT;
				AssemblyEditor.IndicatorClearRange(0, AssemblyEditor.TextLength);
				AssemblyEditor.MarkerDeleteAll(AT_MARKER);
			}
		}

		private DebugDisassembler _Disassembler = new DebugDisassembler();
		private void Clock(byte[] newinstr = null)
		{
			uint ip = Registers[(int)NamedRegister.InstructionPointer];

			if (AssemblyEditor != null)
			{
				AssemblyEditor.IndicatorCurrent = INDICATOR_AT;
				AssemblyEditor.IndicatorClearRange(0, AssemblyEditor.TextLength);

				if (DebugInfo.PtrToAsm.TryGetValue(ip, out var posinfo))
				{
					var line = AssemblyEditor.Lines[AssemblyEditor.LineFromPosition(posinfo.from)];

					AssemblyEditor.MarkerDeleteAll(AT_MARKER);
					line.MarkerAdd(AT_MARKER);

					AssemblyEditor.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);
					line.Goto();
				}
			}

			for (int i = 0; i < 32; i++)
			{
				string @new = "0x" + Registers[i].ToString("X").ToLowerInvariant();

				var x = ListRegisters[i];
				if (x.Value != @new)
				{
					x.Value = @new;
					x.Foreground = Brushes.Red;
				}
				else
					x.Foreground = Brushes.Black;
				//this.RegisterViews[i].ForeColor = this.RegisterViews[i].Text != @new ? Color.Red : Color.Black;
				//this.RegisterViews[i].Text = @new;
			}
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(RegistersListView.ItemsSource);
			view.Refresh();

			//this.CallStackListView.Items.Clear();
			ListCallStacks.Clear();
			ListLocals.Clear();
			{
				uint at = Registers[(int)NamedRegister.InstructionPointer];
				uint bp = Registers[(int)NamedRegister.BasePointer];

				(uint loc, List<(string local, int bp_offset, uint size, string typehint)> locals, string name)? ip_to_func(uint pos)
				{
					string lbl = null;
					int dist = int.MaxValue;

					if (DebugInfo?.SourceFunctions != null)
						foreach (var kv in DebugInfo.SourceFunctions)
						{
							if (kv.Value.loc <= pos && ((int)pos - (int)kv.Value.loc) <= dist)
							{
								dist = ((int)pos - (int)kv.Value.loc);
								lbl = kv.Key;
							}
						}

					if (lbl == null || string.IsNullOrWhiteSpace(lbl))
						return null;

					var ret = DebugInfo.SourceFunctions[lbl];
					return (ret.loc, ret.locals, lbl);
				};

				string ip_to_asm_label(uint pos)
				{
					string lbl = null;
					int dist = int.MaxValue;

					if (DebugInfo != null)
					{
						foreach (var kv in DebugInfo.Labels)
						{
							if (kv.Value <= pos && ((int)pos - (int)kv.Value) <= dist)
							{
								dist = ((int)pos - (int)kv.Value);
								lbl = kv.Key;
							}
						}
					}

					if (lbl == null)
						return $"0x{pos.ToString("X").ToLowerInvariant()}";
					return $"{lbl} + 0x{(dist).ToString("X").ToLowerInvariant()}";
				};

				int n = 0;
				while (true)
				{
					n++;

					string label;
					var func = ip_to_func(at);
					if (func != null)
					{
						label = func.Value.name;

						if (StackBase != null)
						{
							foreach (var local in func.Value.locals)
							{
								ListLocal lviloc = new ListLocal()
								{
									Name = local.local,
									CallLevel = $"{n}: {label}",
								};

								lviloc.Value = this.VisualizeValue(StackBase.Value, bp, local.bp_offset, local.typehint);

								ListLocals.Add(lviloc);
							}
						}
					}
					else
						label = ip_to_asm_label(at);

					ListCallStacks.Add(new ListCallStack()
					{
						N = n,
						Location = label,
					});

					if (StackBase == null || bp <= StackBase.Value)
						break;

					at = BitConverter.ToUInt32(StackData, (int)(bp - 8 - StackBase.Value));
					bp = BitConverter.ToUInt32(StackData, (int)(bp - 4 - StackBase.Value));
				}

				Running = false;
				this.UpdateState();
			}

			if (newinstr != null)
			{
				_Disassembler.Registers = Registers;
				_Disassembler.Clock(ip, newinstr);

				if (DisassemblyEditor != null)
				{
					DisassemblyEditor.ReadOnly = false;

					{
						// find where the view is
						int last = DisassemblyEditor.Scintilla.FirstVisibleLine + DisassemblyEditor.Scintilla.LinesOnScreen - 1;

						var pos = DisassemblyEditor.Scintilla.CurrentPosition;
						DisassemblyEditor.Text = _Disassembler.ToString();
						{
							//last.Goto();
							var line = DisassemblyEditor.Scintilla.Lines[last];
							line.Goto();
							DisassemblyEditor.Scintilla.ClearSelections();
							//this.DisassemblyEditor.Scintilla.CurrentPosition = line.Position;
							//this.DisassemblyEditor.Scintilla.ScrollCaret();
						}
						DisassemblyEditor.Scintilla.CurrentPosition = pos;

					}

					DisassemblyEditor.ReadOnly = true;

					DisassemblyEditor.IndicatorCurrent = INDICATOR_AT;
					DisassemblyEditor.IndicatorClearRange(0, DisassemblyEditor.TextLength);

					if (_Disassembler.DbgGuessed.PtrToAsm.TryGetValue(ip, out var posinfo))
					{
						var line = DisassemblyEditor.Lines[DisassemblyEditor.LineFromPosition(posinfo.from)];

						DisassemblyEditor.MarkerDeleteAll(AT_MARKER);
						line.MarkerAdd(AT_MARKER);

						DisassemblyEditor.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);
						line.Goto();
					}
				}
			}

			if (Autostepping)
			{
				ConnectionWriter?.Invoke("step-into");
			}
		}

		private TcpListener Listener = new TcpListener(IPAddress.Any, 1337);
		private Action<string> ConnectionWriter = null;

		private void ListenThread()
		{
			Listener.Start();
			while (true)
			{
				TcpClient cl = Listener.AcceptTcpClient();

				var w = new StreamWriter(cl.GetStream());
				ConnectionWriter = delegate (string cmd)
				{
					try
					{
						w.WriteLine(cmd);
						w.Flush();
					}
					catch { }
				};

				string status_message = null;
				var status_color = Brushes.Cyan;

				try
				{
					Dispatcher.Invoke(delegate ()
					{
						Connected = true;
						Running = false;
						this.UpdateState();

						StatusBarLabel.Text = $"Connected to {cl.Client.RemoteEndPoint}";
					});

					using StreamReader r = new StreamReader(cl.GetStream());
					while (true)
					{
						string registers = r.ReadLine();
						string stack = r.ReadLine();
						string instruction = r.ReadLine();

						Regex.Replace(registers, "([0-9]+): 0x([a-zA-Z0-9]+)",
							delegate (Match m)
							{
								uint regid = uint.Parse(m.Groups[1].Value);
								uint value = Convert.ToUInt32(m.Groups[2].Value, 16);
								Registers[regid] = value;
								return m.Value;
							}
						);

						if (string.IsNullOrEmpty(stack))
						{ }
						else if (stack == "=")
						{ }
						else
						{
							Match m = Regex.Match(stack, @"([0-9]+)\+([0-9]+): (.+)");
							StackBase = uint.Parse(m.Groups[1].Value);
							int index = int.Parse(m.Groups[2].Value);

							byte[] data = Convert.FromBase64String(m.Groups[3].Value);

							if (index + data.Length >= StackData.Length)
							{
								int newsz = StackData.Length * 2;
								while (newsz <= index + data.Length)
									newsz *= 2;
								byte[] newdat = new byte[newsz];
								Buffer.BlockCopy(StackData, 0, newdat, 0, StackData.Length);
								for (int i = StackData.Length; i < newdat.Length; i++)
									newdat[i] = 0;
								StackData = newdat;
							}

							Buffer.BlockCopy(data, 0, StackData, index, data.Length);
						}

						byte[] instr = Convert.FromBase64String(instruction);

						Dispatcher.Invoke(delegate ()
						{
							this.Clock(instr);
						});
					}
				}
				catch (IOException io) when (io.InnerException is SocketException && io.InnerException.Message.Contains("forcibly closed"))
				{
					status_message = "Disconnected";
					status_color = Brushes.Crimson;
				}
				catch (Exception ex)
				{
					status_message = $"Exception: {ex}";
					status_color = Brushes.Crimson;
				}
				ConnectionWriter = null;

				Dispatcher.Invoke(delegate ()
				{
					StatusBarLabel.Text = $"Disconnected";
					Connected = false;
					Running = false;
					this.UpdateState();

					if (status_message != null)
					{
						StatusBar.Background = status_color;
						StatusBarLabel.Text = status_message;
					}
				});
			}
		}

		private const int INDICATOR_BREAKPOINT = 8;
		private const int INDICATOR_BREAKPOINT_FORE = 9;
		private const int INDICATOR_AT = 10;
		private const int NUMBER_MARGIN = 2;
		private const int BOOKMARK_MARGIN = 1;
		private const int BOOKMARK_MARKER = 1;
		private const int AT_MARKER = 2;

		private ScintillaWPF CreateEditor(string file, string source, string language)
		{
			//const int FOLDING_MARGIN = 3;
			//const bool CODEFOLDING_CIRCULAR = true;

			var ta = new ScintillaWPF
			{
				BorderStyle = System.Windows.Forms.BorderStyle.None,
				Text = source,
				Margin = new System.Windows.Thickness(0),
				WrapMode = WrapMode.None,
				ReadOnly = true,
				IndentationGuides = IndentView.LookBoth,
			};
			//ta.ReadOnly = true;

			// setup
			{
				ta.StyleResetDefault();
				ta.Styles[SStyle.Default].Font = "Consolas";
				ta.Styles[SStyle.Default].Size = 10;

				// colors
				if (language == "asm")
				{
					ta.Styles[SStyle.Asm.String].ForeColor = IntToColor(0xaa00aa);
					ta.Styles[SStyle.Asm.Comment].ForeColor = IntToColor(0x888888);
					ta.Styles[SStyle.Asm.CpuInstruction].ForeColor = IntToColor(0x9b00e6);
					ta.Styles[SStyle.Asm.Register].ForeColor = IntToColor(0xc600ff);
					ta.Styles[SStyle.Asm.Directive].ForeColor = IntToColor(0x6f008a);

					ta.Styles[SStyle.Asm.StringEol].ForeColor = ta.Styles[SStyle.Asm.String].ForeColor;
					ta.Styles[SStyle.Asm.CommentBlock].ForeColor = ta.Styles[SStyle.Asm.Comment].ForeColor;

					StringBuilder instructions = new StringBuilder();
					string pre = "";
					foreach (var x in Assembler.OpcodeMap)
					{
						instructions.Append($"{pre}{x.Key.TrimEnd('R')}");
						pre = " ";
					}

					StringBuilder registers = new StringBuilder();
					foreach (var x in Assembler.RegisterMap)
					{
						// 8, 16, 32, and 64bits
						if (x.Value >= NamedRegister.A)
						{
							registers.Append($"{pre}{x.Key}l"); pre = " ";
							registers.Append($" {x.Key}x");
							registers.Append($" e{x.Key}x");
							registers.Append($" r{x.Key}x");
						}
						else
						{
							registers.Append($"{pre}{x.Key}l"); pre = " ";
							registers.Append($" {x.Key}");
							registers.Append($" e{x.Key}");
							registers.Append($" r{x.Key}");
						}
					}

					ta.Lexer = Lexer.Asm;
					ta.SetKeywords(0, instructions.ToString());
					ta.SetKeywords(2, registers.ToString());
					ta.SetKeywords(3, ".src .loc .align .data ascii float int int8 int16 int32 int64 pad ptr ptr8 ptr16 ptr32 ptr64");
				}

				// margins
				{
					if (file == "Assembly")
					{
						ta.MarginClick += delegate (object sender, MarginClickEventArgs e)
						{
							// Do we have a marker for this line?
							const uint mask = (1 << BOOKMARK_MARKER);

							int srcpos = -1;
							uint asmptr = 0;

							for (int i = 0; i < 1024 * 10; i++)
								if (AsmToPtr.TryGetValue(e.Position + i, out asmptr))
								{
									if (DebugInfo.PtrToAsm[asmptr].type == DebugData.AsmPtrType.Instruction)
									{
										srcpos = e.Position + i;
										break;
									}
								}

							if (srcpos < 0)
								return;

							var line = ta.Lines[ta.LineFromPosition(srcpos)];

							// we need to search for the next instruction
							//var line = this.TextArea.Lines[this.TextArea.LineFromPosition(posinfo.from)];
							//this.TextArea.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);

							if ((line.MarkerGet() & mask) > 0)
							{
								// Remove existing bookmark
								line.MarkerDelete(BOOKMARK_MARKER);
								ta.IndicatorCurrent = INDICATOR_BREAKPOINT;
								ta.IndicatorClearRange(srcpos, line.EndPosition - srcpos);
								ta.IndicatorCurrent = INDICATOR_BREAKPOINT_FORE;
								ta.IndicatorClearRange(srcpos, line.EndPosition - srcpos);
								while (Breakpoints.Remove(asmptr)) ;
							}
							else
							{
								// Add bookmark
								line.MarkerAdd(BOOKMARK_MARKER);
								ta.IndicatorCurrent = INDICATOR_BREAKPOINT;
								ta.IndicatorFillRange(srcpos, line.EndPosition - srcpos);
								ta.IndicatorCurrent = INDICATOR_BREAKPOINT_FORE;
								ta.IndicatorFillRange(srcpos, line.EndPosition - srcpos);
								Breakpoints.Add(asmptr);
							}

							this.SendBreakpoints();
						};
					}

					var nums = ta.Margins[NUMBER_MARGIN];
					nums.Width = 30;
					nums.Type = MarginType.Number;
					nums.Sensitive = true;
					nums.Mask = 0;

					var margin = ta.Margins[BOOKMARK_MARGIN];
					margin.Width = 24;
					margin.Sensitive = true;
					margin.Type = MarginType.Symbol;
					margin.Mask = (1 << BOOKMARK_MARKER) | (1 << AT_MARKER);

					var marker = ta.Markers[BOOKMARK_MARKER];
					marker.Symbol = MarkerSymbol.Circle;
					marker.SetBackColor(IntToColor(0xe41400));
					marker.SetForeColor(Color.Transparent);
					marker.SetAlpha(100);

					var atmark = ta.Markers[AT_MARKER];
					atmark.Symbol = MarkerSymbol.Arrow;
					atmark.SetBackColor(IntToColor(0xfff181));
					atmark.SetForeColor(Color.Black);
					atmark.SetAlpha(100);
				}

				// indicators
				{
					ta.Indicators[INDICATOR_BREAKPOINT_FORE].Style = IndicatorStyle.TextFore;
					ta.Indicators[INDICATOR_BREAKPOINT_FORE].ForeColor = IntToColor(0xffffff);

					ta.Indicators[INDICATOR_BREAKPOINT].Style = IndicatorStyle.StraightBox;
					ta.Indicators[INDICATOR_BREAKPOINT].Under = true;
					ta.Indicators[INDICATOR_BREAKPOINT].ForeColor = IntToColor(0xab616b);
					ta.Indicators[INDICATOR_BREAKPOINT].OutlineAlpha = 255;
					ta.Indicators[INDICATOR_BREAKPOINT].Alpha = 255;

					ta.Indicators[INDICATOR_AT].Style = IndicatorStyle.StraightBox;
					ta.Indicators[INDICATOR_AT].Under = true;
					ta.Indicators[INDICATOR_AT].ForeColor = IntToColor(0xfff181);
					ta.Indicators[INDICATOR_AT].OutlineAlpha = 0;
					ta.Indicators[INDICATOR_AT].Alpha = 255;
				}
			}

			DocumentPane.Children.Add(new AvalonDock.Layout.LayoutDocument()
			{
				Title = System.IO.Path.GetFileName(file),
				ToolTip = file,
				Content = ta,
				CanClose = language != "asm",
			});
			return ta;
		}

		public static Color IntToColor(int rgb)
		{
			return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}
	}
}