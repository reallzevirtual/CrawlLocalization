using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    internal static class HeroStatsDisplay
    {
        internal static void Refresh(TextMesh mesh, string lookup, string fallback)
        {
            if (mesh == null || Plugin.Instance == null) return;
            LocalizedMenuLabel.GetOrCreate(mesh.gameObject).RefreshDirect(lookup, fallback);
        }
    }

    [HarmonyPatch(typeof(GUIHeroStats), "UpdateText")]
    internal static class HeroStatsNamesPatch
    {
        private static void Postfix(PlayerGameData ___m_player, TextMesh ___m_weaponNoSplit,
            TextMesh ___m_weaponSplit1, TextMesh ___m_weaponSplit2, TextMesh ___m_spell)
        {
            if (Plugin.Instance == null || ___m_player == null || ___m_player.GetWeaponData() == null) return;
            // The original method already chose and wrote these individual components.
            // Do not call name generators or modify the original split/single-row layout.
            if (___m_weaponNoSplit != null && ___m_weaponNoSplit.gameObject.activeSelf)
                HeroStatsDisplay.Refresh(___m_weaponNoSplit, ___m_weaponNoSplit.text, null);
            else
            {
                if (___m_weaponSplit1 != null)
                    HeroStatsDisplay.Refresh(___m_weaponSplit1, "@shop.prefix:" + ___m_weaponSplit1.text, null);
                if (___m_weaponSplit2 != null)
                    HeroStatsDisplay.Refresh(___m_weaponSplit2, ___m_weaponSplit2.text, null);
            }
            if (___m_spell != null) HeroStatsDisplay.Refresh(___m_spell, ___m_spell.text, null);
        }
    }

    [HarmonyPatch(typeof(GUIHeroStats), "UpdateArtifactIcons")]
    internal static class HeroStatsArtifactsPatch
    {
        private static void Prefix(int ___m_numArtifacts, out int __state) { __state = ___m_numArtifacts; }

        private static void Postfix(bool onlyIfCountChanged, int __state, int ___m_numArtifacts,
            PlayerGameData ___m_player, List<GUIArtifactIcon> ___m_artifactIcons)
        {
            if (Plugin.Instance == null || ___m_player == null || ___m_artifactIcons == null ||
                (onlyIfCountChanged && __state == ___m_numArtifacts)) return;
            // Mirror the game's artifact order only when it actually rewrote icon labels.
            // Hidden scroll rows must also be translated before they become visible.
            int index = 0;
            foreach (Powerup powerup in ___m_player.GetPowerups())
            {
                if (powerup.GetPurchasableType() != ePurchasableType.Artifact) continue;
                if (index >= ___m_numArtifacts || index >= ___m_artifactIcons.Count) break;
                GUIArtifactIcon icon = ___m_artifactIcons[index++];
                if (icon == null) continue;
                string name = string.IsNullOrEmpty(powerup.m_displayNameHud) ? powerup.m_displayName : powerup.m_displayNameHud;
                HeroStatsDisplay.Refresh(icon.m_text, name, powerup.m_displayName);
            }
        }
    }
}
