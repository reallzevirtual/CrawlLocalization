using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CrawlLocalization
{
    internal sealed class NumericTranslation
    {
        private static readonly Regex Tokens = new Regex(@"\{([0-9])\}");
        internal readonly string Source;
        private readonly string target;
        private readonly Regex pattern;

        private NumericTranslation(string source, string target, string pattern)
        {
            Source = source;
            this.target = target;
            this.pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static NumericTranslation Create(string source, string target)
        {
            MatchCollection matches = Tokens.Matches(source);
            if (matches.Count == 0) return null;
            HashSet<string> ids = new HashSet<string>();
            StringBuilder pattern = new StringBuilder(@"\A");
            int offset = 0;
            foreach (Match match in matches)
            {
                pattern.Append(Regex.Escape(source.Substring(offset, match.Index - offset)));
                string id = match.Groups[1].Value;
                pattern.Append(ids.Add(id) ? "(?<n" + id + ">[0-9]+)" : @"\k<n" + id + ">");
                offset = match.Index + match.Length;
            }
            pattern.Append(Regex.Escape(source.Substring(offset))).Append(@"\z");
            HashSet<string> targetIds = new HashSet<string>();
            foreach (Match match in Tokens.Matches(target)) targetIds.Add(match.Groups[1].Value);
            if (!ids.SetEquals(targetIds)) throw new FormatException("Numeric translation must preserve all source placeholders without adding new ones.");
            return new NumericTranslation(source, target, pattern.ToString());
        }

        internal bool TryTranslate(string source, out string translated)
        {
            Match match = pattern.Match(source);
            translated = null;
            if (match.Success)
                translated = Tokens.Replace(target, delegate(Match token) { return match.Groups["n" + token.Groups[1].Value].Value; });
            return match.Success;
        }
    }
}
