using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CrawlLocalization
{
    internal sealed class FontRule
    {
        internal string Kind, File, Name;
        internal float Scale;
    }

    // No Unity dependency: language packs can be validated without starting Crawl.
    internal sealed class LanguagePack
    {
        internal string Id;
        internal string[] FallbackFonts = new string[0];
        internal readonly Dictionary<string, FontRule> Fonts = new Dictionary<string, FontRule>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> exact = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<NumericTranslation> numeric = new List<NumericTranslation>();
        internal readonly HashSet<string> TextMeshPaths = new HashSet<string>(StringComparer.Ordinal);
        internal int Count { get { return exact.Count + numeric.Count; } }

        internal static string NormalizeId(string id)
        {
            id = (id ?? "").Trim();
            if (string.Equals(id, "zh-CN", StringComparison.OrdinalIgnoreCase) || string.Equals(id, "zh-Hans", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
            if (string.Equals(id, "zh-TW", StringComparison.OrdinalIgnoreCase) || string.Equals(id, "zh-Hant", StringComparison.OrdinalIgnoreCase)) return "zh-Hans"; // Migrate the retired Traditional Chinese setting.
            if (string.Equals(id, "en", StringComparison.OrdinalIgnoreCase)) return "en";
            return Regex.IsMatch(id, @"\A[A-Za-z0-9][A-Za-z0-9_-]*\z") ? id : "en";
        }

        internal static string ResolveFile(string root, string relative)
        {
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)) throw new FormatException("Expected a relative file path.");
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(prefix, relative));
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new FormatException("File must stay inside the plugin folder.");
            return full;
        }

        internal static LanguagePack Load(string root, string id, Action<string> warn)
        {
            LanguagePack pack = new LanguagePack { Id = NormalizeId(id) };
            if (pack.Id == "en") return pack;
            string directory = Path.Combine(Path.Combine(root, "Languages"), pack.Id);
            foreach (string table in new string[] { "translations.tsv", "vault-translations.tsv", "deity-translations.tsv", "chapter-translations.tsv", "shop-translations.tsv", "gameplay-translations.tsv", "progression-translations.tsv", "shop-il-translations.tsv" })
            {
                string path = Path.Combine(directory, table);
                if (table != "translations.tsv" && !File.Exists(path)) continue;
                ReadLines(path, warn, delegate(string line)
                {
                    string[] cells = line.Split('\t');
                    if ((cells.Length != 2 && cells.Length != 3) || cells[0].Length == 0 || (cells[1].Length == 0 && !cells[0].StartsWith("@shop.il:", StringComparison.Ordinal))) throw new FormatException("Expected source<TAB>translation[<TAB>comment].");
                    string source = NormalizeText(Unescape(cells[0])), target = Unescape(cells[1]);
                    NumericTranslation template = NumericTranslation.Create(source, target);
                    if (source.StartsWith("@shop.", StringComparison.Ordinal) || source.StartsWith("@progression.", StringComparison.Ordinal)) template = null;
                    if (template != null)
                    {
                        foreach (NumericTranslation entry in pack.numeric)
                            if (string.Equals(entry.Source, source, StringComparison.OrdinalIgnoreCase)) throw new FormatException("Duplicate source: " + source);
                        pack.numeric.Add(template);
                    }
                    else
                    {
                        if (pack.exact.ContainsKey(source)) throw new FormatException("Duplicate source: " + source);
                        pack.exact.Add(source, target);
                    }
                });
            }
            ReadLines(Path.Combine(directory, "fonts.tsv"), warn, delegate(string line)
            {
                string[] c = line.Split('\t');
                float scale;
                if (c.Length != 5 || c[0].Length == 0 || c[3].Length == 0 || !float.TryParse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture, out scale) || float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0 || scale > 10)
                    throw new FormatException("Expected original<TAB>kind<TAB>file<TAB>family/asset<TAB>scale (0..10).");
                if (c[1] != "ttf" && c[1] != "bundle" && c[1] != "system") throw new FormatException("Font kind must be ttf, bundle or system.");
                string file = c[1] == "system" ? "" : ResolveFile(root, c[2]);
                if (pack.Fonts.ContainsKey(c[0])) throw new FormatException("Duplicate font mapping: " + c[0]);
                pack.Fonts.Add(c[0], new FontRule { Kind = c[1], File = file, Name = c[3], Scale = scale });
            });
            ReadLines(Path.Combine(directory, "language.ini"), warn, delegate(string line)
            {
                int separator = line.IndexOf('=');
                if (separator <= 0) throw new FormatException("Expected key=value.");
                string key = line.Substring(0, separator).Trim();
                if (key == "FallbackFonts")
                    pack.FallbackFonts = line.Substring(separator + 1).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                else if (key != "Name") throw new FormatException("Unknown setting: " + key);
            });
            string paths = Path.Combine(directory, "textmesh-paths.txt");
            if (File.Exists(paths)) ReadLines(paths, warn, delegate(string line) { pack.TextMeshPaths.Add(line.Trim()); });
            return pack;
        }

        internal static string[] ReadTextLines(string path)
        {
            // Editors can hold a write handle while the user adjusts a language pack.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                List<string> lines = new List<string>(); string line;
                while ((line = reader.ReadLine()) != null) lines.Add(line);
                return lines.ToArray();
            }
        }

        private static void ReadLines(string path, Action<string> warn, Action<string> read)
        {
            try
            {
                string[] lines = ReadTextLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Length == 0 || lines[i].StartsWith("#", StringComparison.Ordinal)) continue;
                    try { read(lines[i]); }
                    catch (Exception ex) { warn(path + ":" + (i + 1) + ": " + ex.Message); }
                }
            }
            catch (Exception ex) { warn("Cannot read " + path + ": " + ex.Message + "; keeping original text/font where needed."); }
        }

        internal bool TryTranslate(string source, out string translated)
        {
            translated = null;
            if (source == null) return false;
            source = NormalizeText(source);
            if (exact.TryGetValue(source, out translated)) return true;
            foreach (NumericTranslation entry in numeric)
                if (entry.TryTranslate(source, out translated)) return true;
            return false;
        }

        internal static string NormalizeText(string text) { return text.Replace("\r\n", "\n").Replace('\r', '\n'); }

        private static string Unescape(string value)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\') { result.Append(value[i]); continue; }
                if (++i == value.Length) throw new FormatException("Trailing backslash; escape literal backslashes.");
                switch (value[i])
                {
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case '\\': result.Append('\\'); break;
                    default: throw new FormatException("Unsupported escape sequence.");
                }
            }
            return result.ToString();
        }
    }
}
