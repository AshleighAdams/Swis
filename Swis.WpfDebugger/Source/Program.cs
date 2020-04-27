using System;

namespace Swis.WpfDebugger
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			var app = new App();
			app.Run(new MainWindow());
		}
	}
}
