# Swis

LLVM-IR to Swis

For emulated CPUs in sandbox video games.
Because it's not backed by actual hardware, registers aren't specialized or annything.
It's also for fun, not for actual computation.

## Instructions

```
struct register
{
	union
	{
		int8   s8;
		int16  s16;
		int32  s32;
	};
};

struct registers
{
	register zero; // always reads 0
	register ip;
	register sp;
	register flags;
	
	register r0, r1, r2, r3, r4; // will be the same as you left 'em
	register t0, t1, t2, t3, t4; // will most likely change
	
}

stack int offset, int size // set the bounds of the stack
push reg // the stack down
pop reg // push the stack up

call reg
call addr
ret

load reg, addr, len
store reg, addr, len
const reg, value

div x, l, r // the method these eventually call depends on the type of
mul x, l, r
add x, l, r
sub x, l, r
sqrt x, a
pow x, l, r
log x, a
exp x, b

int // interupt
halt // end execution, requires power cycle
trap
trap
singal
```

## Example

```llvm-ir
@.str = internal constant [14 x i8] c"hello, world\0A\00"

declare i32 @printf(i8*, ...)

define i32 @main(i32 %argc, i8** %argv) nounwind {
entry:
    %tmp1 = getelementptr [14 x i8], [14 x i8]* @.str, i32 0, i32 0
    %tmp2 = call i32 (i8*, ...) @printf( i8* %tmp1 ) nounwind
    ret i32 0
}
```

```
.str:
	data 68656c6c6f202c20776f72640a00 // "hello, world\0A\00"

main:
	const t0, $.str
	push t0
		call $print // push ip64; mov ip64, $print
		pop none
	const t0, 0
		push t0
		ret

print:
	
```
