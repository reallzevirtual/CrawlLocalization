using System;
using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    [HarmonyPatch(typeof(MenuPrompt), "Initialise")]
    internal static class PausePromptPatch
    {
        private static void Prefix(string title, string text, global::GUIText ___m_textTitle, global::GUIText ___m_textText)
        {
            if (Plugin.Instance == null) return;
            PausePlayerText snapshot = null; bool joined = false;
            // Inspect game data only for the two known pause notification families.
            if (title != null && title.StartsWith("_P", StringComparison.Ordinal) &&
                (title.EndsWith("_DROPPED_IN_", StringComparison.Ordinal) || title.EndsWith("_DROPPED_OUT_", StringComparison.Ordinal)))
            {
                joined = title.EndsWith("_DROPPED_IN_", StringComparison.Ordinal);
                for (int i=0;i<SystemPlayers.GetMaxPlayers();i++)
                {
                    PlayerData player=SystemPlayers.GetPlayer(i);
                    if (player==null) continue;
                    Deity deity=joined && player.GameData!=null ? player.GameData.GetDeity() : null;
                    if (joined && deity==null) continue;
                    PausePlayerText candidate=new PausePlayerText(i+1,player.Name,player.Level.ToString(),deity==null ? "" : deity.GetName(),deity==null ? "" : deity.GetText());
                    // Full equality against original source composition avoids parsing names or mod prompts.
                    if (candidate.Matches(title,text,joined)) { snapshot=candidate; break; }
                }
            }
            Bind(___m_textTitle,title,snapshot==null ? null : (Func<LanguagePack,string>)delegate(LanguagePack pack) { return snapshot.Title(pack,joined); });
            Bind(___m_textText,text,snapshot==null ? null : (Func<LanguagePack,string>)delegate(LanguagePack pack) { return snapshot.Body(pack,joined); });
            // Original Initialise writes text through GUIText, then sizes the backdrop.
            // Its button callbacks, player ID and pause/quit/drop logic remain untouched.
        }
        private static void Bind(global::GUIText gui,string source,Func<LanguagePack,string> provider)
        {
            if (gui==null) return;
            LocalizedMenuLabel label=LocalizedMenuLabel.GetOrCreate(gui.gameObject);
            if (label==null) return;
            label.TextProvider=provider; label.ResetSource(source);
        }
    }

    [HarmonyPatch(typeof(MenuEvolvePlayer), "Initialise")]
    internal static class EvolutionBindPatch
    {
        private static void Postfix(MenuEvolvePlayer __instance)
        { if (Plugin.Instance!=null) MenuTextMeshes.Scan(__instance.gameObject); }
    }

    [HarmonyPatch(typeof(MenuEvolvePlayer), "UpdateMenuDescriptionText")]
    internal static class EvolutionDescriptionPatch
    {
        private static void Postfix(TextMesh ___m_textMenuDescription)
        {
            if (Plugin.Instance==null || ___m_textMenuDescription==null) return;
            // This method may leave the existing display alone when no button is selected.
            // Standalone labels watch actual changes without capturing translated text as a new source.
            LocalizedMenuLabel label=MenuTextMeshes.Bind(___m_textMenuDescription.gameObject,true);
            if (label!=null) label.RefreshChangedText();
        }
    }

    [HarmonyPatch(typeof(MenuWin), "Start")]
    internal static class ResultsTextPatch
    {
        private static void Postfix(MenuWin __instance, bool ___m_heroWon, TextMesh ___m_winText, TextMesh ___m_bossKillerText)
        {
            if (Plugin.Instance==null) return;
            // Capture the selected title before the path scan could replace it. The game
            // splits its pool on LF only; lookup trims CR but English restore keeps it.
            if (___m_heroWon && ___m_bossKillerText!=null)
                LocalizedMenuLabel.GetOrCreate(___m_bossKillerText.gameObject).RefreshDirect(___m_bossKillerText.text.Trim(),null);
            MenuTextMeshes.Scan(__instance.gameObject);
            if (___m_winText!=null)
            {
                LocalizedMenuLabel label=MenuTextMeshes.Bind(___m_winText.gameObject,true);
                if (label!=null) label.RefreshChangedText();
            }
        }
    }

    [HarmonyPatch(typeof(MenuTextMenuItem), "Start")]
    internal static class BossEntranceTextPatch
    {
        private static void Postfix(MenuTextMenuItem __instance)
        {
            if (Plugin.Instance==null) return;
            string name=__instance.gameObject.name.Replace("(Clone)","");
            if (name!="EffectBossName" && name!="EffectBossAttemptText") return;
            TextMesh mesh=__instance.GetComponent<TextMesh>();
            if (mesh==null) return;
            LocalizedMenuLabel label=LocalizedMenuLabel.GetOrCreate(__instance.gameObject);
            label.Standalone=true; label.RefreshChangedText();
            // BossEncounter wrote the name after spawning, before Start. Subsequent pooled
            // writes are watched; Boss.GetDisplayName itself stays English for achievements.
        }
    }
}
