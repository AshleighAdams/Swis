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

			string asm = @"
jmp $main
number_a:
	data float 3.14
number_b:
	data float 1.57
result:
	data float 0
call_stack:
	data pad 64
stack:
	data pad 64
main:
	// setup the stacks
	mov cp, $call_stack
	mov sp, $stack
	mov bp, sp

	load ta, $number_a
	load tb, $number_b
	push ta // args
	push tb
	call $do_it
	pop tc // the result
	store $result, tc
	halt
do_it:
	pop tb
	pop ta
	sin ta, ta
	cos tb, tb
	addf ta, ta, tb
	push ta
	ret
";
			asm = @"
jmp $main
result:
	data hex ffffffff
main:
	mov ta8, 255
	mov tb8, 10
	add tc8, ta8, tb8
	store $result, tc32
	halt
";

			(byte[] assembled, var labels) = Assembler.Assemble(asm);


			Emulator emu = new Emulator();
			byte[] mem = emu.Memory = new byte[256];

			for (int i = 0; i < assembled.Length; i++)
				mem[i] = assembled[i];

			while (!emu.Halted)
				emu.Clock(1);
			
			Caster c; c.I32 = 0;
			c.ByteA = emu.Memory[labels["result"] + 0];
			c.ByteB = emu.Memory[labels["result"] + 1];
			c.ByteC = emu.Memory[labels["result"] + 2];
			c.ByteD = emu.Memory[labels["result"] + 3];
			Console.WriteLine(c.I32);
			Console.ReadLine();
		}
    }
}
