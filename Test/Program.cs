using System;

using Swis;
using System.Diagnostics;
using System.Net.Sockets;

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

			System.IO.File.WriteAllBytes("TestProgram/program.bin", assembled);
			System.IO.File.WriteAllText("TestProgram/program.dbg", DebugData.Serialize(dbg));

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

			StreamDebugger dbger = null;
			try
			{
				TcpClient cl = new TcpClient();
				cl.Connect("localhost", 1337);

				dbger = new StreamDebugger(cl.GetStream(), dbg: dbg, flush: true);
			}
			catch { }

			byte line0_in = 0;
			var cpu = new InterpretedCpu
			{
				Memory = new PointerMemoryController(assembled),
				//Memory = new ByteArrayMemoryController(assembled),
				Debugger = dbger,
				LineWrite = (line, what) => Console.Write((char)what),
				LineRead = (line) => line0_in,
			};

			DateTime start = DateTime.UtcNow;
			DateTime next = DateTime.UtcNow;

			int total_clocks = 0;
			while (!cpu.Halted)
			{
				int clocks = 100;// target_frequency / tickrate;
				total_clocks += cpu.Clock(clocks);

				System.Threading.Thread.Sleep(16);

				if (Console.KeyAvailable)
				{
					line0_in = (byte)Console.ReadKey(true).KeyChar;
					cpu.Interrupt((uint)Swis.Interrupts.InputBase + 0);
				}
			}

			DateTime end = DateTime.UtcNow;
			Console.WriteLine();
			Console.WriteLine($"Executed {cpu.TimeStampCounter} instructions in {(end - start).TotalMilliseconds:0.00} ms");
		}

		static void TestJit()
		{
			(byte[] assembled, var dbg) = Assembler.Assemble(
@"
; code
mov esp, $stack
mov ebp, esp
call $main
halt
$main:
	mov efx, 0
	$loop:
		add esp, esp, 8
			mov ptr32 [esp - 4], 5
			call $factorial
		sub esp, esp, 8
		add efx, efx, 1
		cmp efx, 100
		jl $loop
	ret

$factorial:
	; params: %n = ebp - 12
	; return: ret = ebp - 16
	; locals: %retval = ebp + 0
	add esp, esp, 4 ; alloca
	cmpu ptr32 [ebp - 12], 1
	jg $@_Z9factoriali_label_3

	$@_Z9factoriali_label_2:
	mov ptr32 [ebp + 0], 1
	jmp $@_Z9factoriali_label_6

	$@_Z9factoriali_label_3:
	mov eax, ptr32 [ebp - 12]
	sub ebx, ptr32 [ebp - 12], 1

	; call i32 @_Z9factoriali(i32)
	push eax
	add esp, esp, 8 ; allocate space for return and arguments
	mov ptr32 [esp - 4], ebx ; copy arg #1
	call $factorial
	mov ebx, ptr32 [esp - 8] ; copy return
	sub esp, esp, 8 ; pop args and ret
	pop eax
	mulu ptr32 [ebp + 0], eax, ebx

	$@_Z9factoriali_label_6:
	mov ptr32 [ebp - 16], ptr32 [ebp + 0]
	ret

; globals
.align 4
$stack:
	.data pad 1024
");

			InterpretedCpu old = new InterpretedCpu()
			{
				Memory = new PointerMemoryController(assembled),
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};

			JitCpu cpu = new JitCpu()
			{
				Memory = new PointerMemoryController(assembled),
				JitCostFactor = 0,
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};

			

			Stopwatch w = new Stopwatch();
			TimeSpan jitfirst, jit, notjit;

			w.Start();
			{
				cpu.Clock(1000);
			}
			w.Stop();
			notjit = w.Elapsed;


			cpu = new JitCpu()
			{
				Memory = new PointerMemoryController(assembled),
			};

			//cpu.Reset();
			//cpu.ClearJitCache();
			w.Restart();
			{
				cpu.Clock(1000);
			}
			w.Stop();
			jitfirst = w.Elapsed;

			cpu.Reset();
			w.Restart();
			{
				cpu.Clock(1000);
			}
			w.Stop();
			jit = w.Elapsed;
			
			Console.WriteLine($"JIT(jit): {notjit.TotalMilliseconds:0.00}ms; JIT(1): {jitfirst.TotalMilliseconds:0.00}ms; JIT: {jit.TotalMilliseconds:0.00}ms");
			Console.ReadLine();
		}

		static void Main(string[] args)
        {
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string asm = null;

			asm = IrCompileTest();
			ExecuteTest(asm);

			//TestJit();
			
		}
    }
}
