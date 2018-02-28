mov esp, $stack
mov ebp, esp
add esp, esp, 12 ; int(int, char*) ; add this just in case it's defined with the prototypes
call $@main
sub esp, esp, 12
halt


$@_Z7displayv:
	.src func "void _Z7displayv()"
	.align 1024
	.data pad 12288
	ret

$@_Z3outjh:
	.src func "void _Z3outjh(i32 port, i8 val)"
	.src local "val" "i8" -9 1
	.src local "port" "i32" -13 4
	
	
	out ptr32 [ebp - 13], ptr8 [ebp - 9]
	ret

$@_Z3getv:
	.src func "i32 _Z3getv()"
	.src local "ret" "i32" 0 4
	.src local "return" "i32" -12 4
	add esp, esp, 4 ; alloca
	in ptr32 [ebp + 0], 0
	
	mov ptr32 [ebp - 12], ptr32 [ebp + 0]
	ret

$@_Z3putc:
	.src func "void _Z3putc(i8 c)"
	.src local "c" "i8" -9 1
	
	out 0, ptr8 [ebp - 9]
	ret

$@_Z4putsPKc:
	.src func "void _Z4putsPKc(i8* str)"
	.src local "str" "i8*" -12 4
	
	
	$@_Z4putsPKc_label_1:
	mov eax, ptr32 [ebp - 12]
	
	sext eax, ptr8 [eax], 8
	jz eax, $@_Z4putsPKc_label_8
	
	
	$@_Z4putsPKc_label_4:
	mov eax, ptr32 [ebp - 12]
	; call void @_Z3putc(i8)
	add esp, esp, 1 ; allocate space for return and arguments
	mov ptr8 [esp - 1], ptr8 [eax] ; copy arg #1
	call $@_Z3putc
	sub esp, esp, 1 ; pop args and ret
	
	mov eax, ptr32 [ebp - 12]
	; constexp getelementptr: %incdec.ptr
	mov ptr32 [ebp - 12], eax + 1
	jmp $@_Z4putsPKc_label_1
	
	$@_Z4putsPKc_label_8:
	ret

$@main:
	.src func "i32 main()"
	.src local "pix" "%struct.pixel*" 16 4
	.src local "x" "i32" 12 4
	.src local "y" "i32" 8 4
	.src local "display" "%struct.pixel*" 4 4
	.src local "retval" "i32" 0 4
	.src local "return" "i32" -12 4
	add esp, esp, 20 ; alloca
	mov ptr32 [ebp + 0], 0
	
	mov ptr32 [ebp + 4], 1024
	mov ptr32 [ebp + 8], 0
	
	
	$@main_label_1:
	
	cmp ptr32 [ebp + 8], 64
	jge $@main_label_20
	
	
	$@main_label_3:
	mov ptr32 [ebp + 12], 0
	
	
	$@main_label_4:
	
	cmp ptr32 [ebp + 12], 64
	jge $@main_label_17
	
	
	$@main_label_6:
	mov eax, ptr32 [ebp + 4]
	
	mul ebx, ptr32 [ebp + 8], 64
	
	add ebx, ebx, ptr32 [ebp + 12]
	; constexp getelementptr: %arrayidx
	mov ptr32 [ebp + 16], eax + ebx * 3
	
	; trunc i32 -> i8
	mov eax, ptr32 [ebp + 16]
	; constexp getelementptr: %r
	mov ptr8 [eax], ptr32 [ebp + 12]
	mov eax, ptr32 [ebp + 16]
	; constexp getelementptr: %g
	mov ptr8 [eax + 1], 0
	
	; trunc i32 -> i8
	mov eax, ptr32 [ebp + 16]
	; constexp getelementptr: %b
	mov ptr8 [eax + 2], ptr32 [ebp + 8]
	
	
	$@main_label_15:
	
	add ptr32 [ebp + 12], ptr32 [ebp + 12], 1
	
	jmp $@main_label_4
	
	$@main_label_17:
	
	
	$@main_label_18:
	
	add ptr32 [ebp + 8], ptr32 [ebp + 8], 1
	
	jmp $@main_label_1
	
	$@main_label_20:
	mov ptr32 [ebp - 12], 0
	ret


.align 4
$stack:
	.data pad 1024
