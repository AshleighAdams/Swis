using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		uint Reg0;
		#region Other Registers
		uint Reg1;
		uint Reg2;
		uint Reg3;
		uint Reg4;
		uint Reg5;
		uint Reg6;
		uint Reg7;
		uint Reg8;
		uint Reg9;
		uint Reg10;
		uint Reg11;
		uint Reg12;
		uint Reg13;
		uint Reg14;
		uint Reg15;
		uint Reg16;
		uint Reg17;
		uint Reg18;
		uint Reg19;
		uint Reg20;
		uint Reg21;
		uint Reg22;
		uint Reg23;
		uint Reg24;
		uint Reg25;
		uint Reg26;
		uint Reg27;
		uint Reg28;
		uint Reg29;
		uint Reg30;
		uint Reg31;

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