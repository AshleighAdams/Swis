


; http://kripken.github.io/mloc_emscripten_talk/llvm.html#/32
; https://github.com/nael8r/How-To-Write-An-LLVM-Register-Allocator/blob/master/HowToWriteAnLLVMRegisterAllocator.rst

mov sp, $stack
mov bp, sp
call $@main
halt

$stack:
	.data pad 1024

$@_ZZ4mainE3msg:
	.data string "Hello, world! \x00"

$@_Z3outjh: ; void(u32, u8)
	out ptr32 [bp - 13], ptr8 [bp - 9]
	ret

$@_Z5printPc: ; void(u8*)
	; no need to allocate more memory, use it from the args
	$loop:
		; while (*str != 0)
		mov ta, [bp - 12] ; ptr = 4 bytes 
		cmp ptr8 [ta], 0
		je $end_loop

		; void out(0, *str);
		push 0            ; 0
		mov ta, [bp - 12] ; str
		push ptr8 [ta]    ; *str
		call $@_Z3outjh
		sub sp, sp, 5 ; pop the args (5 bytes)

		; str++
		mov ta, [bp - 12]
		add [bp - 12], ta, 1
		jmp $loop
	$end_loop:
	ret

$@main:
	; START FUNC CALL
	; push return placeholders
	;nop
	; push args
	push $@_ZZ4mainE3msg
	; do the call
	call $@_Z5printPc
	; pop the args
	pop ta
	; pop the returns
	;nop
	; END FUNC CALL
	jmp $@main
	ret






















































; calling convention
;
; push const32 0 ; ret1
; push const32 0 ; ret2
; push const32 arg1
; push const32 arg2
; call $func
; sub sp, sp, sizeof(arg1) + sizeof(arg2)
; ; returns are on the stack




;$boot:
;	mov cp, $call_stack
;	mov sp, $stack
;	mov bp, sp
;	call $main
;	halt
;$call_stack:
;	.data pad 64 ; 8 bytes per call (bp and ret addr). can go 8 calls deep
;$stack:
;	.data pad 64
;$marker:
;	.data hex f0f0f0f0
;$msg:
;	.data string "Hello, world!\x0a\x00"
;$main:
;	mov ta, $msg       ;	uint8_t* ga = &msg;
;	$loop:
;		cmp ptr8 [ta], 0  ;	while(*ta != 0)
;		je $end_loop   ;	{
;		out 0, ptr8 [ta]  ;		out(0, *ta);
;
;		add ta, ta, 1  ;		ga = ga + 1;
;		jmp $loop      ;	}
;	$end_loop:
;	jmp $main
;	ret