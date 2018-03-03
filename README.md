# Swis

Simple Wire Instruction Set, intended for an emulated CPU in a sandbox-style game.
Includes a super-bad-but-gets-the-job-done IR to SwisASM compiler, assembler, interpreted CPU, and jitted CPU.

## Sample Code

```asm
; setup the stack and base pointer
mov esp, $stack
mov ebp, esp

; call main, then halt
add esp, esp, 4            ; allocate stack space for the argument
mov ptr32 [esp - 4], $msg  ; both of these are equivalent to a `push $msg`
call $print
sub esp, esp, 4            ; pop to nowhere
halt                       ; finish execution

$print:
	mov eax, [ebp - 12]       ; -1 to -8 = call return info, -12 = first 4 byte arg

	$main_loop:
	jz ptr8 [eax], $main_end  ; jump if zero to

	out 0, ptr8 [eax]         ; write a character out

	add eax, eax, 1           ; and re-loop
	jmp $main_loop
	$main_end:
	ret

$msg:
	.data ascii "Hello, world!\x0a\x00"

.align 4
$stack:
	.data pad 128 ; give the stack 128 bytes
```

```C#
(byte[] assembled, var dbg) = Swis.Assembler.Assemble(File.ReadAllText("program.asm"));

byte line0_in = 0;
var cpu = new Swis.JitCpu()
{
	Memory = new PointerMemoryController(assembled),

	LineWrite = (line, what) => Console.Write((char)what),
	//LineRead = (line) => (char)Console.Read(), // < this will cause `in dest, line` to block and freeze the cpu while waiting for input
	LineRead = (line) => line0_in, // use the interrupt version, so we won't freeze (if desired); note: interrupt handler not included in this demo, so it will be silently ignored
};

while(!cpu.Halted)
{
	cpu.Clock(1000); // execute 1k instructions

	if (Console.KeyAvailable)
	{
		line0_in = (byte)Console.ReadKey(true).KeyChar;
		cpu.Interrupt((uint)Swis.Interrupts.InputBase + 0); // interrupt for io input #0
	}
}
```

## Assembly Syntax

The syntax closely resembles Intel's syntax.  The main differences are:

 - indirection sizes are, if implicit, always the size of a pointer (32 bits),
	and if explicit, the size specified in bits in the form of `ptr8 [...]` as opposed to `byte ptr [...]`;
 - directives do not use a `.data` or `.text` segment;
 - the type an instruction operates on is specified after, i.e. `addf` as opposed to `fadd`;
 - there is no need for load effective address, as `lea eax, [ebx + ecx]` is equivalent to `mov eax, ebx + ecx`; and
 - placeholders (labels) are explicitly referenced by prefixing them with `$` (e.g. `mov sp, $stack`).

### Directives

Directives are prefaced with a period, and instruct the assembler to do something,
such as including debugging information (`.src`), inserting padding (`.data pad N`),
aligning (`.align N`), and so on.

## Operands

Operands in Swis are fully orthogonal; each operand can use indirection, segments<sup>1</sup>,
along with one of the following addressing modes:

 1. `a`
 2. `a + b`
 3. `c * d`
 4. `a + b + c * d`

When subtracting a constant, i.e. `ebp - 4`, the subtracted constant is encoded as `ebp + -4`,
this means operations such as `eax - ebx` are not possible—tho it may be encoded as `eax + 0 + ebx * -1`.

Each of the address parts can specify a register or a constant.
If a constant can be encoded in 4 bits, 12 bits, or 20 bits then it will be encoded in 0, 1, or 2 extra bytes respectively.
Above 20 bits, it will be encoded in 4 extra bytes, with 4 unused bits.

<sup><sup>1</sup> Segments are not finalized, and are likely to be removed if I find a better use for those bits.</sup>

## Registers

Registers specify their size by the first and last letter.
The following are the register sizes possible, demonstrated on the general purpose A register and the base pointer register.


| Bits               | Example       |
| ------------------ | ------------- |
| 8 bits             | `al`, `bpl`  |
| 16 bits            | `ax`, `bp`   |
| 32 bits            | `eax`, `ebp` |
| 64 bits<sup>2</sup>| `rax`, `rbp` |

<sup><sup>2</sup> 64 bit is not currently used, but infrastructure is in place to support it in future.</sup>

### Special Registers

 - Time Stamp Counter (`etsc`): Increases by 1 with every instruction executed.
 - Instruction Pointer (`eip`): Points to the next instruction to be executed.
 - Stack Pointer (`esp`): Points to the next free space on the stack.
   The stack grows up, i.e. add to it to allocate, and subtract to deallocate.
 - Base Pointer (`ebp`): Points to the current frame pointer.  Note, the `call` and `ret` instructions control
   this register automatically, which also makes it easy to unwind the stack.
 - Flag (`eflag`): Stores various flags about the system, such as halted and compare results.
 - Protected Mode (`epm`): Not used currently, will be used to control privileges.
 - Protected Interrupt (`epi`): Store the interrupt mode, the location of the Interrupt Vector Table,
   and such.  See [Interrupts](#interrupts) for more information.

### General Purpose Registers

The general purpose registers are `eax` thru `elx`.

## Interrupts

TOWRITE: this, talk about the IVT, enabling interrupts, and cli/sti.

## Debugger

The CPU communicates to the debugger by setting the `Cpu`'s `Debugger` property to a `StreamDebugger`,
which will pipe the necessary information to and from the debugger that it connects to over a TCP stream.
Modifying the `Debugger` property on a `JitCpu` will cause the JIT cache to be flushed.

![](Images/swis-wpf-debugger.png?raw=true "WPF Debugger")

### Features

 - Set breakpoints.
 - Step into, out, and over.
 - View locals.
 - Inspect the call stack.
 - Reset/halt the CPU.

Note, to view the current instruction being executed, the locals, and labels for the call stack,
then debugging symbols must be loaded.

#### Future Features

 - Decode instructions and slowly build up the program as it executes without needing to load
   the debugging symbols.
 - Add support for loading the source files that generated the assembly, along with
   + breakpoint support,
   + viewing the current execution position, and
   + inspecting locals from the code editor.

## LLVM-IR

The LLVM-IR translator is strictly works-on-my-machine.
It needs to be rebuilt from the ground up, but for now, it is sufficient.

Compile your IR by calling `string asm = LlvmIrCompiler.Compile(ircode)`.

## JIT

TOWRITE: this