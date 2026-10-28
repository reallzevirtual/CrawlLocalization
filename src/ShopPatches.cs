using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    internal static class ShopDisplay
    {
        private static readonly FieldInfo Prefix = AccessTools.Field(typeof(WeaponData), "m_namePrefix");

        internal static ShopWeaponText Parts(WeaponData weapon)
        {
            if (weapon == null || weapon.m_weapon == null) return null;
            Combo combo = weapon.m_combo;
            float speed = combo == null ? 1 : combo.m_speedMult;
            float damage = combo == null ? 1 : combo.m_damageMult;
            float tolerance = combo == null ? 0 : 0.001f;
            // Read only. In particular, do not call GetDisplayNamePrefix: it can generate a name.
            return new ShopWeaponText {
                Prefix = (string)Prefix.GetValue(weapon),
                Name = combo == null ? weapon.m_weapon.m_weaponName : combo.m_name,
                Description = combo != null && !string.IsNullOrEmpty(combo.m_description) ? combo.m_description : weapon.m_weapon.m_weaponDescription,
                Magic = weapon.IsMagic() && weapon.m_magic != null ? weapon.m_magic.GetDisplayName() : null,
                MagicTemplate = ShopMagicChoice.Get(weapon),
                OwnerStatus = weapon.m_ownerStatus == null ? null : weapon.m_ownerStatus.m_description,
                HitStatus = weapon.m_status == null ? null : weapon.m_status.m_description,
                Chance = Mathf.RoundToInt(weapon.m_statusChance * 100f),
                Speed = weapon.m_speedMult > speed + tolerance ? 1 : weapon.m_speedMult < speed - tolerance ? -1 : 0,
                Damage = weapon.m_damageMult > damage + tolerance ? 1 : weapon.m_damageMult < damage - tolerance ? -1 : 0
            };
        }

        internal static string Name(LanguagePack pack, IPurchasable item)
        {
            WeaponData weapon = item as WeaponData;
            ShopWeaponText parts = weapon == null ? null : Parts(weapon);
            if (parts != null) return ShopText.WeaponName(pack, parts);
            Powerup powerup = item as Powerup;
            return powerup == null ? null : ShopText.Term(pack, powerup.m_displayName);
        }

        internal static string Body(LanguagePack pack, IPurchasable item, PlayerGameData player)
        {
            WeaponData weapon = item as WeaponData;
            Powerup powerup = item as Powerup;
            ShopWeaponText parts = weapon == null ? null : Parts(weapon);
            string body = parts != null ? ShopText.WeaponDescription(pack, parts) : powerup == null ? null : ShopText.Description(pack, powerup.m_description);
            if (body == null || player == null) return null;
            IPurchasable old = null;
            string verb = "Replaces";
            if (weapon != null)
            {
                old = player.GetWeaponData();
                if (Singleton<Constants>.Get.m_weaponTradeMultiplier > 0) verb = "Trade";
            }
            else if (item.GetPurchasableType() == ePurchasableType.Spell) old = player.GetPowerupSwappable();
            if (old != null)
            {
                string action = ShopText.Term(pack, verb), name = Name(pack, old);
                if (action == null || name == null) return null;
                body += "\n\n" + action + " " + name;
            }
            else if (powerup != null && item.GetPurchasableType() == ePurchasableType.Artifact && powerup.GetStackType() == Powerup.eStackType.NoStack && player.HasPowerup(powerup))
            {
                string owned = ShopText.Term(pack, "You already own this artifact.");
                if (owned == null) return null;
                body += "\n\n" + owned;
            }
            if (weapon != null)
            {
                WeaponData previous = player.GetWeaponData();
                if (previous == null) return null;
                string[] comparisons = {
                    ShopText.Comparison(pack, weapon.CompareWeaponValue(previous), "VALUE"),
                    ShopText.Comparison(pack, weapon.CompareWeaponDamage(previous), "DAMAGE"),
                    ShopText.Comparison(pack, weapon.CompareWeaponSpeed(previous), "SPEED"),
                    ShopText.Comparison(pack, weapon.CompareWeaponRange(previous), "RANGE") };
                foreach (string comparison in comparisons) if (comparison == null) return null;
                body += " " + string.Join(" ", comparisons);
            }
            return body;
        }

        internal static void Bind(TextMesh mesh, Func<LanguagePack, string> provider)
        {
            if (mesh == null || Plugin.Instance == null) return;
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(mesh.gameObject);
            label.TextProvider = provider;
        }

        internal static void Render(TextMesh mesh)
        {
            if (mesh == null || Plugin.Instance == null || !mesh.gameObject.activeInHierarchy) return;
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(mesh.gameObject);
            label.ResetSource(mesh.text); // Called after the original UI wrote the English source.
            label.Refresh();
        }
    }

    [HarmonyPatch(typeof(GUIStore), "Show")]
    internal static class ShopShowPatch
    {
        private static void Prefix(IPurchasable purchasable, TextMesh ___m_textTitle, global::GUIText ___m_textDescription)
        {
            if (Plugin.Instance == null) return;
            PlayerData player = SystemLevel.GetHeroPlayerData();
            PlayerGameData data = player == null ? null : player.GameData;
            ShopDisplay.Bind(___m_textTitle, delegate(LanguagePack pack) { return ShopDisplay.Name(pack, purchasable); });
            ShopDisplay.Bind(___m_textDescription.GetComponent<TextMesh>(), delegate(LanguagePack pack) { return ShopDisplay.Body(pack, purchasable, data); });
        }
        private static void Postfix(TextMesh ___m_textTitle, TextMesh ___m_textBuy, TextMesh ___m_textNoBuy, TextMesh ___m_textBeforeTrade)
        {
            ShopDisplay.Render(___m_textTitle);
            ShopDisplay.Render(___m_textBuy);
            ShopDisplay.Render(___m_textNoBuy);
            ShopDisplay.Render(___m_textBeforeTrade);
        }
    }

    [HarmonyPatch(typeof(GUIStoreStatsDisplay), "Show")]
    internal static class ShopStatsPatch
    {
        private static void Postfix(IPurchasable purchasable, TextMesh ___m_textUp, TextMesh ___m_textDown)
        {
            List<HeroStatModifier> modifiers = purchasable.GetHeroStatModifiers();
            if (modifiers == null) return;
            HeroStatModifier up = null, down = null;
            foreach (HeroStatModifier stat in modifiers) { if (stat.m_amount > 0) up = stat; else down = stat; }
            Apply(___m_textUp, up);
            Apply(___m_textDown, down);
        }
        private static void Apply(TextMesh mesh, HeroStatModifier stat)
        {
            if (stat == null) return;
            int amount = stat.m_amount;
            string name = Singleton<Constants>.Get.m_heroStats[(int)stat.m_type].m_name;
            ShopDisplay.Bind(mesh, delegate(LanguagePack pack) { return ShopText.HeroStat(pack, amount, name); });
            ShopDisplay.Render(mesh);
        }
    }

    [HarmonyPatch(typeof(GUIActionIndicator), "SetText")]
    internal static class ShopActionPatch
    {
        private static void Postfix(string text, TextMesh ___m_textMesh)
        {
            if (___m_textMesh == null || Plugin.Instance == null) return;
            bool shop = text == "BUY" || text == "BUY POTION" || text == "BUY ARTIFACT" || text == "BUY SPELL" || text == "BUY WEAPON" || text == "TRADE WEAPON";
            LocalizedMenuLabel label = ___m_textMesh.GetComponent<LocalizedMenuLabel>();
            if (label == null && !shop) return;
            if (label == null) label = LocalizedMenuLabel.GetOrCreate(___m_textMesh.gameObject);
            label.Skip = !shop;
            label.ResetSource(text); // Pooled indicator may now show a non-shop action, including blank text.
            label.Refresh();
        }
    }
}
