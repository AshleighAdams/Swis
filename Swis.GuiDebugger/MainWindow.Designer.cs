namespace Swis.GuiDebugger.cs
{
	partial class MainWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
			this.AssemblyCodePanel = new System.Windows.Forms.Panel();
			this.StatusStrip = new System.Windows.Forms.StatusStrip();
			this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.ToolStripExec = new System.Windows.Forms.ToolStrip();
			this.ContinueButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.PauseButton = new System.Windows.Forms.ToolStripButton();
			this.HaltButton = new System.Windows.Forms.ToolStripButton();
			this.ResetButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.StepInto = new System.Windows.Forms.ToolStripSplitButton();
			this.AutoStep = new System.Windows.Forms.ToolStripMenuItem();
			this.StepOver = new System.Windows.Forms.ToolStripButton();
			this.StepOutButton = new System.Windows.Forms.ToolStripButton();
			this.ToolStripContainer = new System.Windows.Forms.ToolStripContainer();
			this.CodeInfoSplitContainer = new System.Windows.Forms.SplitContainer();
			this.FileTabs = new System.Windows.Forms.TabControl();
			this.AssemblyTab = new System.Windows.Forms.TabPage();
			this.SourceTab = new System.Windows.Forms.TabPage();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.TabControlVariables = new System.Windows.Forms.TabControl();
			this.TabPageRegisters = new System.Windows.Forms.TabPage();
			this.RegisterListView = new System.Windows.Forms.ListView();
			this.ColumnHeaderRegNameA = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderRegValueA = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderRegNameB = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderRegValueB = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderRegNameC = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderRegValueC = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RightTabs = new System.Windows.Forms.TabControl();
			this.TabPageCallstack = new System.Windows.Forms.TabPage();
			this.CallStackListView = new System.Windows.Forms.ListView();
			this.ColumnHeaderN = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnHeaderLocation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.SourceFileButton = new System.Windows.Forms.ToolStripButton();
			this.DebugInfoButton = new System.Windows.Forms.ToolStripButton();
			this.TabPageLocals = new System.Windows.Forms.TabPage();
			this.ListViewLocals = new System.Windows.Forms.ListView();
			this.ColumnName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ColumnValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.StatusStrip.SuspendLayout();
			this.ToolStripExec.SuspendLayout();
			this.ToolStripContainer.ContentPanel.SuspendLayout();
			this.ToolStripContainer.TopToolStripPanel.SuspendLayout();
			this.ToolStripContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.CodeInfoSplitContainer)).BeginInit();
			this.CodeInfoSplitContainer.Panel1.SuspendLayout();
			this.CodeInfoSplitContainer.Panel2.SuspendLayout();
			this.CodeInfoSplitContainer.SuspendLayout();
			this.FileTabs.SuspendLayout();
			this.AssemblyTab.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.TabControlVariables.SuspendLayout();
			this.TabPageRegisters.SuspendLayout();
			this.RightTabs.SuspendLayout();
			this.TabPageCallstack.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.TabPageLocals.SuspendLayout();
			this.SuspendLayout();
			// 
			// AssemblyCodePanel
			// 
			this.AssemblyCodePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AssemblyCodePanel.Location = new System.Drawing.Point(0, 0);
			this.AssemblyCodePanel.Name = "AssemblyCodePanel";
			this.AssemblyCodePanel.Size = new System.Drawing.Size(845, 384);
			this.AssemblyCodePanel.TabIndex = 0;
			// 
			// StatusStrip
			// 
			this.StatusStrip.BackColor = System.Drawing.SystemColors.MenuHighlight;
			this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel});
			this.StatusStrip.Location = new System.Drawing.Point(0, 591);
			this.StatusStrip.Name = "StatusStrip";
			this.StatusStrip.Size = new System.Drawing.Size(853, 22);
			this.StatusStrip.TabIndex = 1;
			this.StatusStrip.Text = "statusStrip1";
			// 
			// StatusLabel
			// 
			this.StatusLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(26, 17);
			this.StatusLabel.Text = "Idle";
			// 
			// ToolStripExec
			// 
			this.ToolStripExec.BackColor = System.Drawing.Color.Transparent;
			this.ToolStripExec.Dock = System.Windows.Forms.DockStyle.None;
			this.ToolStripExec.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ContinueButton,
            this.toolStripSeparator1,
            this.PauseButton,
            this.HaltButton,
            this.ResetButton,
            this.toolStripSeparator2,
            this.StepInto,
            this.StepOver,
            this.StepOutButton});
			this.ToolStripExec.Location = new System.Drawing.Point(61, 0);
			this.ToolStripExec.Name = "ToolStripExec";
			this.ToolStripExec.Size = new System.Drawing.Size(247, 25);
			this.ToolStripExec.TabIndex = 2;
			this.ToolStripExec.Text = "toolStrip1";
			// 
			// ContinueButton
			// 
			this.ContinueButton.Image = ((System.Drawing.Image)(resources.GetObject("ContinueButton.Image")));
			this.ContinueButton.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.ContinueButton.Name = "ContinueButton";
			this.ContinueButton.Size = new System.Drawing.Size(76, 22);
			this.ContinueButton.Text = "Continue";
			this.ContinueButton.Click += new System.EventHandler(this.ContinueButton_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// PauseButton
			// 
			this.PauseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PauseButton.Image = ((System.Drawing.Image)(resources.GetObject("PauseButton.Image")));
			this.PauseButton.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.PauseButton.Name = "PauseButton";
			this.PauseButton.Size = new System.Drawing.Size(23, 22);
			this.PauseButton.Text = "Pause";
			this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
			// 
			// HaltButton
			// 
			this.HaltButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.HaltButton.Image = ((System.Drawing.Image)(resources.GetObject("HaltButton.Image")));
			this.HaltButton.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.HaltButton.Name = "HaltButton";
			this.HaltButton.Size = new System.Drawing.Size(23, 22);
			this.HaltButton.Text = "Halt";
			this.HaltButton.Click += new System.EventHandler(this.HaltButton_Click);
			// 
			// ResetButton
			// 
			this.ResetButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ResetButton.Image = ((System.Drawing.Image)(resources.GetObject("ResetButton.Image")));
			this.ResetButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ResetButton.Name = "ResetButton";
			this.ResetButton.Size = new System.Drawing.Size(23, 22);
			this.ResetButton.Text = "Reset";
			this.ResetButton.Click += new System.EventHandler(this.ResetButton_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// StepInto
			// 
			this.StepInto.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.StepInto.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AutoStep});
			this.StepInto.Image = ((System.Drawing.Image)(resources.GetObject("StepInto.Image")));
			this.StepInto.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StepInto.Name = "StepInto";
			this.StepInto.Size = new System.Drawing.Size(32, 22);
			this.StepInto.Text = "Step Into (F11)";
			this.StepInto.ButtonClick += new System.EventHandler(this.StepInto_ButtonClick);
			// 
			// AutoStep
			// 
			this.AutoStep.Name = "AutoStep";
			this.AutoStep.Size = new System.Drawing.Size(126, 22);
			this.AutoStep.Text = "Auto Step";
			this.AutoStep.Click += new System.EventHandler(this.AutoStep_Click);
			// 
			// StepOver
			// 
			this.StepOver.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.StepOver.Image = ((System.Drawing.Image)(resources.GetObject("StepOver.Image")));
			this.StepOver.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.StepOver.Name = "StepOver";
			this.StepOver.Size = new System.Drawing.Size(23, 22);
			this.StepOver.Text = "Step Over (F10)";
			this.StepOver.Click += new System.EventHandler(this.StepOver_Click);
			// 
			// StepOutButton
			// 
			this.StepOutButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.StepOutButton.Image = ((System.Drawing.Image)(resources.GetObject("StepOutButton.Image")));
			this.StepOutButton.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.StepOutButton.Name = "StepOutButton";
			this.StepOutButton.Size = new System.Drawing.Size(23, 22);
			this.StepOutButton.Text = "Step Out (Shift+F11)";
			this.StepOutButton.Click += new System.EventHandler(this.StepOutButton_Click);
			// 
			// ToolStripContainer
			// 
			this.ToolStripContainer.BottomToolStripPanelVisible = false;
			// 
			// ToolStripContainer.ContentPanel
			// 
			this.ToolStripContainer.ContentPanel.Controls.Add(this.CodeInfoSplitContainer);
			this.ToolStripContainer.ContentPanel.Size = new System.Drawing.Size(853, 566);
			this.ToolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ToolStripContainer.LeftToolStripPanelVisible = false;
			this.ToolStripContainer.Location = new System.Drawing.Point(0, 0);
			this.ToolStripContainer.Name = "ToolStripContainer";
			this.ToolStripContainer.RightToolStripPanelVisible = false;
			this.ToolStripContainer.Size = new System.Drawing.Size(853, 591);
			this.ToolStripContainer.TabIndex = 3;
			this.ToolStripContainer.Text = "toolStripContainer1";
			// 
			// ToolStripContainer.TopToolStripPanel
			// 
			this.ToolStripContainer.TopToolStripPanel.Controls.Add(this.toolStrip1);
			this.ToolStripContainer.TopToolStripPanel.Controls.Add(this.ToolStripExec);
			// 
			// CodeInfoSplitContainer
			// 
			this.CodeInfoSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CodeInfoSplitContainer.Location = new System.Drawing.Point(0, 0);
			this.CodeInfoSplitContainer.Name = "CodeInfoSplitContainer";
			this.CodeInfoSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// CodeInfoSplitContainer.Panel1
			// 
			this.CodeInfoSplitContainer.Panel1.Controls.Add(this.FileTabs);
			this.CodeInfoSplitContainer.Panel1MinSize = 20;
			// 
			// CodeInfoSplitContainer.Panel2
			// 
			this.CodeInfoSplitContainer.Panel2.Controls.Add(this.splitContainer2);
			this.CodeInfoSplitContainer.Panel2MinSize = 20;
			this.CodeInfoSplitContainer.Size = new System.Drawing.Size(853, 566);
			this.CodeInfoSplitContainer.SplitterDistance = 410;
			this.CodeInfoSplitContainer.SplitterWidth = 2;
			this.CodeInfoSplitContainer.TabIndex = 1;
			// 
			// FileTabs
			// 
			this.FileTabs.Controls.Add(this.AssemblyTab);
			this.FileTabs.Controls.Add(this.SourceTab);
			this.FileTabs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FileTabs.Location = new System.Drawing.Point(0, 0);
			this.FileTabs.Margin = new System.Windows.Forms.Padding(0);
			this.FileTabs.Name = "FileTabs";
			this.FileTabs.Padding = new System.Drawing.Point(0, 0);
			this.FileTabs.SelectedIndex = 0;
			this.FileTabs.Size = new System.Drawing.Size(853, 410);
			this.FileTabs.TabIndex = 0;
			// 
			// AssemblyTab
			// 
			this.AssemblyTab.Controls.Add(this.AssemblyCodePanel);
			this.AssemblyTab.Location = new System.Drawing.Point(4, 22);
			this.AssemblyTab.Margin = new System.Windows.Forms.Padding(0);
			this.AssemblyTab.Name = "AssemblyTab";
			this.AssemblyTab.Size = new System.Drawing.Size(845, 384);
			this.AssemblyTab.TabIndex = 0;
			this.AssemblyTab.Text = "Assembly";
			this.AssemblyTab.UseVisualStyleBackColor = true;
			// 
			// SourceTab
			// 
			this.SourceTab.Location = new System.Drawing.Point(4, 22);
			this.SourceTab.Margin = new System.Windows.Forms.Padding(0);
			this.SourceTab.Name = "SourceTab";
			this.SourceTab.Size = new System.Drawing.Size(845, 384);
			this.SourceTab.TabIndex = 1;
			this.SourceTab.Text = "Source";
			this.SourceTab.UseVisualStyleBackColor = true;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.TabControlVariables);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.RightTabs);
			this.splitContainer2.Size = new System.Drawing.Size(853, 154);
			this.splitContainer2.SplitterDistance = 412;
			this.splitContainer2.SplitterWidth = 2;
			this.splitContainer2.TabIndex = 0;
			// 
			// TabControlVariables
			// 
			this.TabControlVariables.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.TabControlVariables.Controls.Add(this.TabPageRegisters);
			this.TabControlVariables.Controls.Add(this.TabPageLocals);
			this.TabControlVariables.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TabControlVariables.Location = new System.Drawing.Point(0, 0);
			this.TabControlVariables.Name = "TabControlVariables";
			this.TabControlVariables.SelectedIndex = 0;
			this.TabControlVariables.Size = new System.Drawing.Size(412, 154);
			this.TabControlVariables.TabIndex = 1;
			// 
			// TabPageRegisters
			// 
			this.TabPageRegisters.Controls.Add(this.RegisterListView);
			this.TabPageRegisters.Location = new System.Drawing.Point(4, 4);
			this.TabPageRegisters.Margin = new System.Windows.Forms.Padding(0);
			this.TabPageRegisters.Name = "TabPageRegisters";
			this.TabPageRegisters.Size = new System.Drawing.Size(404, 128);
			this.TabPageRegisters.TabIndex = 0;
			this.TabPageRegisters.Text = "Registers";
			this.TabPageRegisters.UseVisualStyleBackColor = true;
			// 
			// RegisterListView
			// 
			this.RegisterListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.RegisterListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnHeaderRegNameA,
            this.ColumnHeaderRegValueA,
            this.ColumnHeaderRegNameB,
            this.ColumnHeaderRegValueB,
            this.ColumnHeaderRegNameC,
            this.ColumnHeaderRegValueC});
			this.RegisterListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RegisterListView.Location = new System.Drawing.Point(0, 0);
			this.RegisterListView.Name = "RegisterListView";
			this.RegisterListView.Size = new System.Drawing.Size(404, 128);
			this.RegisterListView.TabIndex = 0;
			this.RegisterListView.UseCompatibleStateImageBehavior = false;
			this.RegisterListView.View = System.Windows.Forms.View.Details;
			// 
			// ColumnHeaderRegNameA
			// 
			this.ColumnHeaderRegNameA.Text = "Name";
			// 
			// ColumnHeaderRegValueA
			// 
			this.ColumnHeaderRegValueA.Text = "Value";
			// 
			// ColumnHeaderRegNameB
			// 
			this.ColumnHeaderRegNameB.Text = "Name";
			// 
			// ColumnHeaderRegValueB
			// 
			this.ColumnHeaderRegValueB.Text = "Value";
			// 
			// ColumnHeaderRegNameC
			// 
			this.ColumnHeaderRegNameC.Text = "Name";
			// 
			// ColumnHeaderRegValueC
			// 
			this.ColumnHeaderRegValueC.Text = "Value";
			// 
			// RightTabs
			// 
			this.RightTabs.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.RightTabs.Controls.Add(this.TabPageCallstack);
			this.RightTabs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RightTabs.Location = new System.Drawing.Point(0, 0);
			this.RightTabs.Name = "RightTabs";
			this.RightTabs.SelectedIndex = 0;
			this.RightTabs.Size = new System.Drawing.Size(439, 154);
			this.RightTabs.TabIndex = 1;
			// 
			// TabPageCallstack
			// 
			this.TabPageCallstack.Controls.Add(this.CallStackListView);
			this.TabPageCallstack.Location = new System.Drawing.Point(4, 4);
			this.TabPageCallstack.Margin = new System.Windows.Forms.Padding(0);
			this.TabPageCallstack.Name = "TabPageCallstack";
			this.TabPageCallstack.Size = new System.Drawing.Size(431, 128);
			this.TabPageCallstack.TabIndex = 0;
			this.TabPageCallstack.Text = "Call Stack";
			this.TabPageCallstack.UseVisualStyleBackColor = true;
			// 
			// CallStackListView
			// 
			this.CallStackListView.AutoArrange = false;
			this.CallStackListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.CallStackListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnHeaderN,
            this.ColumnHeaderLocation});
			this.CallStackListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CallStackListView.FullRowSelect = true;
			this.CallStackListView.Location = new System.Drawing.Point(0, 0);
			this.CallStackListView.Name = "CallStackListView";
			this.CallStackListView.Size = new System.Drawing.Size(431, 128);
			this.CallStackListView.TabIndex = 0;
			this.CallStackListView.UseCompatibleStateImageBehavior = false;
			this.CallStackListView.View = System.Windows.Forms.View.Details;
			// 
			// ColumnHeaderN
			// 
			this.ColumnHeaderN.Text = "";
			this.ColumnHeaderN.Width = 30;
			// 
			// ColumnHeaderLocation
			// 
			this.ColumnHeaderLocation.Text = "Location";
			this.ColumnHeaderLocation.Width = 300;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SourceFileButton,
            this.DebugInfoButton});
			this.toolStrip1.Location = new System.Drawing.Point(3, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(58, 25);
			this.toolStrip1.TabIndex = 3;
			// 
			// SourceFileButton
			// 
			this.SourceFileButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.SourceFileButton.Image = ((System.Drawing.Image)(resources.GetObject("SourceFileButton.Image")));
			this.SourceFileButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SourceFileButton.Name = "SourceFileButton";
			this.SourceFileButton.Size = new System.Drawing.Size(23, 22);
			this.SourceFileButton.Text = "Open Symbols";
			this.SourceFileButton.Click += new System.EventHandler(this.SourceFileButton_Click);
			// 
			// DebugInfoButton
			// 
			this.DebugInfoButton.Checked = true;
			this.DebugInfoButton.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DebugInfoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.DebugInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("DebugInfoButton.Image")));
			this.DebugInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DebugInfoButton.Name = "DebugInfoButton";
			this.DebugInfoButton.Size = new System.Drawing.Size(23, 22);
			this.DebugInfoButton.Text = "Show Debug Info";
			this.DebugInfoButton.Click += new System.EventHandler(this.DebugInfoButton_Click);
			// 
			// TabPageLocals
			// 
			this.TabPageLocals.Controls.Add(this.ListViewLocals);
			this.TabPageLocals.Location = new System.Drawing.Point(4, 4);
			this.TabPageLocals.Margin = new System.Windows.Forms.Padding(0);
			this.TabPageLocals.Name = "TabPageLocals";
			this.TabPageLocals.Size = new System.Drawing.Size(404, 128);
			this.TabPageLocals.TabIndex = 1;
			this.TabPageLocals.Text = "Locals";
			this.TabPageLocals.UseVisualStyleBackColor = true;
			// 
			// ListViewLocals
			// 
			this.ListViewLocals.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ListViewLocals.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnName,
            this.ColumnValue});
			this.ListViewLocals.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ListViewLocals.Location = new System.Drawing.Point(0, 0);
			this.ListViewLocals.Name = "ListViewLocals";
			this.ListViewLocals.Size = new System.Drawing.Size(404, 128);
			this.ListViewLocals.TabIndex = 0;
			this.ListViewLocals.UseCompatibleStateImageBehavior = false;
			this.ListViewLocals.View = System.Windows.Forms.View.Details;
			// 
			// ColumnName
			// 
			this.ColumnName.Text = "Name";
			this.ColumnName.Width = 90;
			// 
			// ColumnValue
			// 
			this.ColumnValue.Text = "Value";
			this.ColumnValue.Width = 111;
			// 
			// MainWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(853, 613);
			this.Controls.Add(this.ToolStripContainer);
			this.Controls.Add(this.StatusStrip);
			this.Name = "MainWindow";
			this.Text = "Swis Debugger";
			this.Load += new System.EventHandler(this.MainWindow_Load);
			this.StatusStrip.ResumeLayout(false);
			this.StatusStrip.PerformLayout();
			this.ToolStripExec.ResumeLayout(false);
			this.ToolStripExec.PerformLayout();
			this.ToolStripContainer.ContentPanel.ResumeLayout(false);
			this.ToolStripContainer.TopToolStripPanel.ResumeLayout(false);
			this.ToolStripContainer.TopToolStripPanel.PerformLayout();
			this.ToolStripContainer.ResumeLayout(false);
			this.ToolStripContainer.PerformLayout();
			this.CodeInfoSplitContainer.Panel1.ResumeLayout(false);
			this.CodeInfoSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.CodeInfoSplitContainer)).EndInit();
			this.CodeInfoSplitContainer.ResumeLayout(false);
			this.FileTabs.ResumeLayout(false);
			this.AssemblyTab.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.TabControlVariables.ResumeLayout(false);
			this.TabPageRegisters.ResumeLayout(false);
			this.RightTabs.ResumeLayout(false);
			this.TabPageCallstack.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.TabPageLocals.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel AssemblyCodePanel;
		private System.Windows.Forms.StatusStrip StatusStrip;
		private System.Windows.Forms.ToolStrip ToolStripExec;
		private System.Windows.Forms.ToolStripButton StepOver;
		private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
		private System.Windows.Forms.ToolStripButton ContinueButton;
		private System.Windows.Forms.ToolStripButton PauseButton;
		private System.Windows.Forms.ToolStripButton StepOutButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton HaltButton;
		private System.Windows.Forms.ToolStripButton ResetButton;
		private System.Windows.Forms.ToolStripContainer ToolStripContainer;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton DebugInfoButton;
		private System.Windows.Forms.ToolStripButton SourceFileButton;
		private System.Windows.Forms.ToolStripSplitButton StepInto;
		private System.Windows.Forms.ToolStripMenuItem AutoStep;
		private System.Windows.Forms.SplitContainer CodeInfoSplitContainer;
		private System.Windows.Forms.TabControl FileTabs;
		private System.Windows.Forms.TabPage AssemblyTab;
		private System.Windows.Forms.TabPage SourceTab;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.ListView CallStackListView;
		private System.Windows.Forms.ColumnHeader ColumnHeaderN;
		private System.Windows.Forms.TabControl TabControlVariables;
		private System.Windows.Forms.TabPage TabPageRegisters;
		private System.Windows.Forms.ListView RegisterListView;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegNameA;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegValueA;
		private System.Windows.Forms.TabControl RightTabs;
		private System.Windows.Forms.TabPage TabPageCallstack;
		private System.Windows.Forms.ColumnHeader ColumnHeaderLocation;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegNameB;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegValueB;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegNameC;
		private System.Windows.Forms.ColumnHeader ColumnHeaderRegValueC;
		private System.Windows.Forms.TabPage TabPageLocals;
		private System.Windows.Forms.ListView ListViewLocals;
		private System.Windows.Forms.ColumnHeader ColumnName;
		private System.Windows.Forms.ColumnHeader ColumnValue;
	}
}

