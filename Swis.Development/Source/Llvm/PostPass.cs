using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Swis
{
	public static partial class LlvmIrCompiler
	{
		static void RemoveNopJumps(MethodBuilder output)
		{
			//jmp $x
			//$x:

			// ->

			//$x:

			string rx = PatternCompile(
				@"jmp (?<target>\$[a-zA-Z0-9@%#-_\.]+)" +
				@"(?<ws>\s*\n\s*)+" +
				@"(?<dest>\$[^:]+):"
			);

			string asm = output.Assembly.ToString();
			asm = Regex.Replace(asm, rx,
				delegate (Match m)
				{
					if (m.Groups["target"].Value == m.Groups["dest"].Value)
						return // comment out the jump
							$";jmp {m.Groups["dest"].Value}" +
							$"{m.Groups["ws"].Value}" +
							$"{m.Groups["dest"].Value}:";
					return m.Value;
				});
			output.Assembly.Clear();
			output.Assembly.Append(asm);
		}
	}
}