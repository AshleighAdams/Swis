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
			(byte[] assembled, _) = Assembler.Assemble(asm);
			
			Emulator emu = new Emulator();
			emu.Memory = new DirectMemoryController(assembled);
			
			while (!emu.Halted)
			{
				System.Threading.Thread.Sleep(1);
				emu.Clock(1);
			}

			//*/
			//Console.WriteLine(LLVMCompiler.Compile(LLVMCompiler.TestIR));

			Console.ReadLine();
		}
    }
}
