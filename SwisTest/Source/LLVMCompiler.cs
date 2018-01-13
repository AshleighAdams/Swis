﻿namespace Swis
{
	// https://github.com/llvm-mirror/llvm/blob/master/include/llvm/IR/Instruction.def
	public static class LLVMCompiler
	{
		#region Test code

		/*
			// asm
			mov t0, 4
			add sp, sp, t0 // %1 = +0

			mov t0, 4
			add sp, sp, t0 // %2 = +4

			mov t0, 4
			add sp, sp, t0 // %3 = +8
			
			mov t0, 0
			store 0, t0, bp

			mov t0, 5
			store 4, t0, bp 

			mov t0, 10
			store 8, t0, bp 
		*/

		public static string TestCode = @"; ModuleID = 'main.cpp'
source_filename = ""main.cpp""
target datalayout = ""e-m:e-i64:64-f80:128-n8:16:32:64-S128""
target triple = ""x86_64-pc-linux-gnu""

; Function Attrs: noinline norecurse nounwind uwtable
define i32 @main() #0 {
  %1 = alloca i32, align 4
  %2 = alloca i32, align 4
  %3 = alloca i32, align 4
  store i32 0, i32* %1, align 4
  store i32 5, i32* %2, align 4
  store i32 10, i32* %3, align 4
  %4 = load i32, i32* %2, align 4
  %5 = load i32, i32* %3, align 4
  %6 = icmp eq i32 %4, %5
  br i1 %6, label %7, label %11

; <label>:7:                                      ; preds = %0
  %8 = load i32, i32* %2, align 4
  %9 = load i32, i32* %3, align 4
  %10 = add nsw i32 %8, %9
  store i32 %10, i32* %2, align 4
  br label %11

; <label>:11:                                     ; preds = %7, %0
  %12 = load i32, i32* %2, align 4
  ret i32 %12
}

attributes #0 = { noinline norecurse nounwind uwtable ""correctly-rounded-divide-sqrt-fp-math""=""false"" ""disable-tail-calls""=""false"" ""less-precise-fpmad""=""false"" ""no-frame-pointer-elim""=""true"" ""no-frame-pointer-elim-non-leaf"" ""no-infs-fp-math""=""false"" ""no-jump-tables""=""false"" ""no-nans-fp-math""=""false"" ""no-signed-zeros-fp-math""=""false"" ""no-trapping-math""=""false"" ""stack-protector-buffer-size""=""8"" ""target-cpu""=""x86-64"" ""target-features""=""+fxsr,+mmx,+sse,+sse2,+x87"" ""unsafe-fp-math""=""false"" ""use-soft-float""=""false"" }

!llvm.ident = !{!0}

!0 = !{!""clang version 4.0.0-1ubuntu1~16.04.2 (tags/RELEASE_400/rc1)""}
";
		#endregion

		public static string Compile()
		{
			return "nop";
		}
	}
}