using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    [HarmonyPatch(typeof(LevelTitle), "Start")]
    internal static class ChapterTitlePatch
    {
        private static void Postfix(TextMesh ___m_textLevel, TextMesh ___m_textFloor)
        {
            if (Plugin.Instance == null) return;
            Bind(___m_textLevel);
            Bind(___m_textFloor);
        }

        private static void Bind(TextMesh mesh)
        {
            if (mesh == null) return;
            // Both chapter labels also have MenuTextMenuItem, but LevelTitle writes
            // them directly without menu Initialise. The generic scanner skips them.
            // Use the game's own references after Start has selected the original title.
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(mesh.gameObject);
            label.Standalone = true;
            label.ResetSource(mesh.text);
            label.Refresh();
        }
    }
}
