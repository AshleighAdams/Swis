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


			var cpu = new JitCpu
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

			JitCpu cpu = new JitCpu()
			{
				Memory = new PointerMemoryController(assembled),
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};

			InterpretedCpu old = new InterpretedCpu()
			{
				Memory = new PointerMemoryController(assembled),
				//Debugger = new StreamDebugger(Console.Out, dbg),
			};
			
			TimeSpan jitfirst, jit, notjit;
			DateTime start, end;

			{
				start = DateTime.UtcNow;
				//while (!old.Halted)
					old.Clock(1000);
				end = DateTime.UtcNow;
				notjit = end - start;
			}

			{
				start = DateTime.UtcNow;
				//while (!cpu.Halted)
					cpu.Clock(1000);
				end = DateTime.UtcNow;
				jitfirst = end - start;

				cpu.Reset();

				start = DateTime.UtcNow;
				//while(!cpu.Halted)
					cpu.Clock(1000);
				end = DateTime.UtcNow;
				jit = end - start;
			}
			
			Console.WriteLine($"JIT(1): {jitfirst.TotalMilliseconds:0.00}ms; JIT: {jit.TotalMilliseconds:0.00}ms; Interpreted: {notjit.TotalMilliseconds:0.00}ms");
		}

		static void Main(string[] args)
        {
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			string asm = null;

			asm = IrCompileTest();
			ExecuteTest(asm);

			//TestJit();

			Console.ReadLine();
		}
    }
}
