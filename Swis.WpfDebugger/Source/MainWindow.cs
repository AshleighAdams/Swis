using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ScintillaNET;
using ScintillaNET.WPF;

using SStyle = ScintillaNET.Style;
using Color = System.Drawing.Color;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Swis.WpfDebugger
{
	public partial class MainWindow : System.Windows.Window
	{
		string VisualizeValue(uint stack_base, uint base_ptr, int bp_offset, string type)
		{
			int pos = (int)(base_ptr + bp_offset - stack_base);
			uint stack_ptr = this.Registers[(int)NamedRegister.StackPointer] - stack_base;

			if (pos > stack_ptr || pos < 0)
				return "<not in stack>";

			if (type.EndsWith("*"))
			{
				uint ptr = BitConverter.ToUInt32(this.StackData, pos);

				if (ptr > stack_base && ptr < stack_base + stack_ptr)
				{
					return $"0x{ptr.ToString("X").ToLowerInvariant()}: {this.VisualizeValue(stack_base, base_ptr, (int)(stack_base + bp_offset - pos), type.Substring(0, type.Length-1))}";
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
				return BitConverter.ToInt32(this.StackData, pos).ToString().ToLowerInvariant();
			else if (type == "uint" || type == "uint32" || type == "u32")
				return BitConverter.ToUInt32(this.StackData, pos).ToString().ToLowerInvariant();
			else if (type == "char" || type == "i8")
				return ((char)this.StackData[pos]).ToString();

			return "?";
		}

		ScintillaWPF AssemblyEditor;
		DebugData _Dbg = null;
		DebugData DebugInfo
		{
			get
			{
				return this._Dbg;
			}
			set
			{
				this.DocumentPane.Children.Clear();
				this._Dbg = value;
				this.AssemblyEditor = this.CreateEditor("Assembly", this._Dbg.AssemblySource, "asm");

				this.AsmToPtr = new Dictionary<int, uint>();
				foreach (var kv in value.PtrToAsm)
					this.AsmToPtr[kv.Value.from] = kv.Key;
			}
		}
		Dictionary<int, uint> AsmToPtr = null;

		uint? StackBase = null;
		byte[] StackData = new byte[128];
		uint[] Registers = new uint[32];

		List<uint> Breakpoints = new List<uint>();
		void SendBreakpoints()
		{
			string s = "";
			foreach (uint bp in this.Breakpoints)
				s += $" {bp}";
			this.ConnectionWriter?.Invoke($"break {s}");
		}

		bool Connected { get; set; }
		bool Running { get; set; }
		
		void UpdateState()
		{
			this.ContinueButton.IsEnabled = this.Connected && !this.Running;
			this.PauseButton.IsEnabled = this.Connected && this.Running;
			this.StopButton.IsEnabled = this.Connected;
			this.ResetButton.IsEnabled = this.Connected;
			this.StepInButton.IsEnabled = this.Connected && !this.Running;
			this.StepOverButton.IsEnabled = this.Connected && !this.Running;
			this.StepOutButton.IsEnabled = this.Connected && !this.Running;

			this.StatusBar.Background 
				= this.Connected
				? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xca, 0x51, 0x00))
				: new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x7a, 0xcc));
		}
		
		private void Clock()
		{
			uint ip = this.Registers[(int)NamedRegister.InstructionPointer];
			
			if (this.AssemblyEditor != null)
			{
				this.AssemblyEditor.IndicatorCurrent = INDICATOR_AT;
				this.AssemblyEditor.IndicatorClearRange(0, this.AssemblyEditor.TextLength);

				if (this.DebugInfo.PtrToAsm.TryGetValue(ip, out var posinfo))
				{
					var line = this.AssemblyEditor.Lines[this.AssemblyEditor.LineFromPosition(posinfo.from)];

					this.AssemblyEditor.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);
					line.Goto();
				}
			}
			
			for (int i = 0; i < 32; i++)
			{
				string @new = "0x" + this.Registers[i].ToString("X").ToLowerInvariant();

				var x = this.ListRegisters[i];
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
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.RegistersListView.ItemsSource);
			view.Refresh();

			//this.CallStackListView.Items.Clear();
			this.ListCallStacks.Clear();
			this.ListLocals.Clear();
			{
				int i = 1;
				uint at = this.Registers[(int)NamedRegister.InstructionPointer];
				uint bp = this.Registers[(int)NamedRegister.BasePointer];
				
				(uint loc, List<(string local, int bp_offset, uint size, string typehint)> locals, string name)? ip_to_func(uint pos)
				{
					string lbl = null;
					int dist = int.MaxValue;

					if (this.DebugInfo?.SourceFunctions != null)
						foreach (var kv in this.DebugInfo.SourceFunctions)
						{
							if (kv.Value.loc <= pos && ((int)pos - (int)kv.Value.loc) <= dist)
							{
								dist = ((int)pos - (int)kv.Value.loc);
								lbl = kv.Key;
							}
						}

					if (lbl == null || string.IsNullOrWhiteSpace(lbl))
						return null;

					var ret = this.DebugInfo.SourceFunctions[lbl];
					return (ret.loc, ret.locals, lbl);
				};

				string ip_to_asm_label(uint pos)
				{
					string lbl = null;
					int dist = int.MaxValue;

					if (this.DebugInfo != null)
					{
						foreach (var kv in this.DebugInfo.Labels)
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

					string label = "";
					var func = ip_to_func(at);
					if (func != null)
					{
						label = func.Value.name;

						if (this.StackBase != null)
						{
							foreach (var local in func.Value.locals)
							{
								ListLocal lviloc = new ListLocal()
								{
									Name = local.local,
									CallLevel = $"{n}: {label}",
								};
								
								lviloc.Value = this.VisualizeValue(this.StackBase.Value, bp, local.bp_offset, local.typehint);
								
								this.ListLocals.Add(lviloc);
							}
						}
					}
					else
						label = ip_to_asm_label(at);

					this.ListCallStacks.Add(new ListCallStack()
					{
						N = n,
						Location = label,
					});

					if (this.StackBase == null || bp <= this.StackBase.Value)
						break;

					at = BitConverter.ToUInt32(this.StackData, (int)(bp - 8 - this.StackBase.Value));
					bp = BitConverter.ToUInt32(this.StackData, (int)(bp - 4 - this.StackBase.Value));
				}

				this.Running = false;
				this.UpdateState();
			}

		}
		
		TcpListener Listener = new TcpListener(IPAddress.Any, 1337);
		Action<string> ConnectionWriter = null;
		void ListenThread()
		{
			this.Listener.Start();
			while (true)
			{
				TcpClient cl = this.Listener.AcceptTcpClient();
				
				var w = new StreamWriter(cl.GetStream());
				this.ConnectionWriter = delegate (string cmd)
				{
					try
					{
						w.WriteLine(cmd);
						w.Flush();
					}
					catch { }
				};

				string exception = null;
				try
				{
					this.Dispatcher.Invoke(delegate ()
					{
						this.Connected = true;
						this.Running = false;
						this.UpdateState();

						this.StatusBarLabel.Text = $"Connected to {cl.Client.RemoteEndPoint}";
					});

					using (StreamReader r = new StreamReader(cl.GetStream()))
					{
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
									this.Registers[regid] = value;
									return m.Value;
								}
							);

							if (stack == "")
							{ }
							else if (stack == "=")
							{ }
							else
							{
								Match m = Regex.Match(stack, @"([0-9]+)\+([0-9]+): (.+)");
								this.StackBase = uint.Parse(m.Groups[1].Value);
								int index = int.Parse(m.Groups[2].Value);

								byte[] data = Convert.FromBase64String(m.Groups[3].Value);

								if (index + data.Length >= this.StackData.Length)
								{
									int newsz = this.StackData.Length * 2;
									while (newsz <= index + data.Length)
										newsz *= 2;
									byte[] newdat = new byte[newsz];
									Buffer.BlockCopy(this.StackData, 0, newdat, 0, this.StackData.Length);
									for (int i = this.StackData.Length; i < newdat.Length; i++)
										newdat[i] = 0;
									this.StackData = newdat;
								}

								Buffer.BlockCopy(data, 0, this.StackData, index, data.Length);
							}

							this.Dispatcher.Invoke(delegate ()
							{
								this.Clock();
							});
						}
					}
				}
				catch (Exception ex)
				{
					exception = ex.ToString();
				}
				this.ConnectionWriter = null;
				
				this.Dispatcher.Invoke(delegate ()
				{
					this.StatusBarLabel.Text = $"Disconnected";
					this.Connected = false;
					this.Running = false;
					this.UpdateState();

					if (exception != null)
					{
						this.StatusBar.Background = Brushes.Crimson;
						this.StatusBarLabel.Text = $"Exception: {exception}";
					}
				});
			}
		}

		const int INDICATOR_BREAKPOINT = 8;
		const int INDICATOR_AT = 9;
		ScintillaWPF CreateEditor(string file, string source, string language)
		{
			const int NUMBER_MARGIN = 2;
			const int BOOKMARK_MARGIN = 1;
			const int BOOKMARK_MARKER = 1;
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
					if (language == "asm")
					{
						ta.MarginClick += delegate (object sender, MarginClickEventArgs e)
						{
							// Do we have a marker for this line?
							const uint mask = (1 << BOOKMARK_MARKER);

							int srcpos = -1;
							uint asmptr = 0;

							for (int i = 0; i < 1024 * 10; i++)
								if (this.AsmToPtr.TryGetValue(e.Position + i, out asmptr))
								{
									if (this.DebugInfo.PtrToAsm[asmptr].type == DebugData.AsmPtrType.Instruction)
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

							ta.IndicatorCurrent = INDICATOR_BREAKPOINT;

							if ((line.MarkerGet() & mask) > 0)
							{
								// Remove existing bookmark
								line.MarkerDelete(BOOKMARK_MARKER);
								ta.IndicatorClearRange(srcpos, line.EndPosition - srcpos);
								while (this.Breakpoints.Remove(asmptr)) ;
							}
							else
							{
								// Add bookmark
								line.MarkerAdd(BOOKMARK_MARKER);
								ta.IndicatorFillRange(srcpos, line.EndPosition - srcpos);
								this.Breakpoints.Add(asmptr);
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
					margin.Mask = (1 << BOOKMARK_MARKER);

					var marker = ta.Markers[BOOKMARK_MARKER];
					marker.Symbol = MarkerSymbol.Circle;
					marker.SetBackColor(IntToColor(0xFF003B));
					marker.SetForeColor(Color.Transparent);
					marker.SetAlpha(100);
				}

				// indicators
				{
					ta.Indicators[INDICATOR_BREAKPOINT].Style = IndicatorStyle.StraightBox;
					ta.Indicators[INDICATOR_BREAKPOINT].Under = true;
					ta.Indicators[INDICATOR_BREAKPOINT].ForeColor = IntToColor(0xcc8888);
					ta.Indicators[INDICATOR_BREAKPOINT].OutlineAlpha = 128;
					ta.Indicators[INDICATOR_BREAKPOINT].Alpha = 255;

					ta.Indicators[INDICATOR_AT].Style = IndicatorStyle.StraightBox;
					ta.Indicators[INDICATOR_AT].Under = true;
					ta.Indicators[INDICATOR_AT].ForeColor = Color.Yellow;
					ta.Indicators[INDICATOR_AT].OutlineAlpha = 255;
					ta.Indicators[INDICATOR_AT].Alpha = 128;
				}
			}
			
			this.DocumentPane.Children.Add(new Xceed.Wpf.AvalonDock.Layout.LayoutDocument()
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