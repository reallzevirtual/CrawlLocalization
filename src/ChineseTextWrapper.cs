using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CrawlLocalization
{
    // Unlike Crawl's space-based wrapper, Chinese can break between characters.
    internal static class ChineseTextWrapper
    {
        private static readonly Regex Units = new Regex(@"</?(?:b|i|size|color|material)(?:=[^>]*)?>|<quad\s[^>]*>|\r\n|[\r\n]|[A-Za-z0-9]+(?:[-'][A-Za-z0-9]+)*|.", RegexOptions.IgnoreCase);
        private const string Closing = "，。！？；：、）】》」』”’％%.,!?;:)";
        private const string Opening = "（【《「『“‘(";

        private static bool IsTag(string unit) { return unit.StartsWith("<", StringComparison.Ordinal) && unit.EndsWith(">", StringComparison.Ordinal); }

        internal static bool ContainsChinese(string text)
        {
            if (text == null) return false;
            foreach (char ch in text) if (ch >= '\u3400' && ch <= '\u9fff') return true;
            return false;
        }

        internal static string Wrap(string text, float width, Func<char, float> advance)
        {
            if (string.IsNullOrEmpty(text) || width <= 0) return text;
            List<string> units = new List<string>();
            foreach (Match match in Units.Matches(text))
            {
                string unit = match.Value;
                if (units.Count > 0 && !IsTag(units[units.Count - 1]) && unit.Length == 1 && Closing.IndexOf(unit[0]) >= 0 && units[units.Count - 1] != "\n" && units[units.Count - 1] != "\r" && units[units.Count - 1] != "\r\n")
                    units[units.Count - 1] += unit;
                else if (units.Count > 0 && units[units.Count - 1].Length == 1 && Opening.IndexOf(units[units.Count - 1][0]) >= 0 && !IsTag(unit) && unit != "\n" && unit != "\r" && unit != "\r\n")
                    units[units.Count - 1] += unit;
                else units.Add(unit);
            }
            StringBuilder result = new StringBuilder();
            float used = 0;
            foreach (string unit in units)
            {
                if (unit == "\r\n" || unit == "\r" || unit == "\n") { result.Append('\n'); used = 0; continue; }
                if (IsTag(unit)) { result.Append(unit); continue; }
                float length = 0;
                foreach (char ch in unit) length += advance(ch);
                if (used > 0 && used + length > width) { result.Append('\n'); used = 0; }
                if (used == 0 && unit == " ") continue;
                // Keep Latin names/numbers together where possible; very long tokens
                // still need to fit in the panel.
                foreach (char ch in unit)
                {
                    float step = advance(ch);
                    if (used > 0 && used + step > width) { result.Append('\n'); used = 0; }
                    result.Append(ch);
                    used += step;
                }
            }
            return result.ToString();
        }
    }
}
