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
    public static class LlvmUtil
    {
		static Dictionary<string, Regex> _RegexCache = new Dictionary<string, Regex>();
		static Dictionary<string, string> GlobalNamedPatterns = new Dictionary<string, string>();

		static bool GlobalPatternsSetup = false;
		static void SetupGlobalPatterns()
		{
			if (GlobalPatternsSetup) return;
			GlobalPatternsSetup = true;

			GlobalNamedPatterns["parentheses"] = PatternCompile(@"\((?<inside>(?:[^\(\)]|(?<__unique__>\()|(?<-__unique__>\)))+(?(__unique__)(?!)))\)", GlobalNamedPatterns);
			GlobalNamedPatterns["braces"]      = PatternCompile(@"\{(?<inside>(?:[^\{\}]|(?<__unique__>\{)|(?<-__unique__>\}))+(?(__unique__)(?!)))\}", GlobalNamedPatterns);
			GlobalNamedPatterns["brackets"]    = PatternCompile(@"\[(?<inside>(?:[^\[\]]|(?<__unique__>\[)|(?<-__unique__>\]))+(?(__unique__)(?!)))\]", GlobalNamedPatterns);
			GlobalNamedPatterns["angled"]      = PatternCompile(@"\<(?<inside>(?:[^\<\>]|(?<__unique__>\<)|(?<-__unique__>\>))+(?(__unique__)(?!)))\>", GlobalNamedPatterns);

			GlobalNamedPatterns["alpha"]       = PatternCompile(@"[a-zA-Z]+", GlobalNamedPatterns);
			GlobalNamedPatterns["numeric"]     = PatternCompile(@"[0-9]+", GlobalNamedPatterns);
			GlobalNamedPatterns["alphanumeric"]= PatternCompile(@"[a-zA-Z0-9]+", GlobalNamedPatterns);
		}

		static int uniqueid = 0;
		public static string PatternCompile(string pattern, Dictionary<string, string> named = null) // <> = sub-regex
		{
			SetupGlobalPatterns();
			if (named == null)
				named = GlobalNamedPatterns;
			pattern = pattern.Replace("__unique__", $"__unique__{uniqueid++}__");
			// test = "<register:abc>" -> "(?<abc>[%][0-9]+)"
			// finl = "<test:def>"     -> "(?<def>(?<def.abc>[%][0-9]+))"

			string ret = Regex.Replace(pattern, "[ ]+", @"\s+");

			ret = Regex.Replace(ret, @"(?<!\(\?)<(?<id>[a-z]+):(?<prefix>[a-zA-Z0-9_.-]+)>", delegate (Match m)
			{
				if (!named.TryGetValue(m.Groups["id"].Value, out string sub_regex))
					if (!GlobalNamedPatterns.TryGetValue(m.Groups["id"].Value, out sub_regex))
						throw new Exception($"unknown sub-pattern: {m.Groups["id"].Value}");
				string prefix = m.Groups["prefix"].Value;

				sub_regex = Regex.Replace(sub_regex, @"\(\?\<(?<id>[a-zA-Z0-9_.-]+)\>",
					delegate (Match subid)
					{
						string id = subid.Groups["id"].Value;
						if (id.Contains("__unique__"))
							return subid.Value;
						return $@"(?<{prefix}_{id}>";
					});

				sub_regex = sub_regex.Replace("__unique__", $"__unique__{prefix}__");
				//$@"(?<{prefix}_$1>");

				return $"(?<{prefix}>{sub_regex})";
			});

			ret = Regex.Replace(ret, @"(?<!\?)<(?<id>[a-z]+)>", delegate (Match m)
			{
				if (!named.TryGetValue(m.Groups["id"].Value, out string sub_regex))
					if (!GlobalNamedPatterns.TryGetValue(m.Groups["id"].Value, out sub_regex))
						throw new Exception($"unknown sub-pattern: {m.Groups["id"].Value}");

				sub_regex = sub_regex.Replace("__unique__", $"__unique__{uniqueid++}__");
				return $"({sub_regex})";
			});

			return ret;
		}


		public static dynamic PatternMatch(this string self, string pattern, Dictionary<string, string> named)
		{
			if (!_RegexCache.TryGetValue(pattern, out Regex r))
				_RegexCache[pattern] = r = new Regex(PatternCompile(pattern, named));

			Match m = r.Match(self);

			if (!m.Success)
				return null;

			dynamic ret = new ExpandoObject();
			var ret_dict = (IDictionary<string, object>)ret;
			
			foreach (string name in r.GetGroupNames())
				ret_dict[name] = m.Groups[name].Value;

			return ret;
		}
		public static dynamic[] PatternMatches(this string self, string pattern, Dictionary<string, string> named)
		{
			if (!_RegexCache.TryGetValue(pattern, out Regex r))
				_RegexCache[pattern] = r = new Regex(PatternCompile(pattern, named));

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
