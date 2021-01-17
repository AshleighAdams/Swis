; setup the stack
mov esp, $stack
mov ebp, esp
; setup interrupts
;; set the vector table location
shr epi, $interrupt_vector_table, 8 
and epi, epi, 255
;; activate them
sti

call $@main
halt

.align 256
$interrupt_vector_table:
	.data pad 1024
;############

$string:
	.data ascii "Lorem ipsum... hello world\x0d"
	.data ascii "Abcdefghijklmnopqrstuvwxyz\x0d"
	.data ascii "Testing long output complete...\x0a\x00"

$stdin_buff:
	.data pad 16
$stdin_buffreadpos:
	.data int32 0
$stdin_buffwritepos:
	.data int32 0

$int251_stdin_old:
	in ptr8 [ebp + 0], 0
	mov ptr32 [ebp + 1], ptr32 [$stdin_buffwritepos]
	add ptr32 [$stdin_buffwritepos], ptr32 [$stdin_buffwritepos], 1
	add esp, esp, 5 ; allocate stack after
	push eax
	modu eax, ptr32 [ebp + 1], 16
	mov ptr8 [$stdin_buff + eax], ptr8 [ebp + 0]
	pop eax
	iret

$int251_stdin:
	in ptr8 [ebp + 0], 0
	modu eflag, ptr32 [$stdin_buffwritepos], 16
	mov ptr8 [$stdin_buff + eflag], ptr8 [ebp + 0]
	add ptr32 [$stdin_buffwritepos], ptr32 [$stdin_buffwritepos], 1
	iret

$@main:
	; set up the interrupt for stdin
	mov [$interrupt_vector_table + 251 * 4], $int251_stdin
	
	push $string
	call $puts
	sub esp, esp, 4

	$stdin_loop:
	cmp ptr32 [$stdin_buffreadpos], ptr32 [$stdin_buffwritepos]
	jge $main_ret

	modu eax, ptr32 [$stdin_buffreadpos], 16
	out 0, ptr8 [$stdin_buff + eax]
	add ptr32 [$stdin_buffreadpos], ptr32 [$stdin_buffreadpos], 1

	jmp $stdin_loop
	$main_ret:
	ret

$puts:
	.src func "void puts(char* str)"
	.src local "str" "int8*" -12 4
	; ptr = -12
	mov eax, [ebp - 12]

	$puts_loop:
	jz ptr8 [eax], $puts_ret

	out 0, ptr8 [eax]
	
	add eax, eax, 1
	jmp $puts_loop

	$puts_ret:
	ret;

.align 4
$stack:
	.data pad 1024