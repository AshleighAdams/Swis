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

			//
			//
			///*
			string asm = System.IO.File.ReadAllText("TestProgram/program.asm");
			(byte[] assembled, var dbg) = Assembler.Assemble(asm);

			string x = DebugData.Serialize(dbg);

			Cpu emu = new Cpu();
			emu.Memory = new DirectMemoryController(assembled);
			emu.Debugger = new StreamDebugger(Console.Out, null/*dbg*/);

			int clock = 0;
			while (!emu.Halted)
			{
				emu.Clock(1);
				//System.Threading.Thread.Sleep(1000);
			}

			//*/
			//Console.WriteLine(LLVMCompiler.Compile(LLVMCompiler.TestIR));

			Console.ReadLine();
		}
    }
}
