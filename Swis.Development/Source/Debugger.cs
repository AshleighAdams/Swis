using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Swis
{
	[Serializable]
	public class DebugData
	{
		[Serializable]
		public enum AsmPtrType
		{
			None = 0,
			Instruction = 1,
			Operand,
			DataString,
			DataSigned,
			DataUnsigned,
			DataFloat,
			DataHex,
			DataPadding,
		}

		// what the code here corrosponds to, in absolute position.
		// type =
		public Dictionary<int, (string file, int from, int to, AsmPtrType type)> PtrToAsm;
		public Dictionary<int, (string file, int from, int to)> AsmToSrc;

		// for asm disasm
		public Dictionary<string, int> Labels;

		public static string Serialize(DebugData data)
		{
			return "";
		}

		public DebugData Deserialize(string str)
		{
			return null;
		}
	}
}