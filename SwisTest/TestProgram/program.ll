; ModuleID = 'main.cpp'
source_filename = "main.cpp"
target datalayout = "e-m:e-p:32:32-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-eabi"

@_ZZ4mainE3msg = private unnamed_addr constant [15 x i8] c"Hello, world!\0A\00", align 1

; Function Attrs: noinline nounwind
define void @_Z3outjh(i32, i8 zeroext) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i8, align 1
  store i32 %0, i32* %3, align 4
  store i8 %1, i8* %4, align 1
  %5 = load i32, i32* %3, align 4
  %6 = load i8, i8* %4, align 1
  call void asm sideeffect "out $0, $1", "r,r"(i32 %5, i8 %6) #3, !srcloc !3
  ret void
}

; Function Attrs: noinline nounwind
define void @_Z5printPc(i8*) #0 {
  %2 = alloca i8*, align 4
  store i8* %0, i8** %2, align 4
  br label %3

; <label>:3:                                      ; preds = %8, %1
  %4 = load i8*, i8** %2, align 4
  %5 = load i8, i8* %4, align 1
  %6 = zext i8 %5 to i32
  %7 = icmp ne i32 %6, 0
  br i1 %7, label %8, label %13

; <label>:8:                                      ; preds = %3
  %9 = load i8*, i8** %2, align 4
  %10 = load i8, i8* %9, align 1
  call void @_Z3outjh(i32 0, i8 zeroext %10)
  %11 = load i8*, i8** %2, align 4
  %12 = getelementptr inbounds i8, i8* %11, i32 1
  store i8* %12, i8** %2, align 4
  br label %3

; <label>:13:                                     ; preds = %3
  ret void
}

; Function Attrs: noinline norecurse nounwind
define i32 @main() #1 {
  %1 = alloca i32, align 4
  %2 = alloca [15 x i8], align 1
  store i32 0, i32* %1, align 4
  %3 = bitcast [15 x i8]* %2 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i32(i8* %3, i8* getelementptr inbounds ([15 x i8], [15 x i8]* @_ZZ4mainE3msg, i32 0, i32 0), i32 15, i32 1, i1 false)
  %4 = getelementptr inbounds [15 x i8], [15 x i8]* %2, i32 0, i32 0
  call void @_Z5printPc(i8* %4)
  ret i32 0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i32(i8* nocapture writeonly, i8* nocapture readonly, i32, i32, i1) #2

attributes #0 = { noinline nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="true" "no-frame-pointer-elim-non-leaf" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="cortex-a8" "target-features"="+dsp,+soft-float,+vfp3,-crypto,-neon" "unsafe-fp-math"="false" "use-soft-float"="true" }
attributes #1 = { noinline norecurse nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "less-precise-fpmad"="false" "no-frame-pointer-elim"="true" "no-frame-pointer-elim-non-leaf" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="false" "stack-protector-buffer-size"="8" "target-cpu"="cortex-a8" "target-features"="+dsp,+soft-float,+vfp3,-crypto,-neon" "unsafe-fp-math"="false" "use-soft-float"="true" }
attributes #2 = { argmemonly nounwind }
attributes #3 = { nounwind }

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 1, !"min_enum_size", i32 4}
!2 = !{!"clang version 4.0.0-1ubuntu1~16.04.2 (tags/RELEASE_400/rc1)"}
!3 = !{i32 66}
