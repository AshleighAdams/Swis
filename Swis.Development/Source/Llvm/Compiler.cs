using System;
using System.Reflection;
using System.Text;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		#region Test code

		/*
			
		*/

		public static string TestIR = @"
define i8* @_Z4itoaiPci(i32 %num, i8* %str, i32 %base) #0 {
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
}";
		#endregion
		
		public static string Compile(string code)
		{
			LlvmIrCompiler.Setup();

			string parenth_bal = @"\((?<args>(?:[^()]|(?<countera>\()|(?<-countera>\)))+(?(countera)(?!)))\)";
			string brace_bal   = @"\{(?<body>(?:[^{}]|(?<counterb>\{)|(?<-counterb>\}))+(?(counterb)(?!)))\}";

			//string func_rx = PatternCompile($@"define <type:ret_type> <ident:id>( )*\((?<args>{parenth_bal})\) #<numeric:attrib> {brace_bal}");

			dynamic[] funcs = code.PatternMatches($@"define <type:ret_type> <ident:id>( )*{parenth_bal} #<numeric:attrib> {brace_bal}");

			StringBuilder all = new StringBuilder();

			foreach (dynamic func in funcs)
			//var funcs = Regex.Matches(code, func_regex);
			//foreach (Match func in funcs)
			{
				MethodBuilder builder = new MethodBuilder()
				{
					Code = func.body,
					Id = func.id,
				};

				builder.Emit($"${func.id}:");
				builder.EmitPrefix = "\t";

				BuildMethodLocals(builder, func.ret_type, func.args/*, optimize_args: false*/);
				ReplacePhis(builder);
				SimplifyCompareBranches(builder);

				string[] lines = builder.Code.Split('\n');
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line))
						continue;

					dynamic match;

					if ((match = line.PatternMatch(@"^\s*(<operand> = )?<keyword:op>")) != null)
					{
						if (IrInstructions.TryGetValue(match.op, out (string pattern, MethodInfo func) var))
						{
							dynamic args = line.PatternMatch(var.pattern);

							if (args == null)
								throw new Exception($"{match.op} is malformed: {line.Trim()}");
							var.func.Invoke(null, new object[] { builder, args });
						}
						else
						{
							//builder.Emit($"; unknown instrunction \"{match.op}\"");
							builder.Emit($";{line.Trim()}");
						}
						continue;
					}
					else if ((match = line.PatternMatch(@"[<]label[>]:<numeric:id>:")) != null)
					{
						builder.Emit("");
						builder.Emit($"${builder.Id}_label_{match.id}:");
					}
					else
						throw new Exception();
				}

				RemoveNopJumps(builder);

				all.AppendLine(builder.Assembly.ToString());
			}

			return all.ToString();
		}
	}
}