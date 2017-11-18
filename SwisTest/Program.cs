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
	data float 5
number_b:
	data float 10
result:
	data float 0
call_stack:
	data pad 32
stack:
	data pad 32
main:
	// setup the stacks
	mov cp, $call_stack
	mov sp, $stack

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
			byte[] assembled = Assembler.Assemble(asm);
			Emulator emu = new Emulator();
			byte[] mem = emu.Memory = new byte[256];

			for (int i = 0; i < assembled.Length; i++)
				mem[i] = assembled[i];

			while (true)
			{
				emu.Clock(1);

				Caster c; c.F32 = 0;
				c.ByteA = emu.Memory[13 + 0];
				c.ByteB = emu.Memory[13 + 1];
				c.ByteC = emu.Memory[13 + 2];
				c.ByteD = emu.Memory[13 + 3];
				Console.WriteLine(c.F32);
			}
        }
    }
}
