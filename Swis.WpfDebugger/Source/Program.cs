using System;

namespace Swis.WpfDebugger
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			var app = new App();
			app.Run(new MainWindow());
		}
	}
}
