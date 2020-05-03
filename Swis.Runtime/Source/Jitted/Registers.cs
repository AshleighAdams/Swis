using System;

namespace Swis
{
	public sealed partial class JittedCpu
	{
		private uint Reg0;
		#region Other Registers
		private uint Reg1;
		private uint Reg2;
		private uint Reg3;
		private uint Reg4;
		private uint Reg5;
		private uint Reg6;
		private uint Reg7;
		private uint Reg8;
		private uint Reg9;
		private uint Reg10;
		private uint Reg11;
		private uint Reg12;
		private uint Reg13;
		private uint Reg14;
		private uint Reg15;
		private uint Reg16;
		private uint Reg17;
		private uint Reg18;
		private uint Reg19;
		private uint Reg20;
		private uint Reg21;
		private uint Reg22;
		private uint Reg23;
		private uint Reg24;
		private uint Reg25;
		private uint Reg26;
		private uint Reg27;
		private uint Reg28;
		private uint Reg29;
		private uint Reg30;
		private uint Reg31;
		#endregion


		#region RegisterProxy
		private struct RegisterProxy : IReadWriteList<uint>
		{
			private readonly JittedCpu Source;

			public RegisterProxy(JittedCpu source)
			{
				Source = source;
			}

			public uint this[int index]
			{
				get
				{
#pragma warning disable IDE0066 // Convert switch statement to expression
					switch (index)
#pragma warning restore IDE0066 // Convert switch statement to expression
					{
						case 0: return Source.Reg0;
						case 1: return Source.Reg1;
						case 2: return Source.Reg2;
						case 3: return Source.Reg3;
						case 4: return Source.Reg4;
						case 5: return Source.Reg5;
						case 6: return Source.Reg6;
						case 7: return Source.Reg7;
						case 8: return Source.Reg8;
						case 9: return Source.Reg9;
						case 10: return Source.Reg10;
						case 11: return Source.Reg11;
						case 12: return Source.Reg12;
						case 13: return Source.Reg13;
						case 14: return Source.Reg14;
						case 15: return Source.Reg15;
						case 16: return Source.Reg16;
						case 17: return Source.Reg17;
						case 18: return Source.Reg18;
						case 19: return Source.Reg19;
						case 20: return Source.Reg20;
						case 21: return Source.Reg21;
						case 22: return Source.Reg22;
						case 23: return Source.Reg23;
						case 24: return Source.Reg24;
						case 25: return Source.Reg25;
						case 26: return Source.Reg26;
						case 27: return Source.Reg27;
						case 28: return Source.Reg28;
						case 29: return Source.Reg29;
						case 30: return Source.Reg30;
						case 31: return Source.Reg31;
						default: throw new IndexOutOfRangeException();
					};
				}
				set
				{
					switch (index)
					{
						case 0: Source.Reg0 = value; break;
						case 1: Source.Reg1 = value; break;
						case 2: Source.Reg2 = value; break;
						case 3: Source.Reg3 = value; break;
						case 4: Source.Reg4 = value; break;
						case 5: Source.Reg5 = value; break;
						case 6: Source.Reg6 = value; break;
						case 7: Source.Reg7 = value; break;
						case 8: Source.Reg8 = value; break;
						case 9: Source.Reg9 = value; break;
						case 10: Source.Reg10 = value; break;
						case 11: Source.Reg11 = value; break;
						case 12: Source.Reg12 = value; break;
						case 13: Source.Reg13 = value; break;
						case 14: Source.Reg14 = value; break;
						case 15: Source.Reg15 = value; break;
						case 16: Source.Reg16 = value; break;
						case 17: Source.Reg17 = value; break;
						case 18: Source.Reg18 = value; break;
						case 19: Source.Reg19 = value; break;
						case 20: Source.Reg20 = value; break;
						case 21: Source.Reg21 = value; break;
						case 22: Source.Reg22 = value; break;
						case 23: Source.Reg23 = value; break;
						case 24: Source.Reg24 = value; break;
						case 25: Source.Reg25 = value; break;
						case 26: Source.Reg26 = value; break;
						case 27: Source.Reg27 = value; break;
						case 28: Source.Reg28 = value; break;
						case 29: Source.Reg29 = value; break;
						case 30: Source.Reg30 = value; break;
						case 31: Source.Reg31 = value; break;
						default: throw new IndexOutOfRangeException();
					};
				}
			}
			public int Count { get => 32; }
		}
		#endregion
		public override IReadWriteList<uint> Registers { get => new RegisterProxy(this); }

		#region NamedRegisterAccessors
		public override ref uint TimeStampCounter
		{
			get { return ref Reg0; }
		}

		public override ref uint InstructionPointer
		{
			get { return ref Reg1; }
		}

		public override ref uint StackPointer
		{
			get { return ref Reg2; }
		}

		public override ref uint BasePointer
		{
			get { return ref Reg4; }
		}

		public override ref uint Flags
		{
			get { return ref Reg5; }
		}

		public override ref uint ProtectedMode
		{
			get { return ref Reg6; }
		}

		public override ref uint ProtectedInterrupt
		{
			get { return ref Reg7; }
		}
		#endregion

		public override void Reset()
		{
			Reg0 = Reg1 = Reg2 = Reg3 = Reg4 = Reg5 = Reg6 = Reg7
				= Reg8 = Reg9 = Reg10 = Reg11 = Reg12 = Reg13 = Reg14 = Reg15
				= Reg16 = Reg17 = Reg18 = Reg19 = Reg20 = Reg21 = Reg22 = Reg23
				= Reg24 = Reg25 = Reg26 = Reg27 = Reg28 = Reg29 = Reg30 = Reg31 = 0;
			CycleBank = 0;
		}
	}
}