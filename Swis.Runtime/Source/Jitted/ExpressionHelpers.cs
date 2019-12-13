using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		internal static class ReinterpretCast<TSrc, TDst>
			where TSrc : struct
			where TDst : struct
		{
			[StructLayout(LayoutKind.Explicit)]
			private struct UnionStruct
			{
				[FieldOffset(0)] public TSrc Src;
				[FieldOffset(0)] public TDst Dst;
			}

			private static readonly Func<TSrc, TDst> Implementation = (val) =>
			{
				UnionStruct converter;
				converter.Dst = default;
				converter.Src = val;
				return converter.Dst;
			};
			public static readonly Expression<Func<TSrc, TDst>> Expression = (val) => Implementation(val);
		}

		internal static class ReinterpretCastExpressionHelpers
		{
			private static readonly Func<uint, uint, int> ReinterpretUInt32AsInt32 = (val, bits) =>
			{
				// todo use bits to signextend
				Caster c; c.I32 = 0;
				c.U32 = val;
				return c.I32;
			};
			public static readonly Expression<Func<uint, uint, int>> ReinterpretUInt32AsInt32Expression = (val, bits) => ReinterpretUInt32AsInt32(val, bits);
			private static readonly Func<int, uint> ReinterpretInt32AsUInt32 = (val) =>
			{
				Caster c; c.U32 = 0;
				c.I32 = val;
				return c.U32;
			};
			public static readonly Expression<Func<int, uint>> ReinterpretInt32AsUInt32Expression = (val) => ReinterpretInt32AsUInt32(val);
		}

		private Expression RegisterExpression(NamedRegister reg, uint size, bool reading)
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

		private IndexExpression PointerExpression(Expression memloc, uint indirection_size)
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

		private Expression OperandExpression(ref Operand arg, bool write)
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
						return this.RegisterExpression((NamedRegister)regid, size, within_indirection || !write);
					else if (within_indirection || !write)
						return Expression.Convert(this.RegisterExpression((NamedRegister)regid, size, within_indirection || !write), typeof(int));
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
			return this.PointerExpression(inside, arg.IndirectionSize);
		}
	}
}