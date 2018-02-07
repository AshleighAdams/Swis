using System;

using Swis;
using System.Diagnostics;

namespace SwisTest
{
    class Program
    {

        static void Main(string[] args)
        {
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			LlvmIrCompiler._teststrutinfo();
			string asm = 
				//LlvmIrCompiler.Compile(LlvmIrCompiler.TestIR);
				System.IO.File.ReadAllText("TestProgram/program.asm");

			//Console.WriteLine(asm);
			//Console.ReadLine();
			//return;

			(byte[] assembled, var dbg) = Assembler.Assemble(asm);

			//Console.ReadLine();
			//return;

			string x = DebugData.Serialize(dbg);

			Cpu emu = new Cpu();
			emu.Memory = new DirectMemoryController(assembled);
			emu.Debugger = new StreamDebugger(Console.Out, dbg);
			Console.ReadLine();
			while (!emu.Halted)
			{
				emu.Clock(1);
				System.Threading.Thread.Sleep(10);
			}
			
			Console.ReadLine();
		}
    }
}
