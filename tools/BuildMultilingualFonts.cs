using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Compiled by Unity 5.4's C# compiler, independently of the BepInEx plugin.
public static class BuildMultilingualFonts
{
    private const string BundleName = "crawl-multilingual.bundle";
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
            string root = Environment.GetEnvironmentVariable("CRAWL_FONT_ROOT");
            string output = Path.Combine(root, "artifacts/universal-fonts");
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
                importer.fontNames = new string[] { "Crawl Multi " + name };
                importer.fontReferences = new Font[0];
                importer.SaveAndReimport();
                TrueTypeFontImporter original = AssetImporter.GetAtPath("Assets/Original/" + name + ".ttf") as TrueTypeFontImporter;
                original.fontTextureCase = FontTextureCase.Dynamic;
                original.includeFontData = true;
                original.fontSize = importer.fontSize;
                original.fontRenderingMode = importer.fontRenderingMode;
                original.fontReferences = new Font[0];
                original.SaveAndReimport();
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
        string root = Environment.GetEnvironmentVariable("CRAWL_FONT_ROOT");
        string path = Path.Combine(root, "artifacts/universal-fonts/" + BundleName);
        string[] assets = new string[Names.Length];
        for (int i = 0; i < Names.Length; i++) assets[i] = "Assets/Fonts/" + Names[i] + ".ttf";
        Validate(path, root, assets, true);
        Debug.Log("CRAWL_FONT_BUNDLE_OK " + path);
    }

    private static void Validate(string path, string root, string[] assets, bool checkLayout)
    {
        Dictionary<string, float[]> layout = ReadLayout(root);
        HashSet<char> glyphs = new HashSet<char>();
        foreach (char ch in File.ReadAllText(Path.Combine(root, "Fonts/Multilingual/coverage.txt"), Encoding.UTF8))
            if (!char.IsWhiteSpace(ch)) glyphs.Add(ch);
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
                if (checkLayout) ValidateGeometry(root, font);
                Debug.Log("FONT_VALIDATED " + asset + "; glyphs=" + glyphs.Count);
            }
            if (checkLayout) RenderSamples(root, bundle);
        }
        finally { bundle.Unload(true); }
    }

    private static void ValidateGeometry(string root, Font font)
    {
        Font original = AssetDatabase.LoadAssetAtPath<Font>("Assets/Original/" + font.name + ".ttf");
        if (original == null) throw new Exception("Missing original " + font.name);
        foreach (int size in new int[] { 16, 64, 160 })
        {
            string ascii = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?+-";
            original.RequestCharactersInTexture(ascii, size, FontStyle.Normal);
            font.RequestCharactersInTexture(ascii, size, FontStyle.Normal);
            foreach (char ch in ascii)
            {
                CharacterInfo a, b;
                if (!original.GetCharacterInfo(ch, out a, size, FontStyle.Normal)) continue;
                if (!font.GetCharacterInfo(ch, out b, size, FontStyle.Normal) ||
                    a.advance != b.advance || a.minX != b.minX || a.maxX != b.maxX ||
                    a.minY != b.minY || a.maxY != b.maxY)
                    throw new Exception("Original glyph geometry changed: " + font.name + "/" + ch + "/" + size);
            }
            string text = "НАЧАТЬ ИГРУ ОПЦИИ ЁЙёй ÁÉÍÓÚÜÑáéíóúüñ";
            font.RequestCharactersInTexture(text, size, FontStyle.Normal);
            foreach (char ch in text)
            {
                CharacterInfo info;
                if (ch != ' ' && (!font.GetCharacterInfo(ch, out info, size, FontStyle.Normal) || info.glyphWidth <= 0 || info.glyphHeight <= 0))
                    throw new Exception("Empty rasterized glyph: " + font.name + "/" + ch + "/" + size);
            }
        }
    }

    private static void RenderSamples(string root, AssetBundle bundle)
    {
        GameObject cameraObject = new GameObject("Font sample camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 50;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f,.08f,.1f,1);
        camera.transform.position = new Vector3(65,0,-10);
        RenderTexture target = new RenderTexture(1800,1200,24);
        camera.targetTexture = target;
        List<GameObject> objects = new List<GameObject>();
        Font labelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Original/CrawlLarge.ttf");
        for (int i=0;i<Names.Length;i++)
        {
            Font font = bundle.LoadAsset<Font>("assets/fonts/"+Names[i].ToLowerInvariant()+".ttf");
            GameObject go = new GameObject(Names[i]); objects.Add(go);
            TextMesh mesh = go.AddComponent<TextMesh>(); mesh.font=font;
            mesh.fontSize=64;mesh.characterSize=1;mesh.anchor=TextAnchor.MiddleLeft;
            mesh.text="ÁÉÍÓÚÜÑ / НАЧАТЬ ЁЙ / áñ / абв ёй";
            go.GetComponent<MeshRenderer>().sharedMaterial=font.material;
            go.transform.position=new Vector3(0,43-i*9,0);
            GameObject label = new GameObject("Label");objects.Add(label);
            TextMesh lm=label.AddComponent<TextMesh>();lm.font=labelFont;lm.fontSize=32;lm.characterSize=.6f;
            lm.text=Names[i];label.GetComponent<MeshRenderer>().sharedMaterial=labelFont.material;
            label.transform.position=new Vector3(0,48-i*9,0);
        }
        camera.Render(); RenderTexture previous=RenderTexture.active;RenderTexture.active=target;
        Texture2D image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(root,"artifacts/universal-fonts/unity-samples.png"),image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        foreach(GameObject go in objects) UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
