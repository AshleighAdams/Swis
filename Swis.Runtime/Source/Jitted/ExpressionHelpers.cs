using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Swis
{
	public sealed partial class JittedCpu : Cpu
	{
		private static Expression ReinterpretCastExpression<TSrc, TDst>(Expression src)
			   where TSrc : unmanaged
			   where TDst : unmanaged
		{
			if (typeof(TSrc) == typeof(TDst))
				return src;
			Expression<Func<TSrc, TDst>> lambda = (val) => Util.ReinterpretCast<TSrc, TDst>(val);
			return Expression.Invoke(lambda, src);
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
			// optimize
			if (bitexp is ConstantExpression @const && @const.Value is uint const_val && const_val == Cpu.NativeSizeBits)
				return srcexp;

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
#else
			return this.RaiseInterruptExpression(Expression.Constant((uint)interrupt, typeof(uint)), ref sequential);
#endif
		}

		private Expression ReadWriteRegisterExpression(NamedRegister reg)
		{
			FieldInfo reg_field = typeof(JittedCpu).GetField($"Reg{(int)reg}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			return Expression.Field(Expression.Constant(this), reg_field);
		}

		private Expression ReadRegisterExpression<T>(NamedRegister reg, uint size)
			where T : unmanaged
		{
			Expression expr = LimitSizeExpression(this.ReadWriteRegisterExpression(reg), size);

			var t = typeof(T);
			if (t == typeof(Int16) || t == typeof(Int32) || t == typeof(Int64))
				expr = SignExtendExpression(expr, Expression.Constant(size));
			if (t != typeof(uint))
				expr = ReinterpretCastExpression<uint, T>(expr);

			return expr;
		}

		private Expression AccessOperandExpression(ref Operand arg)
		{
			Expression jit_part(int regid, uint size, uint constant, bool signed = false)
			{
				if (regid == -1)
					return signed ?
						Expression.Constant((int)Util.SignExtend(constant, size)) :
						Expression.Constant(constant);
				else
					return signed ?
						this.ReadRegisterExpression<int>((NamedRegister)regid, size) :
						this.ReadRegisterExpression<uint>((NamedRegister)regid, size);
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


		private Expression ReadOperandExpression<T>(ref Operand arg)
			where T : unmanaged
		{
			var expr = this.AccessOperandExpression(ref arg);

			var t = typeof(T);
			if (t == typeof(Int16) || t == typeof(Int32) || t == typeof(Int64))
				expr = SignExtendExpression(expr, Expression.Constant(arg.ValueSize));

			return ReinterpretCastExpression<uint, T>(expr);
		}

		private Expression WriteOperandExpression<T>(ref Operand arg, ref bool sequential, Expression src)
			where T : unmanaged
		{
			Expression dstexpr;
			if (!arg.Indirect)
			{
				if (arg.WriteAffectsFlow)
					sequential = false;
				if (arg.AddressingMode != 0)
					throw new Exception();
				dstexpr = this.ReadWriteRegisterExpression((NamedRegister)arg.RegIdA);
			}
			else
			{
				// will be a pointer type, so we can write to it and reuse logic
				dstexpr = this.AccessOperandExpression(ref arg);
			}

			return Expression.Assign(dstexpr,
				ReinterpretCastExpression<T, uint>(LimitSizeExpression(src, arg.ValueSize))
			);
		}
	}
}