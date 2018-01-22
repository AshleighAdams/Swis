using System;

using Swis;

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
			(byte[] assembled, var labels) = Assembler.Assemble(asm);

			int should_halt = labels["stack"];

			Emulator emu = new Emulator();
			emu.Memory = new DirectMemoryController(assembled);
			
			while (!emu.Halted)
			{
				emu.Clock(10);
			}

			//*/
			//Console.WriteLine(LLVMCompiler.Compile(LLVMCompiler.TestIR));

			Console.ReadLine();
		}
    }
}
