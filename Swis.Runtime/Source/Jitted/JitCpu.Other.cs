using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Swis
{

	public sealed partial class JittedCpu : Cpu
	{
		class JitCacheInvalidator : MemoryController
		{
			MemoryController Parent;
			JittedCpu Cpu;

			public JitCacheInvalidator(JittedCpu cpu, MemoryController parent)
			{
				this.Parent = parent;
				this.Cpu = cpu;
			}

			public override uint Length { get { return this.Parent.Length; } }

			public override byte this[uint x]
			{
				get { return this.Parent[x]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= this.Cpu.JitCacheFirst && x <= this.Cpu.JitCacheLast)
						this.Cpu.ClearJitCache();
					this.Parent[x] = value;
				}
			}
			public override uint this[uint x, uint bits]
			{
				get { return this.Parent[x, bits]; }
				set
				{
					// if we write in areas that have been jitted, clear the jit cache
					if (x >= this.Cpu.JitCacheFirst && x <= this.Cpu.JitCacheLast)
						this.Cpu.ClearJitCache();
					this.Parent[x, bits] = value;
				}
			}

			public override Span<byte> Span(uint x, uint length)
			{
				return this.Parent.Span(x, length);
			}
		}

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

		Expression RegisterExpression(NamedRegister reg, uint size, bool reading)
		{
			FieldInfo reg_field = typeof(JittedCpu).GetField($"Reg{(int)reg}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (reading && size != Cpu.NativeSizeBits)
				return Expression.And( // TODO: sign extend it if signed
					Expression.Constant((1u << (int)size) - 1, typeof(uint)), // ensure that we don't read/write too much from this register
					Expression.Field(Expression.Constant(this), reg_field)
				);
			else
				return Expression.Field(Expression.Constant(this), reg_field);
		}

		IndexExpression PointerExpression(Expression memloc, uint indirection_size)
		{
			MemberExpression mem = Expression.Field(Expression.Constant(this), typeof(JittedCpu).GetField("_Memory", BindingFlags.NonPublic | BindingFlags.Instance));

			PropertyInfo mem_indexer = (from p in mem.Type.GetDefaultMembers().OfType<PropertyInfo>()
											// check return type
										where p.PropertyType == typeof(uint)
										let q = p.GetIndexParameters()
										// check params
										where q.Length == 2 && q[0].ParameterType == typeof(uint) && q[1].ParameterType == typeof(uint)
										select p).Single();

			return Expression.Property(mem, mem_indexer, memloc, Expression.Constant(indirection_size, typeof(uint)));
		}

		Expression JitOperand(ref Operand arg, bool write)
		{
			Expression jit_part(int regid, uint size, uint constant, bool within_indirection, bool signed = false)
			{
				if (regid == -1)
				{
					if (!signed)
						return Expression.Constant(constant, typeof(uint));
					else
					{
						Caster c; c.I32 = 0;
						c.U32 = constant;
						return Expression.Constant(c.I32, typeof(int));
					}
				}
				else
				{
					if (!signed)
						return RegisterExpression((NamedRegister)regid, size, within_indirection || !write);
					else if (within_indirection || !write)
						return Expression.Convert(RegisterExpression((NamedRegister)regid, size, within_indirection || !write), typeof(int));
					else
						// TODO: only throw if invoked
						throw new Exception("can't write to addressing mode calculation");
				}
			}

			bool has_indirection = arg.Indirect;
			Expression inside = null;

			switch (arg.AddressingMode)
			{
			case 0: // a
				inside = jit_part(arg.RegIdA, arg.SizeA, arg.ConstA, has_indirection);
				break;
			case 1: // a + b
				inside = Expression.Add(
					jit_part(arg.RegIdA, arg.SizeA, arg.ConstA, has_indirection),
					jit_part(arg.RegIdB, arg.SizeB, arg.ConstB, has_indirection)
				);
				break;
			case 2: // c * d
				inside = Expression.Convert(
					Expression.Multiply(
						jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, has_indirection, true),
						jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, has_indirection, true)
					),
					typeof(uint)
				);
				break;
			case 3: // a + b + c * d
				inside = Expression.Add(
					Expression.Add(
						jit_part(arg.RegIdA, arg.SizeA, arg.ConstA, has_indirection),
						jit_part(arg.RegIdB, arg.SizeB, arg.ConstB, has_indirection)
					),
					Expression.Convert(
						Expression.Multiply(
							jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, has_indirection, true),
							jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, has_indirection, true)
						),
						typeof(uint)
					)
				);
				break;
			default: throw new Exception();
			}

			if (!has_indirection)
				return inside;
			return PointerExpression(inside, arg.IndirectionSize);
		}

	}
}