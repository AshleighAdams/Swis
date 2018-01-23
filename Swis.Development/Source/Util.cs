using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Swis
{
    public static class Util
    {
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
