using System;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    // Keep the source on the game component; translate only GUIText's rendering input.
    [HarmonyPatch(typeof(MenuTextMenuItem), "Initialise")]
    internal static class MenuInitialisePatch
    {
        private static void Prefix(MenuTextMenuItem __instance, MenuTextMenuItemData data)
        {
            if (Plugin.Instance == null || data == null) return;
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(__instance.gameObject);
            if (label == null) return;
            label.Skip = data.m_message == "MsgCharSelect" || data.m_message == "MsgNameSelect";
            label.ResetSource(data.m_text);
        }
    }

    // This override writes TextMesh.text again after base.Initialise; restore the localized display.
    [HarmonyPatch(typeof(MenuItemTextButton), "Initialise")]
    internal static class TextButtonInitialisePatch
    {
        private static void Postfix(MenuItemTextButton __instance)
        {
            LocalizedMenuLabel label = __instance.GetComponent<LocalizedMenuLabel>();
            if (label != null) label.Refresh();
        }
    }

    [HarmonyPatch(typeof(global::GUIText), "SetText")]
    internal static class MenuDisplayPatch
    {
        private static void Prefix(global::GUIText __instance, ref string text)
        {
            if (Plugin.Instance == null || __instance.GetComponent<MenuTextMenuItem>() == null) return;
            LocalizedMenuLabel label = LocalizedMenuLabel.GetOrCreate(__instance.gameObject);
            if (label != null) text = label.Translate(text);
        }
    }

    // Text buttons calculate their flash pivot immediately after writing the raw text.
    [HarmonyPatch(typeof(MenuFlashEffect), "StartFlash")]
    internal static class MenuFlashPatch
    {
        private static void Prefix(MenuFlashEffect __instance)
        {
            LocalizedMenuLabel label = __instance.GetComponent<LocalizedMenuLabel>();
            if (label != null) label.Refresh();
        }
    }

    [HarmonyPatch(typeof(global::GUIText), "SetFontScale")]
    internal static class MenuFontScalePatch
    {
        private static void Prefix(global::GUIText __instance, ref float scale)
        {
            LocalizedMenuLabel label = __instance.GetComponent<LocalizedMenuLabel>();
            if (label == null) return;
            label.BaseScale = scale;
            scale *= label.ScaleFactor;
        }
    }

    public sealed class LocalizedMenuLabel : MonoBehaviour
    {
        internal bool Skip;
        internal float BaseScale, ScaleFactor = 1f;
        private TextMesh mesh;
        private Renderer textRenderer;
        private Font originalFont;
        private Material originalMaterial, localMaterial;
        private int originalSize;
        private FontStyle originalStyle;
        private string source, display;

        internal static LocalizedMenuLabel GetOrCreate(GameObject obj)
        {
            LocalizedMenuLabel label = obj.GetComponent<LocalizedMenuLabel>();
            if (label != null) return label;
            TextMesh mesh = obj.GetComponent<TextMesh>();
            if (mesh == null) return null;
            label = obj.AddComponent<LocalizedMenuLabel>();
            label.mesh = mesh;
            label.textRenderer = mesh.GetComponent<Renderer>();
            label.originalFont = mesh.font;
            label.originalMaterial = label.textRenderer == null ? null : label.textRenderer.sharedMaterial;
            label.originalSize = mesh.fontSize;
            label.originalStyle = mesh.fontStyle;
            label.BaseScale = mesh.characterSize;
            Font.textureRebuilt += label.OnTextureRebuilt;
            Plugin.Instance.Labels.Add(label);
            return label;
        }

        internal void ResetSource(string text) { source = text; display = null; }

        internal string Translate(string text)
        {
            // GUIText.OnEnable can replay its previously translated pending text.
            if (display == null || text != display) source = text;
            try
            {
                string translated;
                FontChoice choice;
                Plugin plugin = Plugin.Instance;
                int size = originalSize > 0 ? originalSize : (originalFont != null && originalFont.fontSize > 0 ? originalFont.fontSize : 16);
                if (!Skip && plugin.Language.TryTranslate(source, out translated) && plugin.Fonts.TryChoose(plugin.Language, originalFont, size, originalStyle, translated, out choice))
                {
                    Apply(choice);
                    display = translated;
                    return display;
                }
            }
            catch (Exception ex) { Plugin.Instance.Warn("Menu localization failed for " + source + ": " + ex.Message); }
            Restore(false);
            display = source;
            return source;
        }

        private void Apply(FontChoice choice)
        {
            ScaleFactor = choice.Scale;
            mesh.font = choice.Font;
            mesh.fontSize = choice.Size;
            mesh.fontStyle = choice.Style;
            mesh.characterSize = BaseScale * ScaleFactor;
            if (textRenderer == null) return;
            if (choice.Font == originalFont)
            {
                textRenderer.sharedMaterial = originalMaterial;
                return;
            }
            // Retain Crawl's shader, render queue and properties; change only this label's atlas.
            if (localMaterial == null) localMaterial = new Material(originalMaterial != null ? originalMaterial : choice.Font.material);
            textRenderer.sharedMaterial = localMaterial;
            UpdateAtlas();
        }

        private void OnTextureRebuilt(Font font)
        {
            if (mesh != null && font == mesh.font && font != originalFont) UpdateAtlas();
        }

        private void UpdateAtlas()
        {
            if (mesh == null || mesh.font == null || mesh.font.material == null || localMaterial == null) return;
            Texture atlas = mesh.font.material.mainTexture;
            if (atlas != null) atlas.filterMode = FilterMode.Point;
            localMaterial.mainTexture = atlas;
            // GUIText.ParseImages accesses Renderer.materials, which may instantiate material 0.
            if (textRenderer != null && textRenderer.sharedMaterial != null && textRenderer.sharedMaterial != originalMaterial)
                textRenderer.sharedMaterial.mainTexture = atlas;
        }

        internal void Refresh()
        {
            if (source == null || mesh == null) return;
            global::GUIText gui = GetComponent<global::GUIText>();
            if (gui != null) gui.SetText(source);
            // Also update inactive labels immediately; GUIText will replay its pending text later.
            if (!gameObject.activeInHierarchy) mesh.text = display ?? source;
        }

        internal void Restore(bool restoreText)
        {
            if (mesh == null) return;
            mesh.font = originalFont;
            mesh.fontSize = originalSize;
            mesh.fontStyle = originalStyle;
            mesh.characterSize = BaseScale;
            ScaleFactor = 1f;
            if (textRenderer != null) textRenderer.sharedMaterial = originalMaterial;
            if (restoreText && source != null)
            {
                global::GUIText gui = GetComponent<global::GUIText>();
                if (gui != null) gui.SetText(source);
                mesh.text = source;
            }
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= OnTextureRebuilt;
            if (Plugin.Instance != null) Plugin.Instance.Labels.Remove(this);
            if (localMaterial != null) Destroy(localMaterial);
        }
    }
}
