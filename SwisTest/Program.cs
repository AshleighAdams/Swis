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
	data int 5
number_b:
	data int 10
result:
	data int 0
call_stack:
	data pad 32
stack:
	data pad 32
main:
	mov cp, $call_stack
	mov sp, $stack

	load ta, $number_a
	load tb, $number_b
	push ta // args
	push tb
	call $add_maybe
	pop tc // the result
	store $result, tc
	halt
add_maybe:
	pop tb
	pop ta
	cmp ta, tb
	je $skip
	add ta, ta, tb
skip:
	push ta
	ret
";
			byte[] assembled = Assembler.Assemble(asm);
			Emulator emu = new Emulator();
			byte[] mem = emu.Memory = new byte[256];

			for (int i = 0; i < assembled.Length; i++)
				mem[i] = assembled[i];
			
			while (true)
				emu.Clock(1);
        }
    }
}
