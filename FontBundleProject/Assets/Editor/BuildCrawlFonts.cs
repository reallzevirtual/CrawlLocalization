using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

// Compiled by Unity 5.4's C# compiler, independently of the BepInEx plugin.
public static class BuildCrawlFonts
{
    private const string BundleName = "crawl-fonts.bundle";
    private static bool BatchMode { get { return Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0; } }
    private static readonly string[] Names = {
        "CrawlLarge", "CrawlTextBodyWide", "CrawlTextBodyThin", "CrawlSmall",
        "04B_04-Black", "04B_03-White", "CrawlTextBodyTeensy", "CrawlMedium",
        "CrawlInvLarge", "CrawlTiny"
    };

    [MenuItem("Crawl Localization/Build Font Bundle")]
    public static void Build()
    {
        try
        {
            if (Application.unityVersion != "5.4.2f2")
                throw new InvalidOperationException("Use Unity 5.4.2f2; found " + Application.unityVersion);
            string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string output = Path.Combine(root, "artifacts/font-bundle");
            Directory.CreateDirectory(output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] assets = new string[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                string name = Names[i];
                string path = "Assets/Fonts/" + name + ".ttf";
                TrueTypeFontImporter importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                if (importer == null) throw new InvalidOperationException("Font import failed: " + path);
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.includeFontData = true;
                importer.fontSize = name == "04B_04-Black" ? 200 : (name == "04B_03-White" ? 150 : 16);
                // Preserve the render-mode enum values in Crawl's original font assets.
                importer.fontRenderingMode = (FontRenderingMode)((name == "04B_04-Black" || name == "CrawlTiny") ? 2 : 0);
                importer.fontNames = new string[] { name.StartsWith("04B_", StringComparison.Ordinal) ? "04b03" : name };
                importer.fontReferences = new Font[0];
                importer.SaveAndReimport();
                assets[i] = path;
            }
            AssetDatabase.SaveAssets();
            AssetBundleBuild definition = new AssetBundleBuild();
            definition.assetBundleName = BundleName;
            definition.assetNames = assets;
            // Crawl.exe is PE x86. Build for Windows x86 with the original Unity version.
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output,
                new AssetBundleBuild[] { definition },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows);
            if (manifest == null) throw new InvalidOperationException("BuildPipeline returned no manifest.");

            string bundlePath = Path.Combine(output, BundleName);
            Validate(bundlePath, root, assets, false);
            Debug.Log("CRAWL_FONT_BUNDLE_RAW_OK " + bundlePath);
            if (BatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (BatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    private static Dictionary<string, float[]> ReadLayout(string root)
    {
        Dictionary<string, float[]> result = new Dictionary<string, float[]>();
        foreach (string line in File.ReadAllLines(Path.Combine(root, "FontBundleProject/font-layout.tsv")))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] fields = line.Split('\t');
            if (fields.Length != 4) throw new InvalidOperationException("Invalid font layout row: " + line);
            result.Add(fields[0], new float[] {
                float.Parse(fields[1], CultureInfo.InvariantCulture),
                float.Parse(fields[2], CultureInfo.InvariantCulture),
                float.Parse(fields[3], CultureInfo.InvariantCulture)
            });
        }
        return result;
    }

    private static readonly string[] LayoutProperties = { "m_Ascent", "m_Descent", "m_LineSpacing" };

    // Run in a fresh editor process after serialized layout restoration; never
    // validate layout against Font objects left in the build pipeline's cache.
    public static void ValidateBuiltBundle()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string path = Path.Combine(root, "artifacts/font-bundle/" + BundleName);
        string[] assets = new string[Names.Length];
        for (int i = 0; i < Names.Length; i++) assets[i] = "Assets/Fonts/" + Names[i] + ".ttf";
        Validate(path, root, assets, true);
        Debug.Log("CRAWL_FONT_BUNDLE_OK " + path);
    }

    private static void Validate(string path, string root, string[] assets, bool checkLayout)
    {
        Dictionary<string, float[]> layout = ReadLayout(root);
        HashSet<char> glyphs = new HashSet<char>();
        foreach (string language in new string[] { "zh-Hans", "zh-Hant" })
        {
            string table = Path.Combine(root, "Languages/" + language + "/translations.tsv");
            foreach (string line in File.ReadAllLines(table, Encoding.UTF8))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] columns = line.Split('\t');
                if (columns.Length != 2) throw new InvalidOperationException("Invalid translation row: " + line);
                foreach (char ch in columns[1].Replace("{0}", "0123456789"))
                    if (!char.IsWhiteSpace(ch)) glyphs.Add(ch);
            }
        }
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidOperationException("Cannot reopen the generated bundle.");
        try
        {
            if (bundle.GetAllAssetNames().Length != assets.Length) throw new InvalidOperationException("Unexpected bundle asset count.");
            foreach (string asset in assets)
            {
                Font font = bundle.LoadAsset<Font>(asset.ToLowerInvariant());
                if (font == null || !font.dynamic) throw new InvalidOperationException("Missing dynamic Font: " + asset);
                SerializedObject serialized = new SerializedObject(font);
                for (int i = 0; checkLayout && i < LayoutProperties.Length; i++)
                {
                    SerializedProperty property = serialized.FindProperty(LayoutProperties[i]);
                    if (property == null || property.floatValue != layout[font.name][i])
                        throw new InvalidOperationException("Font layout changed during build: " + font.name + "/" + LayoutProperties[i]);
                }
                foreach (char ch in glyphs)
                    if (!font.HasCharacter(ch)) throw new InvalidOperationException("Missing glyph in " + asset + ": " + ch);
                Debug.Log("FONT_VALIDATED " + asset + "; glyphs=" + glyphs.Count);
            }
        }
        finally { bundle.Unload(true); }
    }
}
