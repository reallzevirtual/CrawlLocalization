using System;
using System.Collections.Generic;
using System.IO;
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

        internal FontService(Action<string> warn, Action<string> info) { this.warn = warn; this.info = info; }

        internal bool TryChoose(LanguagePack language, Font original, int size, FontStyle style, string text, out FontChoice choice)
        {
            choice = null;
            FontRule rule;
            if (language.Fonts.TryGetValue(original == null ? "" : original.name, out rule) || language.Fonts.TryGetValue("*", out rule))
            {
                Font candidate = Load(rule);
                int requestedSize = rule.Kind == "system" ? 32 : SamplingSize(size, text);
                if (Covers(candidate, text, requestedSize, style))
                {
                    choice = new FontChoice { Font = candidate, Size = requestedSize, Style = style,
                        Scale = rule.Scale * (rule.Kind == "system" ? HeightRatio(original, size, style, candidate, requestedSize, text) : (float)size / requestedSize) };
                    return true;
                }
                warn("Mapped font unavailable or missing glyphs: " + rule.Name + "; trying original/system fonts.");
            }
            int originalRenderSize = SamplingSize(size, text);
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
        internal static int SamplingSize(int size, string text)
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

        private bool Covers(Font font, string text, int size, FontStyle style)
        {
            try
            {
                if (font == null || font.material == null) return false;
                text = RichTags.Replace(text, "");
                if (font.dynamic) font.RequestCharactersInTexture(text, size, style);
                foreach (char ch in text)
                {
                    if (char.IsWhiteSpace(ch) || char.IsControl(ch)) continue;
                    CharacterInfo glyph;
                    if (!font.GetCharacterInfo(ch, out glyph, size, style) || glyph.glyphWidth <= 0 || glyph.glyphHeight <= 0) return false;
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
            fonts.Clear(); bundles.Clear(); ownedFonts.Clear(); privateFiles.Clear();
        }
    }
}
