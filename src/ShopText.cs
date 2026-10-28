using System;
using System.Collections.Generic;

namespace CrawlLocalization
{
    internal sealed class ShopWeaponText
    {
        internal string Prefix, Name, Description, MagicTemplate, Magic, OwnerStatus, HitStatus;
        internal int Chance, Speed, Damage;
    }

    // Pure display composition from selected source parts. Never parses the final English sentence.
    internal static class ShopText
    {
        internal static string Term(LanguagePack pack, string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            string value;
            return pack.TryTranslate(source, out value) ? value : null;
        }

        internal static string Description(LanguagePack pack, string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            string translated = Term(pack, source);
            if (translated == null && !source.EndsWith(".", StringComparison.Ordinal)) translated = Term(pack, source + ".");
            return translated;
        }

        internal static string WeaponName(LanguagePack pack, ShopWeaponText data)
        {
            string name = Term(pack, data.Name);
            if (name == null) return null;
            if (string.IsNullOrEmpty(data.Prefix)) return name;
            string prefix = Term(pack, "@shop.prefix:" + data.Prefix);
            return prefix == null ? null : prefix + name;
        }

        internal static string WeaponDescription(LanguagePack pack, ShopWeaponText data)
        {
            string body = Description(pack, data.Description);
            if (body == null) return null;
            List<string> lines = new List<string>();
            if (body.Length > 0) lines.Add(body);
            if (!string.IsNullOrEmpty(data.Magic))
            {
                if (string.IsNullOrEmpty(data.MagicTemplate)) return null;
                string format = Term(pack, "@shop.magic:" + data.MagicTemplate);
                string magic = Term(pack, "@shop.magic-name:" + data.Magic);
                if (format == null || magic == null) return null;
                lines.Add(string.Format(format, "<color=#28bbcc>" + magic + "</color>"));
            }
            if (!string.IsNullOrEmpty(data.OwnerStatus))
            {
                string status = Term(pack, "@shop.status:" + data.OwnerStatus);
                if (status == null) return null;
                lines.Add("<color=#774789>" + status + "。</color>");
            }
            List<string> traits = new List<string>();
            foreach (string key in new string[] {
                data.Speed > 0 ? "fast attack speed" : null,
                data.Damage > 0 ? "high damage" : null,
                data.Speed < 0 ? "low attack speed" : null,
                data.Damage < 0 ? "poor damage" : null })
            {
                if (key == null) continue;
                string value = Term(pack, "@shop.trait:" + key);
                if (value == null) return null;
                traits.Add("<color=#a19473>" + value + "</color>");
            }
            if (traits.Count > 0)
            {
                string format = Term(pack, "@shop.traits:for a {0}: {1}");
                string name = Term(pack, data.Name);
                if (format == null || name == null) return null;
                lines.Add(string.Format(format, name, string.Join("、", traits.ToArray())));
            }
            if (!string.IsNullOrEmpty(data.HitStatus))
            {
                string status = Term(pack, "@shop.status:" + data.HitStatus);
                string format = Term(pack, "@shop.chance:{0}% chance of {1}.");
                if (status == null || format == null) return null;
                lines.Add("<color=#b78f2d>" + string.Format(format, data.Chance, status) + "</color>");
            }
            return string.Join("\n", lines.ToArray());
        }

        internal static string Comparison(LanguagePack pack, int value, string name)
        {
            string label = Term(pack, "@shop.compare:" + name);
            if (label == null) return null;
            string signs = value < -1 ? "--" : value == -1 ? "-" : value == 0 ? "=" : value == 1 ? "+" : "++";
            string text = signs + label;
            return value == 0 ? text : "<color=" + (value < 0 ? "#b6442e" : "#7fa135") + ">" + text + "</color>";
        }

        internal static string HeroStat(LanguagePack pack, int amount, string name)
        {
            string translated = Term(pack, "@shop.stat:" + name);
            return translated == null ? null : (amount > 0 ? "+" : "") + amount + " " + translated;
        }
    }
}
