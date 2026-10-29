using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrawlLocalization
{
    internal static class MenuTextMeshes
    {
        internal static LocalizedMenuLabel Bind(GameObject obj, bool refresh)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || obj.GetComponent<MenuTextMenuItem>() != null) return null;
            LocalizedMenuLabel existing = obj.GetComponent<LocalizedMenuLabel>();
            List<string> names = new List<string>();
            for (Transform t = obj.transform; t != null; t = t.parent) names.Insert(0, t.name);
            bool translate = MenuTextPolicy.MatchesPath(plugin.Language.TextMeshPaths, string.Join("/", names.ToArray()));
            if (existing != null && (!existing.FontOnly || !translate)) return existing;
            TextMesh mesh = obj.GetComponent<TextMesh>();
            if (mesh == null) return null;
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(obj, !translate);
            label.Standalone = true;
            label.ResetSource(mesh.text);
            if (refresh) label.Refresh();
            return label;
        }

        internal static void Scan(GameObject root)
        {
            foreach (TextMesh mesh in root.GetComponentsInChildren<TextMesh>(true)) Bind(mesh.gameObject, true);
        }

        internal static void ScanLoadedScenes()
        {
            // Scene roots include inactive children and exclude prefab assets.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) foreach (GameObject root in scene.GetRootGameObjects()) Scan(root);
            }
        }
    }

    [HarmonyPatch(typeof(MenuStateMachine), "SetState", new System.Type[] { typeof(int) })]
    internal static class MenuStateTextPatch
    {
        private static void Postfix(MenuStateMachine __instance) { MenuTextMeshes.Scan(__instance.gameObject); }
    }

    [HarmonyPatch(typeof(global::GUIText), "OnEnable")]
    internal static class MenuStaticTextEnablePatch
    {
        private static void Postfix(global::GUIText __instance) { MenuTextMeshes.Bind(__instance.gameObject, true); }
    }

    [HarmonyPatch(typeof(MenuOptions), "SetItemTextReplayTutorial")]
    internal static class ReplayTutorialDisplayPatch
    {
        private static bool Prefix(MenuTextMenu ___m_menuOptions)
        {
            if (___m_menuOptions == null) return true;
            bool requested = Singleton<SystemUnlocks>.Get.GetStartTutorialHACK();
            ___m_menuOptions.SetItemString(4, MenuTextPolicy.ReplayTutorialText(requested, SystemTutorial.GetShouldStartTutorial()));
            return false;
        }
    }
}
