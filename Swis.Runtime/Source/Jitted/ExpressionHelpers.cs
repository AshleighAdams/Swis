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

		private static Expression SignExtendExpression(Expression srcexp, Expression bitexp)
		{
			Expression<Func<uint, uint, uint>> sign_extend = (val, frombits) => Util.SignExtend(val, frombits);
			return Expression.Invoke(sign_extend, srcexp, bitexp);
		}

		private static Expression LimitSizeExpression(Expression expr, uint bits)
		{
			if (bits == Cpu.NativeSizeBits)
				return expr;

			return Expression.And(
				Expression.Constant((1u << (int)bits) - 1, typeof(uint)), // ensure that we don't read/write too much from this register
				expr
			);
		}
		private Expression RaiseInterruptExpression(Expression codexpr, ref bool sequential)
		{
			sequential = false;
			Expression<Action<uint>> raise_interrupt = (code) => this.Interrupt(code);
			return Expression.Invoke(raise_interrupt, codexpr);
		}
		private Expression RaiseInterruptExpression(Interrupts interrupt, ref bool sequential)
		{
#if DEBUG
			throw new Exception();
#endif
			return this.RaiseInterruptExpression(Expression.Constant((uint)interrupt, typeof(uint)), ref sequential);
		}

		private Expression ReadWriteRegisterExpression(NamedRegister reg)
		{
			FieldInfo reg_field = typeof(JittedCpu).GetField($"Reg{(int)reg}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			return Expression.Field(Expression.Constant(this), reg_field);
		}

		private Expression ReadRegisterExpression(NamedRegister reg, uint size, bool signed, Type as_type = null)
		{
			Expression expr = LimitSizeExpression(this.ReadWriteRegisterExpression(reg), size);
			if (signed)
				expr = SignExtendExpression(expr, Expression.Constant(size, typeof(uint)));
			if (as_type != null && as_type != typeof(uint))
				expr = Expression.Convert(expr, as_type);

			return expr;
		}

		private Expression ReadOperandExpression(ref Operand arg)
		{
			Expression jit_part(int regid, uint size, uint constant, bool signed = false)
			{
				Expression ret;
				if (regid == -1)
					return signed ?
						Expression.Constant((int)Util.SignExtend(constant, size), typeof(int)) :
						Expression.Constant(constant, typeof(uint));
				else
					return this.ReadRegisterExpression((NamedRegister)regid, size, signed, signed ? typeof(int) : typeof(uint));
			}

			Expression inside;
			switch (arg.AddressingMode)
			{
				case 0: // a
					inside = jit_part(arg.RegIdA, arg.SizeA, arg.ConstA);
					break;
				case 1: // a + b
					inside = Expression.Add(
						jit_part(arg.RegIdA, arg.SizeA, arg.ConstA),
						jit_part(arg.RegIdB, arg.SizeB, arg.ConstB)
					);
					break;
				case 2: // c * d
					inside = Expression.Convert(
						Expression.Multiply(
							jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, true),
							jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, true)
						),
						typeof(uint)
					);
					break;
				case 3: // a + b + c * d
					inside = Expression.Add(
						Expression.Add(
							jit_part(arg.RegIdA, arg.SizeA, arg.ConstA),
							jit_part(arg.RegIdB, arg.SizeB, arg.ConstB)
						),
						Expression.Convert(
							Expression.Multiply(
								jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, true),
								jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, true)
							),
							typeof(uint)
						)
					);
					break;
				default: throw new Exception();
			}

			if (!arg.Indirect)
				return inside;
			return this.PointerExpression(inside, arg.IndirectionSize);
		}


		private Expression ReadOperandExpressionSigned(ref Operand arg)
		{
			return Expression.Convert(
				SignExtendExpression(
					ReadOperandExpression(ref arg),
					Expression.Constant((uint)arg.ValueSize, typeof(uint))
				),
				typeof(int)
			);
		}

		private Expression WriteOperandExpression(ref Operand arg, ref bool sequential, Expression src)
		{
			if (!arg.Indirect)
			{
				if (arg.WriteAffectsFlow)
					sequential = false;
				if (arg.AddressingMode != 0)
					throw new Exception();
				return Expression.Assign(this.ReadWriteRegisterExpression((NamedRegister)arg.RegIdA), LimitSizeExpression(src, arg.SizeA));
			}

			return Expression.Assign(this.ReadOperandExpression(ref arg), LimitSizeExpression(src, arg.IndirectionSize));

		}
	}
}