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

			string rx = Util.PatternCompile(
				@"jmp (?<target>\$[a-zA-Z0-9@%#-_\.]+)" +
				@"(?<ws>\s*\n\s*)+" +
				@"(?<dest>\$[^:]+):", IrPatterns
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

	//todo: convert:
	// cmp %1:32, 0
	// j(ne|e) $@_Z4itoaiPci_label_8
	// jmp...
	//to:
	// j(nz|ez) $@_Z4itoaiPci_label_8
	// jmp...
}