using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CrawlLocalization
{
    internal sealed class FontChoice
    {
        internal Font Font;
        internal int Size;
        internal FontStyle Style;
        internal float Scale = 1f;
    }

    internal sealed class FontService : IDisposable
    {
        private readonly Action<string> warn;
        private readonly Action<string> info;
        private readonly string root;
        private readonly Dictionary<Font, bool[]> coverage = new Dictionary<Font, bool[]>();
        private readonly Dictionary<string, Dictionary<string, bool[]>> bundleCoverage = new Dictionary<string, Dictionary<string, bool[]>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>(StringComparer.Ordinal);
        private readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> privateFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Font> ownedFonts = new List<Font>();
        private static readonly Regex RichTags = new Regex(@"</?(?:b|i|size|color|material)(?:=[^>]*)?>|<quad\s[^>]*>", RegexOptions.IgnoreCase);
        private const uint PrivateFont = 0x10;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int AddFontResourceEx(string file, uint flags, IntPtr reserved);
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool RemoveFontResourceEx(string file, uint flags, IntPtr reserved);

        internal FontService(Action<string> warn, Action<string> info, string root = null) { this.warn = warn; this.info = info; this.root = root; }

        internal bool TryChoose(LanguagePack language, Font original, int size, FontStyle style, string text, out FontChoice choice)
        {
            choice = null;
            FontRule rule;
            if (language.Fonts.TryGetValue(original == null ? "" : original.name, out rule) || language.Fonts.TryGetValue("*", out rule))
            {
                Font candidate = Load(rule);
                int requestedSize = rule.Kind == "system" ? 32 : SamplingSize(language, size, text);
                if (Covers(candidate, text, requestedSize, style))
                {
                    choice = new FontChoice { Font = candidate, Size = requestedSize, Style = style,
                        Scale = rule.Scale * (rule.Kind == "system" ? HeightRatio(original, size, style, candidate, requestedSize, text) : (float)size / requestedSize) };
                    return true;
                }
                warn("Mapped font unavailable or missing glyphs: " + rule.Name + "; trying shared/original/system fonts.");
            }
            if (TryShared(original == null ? null : original.name, size, style, text, false, out choice)) return true;
            int originalRenderSize = SamplingSize(language, size, text);
            if (Covers(original, text, originalRenderSize, style))
            {
                choice = new FontChoice { Font = original, Size = originalRenderSize, Style = style, Scale = (float)size / originalRenderSize };
                return true;
            }
            foreach (string family in language.FallbackFonts)
            {
                Font candidate = Load(new FontRule { Kind = "system", File = "", Name = family.Trim(), Scale = 1f });
                if (!Covers(candidate, text, 32, style)) continue;
                choice = new FontChoice { Font = candidate, Size = 32, Style = style, Scale = HeightRatio(original, size, style, candidate, 32, text) };
                return true;
            }
            warn("No font covers translation: " + text + "; retaining original menu text.");
            return false;
        }

        internal bool TryChooseUntranslated(LanguagePack language, Font original, int size, FontStyle style, string text, out FontChoice choice)
        {
            choice = null;
            bool nonAscii = false;
            if (text != null) foreach (char ch in text) if (ch > 127) { nonAscii = true; break; }
            if (!nonAscii && Covers(original, text, size, style)) return false;
            return TryChoose(language, original, size, style, text, out choice);
        }

        private bool TryShared(string name, int size, FontStyle style, string text, bool gui, out FontChoice choice)
        {
            choice = null;
            if (root == null || string.IsNullOrEmpty(name)) return false;
            // Match the exact original style, never substitute a large font for a small one.
            string[] names = { "CrawlLarge", "CrawlSmall", "CrawlMedium", "CrawlTiny", "CrawlInvLarge", "CrawlTextBodyThin", "CrawlTextBodyWide", "CrawlTextBodyTeensy", "04B_03-White", "04B_04-Black" };
            if (Array.IndexOf(names, name) < 0) return false;
            foreach (string file in new string[] { "crawl-multilingual.bundle", "crawl-chinese-fonts.bundle" })
            {
                Font candidate = Load(new FontRule { Kind = "bundle", File = Path.Combine(Path.Combine(root, "Fonts"), file), Name = "assets/fonts/" + name.ToLowerInvariant() + ".ttf", Scale = 1f });
                // Raster resolution belongs to the fallback font/glyph, not the UI language.
                // This does not enable Chinese translation, punctuation or wrapping rules.
                int requested = !gui && file == "crawl-chinese-fonts.bundle" ? HanSamplingSize(size, text) : size;
                if (!Covers(candidate, text, requested, style)) continue;
                choice = new FontChoice { Font = candidate, Size = requested, Style = style, Scale = (float)size / requested };
                return true;
            }
            return false;
        }

        // IMGUI measures in screen pixels: it must not use TextMesh's sampling scale.
        internal bool TryChooseGui(LanguagePack language, Font original, int size, FontStyle style, string text, out Font font)
        {
            font = original;
            FontRule rule;
            if (language.Fonts.TryGetValue("CrawlOnlineOverlay", out rule))
            {
                Font candidate = Load(rule);
                if (Covers(candidate, text, size, style)) { font = candidate; return true; }
            }
            bool nonAscii = false;
            if (text != null) foreach (char ch in text) if (ch > 127) { nonAscii = true; break; }
            if (!nonAscii && Covers(original, text, size, style)) return true;
            FontChoice shared;
            if (TryShared("CrawlTextBodyThin", size, style, text, true, out shared)) { font = shared.Font; return true; }
            if (Covers(original, text, size, style)) return true;
            foreach (string family in language.FallbackFonts)
            {
                Font candidate = Load(new FontRule { Kind = "system", File = "", Name = family.Trim(), Scale = 1f });
                if (Covers(candidate, text, size, style)) { font = candidate; return true; }
            }
            return false;
        }

        // Crawl's small labels use fontSize=16 and characterSize=10. A merged
        // CJK glyph is only about 5 pixels tall at 16, so enlarging that atlas
        // loses its strokes. Increase raster resolution, not world-space size.
        internal static int SamplingSize(LanguagePack language, int size, string text)
        {
            return language.UsesChineseTextRules ? HanSamplingSize(size, text) : size;
        }

        private static int HanSamplingSize(int size, string text)
        {
            if (size <= 0 || size >= 64 || text == null) return size;
            foreach (char ch in text)
                if (ch >= '\u3400' && ch <= '\u9fff') return size * ((160 + size - 1) / size);
            return size;
        }

        private Font Load(FontRule rule)
        {
            string key = rule.Kind + "|" + rule.File + "|" + rule.Name;
            Font font;
            if (fonts.TryGetValue(key, out font)) return font;
            font = null;
            try
            {
                if (rule.Kind == "bundle")
                {
                    AssetBundle bundle;
                    if (!bundles.TryGetValue(rule.File, out bundle))
                    {
                        if (!File.Exists(rule.File)) throw new FileNotFoundException("Font bundle not found.", rule.File);
                        bundle = AssetBundle.LoadFromFile(rule.File);
                        bundles.Add(rule.File, bundle);
                    }
                    if (bundle == null) throw new InvalidOperationException("Cannot load bundle; use a Windows bundle compatible with Unity 5.4.2.");
                    font = bundle.LoadAsset<Font>(rule.Name);
                    if (font == null) throw new InvalidOperationException("Font asset not found: " + rule.Name);
                    bool[] known;
                    if (ReadCoverage(rule.File).TryGetValue(rule.Name, out known)) coverage[font] = known;
                }
                else
                {
                    if (rule.Kind == "ttf" && !privateFiles.Contains(rule.File))
                    {
                        if (System.Environment.OSVersion.Platform != PlatformID.Win32NT) throw new PlatformNotSupportedException("Private TTF loading requires Windows. Use a compatible bundle on other platforms.");
                        if (!File.Exists(rule.File)) throw new FileNotFoundException("Font file not found.", rule.File);
                        if (AddFontResourceEx(rule.File, PrivateFont, IntPtr.Zero) == 0) throw new InvalidOperationException("Windows rejected font file (error " + Marshal.GetLastWin32Error() + ").");
                        privateFiles.Add(rule.File);
                    }
                    if (rule.Kind == "system" && !Array.Exists(Font.GetOSInstalledFontNames(), delegate(string name) { return string.Equals(name, rule.Name, StringComparison.OrdinalIgnoreCase); }))
                        throw new InvalidOperationException("System font is not installed: " + rule.Name);
                    font = Font.CreateDynamicFontFromOSFont(rule.Name, 32);
                    if (font == null) throw new InvalidOperationException("Unity could not create font: " + rule.Name);
                    UnityEngine.Object.DontDestroyOnLoad(font);
                    ownedFonts.Add(font);
                }
            }
            catch (Exception ex) { warn("Font load failed [" + key + "]: " + ex.Message); }
            fonts[key] = font;
            if (font != null) info((rule.Kind == "bundle" ? "Loaded embedded font: " : "Created font by family (file identity unverified): ") + key);
            return font;
        }

        // Optional, hash-bound coverage avoids Unity's implicit OS-font substitution.
        // Replacement/custom bundles without matching metadata use native probing.
        private Dictionary<string, bool[]> ReadCoverage(string file)
        {
            Dictionary<string, bool[]> result;
            if (bundleCoverage.TryGetValue(file, out result)) return result;
            result = new Dictionary<string, bool[]>(StringComparer.OrdinalIgnoreCase);
            bundleCoverage[file] = result;
            string path = Path.ChangeExtension(file, ".coverage.tsv");
            if (!File.Exists(path)) return result;
            try
            {
                string[] lines = File.ReadAllLines(path);
                string hash;
                using (SHA256 sha = SHA256.Create())
                using (FileStream stream = File.OpenRead(file))
                    hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (lines.Length == 0 || lines[0] != "sha256\t" + hash) throw new FormatException("Bundle hash differs; regenerate coverage after replacing the font.");
                foreach (string line in lines)
                {
                    if (line == lines[0] || line.Length == 0) continue;
                    string[] fields = line.Split('\t');
                    if (fields.Length != 2) throw new FormatException("Expected asset path and codepoint ranges.");
                    bool[] chars = new bool[65536];
                    foreach (string range in fields[1].Split(','))
                    {
                        string[] ends = range.Split('-');
                        if (ends.Length != 2) throw new FormatException("Invalid codepoint range.");
                        int first = int.Parse(ends[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int last = int.Parse(ends[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        if (first < 0 || last < first || last >= chars.Length) throw new FormatException("Invalid codepoint limits.");
                        for (int ch = first; ch <= last; ch++) chars[ch] = true;
                    }
                    result.Add(fields[0], chars);
                }
            }
            catch (Exception ex) { result.Clear(); warn("Font coverage ignored [" + path + "]: " + ex.Message); }
            return result;
        }

        private bool Covers(Font font, string text, int size, FontStyle style)
        {
            try
            {
                if (font == null || font.material == null) return false;
                text = RichTags.Replace(text ?? "", "");
                bool[] known;
                if (coverage.TryGetValue(font, out known))
                    foreach (char ch in text)
                        if (!char.IsWhiteSpace(ch) && !char.IsControl(ch) && !known[ch]) return false;
                if (font.dynamic) font.RequestCharactersInTexture(text, size, style);
                foreach (char ch in text)
                {
                    if (char.IsWhiteSpace(ch) || char.IsControl(ch)) continue;
                    CharacterInfo glyph;
                    if (!font.HasCharacter(ch) || !font.GetCharacterInfo(ch, out glyph, size, style) || glyph.glyphWidth <= 0 || glyph.glyphHeight <= 0) return false;
                }
                return true;
            }
            catch (Exception ex) { warn("Font glyph check failed; trying next font: " + ex.Message); return false; }
        }

        private static float HeightRatio(Font original, int originalSize, FontStyle style, Font replacement, int size, string text)
        {
            CharacterInfo glyph;
            float height = originalSize * 0.7f;
            if (original != null)
            {
                if (original.dynamic) original.RequestCharactersInTexture("A", originalSize, style);
                if (original.GetCharacterInfo('A', out glyph, originalSize, style)) height = Mathf.Max(1, glyph.maxY - glyph.minY);
            }
            // Prefer an actual non-ASCII glyph; this also works for future non-Chinese packs.
            foreach (char ch in RichTags.Replace(text, ""))
                if (ch > 127 && !char.IsWhiteSpace(ch) && replacement.GetCharacterInfo(ch, out glyph, size, style))
                    return height / Mathf.Max(1, glyph.maxY - glyph.minY);
            return height / Mathf.Max(1, size * 0.7f);
        }

        public void Dispose()
        {
            foreach (Font font in ownedFonts) if (font != null) UnityEngine.Object.Destroy(font);
            foreach (AssetBundle bundle in bundles.Values) if (bundle != null) bundle.Unload(true);
            foreach (string file in privateFiles) RemoveFontResourceEx(file, PrivateFont, IntPtr.Zero);
            coverage.Clear(); bundleCoverage.Clear(); fonts.Clear(); bundles.Clear(); ownedFonts.Clear(); privateFiles.Clear();
        }
    }
}
