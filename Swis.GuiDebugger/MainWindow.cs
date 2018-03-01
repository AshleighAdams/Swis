using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScintillaNET;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace Swis.GuiDebugger.cs
{
	public partial class MainWindow : Form
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		bool Connected
		{
			set
			{
				this.ToolStripExec.Enabled = value;
				this.StatusStrip.BackColor = value ? IntToColor(0xca5100) : IntToColor(0x007acc);
			}
		}

		bool Running
		{
			set
			{
				this.ContinueButton.Enabled = !value;
				this.PauseButton.Enabled = value;

				this.StepInto.Enabled = this.ContinueButton.Enabled;
				this.StepOver.Enabled = this.ContinueButton.Enabled;
				this.StepOutButton.Enabled = this.ContinueButton.Enabled;

				//this.ContinueButton.Visible = this.ContinueButton.Enabled;
				//this.PauseButton.Visible = this.PauseButton.Enabled;
				//this.Step.Visible = this.Step.Enabled;
				//this.StepOver.Visible = this.StepOver.Enabled;
				//this.StepOutButton.Visible = this.StepOutButton.Enabled;

				if (value)
				{
					this.TextArea.IndicatorCurrent = INDICATOR_AT;
					this.TextArea.IndicatorClearRange(0, this.TextArea.TextLength);
				}
			}
		}

		Scintilla TextArea;
		TcpListener Listener = new TcpListener(IPAddress.Any, 1337);

		DebugData _Info;
		DebugData DebugInfo
		{
			get
			{
				return this._Info;
			}
			set
			{
				this._Info = value;

				this.TextArea.ReadOnly = false;
				this.TextArea.Text = value.AssemblySource;

				this.AsmToPtr = new Dictionary<int, uint>();
				foreach (var kv in this.DebugInfo.PtrToAsm)
					this.AsmToPtr[kv.Value.from] = kv.Key;

				this.TextArea.ReadOnly = true;
			}
		}
		Dictionary<int, uint> AsmToPtr = null;

		uint? StackBase = null;
		byte[] StackData = new byte[128];
		uint[] Registers = new uint[32];

		Action<string> ConnectionWriter = null;
		void ListenThread()
		{
			this.Listener.Start();
			while (true)
			{
				TcpClient cl = this.Listener.AcceptTcpClient();

				this.Invoke((MethodInvoker)delegate ()
				{
					this.StatusLabel.Text = $"Connected to {cl.Client.RemoteEndPoint}";
				});

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

				try
				{
					this.Invoke((MethodInvoker)delegate ()
					{
						this.Connected = true;
					});

					using (StreamReader r = new StreamReader(cl.GetStream()))
					{
						while (true)
						{
							string registers = r.ReadLine();
							string stack = r.ReadLine();
							string instruction = r.ReadLine();
							
							Regex.Replace(registers, "([0-9]+): 0x([a-zA-Z0-9]+)", 
								delegate(Match m)
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

							this.Invoke((MethodInvoker)delegate ()
							{
								this.Clock();
							});
						}
					}
				}
				catch { }
				this.ConnectionWriter = null;

				this.Invoke((MethodInvoker)delegate ()
				{
					this.StatusLabel.Text = $"Disconnected";
					this.Connected = false;
				});
			}
		}
		
		private void Clock()
		{
			uint ip = this.Registers[(int)NamedRegister.InstructionPointer];
			this.Running = false;

			this.TextArea.IndicatorCurrent = INDICATOR_AT;
			this.TextArea.IndicatorClearRange(0, this.TextArea.TextLength);

			if (this.DebugInfo.PtrToAsm.TryGetValue(ip, out var posinfo))
			{
				var line = this.TextArea.Lines[this.TextArea.LineFromPosition(posinfo.from)];

				this.TextArea.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);
				line.Goto();
			}

			for (int i = 0; i < 32; i++)
			{
				string @new = "0x" + this.Registers[i].ToString("X").ToLowerInvariant();

				
				this.RegisterViews[i].ForeColor = this.RegisterViews[i].Text != @new ? Color.Red : Color.Black;
				this.RegisterViews[i].Text = @new;
			}
			
			this.CallStackListView.Items.Clear();
			{
				int i = 1;
				uint at = this.Registers[(int)NamedRegister.InstructionPointer];
				uint bp = this.Registers[(int)NamedRegister.BasePointer];

				

				(uint loc, List<(string local, int bp_offset, uint size, string typehint)> locals, string name)? ip_to_func(uint pos)
				{
					string lbl = null;
					int dist = int.MaxValue;
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
					foreach (var kv in this.DebugInfo.Labels)
					{
						if (kv.Value <= pos  && ((int)pos - (int)kv.Value) <= dist)
						{
							dist = ((int)pos - (int)kv.Value);
							lbl = kv.Key;
						}
					}
					if (lbl == null)
						return $"0x{pos.ToString("X").ToLowerInvariant()}";
					return $"{lbl} + 0x{(dist).ToString("X").ToLowerInvariant()}";
				};
				
				this.SuspendLayout();
				this.RegisterListView.SuspendLayout();
				this.ListViewLocals.SuspendLayout();
				this.CallStackListView.SuspendLayout();

				this.ListViewLocals.Items.Clear();

				while (true)
				{
					ListViewItem current = new ListViewItem($"{i++}");

					//this.DebugInfo.SourceFunctions

					string label = "";
					var func = ip_to_func(at);
					if (func != null)
					{
						label = func.Value.name;

						if (this.StackBase != null)
						{
							foreach (var local in func.Value.locals)
							{
								ListViewItem lviloc = new ListViewItem(local.local);

								string value = "";
								int pos = (int)(bp + local.bp_offset - this.StackBase.Value);

								if (local.typehint.EndsWith("*"))
									value = $"0x{BitConverter.ToUInt32(this.StackData, pos).ToString("X8").ToLowerInvariant()}";
								else if (local.typehint == "int" || local.typehint == "int32" || local.typehint == "i32")
									value = BitConverter.ToInt32(this.StackData, pos).ToString().ToLowerInvariant();
								else if (local.typehint == "uint" || local.typehint == "uint32" || local.typehint == "u32")
									value = BitConverter.ToUInt32(this.StackData, pos).ToString().ToLowerInvariant();
								else if (local.typehint == "char" || local.typehint == "i8")
									value = BitConverter.ToChar(this.StackData, pos).ToString().ToLowerInvariant();
								else
									value = "<unknown type>";

								lviloc.SubItems.Add(value);
								this.ListViewLocals.Items.Add(lviloc);
							}
						}
					}
					else
						label = ip_to_asm_label(at);

					current.SubItems.Add(label);
					this.CallStackListView.Items.Add(current);

					if (this.StackBase == null || bp <= this.StackBase.Value)
						break;

					at = BitConverter.ToUInt32(this.StackData, (int)(bp - 8 - this.StackBase.Value));
					bp = BitConverter.ToUInt32(this.StackData, (int)(bp - 4 - this.StackBase.Value));
				}

				this.RegisterListView.ResumeLayout();
				this.ListViewLocals.ResumeLayout();
				this.CallStackListView.ResumeLayout();
				this.ResumeLayout();
			}

			// update registers
			//this.RegisterListView.Items.Add(
		}

		ListViewItem.ListViewSubItem[] RegisterViews;
		void InitRegisterListView()
		{
			this.RegisterViews = new ListViewItem.ListViewSubItem[32];

			int i = 0;
			while (i < 32)
			{
				ListViewItem lvi = null;

				for (int n = 0; n < 3 && i < 32; n++, i++)
				{
					string regname = ((NamedRegister)i).Disassemble();
					if (regname.StartsWith("ukn"))
					{
						n--;
						this.RegisterViews[i] = new ListViewItem.ListViewSubItem();
						continue;
					}

					if (i >= (int)NamedRegister.A)
						regname = $"e{regname}x";

					if(lvi == null)
						lvi = new ListViewItem(regname);
					else
						lvi.SubItems.Add(regname);

					this.RegisterViews[i] = lvi.SubItems.Add("0x0");
				}
				lvi.UseItemStyleForSubItems = false;
				this.RegisterListView.Items.Add(lvi);
			}
		}

		private void MainWindow_Load(object sender, EventArgs e)
		{
			this.TextArea = new Scintilla
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				BorderStyle = BorderStyle.None,
			};
			
			this.AssemblyCodePanel.Controls.Add(this.TextArea);

			this.Running = false;
			this.Connected = false;

			this.InitRegisterListView();

			// INITIAL VIEW CONFIG
			this.TextArea.WrapMode = WrapMode.None;
			this.TextArea.IndentationGuides = IndentView.LookBoth;

			// STYLING
			this.InitColors();
			this.InitSyntaxColoring();
			this.InitNumberMargin();
			this.InitBookmarkMargin();

			var thread = new Thread(this.ListenThread);
			thread.IsBackground = true;
			thread.Start();
		}

		private void InitColors()
		{
			this.TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));
		}

		private void InitSyntaxColoring()
		{
			this.TextArea.StyleResetDefault();
			this.TextArea.Styles[Style.Default].Font = "Consolas";
			this.TextArea.Styles[Style.Default].Size = 10;

			
			this.TextArea.Styles[Style.Asm.String].ForeColor = IntToColor(0xaa00aa);
			this.TextArea.Styles[Style.Asm.Comment].ForeColor = IntToColor(0x888888);
			this.TextArea.Styles[Style.Asm.CpuInstruction].ForeColor = IntToColor(0x9b00e6);
			this.TextArea.Styles[Style.Asm.Register].ForeColor = IntToColor(0xc600ff);
			this.TextArea.Styles[Style.Asm.Directive].ForeColor = IntToColor(0x6f008a);
			//this.TextArea.Styles[Style.Asm.Identifier].ForeColor = IntToColor(0xff00ff);

			this.TextArea.Styles[Style.Asm.StringEol].ForeColor = this.TextArea.Styles[Style.Asm.String].ForeColor;
			this.TextArea.Styles[Style.Asm.CommentBlock].ForeColor = this.TextArea.Styles[Style.Asm.Comment].ForeColor;

			this.TextArea.Lexer = Lexer.Asm;

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

			this.TextArea.SetKeywords(0, instructions.ToString());
			this.TextArea.SetKeywords(2, registers.ToString());
			this.TextArea.SetKeywords(3, ".src .loc .align .data ascii float int int8 int16 int32 int64 pad ptr ptr8 ptr16 ptr32 ptr64");

			

			/*
			this.TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
			this.TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);*/
			//this.TextArea.StyleClearAll();

			/*
			this.TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
			this.TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
			this.TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
			this.TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);
			*/

			// Configure the CPP (C#) lexer styles
			/*
			this.TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
			this.TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
			this.TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
			this.TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
			this.TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
			this.TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
			this.TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
			this.TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
			this.TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
			this.TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
			this.TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
			this.TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
			this.TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
			this.TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
			this.TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
			this.TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);
			*/
			//TextArea.Lexer = Lexer.Cpp;


			//TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
			//TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

		}

		public static Color IntToColor(int rgb)
		{
			return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}
		/// <summary>
		/// the background color of the text area
		/// </summary>
		private const int BACK_COLOR = 0x2A211C;

		/// <summary>
		/// default text color of the text area
		/// </summary>
		private const int FORE_COLOR = 0xB7B7B7;
		
		private const int NUMBER_MARGIN = 2;
		private const int BOOKMARK_MARGIN = 1;
		private const int BOOKMARK_MARKER = 1;
		private const int FOLDING_MARGIN = 3;
		private const bool CODEFOLDING_CIRCULAR = true;

		private void InitNumberMargin()
		{
			var nums = this.TextArea.Margins[NUMBER_MARGIN];
			nums.Width = 30;
			nums.Type = MarginType.Number;
			nums.Sensitive = true;
			nums.Mask = 0;
			
			this.TextArea.MarginClick += this.TextArea_MarginClick;
		}
		private void InitBookmarkMargin()
		{
			//TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

			var margin = this.TextArea.Margins[BOOKMARK_MARGIN];
			margin.Width = 24;
			margin.Sensitive = true;
			margin.Type = MarginType.Symbol;
			margin.Mask = (1 << BOOKMARK_MARKER);
			
			//margin.Cursor = MarginCursor.Arrow;

			var marker = this.TextArea.Markers[BOOKMARK_MARKER];
			marker.Symbol = MarkerSymbol.Circle;
			marker.SetBackColor(IntToColor(0xFF003B));
			marker.SetForeColor(Color.Transparent);
			marker.SetAlpha(100);

			this.TextArea.Indicators[INDICATOR_BREAKPOINT].Style = IndicatorStyle.StraightBox;
			this.TextArea.Indicators[INDICATOR_BREAKPOINT].Under = true;
			this.TextArea.Indicators[INDICATOR_BREAKPOINT].ForeColor = IntToColor(0xcc8888);
			this.TextArea.Indicators[INDICATOR_BREAKPOINT].OutlineAlpha = 128;
			this.TextArea.Indicators[INDICATOR_BREAKPOINT].Alpha = 255;

			this.TextArea.Indicators[INDICATOR_AT].Style = IndicatorStyle.StraightBox;
			this.TextArea.Indicators[INDICATOR_AT].Under = true;
			this.TextArea.Indicators[INDICATOR_AT].ForeColor = Color.Yellow;
			this.TextArea.Indicators[INDICATOR_AT].OutlineAlpha = 255;
			this.TextArea.Indicators[INDICATOR_AT].Alpha = 128;
		}

		private void InitCodeFolding()
		{

			this.TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
			this.TextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

			// Enable code folding
			this.TextArea.SetProperty("fold", "1");
			this.TextArea.SetProperty("fold.compact", "1");

			// Configure a margin to display folding symbols
			this.TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
			this.TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
			this.TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
			this.TextArea.Margins[FOLDING_MARGIN].Width = 20;

			// Set colors for all folding markers
			for (int i = 25; i <= 31; i++)
			{
				this.TextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
				this.TextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
			}

			// Configure folding markers with respective symbols
			this.TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
			this.TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
			this.TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
			this.TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
			this.TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
			this.TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
			this.TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

			// Enable automatic folding
			this.TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

		}

		const int INDICATOR_BREAKPOINT = 8;
		const int INDICATOR_AT = 9;

		List<uint> Breakpoints = new List<uint>();
		void SendBreakpoints()
		{
			string s = "";
			foreach (uint bp in Breakpoints)
				s += $" {bp}";
			this.ConnectionWriter?.Invoke($"break {s}");
		}

		private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
		{
			if (e.Margin == BOOKMARK_MARGIN)
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

				var line = this.TextArea.Lines[this.TextArea.LineFromPosition(srcpos)];

				// we need to search for the next instruction
				//var line = this.TextArea.Lines[this.TextArea.LineFromPosition(posinfo.from)];
				//this.TextArea.IndicatorFillRange(posinfo.from, line.EndPosition - posinfo.from);

				this.TextArea.IndicatorCurrent = INDICATOR_BREAKPOINT;

				if ((line.MarkerGet() & mask) > 0)
				{
					// Remove existing bookmark
					line.MarkerDelete(BOOKMARK_MARKER);
					this.TextArea.IndicatorClearRange(srcpos, line.EndPosition - srcpos);
					while(this.Breakpoints.Remove(asmptr));
				}
				else
				{
					// Add bookmark
					line.MarkerAdd(BOOKMARK_MARKER);
					this.TextArea.IndicatorFillRange(srcpos, line.EndPosition - srcpos);
					this.Breakpoints.Add(asmptr);
				}

				this.SendBreakpoints();
			}
		}

		private void StepInto_ButtonClick(object sender, EventArgs e)
		{
			this.ConnectionWriter?.Invoke("step-into");
		}

		private void StepOver_Click(object sender, EventArgs e)
		{
			this.Running = true;
			this.ConnectionWriter?.Invoke("step-over");
		}
		
		private void ContinueButton_Click(object sender, EventArgs e)
		{
			this.Running = true;
			this.ConnectionWriter?.Invoke("continue");
		}

		private void PauseButton_Click(object sender, EventArgs e)
		{
			this.ConnectionWriter?.Invoke("pause");
		}

		private void HaltButton_Click(object sender, EventArgs e)
		{
			this.ConnectionWriter?.Invoke("halt");
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			this.Running = true;
			this.ConnectionWriter?.Invoke("reset");
		}

		private void StepOutButton_Click(object sender, EventArgs e)
		{
			this.Running = true;
			this.ConnectionWriter?.Invoke("step-out");
		}

		private void AutoStep_Click(object sender, EventArgs e)
		{

		}

		private void DebugInfoButton_Click(object sender, EventArgs e)
		{
			this.CodeInfoSplitContainer.Panel2Collapsed = !this.CodeInfoSplitContainer.Panel2Collapsed;
			this.DebugInfoButton.Checked = !this.CodeInfoSplitContainer.Panel2Collapsed;
		}

		private void SourceFileButton_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog
			{
				AddExtension = true,
				DefaultExt = "dbg",
				Filter = "Debug Symbols (*.dbg) | *.dbg; | All Files (*.*) | *.*;",
				Multiselect = false,
				CheckFileExists = true
			};

			if (ofd.ShowDialog() == DialogResult.OK)
				this.DebugInfo = DebugData.Deserialize(File.ReadAllText(ofd.FileName));
		}
	}
}
