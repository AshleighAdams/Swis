using System;

using Swis;
using System.Diagnostics;

namespace SwisTest
{
    class Program
    {
		static string IrCompileTest()
		{
			LlvmIrCompiler._teststrutinfo();
			string ir = LlvmIrCompiler.TestIR;
			string asm = LlvmIrCompiler.Compile(ir);
			Console.WriteLine(asm);
			return asm;
		}

		static void ExecuteTest(string asm)
		{
			asm = asm ?? System.IO.File.ReadAllText("TestProgram/program.asm");
			(byte[] assembled, var dbg) = Assembler.Assemble(asm);

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

			//string x = DebugData.Serialize(dbg);
			

			Cpu cpu = new Cpu
			{
				Memory = new PointerMemoryController(assembled),
				//Memory = new ByteArrayMemoryController(assembled),
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};

			DateTime start = DateTime.UtcNow;

			double tickrate = 66.666;
			DateTime next = DateTime.UtcNow;
			while (true)
			{
				//double target_frequency = 10000;
				double clocks = 100; // target_frequency / tickrate;
				cpu.Clock((int)clocks);

				if (cpu.Halted)
					break;

				//next = next.AddSeconds(1.0 / tickrate);
				//int ms = (int)(next - DateTime.UtcNow).TotalMilliseconds;
				//if (ms > 0)
				//	System.Threading.Thread.Sleep(ms);
			}

			DateTime end = DateTime.UtcNow;

			Console.WriteLine(end - start);
			Console.WriteLine(cpu.TimeStampCounter);
		}

		static void Main(string[] args)
        {
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string asm = null;
			
			asm = IrCompileTest();
			//ExecuteTest(asm);

			Console.ReadLine();
		}
    }
}
