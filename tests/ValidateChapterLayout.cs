using System;
using System.IO;
using UnityEngine;
using CrawlLocalization;

public static class ValidateChapterLayout
{
    [Serializable] public class Entry { public string text, kind, source; }
    [Serializable] public class Layout
    {
        public string kind, source, path, font;
        public int size, anchor, alignment;
        public float character_size, line_spacing, scale_x;
    }
    [Serializable] public class Catalog { public Entry[] entries; public Layout[] layouts; }

    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/chapter-layout");
        Catalog catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output, "catalog.json")));
        if (catalog.entries.Length == 0 || catalog.layouts.Length != 2) throw new Exception("Incomplete chapter extraction");
        LanguagePack pack = LanguagePack.Load(root, "zh-Hans", delegate(string warning) { throw new Exception(warning); });
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(root, "Fonts/crawl-chinese-fonts.bundle"));
        if (bundle == null) throw new Exception("Font bundle not loaded");
        GameObject obj = new GameObject("ChapterTextValidation");
        int count = 0;
        string report = "";
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
                float maxOriginal = 0, maxChinese = 0, maxHeight = 0;
                foreach (Entry entry in catalog.entries)
                {
                    if (entry.kind != layout.kind) continue;
                    string[] sources = entry.kind == "chapter" ? new string[] { string.Format(entry.text, 1), string.Format(entry.text, 10), string.Format(entry.text, 99) } : new string[] { entry.text };
                    foreach (string source in sources)
                    {
                        font.RequestCharactersInTexture(source, layout.size, FontStyle.Normal);
                        mesh.text = source;
                        maxOriginal = Math.Max(maxOriginal, mesh.GetComponent<Renderer>().bounds.size.x);
                        string text;
                        if (!pack.TryTranslate(source, out text) || !ChineseTextWrapper.ContainsChinese(text)) throw new Exception("Untranslated: " + entry.source);
                        if (text.Contains("\n") || text.Contains("\r")) throw new Exception("Title should stay on one line: " + entry.source);
                        font.RequestCharactersInTexture(text, layout.size, FontStyle.Normal);
                        foreach (char ch in text)
                        {
                            CharacterInfo glyph;
                            if (!font.GetCharacterInfo(ch, out glyph, layout.size, FontStyle.Normal)) throw new Exception("Missing glyph: " + ch);
                        }
                        mesh.text = text;
                        maxChinese = Math.Max(maxChinese, mesh.GetComponent<Renderer>().bounds.size.x);
                        maxHeight = Math.Max(maxHeight, mesh.GetComponent<Renderer>().bounds.size.y);
                        count++;
                    }
                }
                if (maxChinese > maxOriginal + 0.01f) throw new Exception("Chinese titles exceed widest original title: " + layout.kind);
                report += layout.kind + ": widest original=" + maxOriginal + ", widest Chinese=" + maxChinese + ", maximum Chinese height=" + maxHeight + ".\n";
            }
            report = "PASS: " + count + " chapter display cases measured in Unity 5.4 with actual font/size/scale/spacing.\n" + report + "All Chinese titles stay within the original title set's maximum width. In-game spawning and placement still require user verification.";
            File.WriteAllText(Path.Combine(output, "geometry.txt"), report);
            Debug.Log("CRAWL_CHAPTER_LAYOUT_OK\n" + report);
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); bundle.Unload(true); }
    }
}
