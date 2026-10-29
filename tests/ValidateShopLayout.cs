using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using CrawlLocalization;

public static class ValidateShopLayout
{
    [Serializable] public class Entry { public string text, kind, source; }
    [Serializable] public class Layout
    {
        public string source, path, font;
        public int size, anchor, alignment;
        public float character_size, line_spacing, scale_x;
    }
    [Serializable] public class Catalog { public Entry[] entries; public Layout[] layouts; }
    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/shop-layout");
        Catalog catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output, "cases.json")));
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(root, "Fonts/crawl-chinese-fonts.bundle"));
        if (bundle == null) throw new Exception("Missing font bundle");
        GameObject obj = new GameObject("ShopTextValidation");
        int count = 0, maxLines = 0; float maxHeight = 0, maxTitleWidth = 0;
        string tallest = "", widest = "";
        try
        {
            TextMesh mesh = obj.AddComponent<TextMesh>();
            foreach (Layout layout in catalog.layouts)
            {
                string kind = layout.path == "GUIGame/Store/Title/TextTitle" ? "title" : layout.path == "GUIGame/Store/TextDescription" ? "body" : null;
                if (kind == null) continue;
                Font font = bundle.LoadAsset<Font>("assets/fonts/" + layout.font.ToLowerInvariant() + ".ttf");
                mesh.font = font; mesh.fontSize = layout.size; mesh.characterSize = layout.character_size;
                mesh.lineSpacing = layout.line_spacing; mesh.anchor = (TextAnchor)layout.anchor; mesh.alignment = (TextAlignment)layout.alignment;
                obj.transform.localScale = new Vector3(layout.scale_x, 1, 1);
                mesh.GetComponent<Renderer>().sharedMaterial = font.material;
                foreach (Entry entry in catalog.entries)
                {
                    if (entry.kind != kind) continue;
                    string text = entry.text;
                    font.RequestCharactersInTexture(text, layout.size, FontStyle.Normal);
                    Dictionary<char,float> advances = new Dictionary<char,float>();
                    foreach (char ch in text)
                    {
                        if (ch == '\r' || ch == '\n') continue;
                        CharacterInfo glyph;
                        if (!font.GetCharacterInfo(ch, out glyph, layout.size, FontStyle.Normal)) throw new Exception("Missing glyph: " + ch);
                        advances[ch] = glyph.advance * 0.1f * layout.character_size * Math.Abs(layout.scale_x);
                    }
                    string wrapped = kind == "body" ? ChineseTextWrapper.Wrap(text, 115, delegate(char ch) { return advances[ch]; }) : text;
                    if (Regex.Replace(wrapped, @"\s", "") != Regex.Replace(text, @"\s", "")) throw new Exception("Text lost: " + entry.source);
                    foreach (string line in wrapped.Split('\n'))
                    {
                        if (line.Length == 0) continue;
                        mesh.text = line;
                        float width = mesh.GetComponent<Renderer>().bounds.size.x;
                        if (width > 115.01f) throw new Exception("Overflow: " + entry.source + " width=" + width);
                        if (kind == "title" && width > maxTitleWidth) { maxTitleWidth = width; widest = entry.source; }
                    }
                    mesh.text = wrapped;
                    if (kind == "body")
                    {
                        maxLines = Math.Max(maxLines, wrapped.Split('\n').Length);
                        float height = mesh.GetComponent<Renderer>().bounds.size.y;
                        if (height > maxHeight) { maxHeight = height; tallest = entry.source; }
                    }
                    count++;
                }
            }
            string report = "PASS: " + count + " shop display cases, Unity 5.4 actual serialized fonts/sizes/scales/spacing.\nBody lines fit serialized width 115; titles measured against the same panel width (not a title clipping boundary).\nRich tags and all non-whitespace content preserved.\nMax body lines=" + maxLines + ", height=" + maxHeight + ", source=" + tallest + "\nMax title width=" + maxTitleWidth + ", source=" + widest + "\nThese are isolated text geometry checks. Panel positioning, vertical clearance and in-game item transitions need user verification.";
            File.WriteAllText(Path.Combine(output, "geometry.txt"), report);
            Debug.Log("CRAWL_SHOP_LAYOUT_OK\n" + report);
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); bundle.Unload(true); }
    }
}
