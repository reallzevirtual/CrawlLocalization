using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using CrawlLocalization;

// Uses the shipping wrapper and bundle in Unity 5.4 without loading Crawl.
public static class ValidateVaultLayout
{
    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/vault-layout");
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(root, "Fonts/crawl-fonts.bundle"));
        if (bundle == null) throw new Exception("Font bundle not loaded");
        GameObject obj = new GameObject("VaultTextValidation");
        try
        {
            Font font = bundle.LoadAsset<Font>("assets/fonts/crawltextbodythin.ttf");
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.fontSize = 160;
            mesh.characterSize = 1;
            mesh.lineSpacing = 0.5f;
            mesh.anchor = TextAnchor.MiddleLeft;
            mesh.GetComponent<Renderer>().sharedMaterial = font.material;
            int count = 0, maxLines = 0;
            float maxHeight = 0;
            string longest = "";
            foreach (string row in File.ReadAllLines(Path.Combine(root, "Languages/zh-Hans/vault-translations.tsv"), Encoding.UTF8))
            {
                if (row.Length == 0 || row[0] == '#') continue;
                string[] cells = row.Split('\t');
                string context = cells[2];
                if (!context.Contains("介绍") && !context.Contains("达成条件") && !context.Contains("统计与怪物挑战记录")) continue;
                string text = Regex.Replace(cells[1], @"\\([nrt\\])", delegate(Match m) {
                    if (m.Groups[1].Value == "n") return "\n";
                    if (m.Groups[1].Value == "r") return "\r";
                    if (m.Groups[1].Value == "t") return "\t";
                    return "\\";
                });
                text = text.Replace("{0}", "12").Replace("{1}", "66").Replace("{2}", "123");
                float width = context.Contains("达成条件") ? 100 : 82;
                if (context.Contains("统计与怪物挑战记录")) width = context.Contains("m_stringHeroChallenge") ? 80 : 105;
                font.RequestCharactersInTexture(text, 160, FontStyle.Normal);
                Dictionary<char, float> advances = new Dictionary<char, float>();
                foreach (char ch in text)
                {
                    if (ch == '\r' || ch == '\n') continue;
                    CharacterInfo glyph;
                    if (!font.GetCharacterInfo(ch, out glyph, 160, FontStyle.Normal)) throw new Exception("Missing glyph: " + ch);
                    advances[ch] = glyph.advance * 0.1f;
                }
                string wrapped = ChineseTextWrapper.Wrap(text, width, delegate(char ch) { return advances[ch]; });
                if (Regex.Replace(wrapped, @"\s", "") != Regex.Replace(text, @"\s", "")) throw new Exception("Text lost during wrapping: " + context);
                foreach (string line in wrapped.Split('\n'))
                {
                    if (line.Length == 0) continue;
                    mesh.text = line;
                    if (mesh.GetComponent<Renderer>().bounds.size.x > width + 0.01f)
                        throw new Exception("Rendered width exceeds " + width + ": " + line + " (" + mesh.GetComponent<Renderer>().bounds.size.x + ")");
                }
                mesh.text = wrapped;
                int lines = wrapped.Split('\n').Length;
                float height = mesh.GetComponent<Renderer>().bounds.size.y;
                if (height > maxHeight) { maxHeight = height; longest = context; }
                maxLines = Math.Max(maxLines, lines);
                count++;
            }
            string report = "PASS: " + count + " Vault descriptions/templates; Unity 5.4 TextMesh, bundled CrawlTextBodyThin, fontSize=160, characterSize=1, widths=80/82/100/105.\nAll rendered lines fit; non-whitespace text preserved.\nMaximum lines=" + maxLines + "; maximum height=" + maxHeight + "; context=" + longest + "\nIn-game panel height and interaction still require user verification.";
            File.WriteAllText(Path.Combine(output, "geometry.txt"), report);
            Debug.Log("CRAWL_VAULT_LAYOUT_OK\n" + report);
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); bundle.Unload(true); }
    }
}
