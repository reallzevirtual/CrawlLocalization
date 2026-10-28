using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace CrawlLocalization
{
    // Only literals marked at audited UI call sites are translated. Everything else
    // (player/lobby names, addresses, versions, numbers) is opaque display data.
    internal sealed class OnlineRecipe
    {
        internal readonly string Source;
        private readonly bool literal;
        private readonly OnlineRecipe[] parts;
        internal OnlineRecipe(string source, bool literal, OnlineRecipe[] parts)
        { Source = new string((source ?? "").ToCharArray()); this.literal = literal; this.parts = parts; }
        internal string Render(LanguagePack pack)
        {
            if (pack.Id == "en") return Source;
            if (parts != null)
            {
                StringBuilder b = new StringBuilder();
                foreach (OnlineRecipe part in parts) b.Append(part.Render(pack));
                return b.ToString();
            }
            string value;
            return literal && pack.TryTranslate("@online:" + Source, out value) ? value : Source;
        }
    }
    internal static class OnlineText
    {
        private sealed class Trace { internal readonly List<OnlineRecipe> Parts = new List<OnlineRecipe>(); }
        private static readonly WeakIdentityMap<OnlineRecipe> strings = new WeakIdentityMap<OnlineRecipe>();
        private static readonly WeakIdentityMap<Trace> builders = new WeakIdentityMap<Trace>();
        internal static OnlineRecipe Find(string source) { return strings.Get(source); }
        private static OnlineRecipe Capture(string source) { return Find(source) ?? new OnlineRecipe(source, false, null); }
        internal static string Literal(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            string copy = new string(source.ToCharArray());
            strings.Set(copy, new OnlineRecipe(source, true, null)); return copy;
        }
        private static void Remember(string source, OnlineRecipe[] parts)
        {
            if (string.IsNullOrEmpty(source)) return;
            StringBuilder check = new StringBuilder();
            foreach (OnlineRecipe p in parts) check.Append(p.Source);
            if (check.ToString() == source) strings.Set(source, new OnlineRecipe(source, false, parts));
        }
        internal static StringBuilder Create() { StringBuilder b = new StringBuilder(); builders.Set(b, new Trace()); return b; }
        internal static StringBuilder Append(StringBuilder b, string s)
        { b.Append(s); Trace t = builders.Get(b); if (t != null) t.Parts.Add(Capture(s)); return b; }
        internal static StringBuilder AppendChar(StringBuilder b, char c) { b.Append(c); Trace t = builders.Get(b); if (t != null) t.Parts.Add(Capture(c.ToString())); return b; }
        internal static string Finish(StringBuilder b)
        { string s = b.ToString(); Trace t = builders.Get(b); if (t != null) Remember(s, t.Parts.ToArray()); return s; }
        internal static string ObjectString(object o) { StringBuilder b = o as StringBuilder; return b == null ? o.ToString() : Finish(b); }
        private static string Join(string source, object[] args)
        {
            OnlineRecipe[] parts = new OnlineRecipe[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                object a = args[i];
                if (a != null && !(a is string) && !a.GetType().IsPrimitive && !(a is decimal)) return source;
                parts[i] = Capture(a == null ? "" : Convert.ToString(a, CultureInfo.CurrentCulture));
            }
            Remember(source, parts); return source;
        }
        internal static string ConcatTwo(string a, string b) { return Join(string.Concat(a,b), new object[] {a,b}); }
        internal static string ConcatThree(string a, string b, string c) { return Join(string.Concat(a,b,c), new object[] {a,b,c}); }
        internal static string ConcatFour(string a, string b, string c, string d) { return Join(string.Concat(a,b,c,d), new object[] {a,b,c,d}); }
        internal static string ConcatStrings(string[] a) { return Join(string.Concat(a), a); }
        internal static string ConcatObjects(object a, object b, object c) { return Join(string.Concat(a,b,c), new object[] {a,b,c}); }
        internal static string ConcatArray(object[] a) { return Join(string.Concat(a), a); }
        // Roster strings arrive preformatted; only the producer's anchored numeric
        // suffix is recognized. The entire name prefix stays opaque, including newlines.
        internal static string Roster(string source)
        {
            if (source == null) return null;
            Match m = Regex.Match(source, @"\A(.*)(  [0-9]+ ms  \()([0-9]+)( controllers?)(\))\z", RegexOptions.Singleline);
            if (!m.Success) return source;
            return ConcatStrings(new string[] { m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, Literal(m.Groups[4].Value), m.Groups[5].Value });
        }
        // These values are read only at UI sites; exception details stay verbatim.
        internal static string Message(string source)
        {
            if (source == null || Find(source) != null) return source;
            foreach (string prefix in new string[] { "Host failed: ", "UDP host failed: ", "Connect failed: ", "lobby creation failed: ", "could not enter lobby (", "Rejected: ", "Host closed the session: " })
                if (source.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string tail = source.Substring(prefix.Length);
                    if (prefix == "Rejected: " || prefix == "Host closed the session: ") tail = Message(tail);
                    return ConcatTwo(Literal(prefix), tail);
                }
            if (source.StartsWith("Hosting (Steam, ", StringComparison.Ordinal) && source.EndsWith(" lobby)", StringComparison.Ordinal))
                return ConcatThree(Literal("Hosting (Steam, "), Literal(source.Substring(16, source.Length-23)), Literal(" lobby)"));
            if (source.StartsWith("Hosting (UDP port ", StringComparison.Ordinal) && source.EndsWith(")", StringComparison.Ordinal))
                return ConcatThree(Literal("Hosting (UDP port "), source.Substring(18, source.Length-19), ")");
            if (source.StartsWith("Joining ", StringComparison.Ordinal) && source.EndsWith("...", StringComparison.Ordinal))
                return ConcatThree(Literal("Joining "), source.Substring(8, source.Length-11), "...");
            const string versionPrefix = "Crawl Online version mismatch: host has ";
            if (source.StartsWith(versionPrefix, StringComparison.Ordinal))
            {
                int split = source.IndexOf(", you have ", versionPrefix.Length, StringComparison.Ordinal);
                if (split >= 0)
                {
                    string tail = source.Substring(split + 11);
                    string suffix = ". Use the same version.";
                    if (tail.EndsWith(suffix, StringComparison.Ordinal))
                        tail = ConcatTwo(tail.Substring(0,tail.Length-suffix.Length),Literal(suffix));
                    return ConcatFour(Literal(versionPrefix),source.Substring(versionPrefix.Length,split-versionPrefix.Length),Literal(", you have "),tail);
                }
            }
            return Literal(source); // Unknown messages have no table match and remain unchanged.
        }
    }
}
