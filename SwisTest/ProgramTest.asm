; http://kripken.github.io/mloc_emscripten_talk/llvm.html#/32
; https://github.com/nael8r/How-To-Write-An-LLVM-Register-Allocator/blob/master/HowToWriteAnLLVMRegisterAllocator.rst


$boot:
	mov cp, $call_stack
	mov sp, $stack
	mov bp, sp
	call $main
	halt
$call_stack:
	.data pad 64 ; 8 bytes per call (bp and ret addr). can go 8 calls deep
$stack:
	.data pad 64
$marker:
	.data hex f0f0f0f0
$msg:
	.data string "Hello, world!\x0a\x00"
$main:
	mov ta, $msg       ;	uint8_t* ga = &msg;
	$loop:
		cmp ptr8 [ta], 0  ;	while(*ta != 0)
		je $end_loop   ;	{
		out 0, ptr8 [ta]  ;		out(0, *ta);

		add ta, ta, 1  ;		ga = ga + 1;
		jmp $loop      ;	}
	$end_loop:
	jmp $main
	ret