.align 2
$@stdout_line:
	.data int16 0

$@_Z3putcs:
	.src func "void _Z3putcs(i8 c, i16 line)"
	.src local "line" "i16" -10 2
	.src local "c" "i8" -11 1
	mov %1:16, ptr16 [ebp - 10]
	mov %2:8, ptr8 [ebp - 11]
	out %1:16, %2:8
	ret

$@_Z4putsPKc:
	.src func "void _Z4putsPKc(i8* str)"
	.src local "line" "i16" 0 2
	.src local "str" "i8*" -12 4
	add esp, esp, 2 ; alloca
	mov %1:16, ptr16 [$@stdout_line]
	mov ptr16 [ebp + 0], %1:16
	jmp $@_Z4putsPKc_label_2
	
	$@_Z4putsPKc_label_2:
	mov %3:ptr, ptr32 [ebp - 12]
	mov %4:8, ptr8 [%3:ptr]
	jz %4:8, $@_Z4putsPKc_label_10
	jmp $@_Z4putsPKc_label_5
	
	$@_Z4putsPKc_label_5:
	mov %6:ptr, ptr32 [ebp - 12]
	mov %7:8, ptr8 [%6:ptr]
	mov %8:16, ptr16 [ebp + 0]
	add esp, esp, 3 ; allocate space for return and arguments
	mov ptr8 [esp - 3], %7:8 ; copy arg #1
	mov ptr16 [esp - 2], %8:16 ; copy arg #2
	call $@_Z3putcs
	sub esp, esp, 3 ; pop args and ret
	mov %9:ptr, ptr32 [ebp - 12]
	mov ptr32 [ebp - 12], %9:ptr + 1
	jmp $@_Z4putsPKc_label_2
	
	$@_Z4putsPKc_label_10:
	mov %11:16, ptr16 [ebp + 0]
	add esp, esp, 3 ; allocate space for return and arguments
	mov ptr8 [esp - 3], 10 ; copy arg #1
	mov ptr16 [esp - 2], %11:16 ; copy arg #2
	call $@_Z3putcs
	sub esp, esp, 3 ; pop args and ret
	ret
