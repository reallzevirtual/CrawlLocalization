using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    [HarmonyPatch(typeof(GUIActionIndicator), "SetText")]
    internal static class GameplayActionPatch
    {
        private static void Postfix(string text, TextMesh ___m_textMesh)
        {
            if (___m_textMesh == null || Plugin.Instance == null) return;
            LocalizedMenuLabel label = ___m_textMesh.GetComponent<LocalizedMenuLabel>();
            if (label == null)
            {
                string translated;
                if (!Plugin.Instance.Language.TryTranslate(text, out translated)) return;
                label = LocalizedMenuLabel.GetOrCreate(___m_textMesh.gameObject);
            }
            // This component displays action prompts, never player names. Unknown/blank
            // actions must update the saved original too, so reload cannot replay an old prompt.
            label.Skip = false;
            label.RefreshDirect(text, null);
        }
    }

    [HarmonyPatch]
    internal static class GameplayStaticTextPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new Type[] { typeof(PropPortal), typeof(PropSign), typeof(PropStoreTeleporter), typeof(PropActionable), typeof(GUIGame) })
                yield return AccessTools.Method(type, "Start");
        }
        private static void Postfix(MonoBehaviour __instance)
        {
            if (Plugin.Instance != null) MenuTextMeshes.Scan(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(GUIPlayer), "Start")]
    internal static class GameplayRewardPatch
    {
        private static void Postfix(GUIPlayer __instance, MenuFlashEffect ___m_favourTallyContainer)
        {
            if (Plugin.Instance == null) return;
            if (___m_favourTallyContainer != null)
            {
                foreach (TextMesh mesh in ___m_favourTallyContainer.GetComponentsInChildren<TextMesh>(true))
                {
                    // Plus sign, amount and gold unit are separate labels. Keep the game's
                    // number and symbol, and use a short unit only in this reward display.
                    string key = null;
                    if (string.Equals(mesh.text, "gold", StringComparison.OrdinalIgnoreCase)) key = "@gameplay.reward-unit:gold";
                    else if (string.Equals(mesh.text, "REWARD", StringComparison.OrdinalIgnoreCase)) key = "REWARD";
                    if (key != null) LocalizedMenuLabel.GetOrCreate(mesh.gameObject).RefreshDirect(key, null);
                }
            }
            MenuTextMeshes.Scan(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(GUIGame), "ShowKillStreak")]
    internal static class GameplayStreakPatch
    {
        private static void Postfix(GameObject ___m_killStreak)
        {
            if (Plugin.Instance == null || ___m_killStreak == null) return;
            TextMesh mesh = ___m_killStreak.GetComponent<TextMesh>();
            if (mesh != null) LocalizedMenuLabel.GetOrCreate(mesh.gameObject).RefreshDirect(mesh.text, null);
        }
    }
}
