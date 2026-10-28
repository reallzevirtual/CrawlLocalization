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

        private static void Postfix(MenuTextMenuItem __instance)
        {
            // Newly created library buttons can carry standalone child badges.
            MenuTextMeshes.Scan(__instance.gameObject);
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
            if (Plugin.Instance == null) return;
            LocalizedMenuLabel label = __instance.GetComponent<LocalizedMenuLabel>();
            if (label == null && __instance.GetComponent<MenuTextMenuItem>() != null)
                label = LocalizedMenuLabel.GetOrCreate(__instance.gameObject);
            if (label == null) label = MenuTextMeshes.Bind(__instance.gameObject, false);
            if (label != null) text = label.Translate(text);
        }

        private static void Postfix(global::GUIText __instance)
        {
            LocalizedMenuLabel label = __instance.GetComponent<LocalizedMenuLabel>();
            if (label != null) label.CaptureRendered();
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
        internal static Func<string, Func<LanguagePack, string>> ExternalProvider;
        private Func<LanguagePack, string> boundExternalProvider;
        internal bool Skip, Standalone;
        internal Func<LanguagePack, string> TextProvider;
        internal float BaseScale, ScaleFactor = 1f;
        private TextMesh mesh;
        private Renderer textRenderer;
        private Font originalFont;
        private Material originalMaterial, localMaterial;
        private int originalSize;
        private FontStyle originalStyle;
        private string source, display, rendered;
        private string directLookup, directFallback;
        private bool usesDirectLookup;

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

        private void BindExternal(string text)
        {
            if (ExternalProvider == null) return;
            Func<LanguagePack, string> provider = ExternalProvider(text);
            if (provider != null) { TextProvider = provider; boundExternalProvider = provider; }
            else if (boundExternalProvider != null && text != source && text != display)
            {
                // A different adapter may already have installed a fresh provider.
                if (ReferenceEquals(TextProvider, boundExternalProvider)) TextProvider = null;
                boundExternalProvider = null;
            }
        }
        internal void ResetSource(string text) { BindExternal(text); source = text; display = null; }

        // Called only after a known UI method writes a fresh original string.
        // Reuse the latest rendered result (including F8 changes) on identical frames.
        internal void RefreshDirect(string lookup, string fallback)
        {
            string text = mesh.text;
            if (usesDirectLookup && source == text && directLookup == lookup && directFallback == fallback && rendered != null)
            {
                mesh.text = rendered;
                return;
            }
            directLookup = lookup;
            directFallback = fallback;
            if (!usesDirectLookup) { TextProvider = TranslateDirect; usesDirectLookup = true; }
            ResetSource(text);
            Refresh();
        }

        private string TranslateDirect(LanguagePack pack)
        {
            string translated;
            if (pack.TryTranslate(directLookup, out translated)) return translated;
            return directFallback != null && pack.TryTranslate(directFallback, out translated) ? translated : null;
        }


        internal string Translate(string text)
        {
            BindExternal(text);
            // GUIText.OnEnable can replay its previously translated pending text.
            if (display == null || text != display) source = text;
            try
            {
                string translated = null;
                FontChoice choice;
                Plugin plugin = Plugin.Instance;
                int size = originalSize > 0 ? originalSize : (originalFont != null && originalFont.fontSize > 0 ? originalFont.fontSize : 16);
                bool found = false;
                if (!Skip)
                {
                    translated = TextProvider == null ? null : TextProvider(plugin.Language);
                    found = translated != null || plugin.Language.TryTranslate(source, out translated);
                }
                if (found && plugin.Fonts.TryChoose(plugin.Language, originalFont, size, originalStyle, translated, out choice))
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
            else mesh.text = Translate(source);
            // Also update inactive labels immediately; GUIText will replay its pending text later.
            if (!gameObject.activeInHierarchy) mesh.text = display ?? source;
            CaptureRendered();
        }

        internal void CaptureRendered() { if (mesh != null) rendered = mesh.text; }

        private void LateUpdate() { RefreshChangedText(); }

        internal void RefreshChangedText()
        {
            // Only watch audited standalone labels, never scan the scene per frame.
            // Capture GUIText's final wrapped/image-expanded output to avoid translating it again.
            if (!Standalone || mesh == null || mesh.text == rendered) return;
            mesh.text = Translate(mesh.text);
            CaptureRendered();
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
