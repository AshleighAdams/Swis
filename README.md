# Swis

Simple Wire Instruction Set, intended for an emulated CPU in a sandbox-style game.
Includes a super-bad-but-gets-the-job-done IR to SwisASM compiler, assembler, interpreted CPU, and jitted CPU.

[![](https://codescene.io/projects/6331/status.svg) Get more details at **codescene.io**.](https://codescene.io/projects/6331/jobs/latest-successful/results)

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
var cpu = new Swis.JittedCpu()
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

Each of the address parts can specify a register or a constant (encoded as a signed int).
If a constant can be encoded in 5 bits<sup>2a</sup>, 13 bits<sup>2b</sup>, or
21 bits<sup>2c</sup> then it will be encoded in 0, 1, or 2 extra bytes respectively.
Above 21 bits, it will be encoded in 4 extra bytes, with 5 unused bits.

<sub>
	<sup>1</sup> Segments are not finalized, and are likely to be removed if I find a better use for those bits. <br/>
	<sup>2a</sup> [-16, 15]&emsp;
	<sup>2b</sup> [-8192, 8191]&emsp;
	<sup>2c</sup> [-2097152, 2097151]<br/>
	<!--<sup>2a</sup> -16 &le; &#x1d465; &le; 15 &emsp;
	<sup>2b</sup> -8192 &le; &#x1d465; &le; 8191 &emsp;
	<sup>2c</sup> -2,097,152 &le; &#x1d465; &le; 2,097,151 <br/>-->
</sub>

## Registers

Registers specify their size by the first and last letter.
The following are the register sizes possible, demonstrated on the general purpose A register and the base pointer register.


| Bits               | Example       |
| ------------------ | ------------- |
| 8 bits             | `al`, `bpl`  |
| 16 bits            | `ax`, `bp`   |
| 32 bits            | `eax`, `ebp` |
| 64 bits<sup>3</sup>| `rax`, `rbp` |

<sub>
	<sup>3</sup> 64 bit is not currently used, but infrastructure is in place to support it in the future. <br/>
</sub>

### Special Registers

 - Time Stamp Counter (`etsc`): Increases by 1 with every instruction executed.
 - Instruction Pointer (`eip`): Points to the next instruction to be executed.
 - Stack Pointer (`esp`): Points to the next free space on the stack.
   The stack grows up, i.e. add to it to allocate, and subtract to deallocate.
 - Base Pointer (`ebp`): Points to the current frame pointer.  Note, the `call` and `ret`
   instructions control this register automatically, which also makes it easy to unwind the stack.
 - Flag (`eflag`): Stores various flags about the system, such as halted and compare results.
 - Protected Mode (`epm`): Not used currently, will be used to control privileges.
 - Protected Interrupt (`epi`): Store the interrupt mode, the location of the Interrupt Vector Table,
   and such.  See [Interrupts](#interrupts) for more information.

### General Purpose Registers

The general purpose registers are `eax` thru `elx`.

## Interrupts

Interrupts to a CPU can be raised by either calling `ICpu.Interrupt(uint code)`, or the `int` instruction being executed by the compiler.
Upon an interrupt being raised, the interrupt code is added to a queue. The when of the interrupt is handled depends upon whether you're using the `InterpretedCpu` or the `JittedCpu`. The former provides the guarantee that the interrupt is handled before the next instruction is executed, while the latter JITed CPU only provides that guarantee when the `int` instruction is used.
If `JittedCpu.Interrupt()` is used, the guarantee is relaxed to the start of the next `Clock()`, or the start of the next JIT instruction batch, which are currently 16 instructions in length.

Once the CPU has begun to handle the interrupts, the mode from the interrupt register will be read, and act in 1 of 4 ways. 3 of these are trivial, and are as follows:

1. disabled-silent (`0b00`): Default mode, clear the queue without handling any interrupts.
1. queued (`0b10`): Yield back to the CPU without clearing the queue or handling any interrupts.
1. disabled-fault (`0b11`): Halt the CPU, no further instructions shall be executed.

If the mode was none of the above, then it means the CPU is ready to handle an interrupt from the queue, assuming one exists.
The Interrupt Vector Table's (IVT) memory location will be read from the Protected Interrupt register's (`epi`) lower 8 bits, then shifted left by 8.
This allows the IVT to be located anywhere below 64K so long as it is 256-byte aligned. The IVT is a fixed array of 256 x 32bit pointers.

Under normal operation, once the IVT location is known, the interrupt address is read from the table, and the interrupt mode set to queued. Registers `eip`, `ebp`, and `flags` are pushed to the stack. Additionally, if the interrupt code is extended (greater than or equal to 255), the exact code is also pushed onto the stack. The instruction pointer `eip` is then set to the IVT's entry for that interrupt&mdash;with interrupts over 255 being clamped to 255. If the address read from the IVT is a null pointer, then a double fault interrupt will be raised instead.

Now that the CPU interrupt has been raised, the CPU will begin executing the registered interrupt routine as a part of its normal instruction path during subsequent clock cycles. The code here must be careful to restore any registers back to their original states upon returning from the interrupt with the `iret` instruction, which will then restore the registers `esp`, `ebp`, `flags`, and finally the original `eip`, at which point the CPU state will be restored to before the interrupt was handled, returning execution as if nothing had changed.

Interrupts can be enabled or disabled using the set interrupt instruction (`sti`) and clear interrupt instruction (`cli`).

A full example of using interrupts can be found in `Test/TestProgram/interrupt-test.asm`. Here are the relevant parts:

```asm
; setup interrupts
;; set the vector table location
shr epi, $interrupt_vector_table, 8 
and epi, epi, 255
;; set up interrupt #251 to handle stdin
mov [$interrupt_vector_table + 251 * 4], $int251_stdin
;; activate them
sti

; carve out 1K of memory to store our IVT
.align 256
$interrupt_vector_table:
	.data pad 1024

; interrupt #251: buffer stdin to a ring buffer so data isn't missed
$stdin_buff:
	.data pad 16
$stdin_buffreadpos:
	.data int32 0
$stdin_buffwritepos:
	.data int32 0
$int251_stdin:
	; read the input line before the data changes into the stack
	in ptr8 [ebp + 0], 0
	; repurpose the eflag register as some temporary storage
	; as iret will restore this, wrap the value around the ring buffer's size
	modu eflag, ptr32 [$stdin_buffwritepos], 16
	; store the data read from the first instruction
	mov ptr8 [$stdin_buff + eflag], ptr8 [ebp + 0]
	; incremeant the next address to write to
	add ptr32 [$stdin_buffwritepos], ptr32 [$stdin_buffwritepos], 1
	; return control back, will restore eflags for us
	iret
```

And the accompanying C# code:

```cs
byte line0_in = 0;
cpu.LineRead = (line) => line0_in; // don't block on IO

// ...

line0_in = (byte)'H';
cpu.Interrupt((uint)Swis.Interrupts.InputBase + 0);
```



## Debugger

The CPU communicates to the debugger by setting the `Cpu`'s `Debugger` property 
to an instance of a `RemoteDebugger`, which will pipe the necessary information to and from the
debugger that it connects to over a TCP stream.<sup>4</sup>

![](Images/swisual-debugger-disassembler.png?raw=true "Swisual Debugger")

<sub>
	<sup>4</sup> Modifying the `Debugger` property on a `JittedCpu` will cause the JIT cache to be flushed.
</sub>

### Features

 - Set breakpoints.
 - Step into, out, and over.
 - View locals.
 - Inspect the call stack.
 - Reset/halt the CPU.
 - Disassemble the program as it runs

Note, to view the locals, and labels for the call stack, and full assembly beyond the disassembly
then debugging symbols must be loaded.

#### Future Features

 - Add support for loading the source files that generated the assembly, along with
   + breakpoint support,
   + viewing the current execution position, and
   + inspecting locals from the code editor.

## LLVM-IR

The LLVM-IR translator is strictly works-on-my-machine.
It needs to be rebuilt from the ground up, but for now, it is sufficient.

Compile your IR by calling `string asm = LlvmIrCompiler.Compile(ircode)`.

## JittedCpu Internals

The `JittedCpu` leverages a `System.Linq.Expressions`'s expression trees to produce IL code at runtime, compiling an expression tree until either `JitInstructionBatchSize` (default: 16) total instructions have been processed, or a potentially branching instruction is reached. This expression tree is then compiled into IL and inserted as a function into a cache, mapping memory addresses to batches. These batches return the number of instructions executed, and the new instruction pointer.

Because self modifying programs are possible, the JIT cache must be cleared if any writes happen to jitted memory areas. For performance reasons, this is done by keeping track of the upper and lower bounds, and thus it is encouraged to not mix data and code sections to reduce or eliminate cache invalidations. Given that JITing code is a very expensive operation and this CPU is intended for emulating CPUs in video games (such as Wiremod's ZCPU in Garry's Mod), the property `JitCostFactor` is exposed to consume extra cycles when JITing to prevent the rouge programs performing denial of service attacks against the host.

Attaching a debugger automatically invalidates the JIT cache, as debugging related operations can be omitted for performance reasons.