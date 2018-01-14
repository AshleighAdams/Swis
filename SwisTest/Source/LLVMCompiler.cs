using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// https://godbolt.org/

namespace Swis
{
	// https://github.com/llvm-mirror/llvm/blob/master/include/llvm/IR/Instruction.def
	public static class LLVMCompiler
	{
		#region Test code

		/*
			
		*/

		public static string TestIR = @"
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
}";
		#endregion

		public static string Compile(string code)
		{
			int ptr_size = 4;
			//scan the code and figure out:
			//	what values are stack offsets and can be hard coded
			//	what values are negative stack offsets (arguments)
			//attempt to reuse any temporary variables

			string type_regex = @"(?<type>(?<type_letter>u|i|f)(?<type_size>8|16|32)|void)(?<ptr>\*{1,})?";

			string ident_regex = @"(?<id>[%@][-a-zA-Z$._][-a-zA-Z$._0-9]*)";
			string local_regex = @"(?<id>[%][-a-zA-Z$._][-a-zA-Z$._0-9]*)";
			string global_regex = @"(?<id>[@][-a-zA-Z$._][-a-zA-Z$._0-9]*)";

			string arglist_regex = @"(?<arglist>[%\-a-zA-Z$._0-9\*, ]*)";

			string func_regex = $@"define {type_regex} {ident_regex}\({arglist_regex}\) #(?<attrib>[0-9]{{1,}}) \{{(?<body>(?:[^{{}}]|(?<counter>\{{)|(?<-counter>\}}))+(?(counter)(?!)))\}}";
			
			// first let's find our arguments and body
			var funcs = Regex.Matches(code, func_regex);
			foreach (Match func in funcs)
			{
				string return_type = func.Groups["type"].Value;
				string func_name = func.Groups["id"].Value;
				string arglist = func.Groups["arglist"].Value;
				string body = func.Groups["body"].Value;
				
				int stack_variable_size = 0; // how much we need to increase the stack ptr by, args and rets are before bp
				var stack_variable_offset = new Dictionary<string, int>();

				// assign the 

				Console.WriteLine($"{return_type} {func_name}({arglist})");
			}

			return "";
		}
	}
}