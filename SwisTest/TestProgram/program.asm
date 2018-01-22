﻿$@_Z11alloca_testsPs:
	add sp, sp, 10; alloca
	mov ptr16 [bp + 0], ptr16 [bp - 6]
	mov ptrptr [bp + 4], ptrptr [bp - 4]
	mov ptr16 [bp + 8], 5
	mov %1:16, ptr16 [bp + 0]
	movsx %conv:32, %1:16
	mov %2:16, ptr16 [bp + 8]
	movsx %conv1:32, %2:16
	cmp %conv:32, %conv1:32
	jg $@_Z11alloca_testsPs_label_3
	jmp $@_Z11alloca_testsPs_label_7

	$@_Z11alloca_testsPs_label_3:
	mov %4:16, ptr16 [bp + 0]
	movsx %conv2:32, %4:16
	mov %5:ptr, ptrptr [bp + 4]
	mov %6:16, ptr16 [%5:ptr]
	movsx %conv3:32, %6:16
	mul %mul:32, %conv2:32, %conv3:32
	mov %conv4:16, %mul:32; trunc
	mov ptr16 [bp + 8], %conv4:16
	jmp $@_Z11alloca_testsPs_label_7

	$@_Z11alloca_testsPs_label_7:
	mov %8:16, ptr16 [bp + 8]
	mov ptr16 [bp - 8], %8:16
	ret



; -8  ret  int16 return_value
; -7
; -6  arg  int16 value
; -5
; -4  arg  int16* other
; -3
; -2
; -1
;  0  var  int16 value_copy
; +1
; +2  <aligning to 4 bytes>
; +3  <aligning to 4 bytes>
; +4  var  int16* other_copy
; +5
; +6
; +7
; +8  var  int16 x
; +9
































; http://kripken.github.io/mloc_emscripten_talk/llvm.html#/32
; https://github.com/nael8r/How-To-Write-An-LLVM-Register-Allocator/blob/master/HowToWriteAnLLVMRegisterAllocator.rst

mov cp, $call_stack
mov sp, $stack
mov bp, sp
call $@main
halt

$call_stack:
	.data pad 128
$stack:
	.data pad 1024

$@_ZZ4mainE3msg:
	.data string "Hello, world!\x0a\x00"

$@_Z3outjh: ; void(u32, u8)
	out ptr32 [bp - 5], ptr8 [bp - 1]
	ret

$@_Z5printPc: ; void(u8*)
	; no need to allocate more memory, use it from the args
	$loop:
		; while (*str != 0)
		mov ta, [bp - 4] ; ptr = 4 bytes 
		cmp ptr8 [ta], 0
		je $end_loop

		; void out(0, *str);
		push 0            ; 0
		mov ta, [bp - 4] ; str
		push ptr8 [ta]    ; *str
		call $@_Z3outjh
		sub sp, sp, 5 ; pop the args (5 bytes)

		; str++
		mov ta, [bp - 4]
		add [bp - 4], ta, 1
		jmp $loop
	$end_loop:
	ret

$@main:
	; START FUNC CALL
	; push return placeholders
	nop
	; push args
	push $@_ZZ4mainE3msg
	; do the call
	call $@_Z5printPc
	; pop the args
	pop ta
	; pop the returns
	nop
	; END FUNC CALL

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