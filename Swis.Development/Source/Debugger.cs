using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Swis
{
	[Serializable]
	public class DebugData
	{
		// what the code here corrosponds to, in absolute position.
		public Dictionary<uint, (string file, uint from, uint to)> PtrToAsm;
		public Dictionary<uint, (string file, uint from, uint to)> AsmToSrc;

		// for asm disasm
		public Dictionary<string, uint> Labels;
		public List<uint> Instructions;

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