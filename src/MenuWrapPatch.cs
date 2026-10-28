using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    [HarmonyPatch(typeof(TextWrapper), "WrapText")]
    internal static class MenuChineseWrapPatch
    {
        private static bool Prefix(string input, float width, TextMesh ___textMesh, ref string __result)
        {
            if (___textMesh == null || !ChineseTextWrapper.ContainsChinese(input) ||
                ___textMesh.GetComponent<LocalizedMenuLabel>() == null ||
                (___textMesh.GetComponentInParent<MenuLibrary>() == null &&
                 ___textMesh.GetComponentInParent<MenuDeitySelectPlayer>() == null &&
                 ___textMesh.GetComponentInParent<GUIStore>() == null &&
                 ___textMesh.GetComponentInParent<MenuPrompt>() == null)) return true;
            TextMesh mesh = ___textMesh;
            Font font = mesh.font;
            if (font == null) return true;
            int size = mesh.fontSize > 0 ? mesh.fontSize : font.fontSize;
            if (font.dynamic) font.RequestCharactersInTexture(input, size, mesh.fontStyle);
            float scale = mesh.characterSize * 0.1f * Mathf.Abs(mesh.transform.lossyScale.x);
            __result = ChineseTextWrapper.Wrap(input, width, delegate(char ch) {
                CharacterInfo glyph;
                return font.GetCharacterInfo(ch, out glyph, size, mesh.fontStyle) ? glyph.advance * scale : size * scale;
            });
            return false;
        }
    }
}
