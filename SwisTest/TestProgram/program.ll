; ModuleID = 'main.cpp'
source_filename = "main.cpp"
target datalayout = "e-m:e-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

@_ZZ4mainE3msg = private unnamed_addr constant [15 x i8] c"Hello, world!\0A\00", align 1

; Function Attrs: noinline nounwind
define void @_Z3outjh(i32 %port, i8 zeroext %val) #0 {
  %port.addr = alloca i32, align 4
  %val.addr = alloca i8, align 1
  store i32 %port, i32* %port.addr, align 4
  store i8 %val, i8* %val.addr, align 1
  %1 = load i32, i32* %port.addr, align 4
  %2 = load i8, i8* %val.addr, align 1
  call void asm sideeffect "out $0, $1", "r,r,~{dirflag},~{fpsr},~{flags}"(i32 %1, i8 %2) #3, !srcloc !1
  ret void
}

; Function Attrs: noinline nounwind
define void @_Z5printPc(i8* %str) #0 {
  %str.addr = alloca i8*, align 8
  store i8* %str, i8** %str.addr, align 8
  br label %1

; <label>:1:                                      ; preds = %4, %0
  %2 = load i8*, i8** %str.addr, align 8
  %3 = load i8, i8* %2, align 1
  %conv = sext i8 %3 to i32
  %cmp = icmp ne i32 %conv, 0
  br i1 %cmp, label %4, label %8

; <label>:4:                                      ; preds = %1
  %5 = load i8*, i8** %str.addr, align 8
  %6 = load i8, i8* %5, align 1
  call void @_Z3outjh(i32 0, i8 zeroext %6)
  %7 = load i8*, i8** %str.addr, align 8
  %incdec.ptr = getelementptr inbounds i8, i8* %7, i32 1
  store i8* %incdec.ptr, i8** %str.addr, align 8
  br label %1

; <label>:8:                                      ; preds = %1
  ret void
}

; Function Attrs: noinline nounwind
define i32 @_Z11alloca_testi(i32 %value) #0 {
  %value.addr = alloca i32, align 4
  %x = alloca i32, align 4
  store i32 %value, i32* %value.addr, align 4
  store i32 5, i32* %x, align 4
  %1 = load i32, i32* %value.addr, align 4
  %2 = load i32, i32* %x, align 4
  %cmp = icmp sgt i32 %1, %2
  br i1 %cmp, label %3, label %6

; <label>:3:                                      ; preds = %0
  %4 = load i32, i32* %x, align 4
  %5 = load i32, i32* %value.addr, align 4
  %mul = mul nsw i32 %4, %5
  store i32 %mul, i32* %x, align 4
  br label %6

; <label>:6:                                      ; preds = %3, %0
  %7 = load i32, i32* %value.addr, align 4
  ret i32 %7
}

; Function Attrs: noinline norecurse nounwind
define i32 @main() #1 {
  %retval = alloca i32, align 4
  %msg = alloca [15 x i8], align 1
  store i32 0, i32* %retval, align 4
  %1 = bitcast [15 x i8]* %msg to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* %1, i8* getelementptr inbounds ([15 x i8], [15 x i8]* @_ZZ4mainE3msg, i32 0, i32 0), i64 15, i32 1, i1 false)
  %arraydecay = getelementptr inbounds [15 x i8], [15 x i8]* %msg, i32 0, i32 0
  call void @_Z5printPc(i8* %arraydecay)
  ret i32 0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* nocapture writeonly, i8* nocapture readonly, i64, i32, i1) #2

attributes #0 = { noinline nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+mmx,+sse,+sse2,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { noinline norecurse nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+mmx,+sse,+sse2,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { argmemonly nounwind }
attributes #3 = { nounwind }

!llvm.ident = !{!0}

!0 = !{!"clang version 4.0.0-1ubuntu1~16.04.2 (tags/RELEASE_400/rc1)"}
!1 = !{i32 66}
