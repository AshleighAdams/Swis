@stdout_line = global i16 0, align 2

define void @_Z3putcs(i8 signext %c, i16 signext %line)
{
	%c.addr = alloca i8, align 1
	%line.addr = alloca i16, align 2
	store i8 %c, i8* %c.addr, align 1
	store i16 %line, i16* %line.addr, align 2
	%1 = load i16, i16* %line.addr, align 2
	%2 = load i8, i8* %c.addr, align 1
	call void asm sideeffect "out $0, $1", "X,X,~{dirflag},~{fpsr},~{flags}"(i16 %1, i8 %2) #1, !srcloc !3
	ret void
}

define void @_Z4putsPKc(i8* %str)
{
	%str.addr = alloca i8*, align 4
	%line = alloca i16, align 2
	store i8* %str, i8** %str.addr, align 4
	%1 = load i16, i16* @stdout_line, align 2
	store i16 %1, i16* %line, align 2
	br label %2

; <label>:2: ; preds = %5, %0
	%3 = load i8*, i8** %str.addr, align 4
	%4 = load i8, i8* %3, align 1
	%tobool = icmp ne i8 %4, 0
	br i1 %tobool, label %5, label %10

; <label>:5: ; preds = %2
	%6 = load i8*, i8** %str.addr, align 4
	%7 = load i8, i8* %6, align 1
	%8 = load i16, i16* %line, align 2
	call void @_Z3putcs(i8 signext %7, i16 signext %8)
	%9 = load i8*, i8** %str.addr, align 4
	%incdec.ptr = getelementptr inbounds i8, i8* %9, i32 1
	store i8* %incdec.ptr, i8** %str.addr, align 4
	br label %2

; <label>:10: ; preds = %2
	%11 = load i16, i16* %line, align 2
	call void @_Z3putcs(i8 signext 10, i16 signext %11)
	ret void
}