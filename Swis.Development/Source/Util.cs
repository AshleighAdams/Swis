using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Runtime.CompilerServices
{
	public class ExtensionAttribute : Attribute { }
}

namespace Swis
{
    public static class Util
    {
		static Dictionary<string, Regex> _RegexCache = new Dictionary<string, Regex>();
		public static dynamic PatternMatch(this string self, string pattern)
		{
			if (!_RegexCache.TryGetValue(pattern, out Regex r))
				_RegexCache[pattern] = r = new Regex(LlvmIrCompiler.PatternCompile(pattern));

			Match m = r.Match(self);

			if (!m.Success)
				return null;

			dynamic ret = new ExpandoObject();
			var ret_dict = (IDictionary<string, object>)ret;
			
			foreach (string name in r.GetGroupNames())
				ret_dict[name] = m.Groups[name].Value;

			return ret;
		}
		public static dynamic[] PatternMatches(this string self, string pattern)
		{
			if (!_RegexCache.TryGetValue(pattern, out Regex r))
				_RegexCache[pattern] = r = new Regex(LlvmIrCompiler.PatternCompile(pattern));

			List<dynamic> rets = new List<dynamic>();
			MatchCollection ms = r.Matches(self);

			foreach (Match m in ms)
			{
				dynamic ret = new ExpandoObject();
				var ret_dict = (IDictionary<string, object>)ret;

				foreach (string name in r.GetGroupNames())
					ret_dict[name] = m.Groups[name].Value;

				rets.Add(ret);
			}
			return rets.ToArray();
		}

		public static Match Match(this string self, string pattern)
		{
			return Regex.Match(self, pattern, RegexOptions.IgnorePatternWhitespace);
		}

		public static MatchCollection Matches(this string self, string pattern)
		{
			return Regex.Matches(self, pattern, RegexOptions.IgnorePatternWhitespace);
		}

		public static string Times(this string self, int count)
		{
			if (count == 0)
				return "";
			StringBuilder sb = new StringBuilder(self.Length * count);
			for (int n = 0; n < count; n++)
				sb.Append(self);
			return sb.ToString();
		}
	}
}
