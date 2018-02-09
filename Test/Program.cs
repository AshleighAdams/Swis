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
			/*
			byte[] test = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
			var dm = new DirectMemoryController(test);

			byte[] back = new byte[test.Length];
			for (uint i = 0; i < back.Length; i++)
				back[i] = (byte)dm[i, 8];

			uint whatitis = dm[1, 32];
			uint shouldbe = BitConverter.ToUInt32(back, 1);

			string abc = Convert.ToString(whatitis, 2);
			string def = Convert.ToString(shouldbe, 2);
			*/

			(byte[] assembled, var dbg) = Assembler.Assemble(asm);

			//Console.ReadLine();
			//return;

			string x = DebugData.Serialize(dbg);


			var mem = new IntArrayMemoryController(assembled);
			//var mem = new PointerMemoryController(assembled);

			Cpu cpu = new Cpu
			{
				Memory = mem,
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};
			
			DateTime start = DateTime.UtcNow;
			
			while (!cpu.Halted)
			{
				cpu.Clock(1000);
				System.Threading.Thread.Sleep(16);
			}

			DateTime end = DateTime.UtcNow;

			Console.WriteLine(end - start);
			Console.WriteLine(cpu.TimeStampCounter);

			Console.ReadLine();
		}
    }
}
