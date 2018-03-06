using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace Swis.WpfDebugger
{
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		
		public MainWindow()
		{
			InitializeComponent();

			{
				this.ListRegisters.Clear();
				for (int i = 0; i < 32; i++)
				{
					string name = ((NamedRegister)i).Disassemble();
					ListRegister.RegisterType t;

					if (name.StartsWith("ukn"))
						t = ListRegister.RegisterType.Unused;
					else if (i < (int)NamedRegister.A)
						t = ListRegister.RegisterType.System;
					else
						t = ListRegister.RegisterType.General;

					this.ListRegisters.Add(new ListRegister()
					{
						Register = name,
						Value = "0x0",
						Type = t,
						Foreground = Brushes.Black,
					});
				}
				this.RegistersListView.ItemsSource = this.ListRegisters;

				CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.RegistersListView.ItemsSource);
				PropertyGroupDescription groupdesc = new PropertyGroupDescription("Type");
				view.GroupDescriptions.Add(groupdesc);
			}

			{
				this.LocalsListView.ItemsSource = this.ListLocals;

				CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.LocalsListView.ItemsSource);
				PropertyGroupDescription groupdesc = new PropertyGroupDescription("CallLevel");
				view.GroupDescriptions.Add(groupdesc);
			}

			{
				this.CallStackListView.ItemsSource = this.ListCallStacks;
			}

			{
				this.DisassemblyEditor = this.CreateEditor("Disassembly", "; disassembled instructions will be populated here as they're executed", "asm");
			}

			this.UpdateState();

			var thread = new Thread(this.ListenThread);
			thread.IsBackground = true;
			thread.Start();
		}

		public class ListLocal
		{
			public string Name { get; set; }
			public string Value { get; set; }
			public string CallLevel { get; set; }
			public Brush Foreground { get; set; } = Brushes.Black;
		}
		public ObservableCollection<ListLocal> ListLocals { get; set; } = new ObservableCollection<ListLocal>();

		public class ListRegister
		{
			public enum RegisterType
			{
				General, System, Unused,
			}
			
			public string Register { get; set; }
			public string Value { get; set; }
			public RegisterType Type { get; set; }
			public Brush Foreground { get; set; } = Brushes.Black;
		}
		public ObservableCollection<ListRegister> ListRegisters { get; set; } = new ObservableCollection<ListRegister>();

		public class ListCallStack
		{
			public int N { get; set; }
			public string Location { get; set; }
		}
		public ObservableCollection<ListCallStack> ListCallStacks { get; set; } = new ObservableCollection<ListCallStack>();

		public static RoutedCommand OpenSymbolsCommand = new RoutedCommand();
		private void OpenSymbolsButton_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
			{
				AddExtension = true,
				DefaultExt = "dbg",
				Filter = "Debug Symbols (*.dbg) | *.dbg; | All Files (*.*) | *.*;",
				Multiselect = false,
				CheckFileExists = true
			};

			if (ofd.ShowDialog() == true)
				this.DebugInfo = DebugData.Deserialize(System.IO.File.ReadAllText(ofd.FileName));

			//this.CreateEditor("Assembly", "", "asm");
			//this.CreateEditor("Some/file.cpp", "", "cpp");
		}

		public static RoutedCommand ContinueCommand = new RoutedCommand();
		private void ContinueButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.ContinueButton.IsEnabled)
			{
				this.Running = true;
				this.UpdateState();
				this.ConnectionWriter?.Invoke("continue");
			}
		}

		public static RoutedCommand PauseCommand = new RoutedCommand();
		private void PauseButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.PauseButton.IsEnabled)
			{
				this.ConnectionWriter?.Invoke("pause");
			}
		}

		public static RoutedCommand StopCommand = new RoutedCommand();
		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.StopButton.IsEnabled)
			{
				this.Running = false;
				this.UpdateState();
				this.ConnectionWriter?.Invoke("halt");
			}
		}

		public static RoutedCommand ResetCommand = new RoutedCommand();
		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.ResetButton.IsEnabled)
			{
				this.Running = true;
				this.UpdateState();
				this.ConnectionWriter?.Invoke("reset");
			}
		}

		public static RoutedCommand StepInCommand = new RoutedCommand();
		private void StepInButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.StepInButton.IsEnabled)
			{
				this.ConnectionWriter?.Invoke("step-into");
			}
		}

		public static RoutedCommand StepOverCommand = new RoutedCommand();
		private void StepOverButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.StepOverButton.IsEnabled)
			{
				this.Running = true;
				this.UpdateState();
				this.ConnectionWriter?.Invoke("step-over");
			}
		}

		public static RoutedCommand StepOutCommand = new RoutedCommand();
		private void StepOutButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.StepOutButton.IsEnabled)
			{
				this.Running = true;
				this.UpdateState();
				this.ConnectionWriter?.Invoke("step-out");
			}
		}

	}
}
