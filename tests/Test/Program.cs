using Swis;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SwisTest
{
	internal class Program
	{
		private static string IrCompileTest(string ir = null)
		{
			//ir = ir ?? System.IO.File.ReadAllText("TestProgram/program.ll");
			ir = ir ?? LlvmIrCompiler.CompileCpp(System.IO.File.ReadAllText("TestProgram/program.cpp"));
			string asm = LlvmIrCompiler.Compile(ir);
			Console.WriteLine(asm);
			return asm;
		}

		class SimpleLineIO : ILineIO
		{
			public byte StandardInput { get; set; }
			
			byte ILineIO.ReadLineValue(ushort line)
			{
				return StandardInput;
			}
			
			void ILineIO.WriteLineValue(ushort line, byte value)
			{
				Console.Write((char)value);
			}
		}
		
		private static void ExecuteTest(string asm)
		{
			int clocks = 100;
			int delay = 10;
			(byte[] assembled, var dbg) = Assembler.Assemble(asm);

			System.IO.File.WriteAllBytes("TestProgram/program.bin", assembled);
			System.IO.File.WriteAllText("TestProgram/program.dbg", DebugData.Serialize(dbg));

			RemoteDebugger dbger = null;
			try
			{
				TcpClient cl = new TcpClient();
				cl.Connect("localhost", 1337);

				dbger = new RemoteDebugger(cl.GetStream(), dbg: dbg, flush: true);
			}
			catch { }

			SimpleLineIO io = new SimpleLineIO();
			var cpu = new JittedCpu(new PointerMemoryController(assembled), io)
			{
				Debugger = dbger,
			};
			
			new Thread(delegate ()
			{
				while (true)
				{
					io.StandardInput = (byte)Console.ReadKey(true).KeyChar;
					cpu.Interrupt((uint)Swis.Interrupts.InputBase + 0);
				}
			})
			{
				IsBackground = true,
				Name = "stdin interrupter"
			}.Start();

			DateTime start = DateTime.UtcNow;
			DateTime next = DateTime.UtcNow;

			while (!cpu.Halted)
			{
				cpu.Clock(clocks);
				Thread.Sleep(delay);
			}

			DateTime end = DateTime.UtcNow;
			Console.WriteLine();
			Console.WriteLine($"Executed {cpu.TimeStampCounter} instructions in {(end - start).TotalMilliseconds:0.00} ms");
		}

		private class TestDebugger : IExternalDebugger
		{
			private bool @break = false;
			public override bool Clock(CpuBase cpu)
			{
				if (cpu.TimeStampCounter % 3 == 0 && @break)
					return @break = false;
				return @break = true;
			}
		}

		private static void TestJit()
		{
			(byte[] assembled, var dbg) = Assembler.Assemble(
				@"$start:
				mov eax, ebx
				mov ebx, ecx
				mov edx, eex
				mov eax, ebx
				mov ebx, ecx
				mov edx, eex
				mov eax, ebx
				mov ebx, ecx
				mov edx, eex
				jmp $start");

			JittedCpu jit = new JittedCpu(new PointerMemoryController(assembled), new NullLineIO())
			{
				Debugger = new TestDebugger(),
			};

			while (!jit.Halted)
				jit.Clock(100);
			
			Console.WriteLine("done");
			Console.ReadLine();
		}

		private static void Main(string[] args)
		{
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string asm = null;

			asm = IrCompileTest();
			//asm = System.IO.File.ReadAllText("TestProgram/program.asm");
			//asm = System.IO.File.ReadAllText("TestProgram/interrupt-test.asm");

			ExecuteTest(asm);

			Console.ReadLine();

			//TestJit();

		}
	}
}
