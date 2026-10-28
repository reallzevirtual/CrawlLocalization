// Test-only stand-ins for Crawl/Harmony. Actual localization components, fonts and
// Unity TextMeshes are used by ValidateChapterBinding; no game DLL is executed.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name) { return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { }
        public HarmonyPatch(Type type, string method, Type[] args) { }
    }
}
public class LevelTitle : MonoBehaviour { }
public class MenuTextMenuItem : MonoBehaviour { }
public class MenuItemTextButton : MonoBehaviour { }
public class MenuTextMenuItemData { public string m_text, m_message; }
public class MenuFlashEffect : MonoBehaviour { }
public class MenuStateMachine : MonoBehaviour { }
public class MenuOptions : MonoBehaviour { }
public class MenuTextMenu { public void SetItemString(int index, string text) { } }
public class SystemUnlocks { public bool GetStartTutorialHACK() { return false; } }
public static class Singleton<T> where T : new() { public static T Get = new T(); }
public static class SystemTutorial { public static bool GetShouldStartTutorial() { return false; } }
public class GUIText : MonoBehaviour
{
    public void SetText(string text)
    {
        object[] args = { this, text };
        typeof(CrawlLocalization.MenuDisplayPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
        GetComponent<TextMesh>().text = (string)args[1];
        typeof(CrawlLocalization.MenuDisplayPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { this });
    }
}
namespace CrawlLocalization
{
    internal sealed class Plugin
    {
        internal static Plugin Instance;
        internal LanguagePack Language;
        internal FontService Fonts;
        internal readonly HashSet<LocalizedMenuLabel> Labels = new HashSet<LocalizedMenuLabel>();
        internal void Warn(string message) { throw new Exception(message); }
    }
}
