using System;
using System.Collections.Generic;
using UnityEngine;
namespace CrawlLocalization
{
    internal static class OnlineOverlay
    {
        private sealed class StyleEntry { internal GUIStyle Original, Display; internal Font Font; }
        private static readonly List<StyleEntry> styles = new List<StyleEntry>();
        internal static void Clear() { styles.Clear(); }
        private static string Prepare(string source, ref GUIStyle style)
        {
            Plugin plugin = Plugin.Instance;
            OnlineRecipe recipe = OnlineText.Find(source);
            if (plugin == null) return source;
            string translated = recipe == null || plugin.Language.Id == "en" ? source : recipe.Render(plugin.Language);
            if (translated == source && !plugin.Fonts.HasCombinedFont) return source;
            Font font;
            int size = style.fontSize > 0 ? style.fontSize : 12;
            if (!plugin.Fonts.TryChooseGui(plugin.Language, style.font ?? GUI.skin.font, size, style.fontStyle, translated, out font)) return source;
            if (translated == source && font == (style.font ?? GUI.skin.font)) return translated;
            foreach (StyleEntry entry in styles)
                if (ReferenceEquals(entry.Original,style) && entry.Font == font) { style = entry.Display; return translated; }
            GUIStyle copy = new GUIStyle(style); copy.font = font; copy.fontSize = size;
            styles.Add(new StyleEntry { Original = style, Display = copy, Font = font });style = copy;
            return translated;
        }
        internal static void Label(string text, params GUILayoutOption[] options)
        { GUIStyle style=GUI.skin.label;text=Prepare(text,ref style);GUILayout.Label(text,style,options); }
        internal static void Label(string text, GUIStyle style, params GUILayoutOption[] options)
        { text=Prepare(text,ref style);GUILayout.Label(text,style,options); }
        internal static bool Button(string text, params GUILayoutOption[] options)
        { GUIStyle style=GUI.skin.button;text=Prepare(text,ref style);return GUILayout.Button(text,style,options); }
        internal static bool Toggle(bool value, string text, params GUILayoutOption[] options)
        { GUIStyle style=GUI.skin.toggle;text=Prepare(text,ref style);return GUILayout.Toggle(value,text,style,options); }
        internal static bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options)
        { text=Prepare(text,ref style);return GUILayout.Toggle(value,text,style,options); }
        internal static void GuiLabel(Rect position, string text, GUIStyle style)
        { text=Prepare(text,ref style);GUI.Label(position,text,style); }
        internal static Rect Window(int id, Rect rect, GUI.WindowFunction function, string text, params GUILayoutOption[] options)
        { GUIStyle style=GUI.skin.window;text=Prepare(text,ref style);return GUILayout.Window(id,rect,function,text,style,options); }
        internal static void SetText(TextMesh mesh, string source)
        {
            if (mesh==null) return;
            mesh.text=source;
            if (Plugin.Instance==null) return;
            LocalizedMenuLabel label=LocalizedMenuLabel.GetOrCreate(mesh.gameObject);
            label.FontOnly=OnlineText.Find(source)==null;
            label.ResetSource(source);label.Refresh();
        }
    }
}
