﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;



namespace Swis
{
	public sealed partial class JittedCpu : CpuBase
	{
		private sealed class CpuInstruction : Attribute
		{
			public readonly Opcode Opcode;
			public CpuInstruction(Opcode opcode)
			{
				Opcode = opcode;
			}
		}

		private delegate Expression OpcodeFunction(ref uint ip, ref bool sequential);
		private OpcodeFunction[] OpcodeDecodeTable = new OpcodeFunction[(int)Opcode.MaxEnum];

		private void InitializeOpcodeTable()
		{
			var methods = this.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(m => m.GetCustomAttributes().OfType<CpuInstruction>().Any());
			foreach (var method in methods)
			{
				var @delegate = (OpcodeFunction)Delegate.CreateDelegate(typeof(OpcodeFunction), this, method);

				var attributes = method.GetCustomAttributes().OfType<CpuInstruction>();
				foreach (var attrib in attributes)
				{
					if (attrib.Opcode < 0 || attrib.Opcode >= Opcode.MaxEnum)
						throw new Exception($"Out of range opcode {(int)attrib.Opcode}");
					if (OpcodeDecodeTable[(int)attrib.Opcode] != null)
						throw new Exception($"Duplicate opcode for {attrib.Opcode}");

					OpcodeDecodeTable[(int)attrib.Opcode] = @delegate;
				}
			}
		}
	}
}