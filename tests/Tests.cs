using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CrawlLocalization
{
    public static class Tests
    {
        private static int checks;
        private static void Check(bool ok, string description)
        {
            checks++;
            if (!ok) throw new Exception("FAILED: " + description);
        }
        private static void Translation(LanguagePack pack, string source, string expected)
        {
            string result;
            Check(pack.TryTranslate(source, out result) && result == expected, pack.Id + ": " + source);
        }
        public static void Run(string root)
        {
            checks = 0;
            List<string> warnings = new List<string>();
            LanguagePack simplified = LanguagePack.Load(root, "zh-CN", warnings.Add);
            LanguagePack migrated = LanguagePack.Load(root, "zh-TW", warnings.Add);
            Check(warnings.Count == 0, "Shipped packs load without warnings: " + string.Join("; ", warnings.ToArray()));
            foreach (string row in File.ReadAllLines(Path.Combine(root, "Languages/zh-Hans/translations.tsv")))
            {
                if (row.Length == 0 || row.StartsWith("#")) continue;
                string[] columns = row.Split('\t');
                if (columns.Length != 3 || string.IsNullOrEmpty(columns[2])) throw new Exception("Shipped translation lacks its context comment: " + row);
            }
            Check(true, "Every shipped translation has a context comment");
            Check(simplified.Id == "zh-Hans" && migrated.Id == "zh-Hans", "Language aliases");
            Translation(simplified, "START GAME", "开始游戏");
            Translation(migrated, "START GAME", "开始游戏");
            Translation(simplified, "Back", "返回");
            Translation(simplified, "< SOUND: 75 >", "< 音效：75 >");
            Translation(simplified, "< 背景音乐: 45 >", "< 音乐：45 >");
            Translation(simplified, "Run In Background: OFF", "后台运行：关");
            Translation(simplified, "Bots: 3", "电脑玩家：3");
            Translation(simplified, "Note: Many keyboards do not\r\nsupport multiple players", "注意：许多键盘不支持\n多名玩家同时操作");
            Translation(simplified, "Note: Many keyboards do not\nsupport multiple players", "注意：许多键盘不支持\n多名玩家同时操作");
            Translation(simplified, "Hold\nbutton B\n(special\n/back)", "按住\nB 键\n（特殊技／\n返回）");
            Translation(simplified, MenuTextPolicy.ReplayTutorialText(false, false), "重玩教程：关");
            Translation(simplified, MenuTextPolicy.ReplayTutorialText(true, true), "重玩教程：开");
            Translation(simplified, MenuTextPolicy.ReplayTutorialText(true, false), "重玩教程：开（当前模式禁用）");
            Check(MenuTextPolicy.MatchesPath(simplified.TextMeshPaths, "MenuMain/MenuOptionsKeyboard(Clone)/TextInfo"), "Instantiated keyboard note is included");
            Check(MenuTextPolicy.MatchesPath(simplified.TextMeshPaths, "MenuJoin/MenuJoinPlayer4/MenuJoinOptions/ContainerBind/TextHold"), "Fourth player binding prompt is included");
            Check(!MenuTextPolicy.MatchesPath(simplified.TextMeshPaths, "MenuJoin/MenuJoinPlayer4/PlayerPlatformName"), "Player name is not a localization target");
            Check(!MenuTextPolicy.MatchesPath(simplified.TextMeshPaths, "NotMenuOptionsKeyboard/TextInfo"), "Path suffix must start at a hierarchy boundary");
            Check(!MenuTextPolicy.MatchesPath(simplified.TextMeshPaths, "MenuLibrary/MockText/MockTextItems"), "Editor mock content is not a localization target");
            string value;
            Check(!simplified.TryTranslate("Bots: Alice", out value), "Numeric templates do not match player names");
            Check(!simplified.TryTranslate("Bots: -1", out value), "Numeric templates reject unexpected formats");
            Check(!simplified.TryTranslate("Unknown mod label", out value), "Missing translations leave source alone");
            Check(!simplified.TryTranslate(null, out value), "Null source");
            foreach (LanguagePack pack in new LanguagePack[] { simplified })
            {
                Check(pack.Fonts.Count == 10, "All ten font mappings: " + pack.Id);
                foreach (FontRule rule in pack.Fonts.Values) Check(File.Exists(rule.File), "Packaged font: " + rule.File);
                Check(pack.FallbackFonts.Length > 0, "System font fallback configured: " + pack.Id);
            }
            LanguagePack english = LanguagePack.Load(root, "en", warnings.Add);
            Check(english.Count == 0 && !english.TryTranslate("START GAME", out value), "English mode restores original text");
            Check(LanguagePack.NormalizeId("../../outside") == "en", "Reject unsafe locale paths");
            bool rejected = false;
            try { LanguagePack.ResolveFile(root, "../outside.ttf"); } catch (FormatException) { rejected = true; }
            Check(rejected, "Reject font path escape");
            rejected = false;
            try { LanguagePack.ResolveFile(root, Path.GetFullPath(Path.Combine(root, "font.ttf"))); } catch (FormatException) { rejected = true; }
            Check(rejected, "Reject absolute font paths");
            LanguagePack missing = LanguagePack.Load(root, "missing-pack", warnings.Add);
            Check(missing.Count == 0 && missing.Fonts.Count == 0, "Missing language pack remains usable with original text");

            string temp = Path.Combine(Path.GetTempPath(), "CrawlLocalization-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                string dir = Path.Combine(Path.Combine(temp, "Languages"), "test");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "translations.tsv"), "# comment\nHello\t你好\t位置注释含任意 \\q 转义不参与解析\nhello\t不应覆盖\nBad row\nCounter: {0}\t计数\nLine\\nTwo\t两行\\n文本\nLiteral\\\\n\t字面\\\\n\nBroken\\q\t错误\nAfter\t后续\n", Encoding.UTF8);
                File.WriteAllText(Path.Combine(dir, "fonts.tsv"), "Bad\tttf\t../outside.ttf\tBad\t1\nInvalid\tttf\tFonts/test.ttf\tName\tNaN\nValid\tsystem\t-\tFamily\t1.25\n", Encoding.UTF8);
                File.WriteAllText(Path.Combine(dir, "language.ini"), "Name=Test\nFallbackFonts=First;Second\n", Encoding.UTF8);
                warnings.Clear();
                LanguagePack pack = LanguagePack.Load(temp, "test", warnings.Add);
                Translation(pack, "HELLO", "你好");
                Translation(pack, "After", "后续");
                Translation(pack, "Line\nTwo", "两行\n文本");
                Translation(pack, "Line\r\nTwo", "两行\n文本");
                Translation(pack, "Line\rTwo", "两行\n文本");
                Translation(pack, "Literal\\n", "字面\\n");
                Check(!pack.TryTranslate("Counter: 12", out value), "Reject templates that discard their numeric value");
                Check(pack.Fonts.Count == 1 && pack.Fonts["Valid"].Scale == 1.25f, "Skip invalid font rows and retain valid settings");
                Check(warnings.Count == 6, "Diagnostics for duplicate, malformed, invalid escape, invalid template and font rows");
                Check(warnings[0].Contains("translations.tsv:3"), "Diagnostics include file and line number");
            }
            finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
            CheckFontFallbacks();
            CheckBundleFallbacks();
            Console.WriteLine("PASS: " + checks + " offline language/configuration/font-selection checks.");
        }

        private static void CheckFontFallbacks()
        {
            UnityEngine.Font.Installed.Clear();
            UnityEngine.Font mapped = new UnityEngine.Font("Mapped", "中文A");
            UnityEngine.Font partial = new UnityEngine.Font("Partial", "中A");
            UnityEngine.Font broken = new UnityEngine.Font("Broken", "中文A") { FailRequests = true };
            UnityEngine.Font fallback = new UnityEngine.Font("Fallback", "中文A");
            foreach (UnityEngine.Font f in new UnityEngine.Font[] { mapped, partial, broken, fallback }) UnityEngine.Font.Installed.Add(f.name, f);
            UnityEngine.Font original = new UnityEngine.Font("CrawlSmall", "中文A");
            LanguagePack pack = new LanguagePack { Id = "test", FallbackFonts = new string[] { "Missing", "Partial", "Broken", "Fallback" } };
            pack.Fonts.Add("CrawlSmall", new FontRule { Kind = "system", File = "", Name = "Mapped", Scale = 1.5f });
            List<string> warnings = new List<string>();
            FontChoice choice;
            using (FontService service = new FontService(warnings.Add, delegate(string message) { }))
            {
                Check(service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == mapped && choice.Scale == 1.5f, "Mapped font takes precedence and uses configured scale");
                Check(service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "<color=#ea9d1a>中文</color>", out choice) && choice.Font == mapped, "Rich text tag syntax does not require glyphs or trigger fallback");
                mapped.Characters = "中A";
                Check(service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == original, "Missing mapped glyph falls back to original");
                original.Characters = "A";
                Check(service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == fallback, "Skip unavailable, incomplete and failing system fonts in order");
                Check(!service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "缺字", out choice) && choice == null, "No complete font tells the caller to preserve source text");
                pack.Fonts["CrawlSmall"] = new FontRule { Kind = "ttf", File = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf"), Name = "Absent", Scale = 1 };
                Check(service.TryChoose(pack, original, 16, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == fallback, "Missing TTF recovers through fallback without aborting the menu");
                Check(!original.Destroyed, "The service does not own the original game font");
                Check(warnings.Count > 0, "Fallback diagnostics recorded");
            }
            Check(mapped.Destroyed && fallback.Destroyed && !original.Destroyed, "Dispose releases owned fonts only");
            UnityEngine.Font.Installed.Clear();
        }

        private static void CheckBundleFallbacks()
        {
            string path = Path.GetTempFileName();
            UnityEngine.AssetBundle bundle = new UnityEngine.AssetBundle();
            UnityEngine.Font small = new UnityEngine.Font("CrawlSmall", "中文A");
            UnityEngine.Font large = new UnityEngine.Font("CrawlLarge", "中文A");
            bundle.Assets.Add("assets/fonts/crawlsmall.ttf", small);
            bundle.Assets.Add("assets/fonts/crawllarge.ttf", large);
            UnityEngine.AssetBundle.Files.Add(path, bundle);
            UnityEngine.AssetBundle.LoadCount = 0;
            LanguagePack pack = new LanguagePack { Id = "bundle-test" };
            FontRule rule = new FontRule { Kind = "bundle", File = path, Name = "assets/fonts/crawlsmall.ttf", Scale = 1f };
            pack.Fonts.Add("CrawlSmall", rule);
            UnityEngine.Font original = new UnityEngine.Font("CrawlSmall", "中文A");
            FontChoice choice;
            List<string> messages = new List<string>();
            try
            {
                using (FontService service = new FontService(messages.Add, messages.Add))
                {
                    Check(service.TryChoose(pack, original, 160, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == small && choice.Size == 160, "Bundle asset selected by full path with original size");
                    rule.Name = "assets/fonts/crawllarge.ttf";
                    Check(service.TryChoose(pack, original, 160, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == large, "Second font resolved from the same bundle");
                    Check(UnityEngine.AssetBundle.LoadCount == 1, "One bundle load shared across its font assets");
                    rule.Name = "assets/fonts/does-not-exist.ttf";
                    Check(service.TryChoose(pack, original, 160, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == original, "Missing bundle asset falls back to original");
                    rule.File = path + ".missing";
                    Check(service.TryChoose(pack, original, 160, UnityEngine.FontStyle.Normal, "中文", out choice) && choice.Font == original, "Missing bundle file falls back to original");
                }
                Check(bundle.Destroyed && !original.Destroyed, "Bundle lifetime ends with service without destroying original font");
            }
            finally
            {
                UnityEngine.AssetBundle.Files.Clear();
                File.Delete(path);
            }
        }
    }
}
