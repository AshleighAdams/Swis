using System;

using Swis;
using System.Diagnostics;

namespace SwisTest
{
    class Program
    {
		static string IrCompileTest(string ir = null)
		{
			//ir = ir ?? System.IO.File.ReadAllText("TestProgram/program.ll");
			ir = ir ?? LlvmIrCompiler.CompileCpp(System.IO.File.ReadAllText("TestProgram/program.cpp"));
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

			
			double target_frequency = 10000;
			double tickrate = 10;

			Console.Write($"Push enter to execute ({target_frequency/1000} kHz): ");
			Console.ReadLine();

			DateTime start = DateTime.UtcNow;
			DateTime next = DateTime.UtcNow;
			while (true)
			{
				double clocks = target_frequency / tickrate;
				cpu.Clock((int)clocks);
				
				if (cpu.Halted)
					break;

				//next = next.AddSeconds(1.0 / tickrate);
				//int ms = (int)(next - DateTime.UtcNow).TotalMilliseconds;
				int ms = (int)(1000 / tickrate);
				if (ms > 0)
					System.Threading.Thread.Sleep(ms);
			}

			DateTime end = DateTime.UtcNow;

			Console.WriteLine();
			Console.WriteLine($"Executed {cpu.TimeStampCounter} instructions in {(end - start).TotalSeconds:0.00} seconds");
		}

		static void TestJit()
		{
			(byte[] assembled, var dbg) = Assembler.Assemble(
@"
$start:
	mov ax, -1
	sext ebx, eax, 16
	out 0, 72  ; H
	out 0, 101 ; e
	out 0, 108 ; l
	out 0, 108 ; l
	out 0, 111 ; o
	jmp $start
");

			JitCpu cpu = new JitCpu()
			{
				Memory = new PointerMemoryController(assembled),
			};

			while(true)
				cpu.Clock(10);
		}

		static void Main(string[] args)
        {
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string asm = null;

			//asm = IrCompileTest();
			//ExecuteTest(asm);

			TestJit();

			Console.ReadLine();
		}
    }
}
