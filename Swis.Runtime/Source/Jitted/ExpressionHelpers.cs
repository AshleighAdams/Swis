using System;
using System.Linq.Expressions;

namespace Swis
{
	internal static class JitExpressionHelpers
	{
		readonly static Func<uint, float> ReinterpretUIntAsFloat = (val) =>
		{
			Caster c; c.F32 = 0;
			c.U32 = val;
			return c.F32;
		};
		public static readonly Expression<Func<uint, float>> ReinterpretUInt32AsFloat32Expression = (val) => ReinterpretUIntAsFloat(val);

		readonly static Func<float, uint> ReinterpretFloat32AsUInt32 = (val) =>
		{
			Caster c; c.U32 = 0;
			c.F32 = val;
			return c.U32;
		};
		public static readonly Expression<Func<float, uint>> ReinterpretFloat32AsUInt32Expression = (val) => ReinterpretFloat32AsUInt32(val);

		readonly static Func<uint, uint, int> ReinterpretUInt32AsInt32 = (val, bits) =>
		{
			// todo use bits to signextend
			Caster c; c.I32 = 0;
			c.U32 = val;
			return c.I32;
		};
		public static readonly Expression<Func<uint, uint, int>> ReinterpretUInt32AsInt32Expression = (val, bits) => ReinterpretUInt32AsInt32(val, bits);

		readonly static Func<int, uint> ReinterpretInt32AsUInt32 = (val) =>
		{
			Caster c; c.U32 = 0;
			c.I32 = val;
			return c.U32;
		};
		public static readonly Expression<Func<int, uint>> ReinterpretInt32AsUInt32Expression = (val) => ReinterpretInt32AsUInt32(val);
	}
}