namespace Swis
{
	public sealed partial class JittedCpu : Cpu
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

		public override uint[] Registers
		{
			get
			{
				return new uint[] {
					this.Reg0,  this.Reg1,  this.Reg2,  this.Reg3,  this.Reg4,  this.Reg5,  this.Reg6,  this.Reg7,  this.Reg8,  this.Reg9,
					this.Reg10, this.Reg11, this.Reg12, this.Reg13, this.Reg14, this.Reg15, this.Reg16, this.Reg17, this.Reg18, this.Reg19,
					this.Reg20, this.Reg21, this.Reg22, this.Reg23, this.Reg24, this.Reg25, this.Reg26, this.Reg27, this.Reg28, this.Reg29,
					this.Reg30, this.Reg31,
				};
			}
		}
		#endregion

		#region NamedRegisterAccessors
		public override ref uint TimeStampCounter
		{
			get { return ref this.Reg0; }
		}

		public override ref uint InstructionPointer
		{
			get { return ref this.Reg1; }
		}

		public override ref uint StackPointer
		{
			get { return ref this.Reg2; }
		}

		public override ref uint BasePointer
		{
			get { return ref this.Reg4; }
		}

		public override ref uint Flags
		{
			get { return ref this.Reg5; }
		}

		public override ref uint ProtectedMode
		{
			get { return ref this.Reg6; }
		}

		public override ref uint ProtectedInterrupt
		{
			get { return ref this.Reg7; }
		}
		#endregion

		public override void Reset()
		{
			this.Reg0 = this.Reg1 = this.Reg2 = this.Reg3 = this.Reg4 = this.Reg5 = this.Reg6 = this.Reg7
				= this.Reg8 = this.Reg9 = this.Reg10 = this.Reg11 = this.Reg12 = this.Reg13 = this.Reg14 = this.Reg15
				= this.Reg16 = this.Reg17 = this.Reg18 = this.Reg19 = this.Reg20 = this.Reg21 = this.Reg22 = this.Reg23
				= this.Reg24 = this.Reg25 = this.Reg26 = this.Reg27 = this.Reg28 = this.Reg29 = this.Reg30 = this.Reg31 = 0;
			this.CycleBank = 0;
		}
	}
}