mov esp, $stack
mov ebp, esp
add esp, esp, 12 ; int(int, char*) ; add this just in case it's defined with the prototypes
call $@main
sub esp, esp, 12
halt

.align 4
$stack:
	.data pad 1024

.align 4
$@_ZL4next:
	.data int32 1
$@.str:
	.data ascii "Hello world.\x0a\x00"
$@.str.1:
	.data ascii "Execution: finished!\x0a\x00"

$@_Z7reversePci:
	; params: %length = ebp - 12
	; params: %str = ebp - 16
	; locals: %start = ebp + 0
	; locals: %end = ebp + 4
	; locals: %tmp = ebp + 8
	add esp, esp, 9 ; alloca
	mov ptr32 [ebp + 0], 0

	sub ptr32 [ebp + 4], ptr32 [ebp - 12], 1



	$@_Z7reversePci_label_2:


	cmp ptr32 [ebp + 0], ptr32 [ebp + 4]
	jge $@_Z7reversePci_label_19


	$@_Z7reversePci_label_5:
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 0]
	; getelementptr: %add.ptr = eax + ebx

	mov ptr8 [ebp + 8], ptr8 [eax + ebx]
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	; getelementptr: %add.ptr1 = eax + ebx

	mov ecx, [ebp - 16]
	mov edx, ptr32 [ebp + 0]
	; getelementptr: %add.ptr2 = ecx + edx
	mov ptr8 [ecx + edx], ptr8 [eax + ebx]

	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	; getelementptr: %add.ptr3 = eax + ebx
	mov ptr8 [eax + ebx], ptr8 [ebp + 8]

	add ptr32 [ebp + 0], ptr32 [ebp + 0], 1


	add ptr32 [ebp + 4], ptr32 [ebp + 4], -1

	jmp $@_Z7reversePci_label_2

	$@_Z7reversePci_label_19:
	ret

$@itoa:
	; params: %base = ebp - 12
	; params: %str = ebp - 16
	; params: %num = ebp - 20
	; return: ret = ebp - 24
	; locals: %retval = ebp + 0
	; locals: %i = ebp + 4
	; locals: %negative = ebp + 8
	; locals: %rem = ebp + 12
	add esp, esp, 16 ; alloca
	mov ptr32 [ebp + 4], 0
	mov ptr8 [ebp + 8], 0

	jnz ptr32 [ebp - 20], $@itoa_label_8


	$@itoa_label_2:
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	add ptr32 [ebp + 4], ebx, 1

	; getelementptr: %arrayidx = eax + ebx
	mov ptr8 [eax + ebx], 48
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	; getelementptr: %arrayidx1 = eax + ebx
	mov ptr8 [eax + ebx], 0

	mov [ebp + 0], [ebp - 16]
	jmp $@itoa_label_41

	$@itoa_label_8:

	cmp ptr32 [ebp - 20], 0
	jge $@itoa_label_14


	$@itoa_label_10:

	cmpu ptr32 [ebp - 12], 10
	jne $@itoa_label_14


	$@itoa_label_12:
	mov ptr8 [ebp + 8], 1

	sub ptr32 [ebp - 20], 0, ptr32 [ebp - 20]



	$@itoa_label_14:


	$@itoa_label_15:

	jz ptr32 [ebp - 20], $@itoa_label_30


	$@itoa_label_17:


	mod ptr32 [ebp + 12], ptr32 [ebp - 20], ptr32 [ebp - 12]


	cmp ptr32 [ebp + 12], 9
	jle $@itoa_label_23


	$@itoa_label_21:

	sub eax, ptr32 [ebp + 12], 10
	add eax, eax, 97
	jmp $@itoa_label_25

	$@itoa_label_23:

	add eax, ptr32 [ebp + 12], 48


	$@itoa_label_25:
	; trunc i32 -> i8
	mov ebx, [ebp - 16]
	mov ecx, ptr32 [ebp + 4]
	add ptr32 [ebp + 4], ecx, 1

	; getelementptr: %arrayidx10 = ebx + ecx
	mov ptr8 [ebx + ecx], eax


	div ptr32 [ebp - 20], ptr32 [ebp - 20], ptr32 [ebp - 12]

	jmp $@itoa_label_15

	$@itoa_label_30:

	and al, ptr8 [ebp + 8], 1 ; trunc to i1
	jz al, $@itoa_label_35


	$@itoa_label_32:
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	add ptr32 [ebp + 4], ebx, 1

	; getelementptr: %arrayidx12 = eax + ebx
	mov ptr8 [eax + ebx], 45


	$@itoa_label_35:
	mov eax, [ebp - 16]
	mov ebx, ptr32 [ebp + 4]
	; getelementptr: %arrayidx13 = eax + ebx
	mov ptr8 [eax + ebx], 0

	; call void @_Z7reversePci(i8*, i32)
	add esp, esp, 8 ; allocate space for return and arguments
	mov ptr32 [esp - 8], [ebp - 16] ; copy arg #1
	mov ptr32 [esp - 4], ptr32 [ebp + 4] ; copy arg #2
	call $@_Z7reversePci
	sub esp, esp, 8 ; pop args and ret


	mov [ebp + 0], [ebp - 16]


	$@itoa_label_41:

	mov [ebp - 24], [ebp + 0]
	ret

$@_Z3outjh:
	; params: %val = ebp - 9
	; params: %port = ebp - 13


	out ptr32 [ebp - 13], ptr8 [ebp - 9]
	ret

$@put:
	; params: %c = ebp - 9

	out 0, ptr8 [ebp - 9]
	ret

$@puts:
	; params: %str = ebp - 12


	$@puts_label_1:
	mov eax, [ebp - 12]

	sext eax, ptr8 [eax], 8
	jz eax, $@puts_label_8


	$@puts_label_4:
	mov eax, [ebp - 12]
	; call void @put(i8)
	add esp, esp, 1 ; allocate space for return and arguments
	mov ptr8 [esp - 1], ptr8 [eax] ; copy arg #1
	call $@put
	sub esp, esp, 1 ; pop args and ret

	mov eax, [ebp - 12]
	; getelementptr: %incdec.ptr = eax + 1
	mov [ebp - 12], eax + 1
	jmp $@puts_label_1

	$@puts_label_8:
	ret

$@rand:
	; return: ret = ebp - 12

	modu ptr32 [ebp - 12], ptr32 [$@_ZL4next], 32768

	ret

$@srand:
	; params: %seed = ebp - 12

	mov ptr32 [$@_ZL4next], ptr32 [ebp - 12]
	ret

$@main:
	; return: ret = ebp - 12
	; locals: %retval = ebp + 0
	; locals: %output = ebp + 4
	; locals: %i = ebp + 40
	add esp, esp, 44 ; alloca
	mov ptr32 [ebp + 0], 0

	; call void @puts([14 x i8]*, i32, i32)
	add esp, esp, 12 ; allocate space for return and arguments
	mov ptr32 [esp - 12], $@.str ; copy arg #1
	mov ptr32 [esp - 8], 0 ; copy arg #2
	mov ptr32 [esp - 4], 0 ; copy arg #3
	call $@puts
	sub esp, esp, 12 ; pop args and ret

	mov ptr32 [ebp + 40], 1330


	$@main_label_1:

	cmp ptr32 [ebp + 40], 1340
	jge $@main_label_6


	$@main_label_3:

	; call i32 @rand()
	add esp, esp, 4 ; allocate space for return and arguments
	call $@rand
	; copy return
	sub esp, esp, 4 ; pop args and ret

	; getelementptr: %arraydecay = ebp + 4

	; call i8* @itoa(i32, i8*, i32)
	add esp, esp, 16 ; allocate space for return and arguments
	mov ptr32 [esp - 12], ptr32 [esp - 4] ; copy arg #1
	mov ptr32 [esp - 8], ebp + 4 ; copy arg #2
	mov ptr32 [esp - 4], 10 ; copy arg #3
	call $@itoa
	mov eax, ptr32 [esp - 16] ; copy return
	sub esp, esp, 16 ; pop args and ret

	; getelementptr: %arraydecay2 = ebp + 4

	; call void @puts(i8*)
	add esp, esp, 4 ; allocate space for return and arguments
	mov ptr32 [esp - 4], ebp + 4 ; copy arg #1
	call $@puts
	sub esp, esp, 4 ; pop args and ret


	; call void @put(i8)
	add esp, esp, 1 ; allocate space for return and arguments
	mov ptr8 [esp - 1], 10 ; copy arg #1
	call $@put
	sub esp, esp, 1 ; pop args and ret



	$@main_label_4:

	add ptr32 [ebp + 40], ptr32 [ebp + 40], 1

	jmp $@main_label_1

	$@main_label_6:

	; call void @puts([22 x i8]*, i32, i32)
	add esp, esp, 12 ; allocate space for return and arguments
	mov ptr32 [esp - 12], $@.str.1 ; copy arg #1
	mov ptr32 [esp - 8], 0 ; copy arg #2
	mov ptr32 [esp - 4], 0 ; copy arg #3
	call $@puts
	sub esp, esp, 12 ; pop args and ret

	mov ptr32 [ebp - 12], 0
	ret