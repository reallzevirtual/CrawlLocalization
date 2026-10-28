using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;

public static class ValidateChapterBinding
{
    [Serializable] public class Layout { public string kind; public string[] components; }
    [Serializable] public class Catalog { public Layout[] layouts; }
    static int checks;
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static void Patch(TextMesh chapter, TextMesh floor)
    {
        typeof(ChapterTitlePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { chapter, floor });
    }
    static TextMesh Create(GameObject root, string name, string fontName)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        TextMesh mesh = obj.AddComponent<TextMesh>();
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 160);
        font.name = fontName;
        mesh.font = font; mesh.fontSize = 160; mesh.characterSize = 1;
        mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
        obj.GetComponent<Renderer>().sharedMaterial = font.material;
        obj.AddComponent<MenuTextMenuItem>();
        obj.AddComponent<GUIText>();
        return mesh;
    }
    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/chapter-binding");
        Catalog catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output, "catalog.json")));
        Check(catalog.layouts.Length == 2, "Both chapter resource labels must be audited");
        foreach (Layout layout in catalog.layouts)
            Check(Array.IndexOf(layout.components,"MenuTextMenuItem") >= 0 && Array.IndexOf(layout.components,"GUIText") >= 0, "Serialized component combination changed: " + layout.kind);
        Plugin plugin = new Plugin(); Plugin.Instance = plugin;
        plugin.Language = LanguagePack.Load(root, "zh-Hans", plugin.Warn);
        plugin.Fonts = new FontService(plugin.Warn, delegate(string message) { });
        GameObject title = new GameObject("GUILevelTitle(Clone)");
        TextMesh chapter = Create(title, "LevelNum", "CrawlTextBodyThin");
        TextMesh floor = Create(title, "FloorName", "CrawlLarge");
        Font originalChapter = chapter.font, originalFloor = floor.font;
        try
        {
            chapter.text = "- Chapter 1 -"; floor.text = "Ganon's Grave";
            // Reproduce the old bug with actual MenuTextMeshes.Scan and real component types.
            MenuTextMeshes.Scan(title);
            Check(plugin.Labels.Count == 0 && chapter.text == "- Chapter 1 -" && floor.text == "Ganon's Grave", "Old scan must reproduce the exclusion bug");
            Patch(chapter, floor);
            Check(chapter.text == "- 第 1 章 -" && floor.text == "加农之墓", "Direct field binding translates both labels");
            Check(chapter.font != originalChapter && floor.font != originalFloor, "Chinese font selected");
            Check(plugin.Labels.Count == 2, "Only two referenced labels bound");
            Check(chapter.anchor == TextAnchor.MiddleCenter && chapter.characterSize == 1 && chapter.fontSize == 160 && chapter.transform.localPosition == Vector3.zero, "Original layout preserved");
            // Refresh must adopt the new source even if the label already existed before Start.
            chapter.text = "- Chapter 10 -"; floor.text = "Mordul's Tomb";
            title.name = "RenamedTitle";
            Patch(chapter, floor);
            Check(chapter.text == "- 第 10 章 -" && floor.text == "莫杜尔陵墓" && plugin.Labels.Count == 2, "Existing labels refresh without depending on hierarchy names");
            plugin.Language = LanguagePack.Load(root, "en", plugin.Warn);
            foreach (LocalizedMenuLabel label in plugin.Labels) label.Refresh();
            Check(chapter.text == "- Chapter 10 -" && floor.text == "Mordul's Tomb", "Reload restores latest English sources");
            Check(chapter.font == originalChapter && floor.font == originalFloor, "Reload restores original fonts");
            plugin.Language = LanguagePack.Load(root, "zh-Hans", plugin.Warn);
            foreach (LocalizedMenuLabel label in plugin.Labels) label.Refresh();
            Check(chapter.text == "- 第 10 章 -" && floor.text == "莫杜尔陵墓", "Reload to Chinese works");
            floor.text = "Unknown mod floor"; Patch(null, floor);
            Check(floor.text == "Unknown mod floor" && floor.font == originalFloor, "Unknown floor and missing field safely fall back");
            title.SetActive(false); chapter.text = "- Chapter 99 -"; floor.text = "Ganon's Grave";
            Patch(chapter, floor);
            Check(chapter.text == "- 第 99 章 -" && floor.text == "加农之墓", "Inactive referenced labels refresh too");
            Plugin.Instance = null; Patch(null, null); Plugin.Instance = plugin;
            string report = "PASS: " + checks + " chapter binding regression checks using actual localization sources and Unity 5.4 TextMeshes/fonts.\nOriginal MenuTextMenuItem exclusion reproduced; direct references, existing labels, unknown text, inactive labels, layout and language/font restoration verified.\nCrawl/Harmony entry points are test doubles; no game launched. Full game transitions remain user verification.";
            File.WriteAllText(Path.Combine(output, "result.txt"), report);
            Debug.Log("CRAWL_CHAPTER_BINDING_OK\n" + report);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(title);
            plugin.Fonts.Dispose(); Plugin.Instance = null;
            UnityEngine.Object.DestroyImmediate(originalChapter); UnityEngine.Object.DestroyImmediate(originalFloor);
        }
    }
}
