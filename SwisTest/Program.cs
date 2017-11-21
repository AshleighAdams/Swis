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
call_stack:
	.data pad 64
stack:
	.data pad 64
msg:
	.data string ""Hello, world!\x00""
main:
	// setup the stacks
	mov cp, $call_stack
	mov sp, $stack
	mov bp, sp

	// call the function
	mov ta, $msg
	push ta
	call $print
	halt

print:
		pop ta         // read the ptr param
		mov tb, 1      // inc amount, sizeof(ascii char)
		mov tc8, 0     // compare to this
	loop:
		load td8, ta   // fetch the next char
		cmp td8, tc8   // is it null?
		je $end_print  // then jump to the end
		out td8, 0     // else output it
		add ta, ta, tb // and increase the pointer
		jmp $loop      // loop back for the next char
	end_print:
		ret
";

			(byte[] assembled, var labels) = Assembler.Assemble(asm);


			Emulator emu = new Emulator();
			emu.Memory = assembled;

			while (!emu.Halted)
				emu.Clock(1);

			Console.ReadLine();
		}
    }
}
