; ModuleID = 'program.cpp'
source_filename = "program.cpp"
target datalayout = "e-m:e-p:32:32-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i386"

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
  call void @_Z3outjh(i32 0, i8 zeroext %6)
  %7 = load i8*, i8** %str.addr, align 4
  %incdec.ptr = getelementptr inbounds i8, i8* %7, i32 1
  store i8* %incdec.ptr, i8** %str.addr, align 4
  br label %1

; <label>:8:                                      ; preds = %1
  ret void
}

; Function Attrs: noinline nounwind
define signext i16 @_Z11alloca_testsPs(i16 signext %value, i16* %other) #0 {
  %value.addr = alloca i16, align 2
  %other.addr = alloca i16*, align 4
  %x = alloca i16, align 2
  store i16 %value, i16* %value.addr, align 2
  store i16* %other, i16** %other.addr, align 4
  store i16 5, i16* %x, align 2
  %1 = load i16, i16* %value.addr, align 2
  %conv = sext i16 %1 to i32
  %2 = load i16, i16* %x, align 2
  %conv1 = sext i16 %2 to i32
  %cmp = icmp sgt i32 %conv, %conv1
  br i1 %cmp, label %3, label %7

; <label>:3:                                      ; preds = %0
  %4 = load i16, i16* %value.addr, align 2
  %conv2 = sext i16 %4 to i32
  %5 = load i16*, i16** %other.addr, align 4
  %6 = load i16, i16* %5, align 2
  %conv3 = sext i16 %6 to i32
  %mul = mul nsw i32 %conv2, %conv3
  %conv4 = trunc i32 %mul to i16
  store i16 %conv4, i16* %x, align 2
  br label %7

; <label>:7:                                      ; preds = %3, %0
  %8 = load i16, i16* %x, align 2
  ret i16 %8
}

; Function Attrs: noinline norecurse nounwind
define i32 @main() #1 {
  %retval = alloca i32, align 4
  %msg = alloca [15 x i8], align 1
  store i32 0, i32* %retval, align 4
  %1 = bitcast [15 x i8]* %msg to i8*
  call void @llvm.memcpy.p0i8.p0i8.i32(i8* %1, i8* getelementptr inbounds ([15 x i8], [15 x i8]* @_ZZ4mainE3msg, i32 0, i32 0), i32 15, i32 1, i1 false)
  %arraydecay = getelementptr inbounds [15 x i8], [15 x i8]* %msg, i32 0, i32 0
  call void @_Z5printPc(i8* %arraydecay)
  ret i32 0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i32(i8* nocapture writeonly, i8* nocapture readonly, i32, i32, i1) #2

attributes #0 = { noinline nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { noinline norecurse nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="false" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-features"="+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { argmemonly nounwind }
attributes #3 = { nounwind }

!llvm.ident = !{!0}

!0 = !{!"clang version 4.0.0-1ubuntu1~16.04.2 (tags/RELEASE_400/rc1)"}
!1 = !{i32 195}
