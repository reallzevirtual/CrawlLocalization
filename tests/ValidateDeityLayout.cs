using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using CrawlLocalization;

public static class ValidateDeityLayout
{
    [Serializable] public class Entry { public string text, kind, source; }
    [Serializable] public class Layout
    {
        public string kind, source, path, font;
        public int size, anchor, alignment;
        public float character_size, line_spacing, scale_x, width;
    }
    [Serializable] public class Catalog { public Entry[] entries; public Layout[] layouts; }

    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/deity-layout");
        Catalog catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output, "catalog.json")));
        if (catalog.entries.Length == 0 || catalog.layouts.Length != 12) throw new Exception("Incomplete deity extraction");
        LanguagePack pack = LanguagePack.Load(root, "zh-Hans", delegate(string warning) { throw new Exception(warning); });
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(root, "Fonts/crawl-chinese-fonts.bundle"));
        if (bundle == null) throw new Exception("Font bundle not loaded");
        GameObject obj = new GameObject("DeityTextValidation");
        int count = 0, maxLines = 0;
        float maxHeight = 0, maxNameWidth = 0;
        try
        {
            TextMesh mesh = obj.AddComponent<TextMesh>();
            foreach (Layout layout in catalog.layouts)
            {
                Font font = bundle.LoadAsset<Font>("assets/fonts/" + layout.font.ToLowerInvariant() + ".ttf");
                mesh.font = font;
                mesh.fontSize = layout.size;
                mesh.characterSize = layout.character_size;
                mesh.lineSpacing = layout.line_spacing;
                mesh.anchor = (TextAnchor)layout.anchor;
                mesh.alignment = (TextAlignment)layout.alignment;
                obj.transform.localScale = new Vector3(layout.scale_x, 1, 1);
                mesh.GetComponent<Renderer>().sharedMaterial = font.material;
                foreach (Entry entry in catalog.entries)
                {
                    if (entry.kind != layout.kind) continue;
                    string text;
                    if (!pack.TryTranslate(entry.text, out text) || !ChineseTextWrapper.ContainsChinese(text)) throw new Exception("Untranslated: " + entry.source);
                    font.RequestCharactersInTexture(text, layout.size, FontStyle.Normal);
                    Dictionary<char, float> advances = new Dictionary<char, float>();
                    foreach (char ch in text)
                    {
                        if (ch == '\r' || ch == '\n') continue;
                        CharacterInfo glyph;
                        if (!font.GetCharacterInfo(ch, out glyph, layout.size, FontStyle.Normal)) throw new Exception("Missing glyph: " + ch);
                        advances[ch] = glyph.advance * 0.1f * layout.character_size * Math.Abs(layout.scale_x);
                    }
                    string wrapped = ChineseTextWrapper.Wrap(text, layout.width, delegate(char ch) { return advances[ch]; });
                    if (Regex.Replace(wrapped, @"\s", "") != Regex.Replace(text, @"\s", "")) throw new Exception("Text lost: " + entry.source);
                    foreach (string line in wrapped.Split('\n'))
                    {
                        if (line.Length == 0) continue;
                        mesh.text = line;
                        float measured = mesh.GetComponent<Renderer>().bounds.size.x;
                        // Name has no wrapper; use the adjacent 35-unit text column as a conservative check.
                        if (measured > (layout.width > 0 ? layout.width : 35) + 0.01f) throw new Exception("Overflow: " + entry.source + " line=" + line + " width=" + measured);
                        if (entry.kind == "name") maxNameWidth = Math.Max(maxNameWidth, measured);
                    }
                    mesh.text = wrapped;
                    maxLines = Math.Max(maxLines, wrapped.Split('\n').Length);
                    maxHeight = Math.Max(maxHeight, mesh.GetComponent<Renderer>().bounds.size.y);
                    count++;
                }
            }
            string report = "PASS: " + count + " deity display/layout combinations, covering all 12 audited labels across four players.\nActual source font/size/scale/spacing used; all translated lines fit within 35 units and preserve content.\nMaximum lines=" + maxLines + "; maximum height=" + maxHeight + "; maximum name width=" + maxNameWidth + ".\nIn-game state transitions and panel placement remain user verification.";
            File.WriteAllText(Path.Combine(output, "geometry.txt"), report);
            Debug.Log("CRAWL_DEITY_LAYOUT_OK\n" + report);
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); bundle.Unload(true); }
    }
}
