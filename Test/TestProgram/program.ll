; ModuleID = 'program.cpp'
source_filename = "program.cpp"
target datalayout = "e-m:e-p:32:32-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i386"

@_ZL4next = internal global i32 229729204, align 4
@.str = private unnamed_addr constant [14 x i8] c"Hello world.\0A\00", align 1
@.str.1 = private unnamed_addr constant [22 x i8] c"Execution: finished!\0A\00", align 1

; Function Attrs: noinline nounwind
define void @_Z7reversePci(i8* %str, i32 %length) #0 {
  %str.addr = alloca i8*, align 4
  %length.addr = alloca i32, align 4
  %start = alloca i32, align 4
  %end = alloca i32, align 4
  %tmp = alloca i8, align 1
  store i8* %str, i8** %str.addr, align 4
  store i32 %length, i32* %length.addr, align 4
  store i32 0, i32* %start, align 4
  %1 = load i32, i32* %length.addr, align 4
  %sub = sub nsw i32 %1, 1
  store i32 %sub, i32* %end, align 4
  br label %2

; <label>:2:                                      ; preds = %5, %0
  %3 = load i32, i32* %start, align 4
  %4 = load i32, i32* %end, align 4
  %cmp = icmp slt i32 %3, %4
  br i1 %cmp, label %5, label %19

; <label>:5:                                      ; preds = %2
  %6 = load i8*, i8** %str.addr, align 4
  %7 = load i32, i32* %start, align 4
  %add.ptr = getelementptr inbounds i8, i8* %6, i32 %7
  %8 = load i8, i8* %add.ptr, align 1
  store i8 %8, i8* %tmp, align 1
  %9 = load i8*, i8** %str.addr, align 4
  %10 = load i32, i32* %end, align 4
  %add.ptr1 = getelementptr inbounds i8, i8* %9, i32 %10
  %11 = load i8, i8* %add.ptr1, align 1
  %12 = load i8*, i8** %str.addr, align 4
  %13 = load i32, i32* %start, align 4
  %add.ptr2 = getelementptr inbounds i8, i8* %12, i32 %13
  store i8 %11, i8* %add.ptr2, align 1
  %14 = load i8, i8* %tmp, align 1
  %15 = load i8*, i8** %str.addr, align 4
  %16 = load i32, i32* %end, align 4
  %add.ptr3 = getelementptr inbounds i8, i8* %15, i32 %16
  store i8 %14, i8* %add.ptr3, align 1
  %17 = load i32, i32* %start, align 4
  %inc = add nsw i32 %17, 1
  store i32 %inc, i32* %start, align 4
  %18 = load i32, i32* %end, align 4
  %dec = add nsw i32 %18, -1
  store i32 %dec, i32* %end, align 4
  br label %2

; <label>:19:                                     ; preds = %2
  ret void
}

; Function Attrs: noinline nounwind
define i8* @itoa(i32 %num, i8* %str, i32 %base) #0 {
  %retval = alloca i8*, align 4
  %num.addr = alloca i32, align 4
  %str.addr = alloca i8*, align 4
  %base.addr = alloca i32, align 4
  %i = alloca i32, align 4
  %negative = alloca i8, align 1
  %rem = alloca i32, align 4
  store i32 %num, i32* %num.addr, align 4
  store i8* %str, i8** %str.addr, align 4
  store i32 %base, i32* %base.addr, align 4
  store i32 0, i32* %i, align 4
  store i8 0, i8* %negative, align 1
  %1 = load i32, i32* %num.addr, align 4
  %cmp = icmp eq i32 %1, 0
  br i1 %cmp, label %2, label %8

; <label>:2:                                      ; preds = %0
  %3 = load i8*, i8** %str.addr, align 4
  %4 = load i32, i32* %i, align 4
  %inc = add nsw i32 %4, 1
  store i32 %inc, i32* %i, align 4
  %arrayidx = getelementptr inbounds i8, i8* %3, i32 %4
  store i8 48, i8* %arrayidx, align 1
  %5 = load i8*, i8** %str.addr, align 4
  %6 = load i32, i32* %i, align 4
  %arrayidx1 = getelementptr inbounds i8, i8* %5, i32 %6
  store i8 0, i8* %arrayidx1, align 1
  %7 = load i8*, i8** %str.addr, align 4
  store i8* %7, i8** %retval, align 4
  br label %41

; <label>:8:                                      ; preds = %0
  %9 = load i32, i32* %num.addr, align 4
  %cmp2 = icmp slt i32 %9, 0
  br i1 %cmp2, label %10, label %14

; <label>:10:                                     ; preds = %8
  %11 = load i32, i32* %base.addr, align 4
  %cmp3 = icmp eq i32 %11, 10
  br i1 %cmp3, label %12, label %14

; <label>:12:                                     ; preds = %10
  store i8 1, i8* %negative, align 1
  %13 = load i32, i32* %num.addr, align 4
  %sub = sub nsw i32 0, %13
  store i32 %sub, i32* %num.addr, align 4
  br label %14

; <label>:14:                                     ; preds = %12, %10, %8
  br label %15

; <label>:15:                                     ; preds = %25, %14
  %16 = load i32, i32* %num.addr, align 4
  %cmp4 = icmp ne i32 %16, 0
  br i1 %cmp4, label %17, label %30

; <label>:17:                                     ; preds = %15
  %18 = load i32, i32* %num.addr, align 4
  %19 = load i32, i32* %base.addr, align 4
  %rem5 = srem i32 %18, %19
  store i32 %rem5, i32* %rem, align 4
  %20 = load i32, i32* %rem, align 4
  %cmp6 = icmp sgt i32 %20, 9
  br i1 %cmp6, label %21, label %23

; <label>:21:                                     ; preds = %17
  %22 = load i32, i32* %rem, align 4
  %sub7 = sub nsw i32 %22, 10
  %add = add nsw i32 %sub7, 97
  br label %25

; <label>:23:                                     ; preds = %17
  %24 = load i32, i32* %rem, align 4
  %add8 = add nsw i32 %24, 48
  br label %25

; <label>:25:                                     ; preds = %23, %21
  %cond = phi i32 [ %add, %21 ], [ %add8, %23 ]
  %conv = trunc i32 %cond to i8
  %26 = load i8*, i8** %str.addr, align 4
  %27 = load i32, i32* %i, align 4
  %inc9 = add nsw i32 %27, 1
  store i32 %inc9, i32* %i, align 4
  %arrayidx10 = getelementptr inbounds i8, i8* %26, i32 %27
  store i8 %conv, i8* %arrayidx10, align 1
  %28 = load i32, i32* %num.addr, align 4
  %29 = load i32, i32* %base.addr, align 4
  %div = sdiv i32 %28, %29
  store i32 %div, i32* %num.addr, align 4
  br label %15

; <label>:30:                                     ; preds = %15
  %31 = load i8, i8* %negative, align 1
  %tobool = trunc i8 %31 to i1
  br i1 %tobool, label %32, label %35

; <label>:32:                                     ; preds = %30
  %33 = load i8*, i8** %str.addr, align 4
  %34 = load i32, i32* %i, align 4
  %inc11 = add nsw i32 %34, 1
  store i32 %inc11, i32* %i, align 4
  %arrayidx12 = getelementptr inbounds i8, i8* %33, i32 %34
  store i8 45, i8* %arrayidx12, align 1
  br label %35

; <label>:35:                                     ; preds = %32, %30
  %36 = load i8*, i8** %str.addr, align 4
  %37 = load i32, i32* %i, align 4
  %arrayidx13 = getelementptr inbounds i8, i8* %36, i32 %37
  store i8 0, i8* %arrayidx13, align 1
  %38 = load i8*, i8** %str.addr, align 4
  %39 = load i32, i32* %i, align 4
  call void @_Z7reversePci(i8* %38, i32 %39)
  %40 = load i8*, i8** %str.addr, align 4
  store i8* %40, i8** %retval, align 4
  br label %41

; <label>:41:                                     ; preds = %35, %2
  %42 = load i8*, i8** %retval, align 4
  ret i8* %42
}

; Function Attrs: noinline nounwind
define void @_Z3outjh(i32 %port, i8 zeroext %val) #0 {
  %port.addr = alloca i32, align 4
  %val.addr = alloca i8, align 1
  store i32 %port, i32* %port.addr, align 4
  store i8 %val, i8* %val.addr, align 1
  %1 = load i32, i32* %port.addr, align 4
  %2 = load i8, i8* %val.addr, align 1
  call void asm sideeffect "out $0, $1", "X,X,~{dirflag},~{fpsr},~{flags}"(i32 %1, i8 %2) #2, !srcloc !1
  ret void
}

; Function Attrs: noinline nounwind
define void @put(i8 signext %c) #0 {
  %c.addr = alloca i8, align 1
  store i8 %c, i8* %c.addr, align 1
  %1 = load i8, i8* %c.addr, align 1
  call void asm sideeffect "out 0, $0", "X,~{dirflag},~{fpsr},~{flags}"(i8 %1) #2, !srcloc !2
  ret void
}

; Function Attrs: noinline nounwind
define void @puts(i8* %str) #0 {
  %str.addr = alloca i8*, align 4
  store i8* %str, i8** %str.addr, align 4
  br label %1

; <label>:1:                                      ; preds = %4, %0
  %2 = load i8*, i8** %str.addr, align 4
  %3 = load i8, i8* %2, align 1
  %conv = sext i8 %3 to i32
  %cmp = icmp ne i32 %conv, 0
  br i1 %cmp, label %4, label %8

; <label>:4:                                      ; preds = %1
  %5 = load i8*, i8** %str.addr, align 4
  %6 = load i8, i8* %5, align 1
  call void @put(i8 signext %6)
  %7 = load i8*, i8** %str.addr, align 4
  %incdec.ptr = getelementptr inbounds i8, i8* %7, i32 1
  store i8* %incdec.ptr, i8** %str.addr, align 4
  br label %1

; <label>:8:                                      ; preds = %1
  ret void
}

; Function Attrs: noinline nounwind
define i32 @rand() #0 {
  %1 = load i32, i32* @_ZL4next, align 4
  %mul = mul i32 %1, 1103515245
  %add = add i32 %mul, 12345
  store i32 %add, i32* @_ZL4next, align 4
  %2 = load i32, i32* @_ZL4next, align 4
  %div = udiv i32 %2, 65536
  %rem = urem i32 %div, 32768
  ret i32 %rem
}

; Function Attrs: noinline nounwind
define void @srand(i32 %seed) #0 {
  %seed.addr = alloca i32, align 4
  store i32 %seed, i32* %seed.addr, align 4
  %1 = load i32, i32* %seed.addr, align 4
  store i32 %1, i32* @_ZL4next, align 4
  ret void
}

; Function Attrs: noinline norecurse nounwind
define i32 @main() #1 {
  %retval = alloca i32, align 4
  %output = alloca [33 x i8], align 1
  %i = alloca i32, align 4
  store i32 0, i32* %retval, align 4
  call void @puts(i8* getelementptr inbounds ([14 x i8], [14 x i8]* @.str, i32 0, i32 0))
  store i32 1330, i32* %i, align 4
  br label %1

; <label>:1:                                      ; preds = %4, %0
  %2 = load i32, i32* %i, align 4
  %cmp = icmp slt i32 %2, 1340
  br i1 %cmp, label %3, label %6

; <label>:3:                                      ; preds = %1
  %call = call i32 @rand()
  %arraydecay = getelementptr inbounds [33 x i8], [33 x i8]* %output, i32 0, i32 0
  %call1 = call i8* @itoa(i32 %call, i8* %arraydecay, i32 10)
  %arraydecay2 = getelementptr inbounds [33 x i8], [33 x i8]* %output, i32 0, i32 0
  call void @puts(i8* %arraydecay2)
  call void @put(i8 signext 10)
  br label %4

; <label>:4:                                      ; preds = %3
  %5 = load i32, i32* %i, align 4
  %inc = add nsw i32 %5, 1
  store i32 %inc, i32* %i, align 4
  br label %1

; <label>:6:                                      ; preds = %1
  call void @puts(i8* getelementptr inbounds ([22 x i8], [22 x i8]* @.str.1, i32 0, i32 0))
  ret i32 0
}

attributes #0 = { noinline nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { noinline norecurse nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { nounwind }

!llvm.ident = !{!0}

!0 = !{!"clang version 4.0.0-1ubuntu1~16.04.2 (tags/RELEASE_400/rc1)"}
!1 = !{i32 1340}
!2 = !{i32 1511}
