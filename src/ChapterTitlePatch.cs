using HarmonyLib;

namespace CrawlLocalization
{
    // The title is spawned after scene loading and writes TextMesh.text directly.
    // Bind only audited labels after the game has chosen and recorded its original name.
    [HarmonyPatch(typeof(LevelTitle), "Start")]
    internal static class ChapterTitlePatch
    {
        private static void Postfix(LevelTitle __instance)
        {
            if (Plugin.Instance != null) MenuTextMeshes.Scan(__instance.gameObject);
        }
    }
}
