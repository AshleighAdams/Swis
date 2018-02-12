using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Swis
{
	public class JitCpu
	{
		volatile uint Reg0;
		#region Other Registers
		volatile uint Reg1;
		volatile uint Reg2;
		volatile uint Reg3;
		volatile uint Reg4;
		volatile uint Reg5;
		volatile uint Reg7;
		volatile uint Reg8;
		volatile uint Reg9;
		volatile uint Reg10;
		volatile uint Reg11;
		volatile uint Reg12;
		volatile uint Reg13;
		volatile uint Reg14;
		volatile uint Reg15;
		volatile uint Reg16;
		volatile uint Reg17;
		volatile uint Reg18;
		volatile uint Reg19;
		volatile uint Reg20;
		volatile uint Reg21;
		volatile uint Reg22;
		volatile uint Reg23;
		volatile uint Reg24;
		volatile uint Reg25;
		volatile uint Reg26;
		volatile uint Reg27;
		volatile uint Reg28;
		volatile uint Reg29;
		volatile uint Reg30;
		volatile uint Reg31;
		#endregion

		public MemoryController Memory;

		//Dictionary<uint page, DateTime> CacheTimes;
		//Dictionary<uint, (Expression λ, bool refsip, uint len)> ExpressionCache = new Dictionary<uint, (Expression, bool, uint)>();
		Dictionary<uint, (Action λ, uint cycles)> JitCache = new Dictionary<uint, (Action, uint)>();

		private int CycleDebt = 0; // if a jit block over runs, remember how much we're in debt here
		public void Clock(int cycles = 1)
		{
			// i is there to make sure we don't end up in a loop forever from a bugged instruction
			//for (int i = 0; cycles > 0 && i < 1000; i++)
			while(cycles > 0)
			{
				if (!this.JitCache.TryGetValue(this.Reg1, out (Action λ, uint cycles) instr))
				{
					uint block_length = 0;
					List<Expression> block_instructions = new List<Expression>();

					uint simulated_ip = this.Reg1;
					for (uint n = 0; n < 16; n++)
					{
						if (simulated_ip >= this.Memory.Length)
							break;

						var jitinst = JitInstruction(simulated_ip);

						block_instructions.Add(jitinst.λ);
						block_length += jitinst.len;
						simulated_ip += jitinst.len;

						if (jitinst.mutates_ip)
							break;
					}

					Expression<Action> λ = Expression.Lambda<Action>(Expression.Block(block_instructions));
					
					instr = this.JitCache[this.Reg1] = (λ.Compile(), (uint)block_instructions.Count);
				}
				
				instr.λ();
				cycles -= (int)instr.cycles;
			}
			this.CycleDebt += cycles;
		}

		Expression RegisterExpression(NamedRegister reg, uint size, bool reading)
		{
			FieldInfo reg_field = typeof(JitCpu).GetField($"Reg{(int)reg}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if (reading)
				return Expression.And(
					Expression.Constant((uint)(((ulong)1 << (int)size) - 1), typeof(uint)), // ensure that we don't read/write too much from this register
					Expression.Field(Expression.Constant(this), reg_field)
				);
			else
				return Expression.Field(Expression.Constant(this), reg_field);
		}

		Expression JitOperand(ref Operand arg, bool write)
		{
			Expression jit_part(int regid, uint size, uint constant, bool within_indirection)
			{
				if (regid == -1)
					return Expression.Constant(constant, typeof(uint));
				else
				{
					return RegisterExpression((NamedRegister)regid, size, within_indirection || !write);
					//FieldInfo reg_field = typeof(JitCpu).GetField($"Reg{regid}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					//if (within_indirection || !write)
					//	return Expression.And(
					//		Expression.Constant((uint)(((ulong)1 << (int)size) - 1), typeof(uint)), // ensure that we don't read/write too much from this register
					//		Expression.Field(Expression.Constant(this), reg_field)
					//	);
					//else
					//	return Expression.Field(Expression.Constant(this), reg_field);
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
				inside = Expression.Multiply(
					jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, has_indirection),
					jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, has_indirection)
				);
				break;
			case 3: // a + b + c * d
				inside = Expression.Add(
					Expression.Add(
						jit_part(arg.RegIdA, arg.SizeA, arg.ConstA, has_indirection),
						jit_part(arg.RegIdB, arg.SizeB, arg.ConstB, has_indirection)
					),
					Expression.Multiply(
						jit_part(arg.RegIdC, arg.SizeC, arg.ConstC, has_indirection),
						jit_part(arg.RegIdD, arg.SizeD, arg.ConstD, has_indirection)
					)
				);
				break;
			default: throw new Exception();
			}

			if (!has_indirection)
				return inside;

			MemberExpression mem = Expression.Field(Expression.Constant(this), typeof(JitCpu).GetField("Memory"));

			PropertyInfo mem_indexer = (from p in mem.Type.GetDefaultMembers().OfType<PropertyInfo>()
			                            // This check is probably useless. You can't overload on return value in C#.
			                            where p.PropertyType == typeof(uint)
			                            let q = p.GetIndexParameters()
			                            // Here we can search for the exact overload. Length is the number of "parameters" of the indexer, and then we can check for their type.
			                            where q.Length == 2 && q[0].ParameterType == typeof(uint) && q[1].ParameterType == typeof(uint)
			                            select p).Single();

			return Expression.Property(mem, mem_indexer, inside, Expression.Constant(arg.IndirectionSize, typeof(uint)));
		}

		(Expression λ, uint len, bool mutates_ip) JitInstruction(uint location)
		{
			uint original_ip = location;
			uint ip = original_ip;
			Opcode op = this.Memory.DecodeOpcode(ref ip);

			bool mutates_ip = false;

			Expression exp = null;

			switch (op)
			{
			#region Misc
			case Opcode.Nop:
				break;
			case Opcode.SignExtendRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);
					Operand bit = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);
					Expression bitexp = JitOperand(ref bit, false);
					
					
					var valbits = Expression.Variable(typeof(uint), "valbits");
					var extbits = Expression.Variable(typeof(uint), "extbits");
					var signbit = Expression.Variable(typeof(uint), "signbit");

					var srcval = Expression.Variable(typeof(uint), "srcval");
					var sign = Expression.Variable(typeof(uint), "sign");

					exp = Expression.Block(
						new ParameterExpression[] { valbits, extbits, signbit, srcval, sign },
						// uint valbits = (1u << (int)frombits) - 1;
						Expression.Assign(valbits,
							Expression.Subtract(
								Expression.LeftShift(
									Expression.Constant(1u),
									Expression.Convert(bitexp, typeof(int))
								),
								Expression.Constant(1u)
							)
						),
						// uint extbits = ~valbits;
						Expression.Assign(extbits,
							Expression.Not(valbits)
						),
						// uint signbit = 1u << (int)(frombits - 1);
						Expression.Assign(signbit,
							Expression.LeftShift(
								Expression.Constant(1u),
								Expression.Convert(
									Expression.Subtract(bitexp, Expression.Constant(1u)),
									typeof(int)
								)
							)
						),
						// uint srcval = src & valbits;
						Expression.Assign(srcval,
							Expression.And(srcexp, valbits)
						),
						// uint sign = (signbit & srcval) >> ((int)frombits - 1);
						Expression.Assign(sign,
							Expression.RightShift(
								Expression.And(signbit, srcval),
								Expression.Convert(
									Expression.Subtract(bitexp, Expression.Constant(1u)),
									typeof(int)
								)
							)
						),
						// dst = srcval | (extbits * sign);
						Expression.Assign(dstexp,
							Expression.Or(
								srcval,
								Expression.Multiply(extbits, sign)
							)
						)
					);
					mutates_ip = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.ZeroExtendRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);
					Operand bit = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);
					Expression bitexp = JitOperand(ref bit, false);

					var valbits = Expression.Variable(typeof(uint), "valbits");
					
					exp = Expression.Block(
						new ParameterExpression[] { valbits },
						// uint valbits = (1u << (int)frombits) - 1;
						Expression.Assign(valbits,
							Expression.Subtract(
								Expression.LeftShift(
									Expression.Constant(1u), bitexp
								),
								Expression.Constant(1u)
							)
						),
						// dst.Value = src.Value & valbits;
						Expression.Assign(dstexp,
							Expression.And(
								srcexp,
								valbits
							)
						)
					);
					mutates_ip = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.Halt:
				{
					FieldInfo flagsfield = typeof(JitCpu).GetField($"Reg{(int)NamedRegister.Flag}", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					exp = Expression.OrAssign(
						Expression.Field(Expression.Constant(this), flagsfield),
						Expression.Constant((uint)FlagsRegisterFlags.Halted)
					);
					mutates_ip = true;
					break;
				}
			case Opcode.InRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand line = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression lineexp = JitOperand(ref line, false);
					
					Expression<Func<uint, uint>> readline = lineval => Console.ReadKey().KeyChar;
					exp = Expression.Assign(dstexp, Expression.Invoke(readline, lineexp));

					mutates_ip = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			case Opcode.OutRR:
				{
					Operand line = this.Memory.DecodeOperand(ref ip, null);
					Operand lttr = this.Memory.DecodeOperand(ref ip, null);

					Expression lineexp = JitOperand(ref line, false);
					Expression lttrexp = JitOperand(ref lttr, false);

					Expression<Action<uint, uint>> writeline = (lineval, charval) => Console.Write((char)charval);

					exp = Expression.Invoke(writeline, lineexp, lttrexp);
					break;
				}
			#endregion
			#region Memory
			case Opcode.MoveRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand src = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression srcexp = JitOperand(ref src, false);
					
					exp = Expression.Assign(dstexp, srcexp);
					mutates_ip = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			#endregion
			#region Flow
			case Opcode.JumpR:
				{
					Operand loc = this.Memory.DecodeOperand(ref ip, null);

					Expression locexp = JitOperand(ref loc, true);

					exp = Expression.Assign(RegisterExpression(NamedRegister.InstructionPointer, 32, false), locexp);

					mutates_ip = true;
					break;
				}
			#endregion
			#region Transformative
			case Opcode.AddRRR:
				{
					Operand dst = this.Memory.DecodeOperand(ref ip, null);
					Operand left = this.Memory.DecodeOperand(ref ip, null);
					Operand right = this.Memory.DecodeOperand(ref ip, null);

					Expression dstexp = JitOperand(ref dst, true);
					Expression leftexp = JitOperand(ref left, false);
					Expression rightexp = JitOperand(ref right, false);

					exp = Expression.Assign(dstexp, Expression.Add(leftexp, rightexp));
					mutates_ip = dst.AddressingMode == 0 && dst.RegIdA == (int)NamedRegister.InstructionPointer;
					break;
				}
			#endregion
			default: throw new Exception("JitCpu: instruction {op} not supported!");
			}

			// reg1 += inst_length();
			FieldInfo ipfield = typeof(JitCpu).GetField($"Reg1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			Expression ip_inc = Expression.AddAssign(
				Expression.Field(Expression.Constant(this), ipfield),
				Expression.Constant(ip - original_ip, typeof(uint))
			);

			// reg0++;
			FieldInfo tscfield = typeof(JitCpu).GetField($"Reg0", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			Expression tsc_inc = Expression.Increment(
				Expression.Field(Expression.Constant(this), ipfield)
			);

			if(exp == null) // a nop
				return (Expression.Block(ip_inc, tsc_inc), ip - original_ip, mutates_ip);

			return (Expression.Block(ip_inc, exp, tsc_inc), ip - original_ip, mutates_ip);
		}
	}
}