using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
namespace CrawlLocalization
{
    public static class ShopTests
    {
        static int checks;
        static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
        static readonly Dictionary<string, LanguagePack> packs = new Dictionary<string, LanguagePack>();
        static LanguagePack Load(string root, string id)
        {
            string key = root + "|" + id; LanguagePack pack;
            if (!packs.TryGetValue(key, out pack)) { pack = LanguagePack.Load(root, id, delegate(string s) { throw new Exception(s); }); packs.Add(key, pack); }
            return pack;
        }
        static List<string> Keys(string root, string prefix)
        {
            List<string> result = new List<string>();
            foreach (string line in File.ReadAllLines(Path.Combine(root, "Languages/zh-Hans/shop-translations.tsv")))
                if (line.StartsWith(prefix, StringComparison.Ordinal)) result.Add(line.Split('\t')[0].Substring(prefix.Length));
            return result;
        }
        public static string Name(string root, string name, string prefix)
        {
            string result = ShopText.WeaponName(Load(root, "zh-Hans"), new ShopWeaponText { Name = name, Prefix = prefix });
            Check(result != null && ChineseTextWrapper.ContainsChinese(result), "Untranslated name: " + prefix + " " + name);
            return result;
        }
        public static string Description(string root, string source)
        {
            string result = ShopText.Description(Load(root, "zh-Hans"), source);
            Check(result != null && (result.Length == 0 || ChineseTextWrapper.ContainsChinese(result)), "Untranslated description: " + source);
            return result;
        }
        public static string StressBody(string root, string name, string description)
        {
            LanguagePack pack = Load(root, "zh-Hans");
            string result = ShopText.WeaponDescription(pack, new ShopWeaponText {
                Name = name, Description = description, Magic = "the purifying fire",
                MagicTemplate = "The glyph of {0} has been hastily scratched onto it's surface.",
                OwnerStatus = "Corrupted: 1.5 X Damage, Wielder is Driven Insane",
                HitStatus = "Stealing Gold", Chance = 100, Speed = -1, Damage = 1
            });
            Check(result != null, "Untranslated weapon composition: " + name);
            return result + "\n\n换购 受诅咒的双手剑 " + ShopText.Comparison(pack, -2, "VALUE") + " " + ShopText.Comparison(pack, 2, "DAMAGE") + " " + ShopText.Comparison(pack, 0, "SPEED") + " " + ShopText.Comparison(pack, 1, "RANGE");
        }
        public static string[] Run(string root)
        {
            checks = 0;
            LanguagePack pack = Load(root, "zh-Hans");
            LanguagePack english = Load(root, "en");
            List<string> bodies = new List<string>();
            Check(Name(root, "Shortsword", "Swift") == "迅捷短剑", "Prefix/base composition");
            Check(Name(root, "Shortsword", null) == "短剑", "No prefix");
            Check(ShopText.WeaponName(pack, new ShopWeaponText { Name = "Shortsword", Prefix = "unknown mod prefix" }) == null, "Unknown prefix must fall back");
            Check(ShopText.WeaponName(english, new ShopWeaponText { Name = "Shortsword" }) == null, "English must use original name");
            foreach (string prefix in Keys(root, "@shop.prefix:")) Name(root, "Shortsword", prefix);
            foreach (string format in Keys(root, "@shop.magic:"))
            foreach (string magic in Keys(root, "@shop.magic-name:"))
            {
                ShopWeaponText parts = new ShopWeaponText { Name = "Shortsword", Description = "", MagicTemplate = format, Magic = magic };
                string body = ShopText.WeaponDescription(pack, parts);
                Check(body != null && body.Contains("<color=#28bbcc>") && !body.Contains("{0}"), "Magic substitution: " + format + magic);
                bodies.Add(body);
                Check(ShopText.WeaponDescription(english, parts) == null, "English magic fallback");
            }
            foreach (string status in Keys(root, "@shop.status:"))
            foreach (int chance in new int[] { 0, 25, 100 })
            {
                string body = ShopText.WeaponDescription(pack, new ShopWeaponText { Description = "", Name = "Shortsword", OwnerStatus = status, HitStatus = status, Chance = chance });
                Check(body != null && body.Contains(chance + "％") && body.Contains("<color=#774789>") && body.Contains("<color=#b78f2d>"), "Status probability/colors: " + status);
                bodies.Add(body);
            }
            for (int speed = -1; speed <= 1; speed++)
            for (int damage = -1; damage <= 1; damage++)
            {
                string body = ShopText.WeaponDescription(pack, new ShopWeaponText { Description = "", Name = "Shortsword", Speed = speed, Damage = damage });
                Check(body != null && body.Contains("较快攻速") == (speed > 0) && body.Contains("较慢攻速") == (speed < 0) && body.Contains("较高伤害") == (damage > 0) && body.Contains("较低伤害") == (damage < 0), "Trait combinations");
                if (body.Length > 0) bodies.Add(body);
            }
            foreach (string key in new string[] { "VALUE", "DAMAGE", "SPEED", "RANGE" })
            for (int value = -3; value <= 3; value++)
            {
                string body = ShopText.Comparison(pack, value, key);
                string visible = Regex.Replace(body, "<[^>]+>", "");
                string signs = value < -1 ? "--" : value == -1 ? "-" : value == 0 ? "=" : value == 1 ? "+" : "++";
                Check(visible.StartsWith(signs, StringComparison.Ordinal) && ChineseTextWrapper.ContainsChinese(visible), "Comparison signs");
                Check(value == 0 ? !body.Contains("<color") : body.Contains(value < 0 ? "#b6442e" : "#7fa135"), "Comparison colors");
            }
            foreach (string key in new string[] { "Vitality", "Strength", "Agility", "Wisdom", "Luck" })
            foreach (int value in new int[] { -20, -1, 0, 1, 20 })
                Check(ShopText.HeroStat(pack, value, key).StartsWith((value > 0 ? "+" : "") + value + " ", StringComparison.Ordinal), "Stat amount preserved");
            Check(ShopText.WeaponDescription(pack, new ShopWeaponText { Description = "", Magic = "the rat" }) == null, "Unobserved magic choice falls back");
            Check(ShopText.WeaponDescription(pack, new ShopWeaponText { Description = "", OwnerStatus = "mod status" }) == null, "Unknown status falls back");
            Check(ShopText.Description(pack, "untranslated mod description") == null, "Unknown item falls back");
            string badRoot = Path.Combine(Path.GetTempPath(), "crawl-shop-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string directory = Path.Combine(badRoot, "Languages/zh-Hans");
                Directory.CreateDirectory(directory);
                foreach (string file in new string[] { "translations.tsv", "fonts.tsv", "language.ini" }) File.WriteAllText(Path.Combine(directory, file), "");
                File.WriteAllText(Path.Combine(directory, "shop-translations.tsv"), "@shop.magic:with {0}\t丢失参数\tinvalid\n");
                List<string> warnings = new List<string>();
                LanguagePack bad = LanguagePack.Load(badRoot, "zh-Hans", warnings.Add);
                Check(warnings.Count == 1 && ShopText.Term(bad, "@shop.magic:with {0}") == null, "Bad format rejected at load");
            }
            finally { Directory.Delete(badRoot, true); }
            Console.WriteLine("PASS: " + checks + " shop composition/fallback checks");
            return bodies.ToArray();
        }
    }
}
