using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    // Attach only to the tutorial's rendering components, before the original
    // method writes GUIText and sizes/positions the message box. Never skip it.
    [HarmonyPatch(typeof(GuiTutorialMessageBox), "SetContent")]
    internal static class TutorialMessagePatch
    {
        private static void Prefix(global::GUIText ___m_textMessage)
        {
            Bind(___m_textMessage);
        }

        internal static void Bind(global::GUIText gui)
        {
            if (Plugin.Instance == null || gui == null) return;
            LocalizedMenuLabel.GetOrCreate(gui.gameObject);
        }
    }

    [HarmonyPatch(typeof(GuiTutorialMessageBox), "ShowButton")]
    internal static class TutorialButtonPatch
    {
        private static void Prefix(global::GUIText ___m_textButton)
        {
            TutorialMessagePatch.Bind(___m_textButton);
        }
    }

    [HarmonyPatch(typeof(TextWrapper), "WrapText")]
    internal static class TutorialWrapPatch
    {
        private static bool Prefix(string input, float width, TextMesh ___textMesh, ref string __result)
        {
            TextMesh mesh = ___textMesh;
            if (mesh == null || mesh.font == null || !ChineseTextWrapper.ContainsChinese(input) ||
                mesh.GetComponent<LocalizedMenuLabel>() == null ||
                mesh.GetComponentInParent<GuiTutorialMessageBox>() == null) return true;
            TutorialTextMetrics metrics = new TutorialTextMetrics(mesh);
            __result = ChineseTextWrapper.Wrap(input, width, metrics.Advance, metrics.TagWidth);
            return false;
        }
    }

    // GUIText expands [A]/[B] into quad tags before wrapping. Honor those widths
    // and the tutorial's size=200/240 tags without rewriting or splitting tags.
    internal sealed class TutorialTextMetrics
    {
        private static readonly Regex Size = new Regex(@"\bsize\s*=\s*[""']?([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
        private readonly Font font;
        private readonly float scale;
        private readonly int baseSize;
        private readonly FontStyle baseStyle;
        private readonly Stack<int> sizes = new Stack<int>();
        private int size, bold, italic;

        internal TutorialTextMetrics(TextMesh mesh)
        {
            font = mesh.font;
            baseSize = size = mesh.fontSize > 0 ? mesh.fontSize : font.fontSize;
            baseStyle = mesh.fontStyle;
            scale = mesh.characterSize * 0.1f * Mathf.Abs(mesh.transform.lossyScale.x);
        }

        internal float Advance(char ch)
        {
            bool b = bold > 0 || baseStyle == FontStyle.Bold || baseStyle == FontStyle.BoldAndItalic;
            bool i = italic > 0 || baseStyle == FontStyle.Italic || baseStyle == FontStyle.BoldAndItalic;
            FontStyle style = b ? (i ? FontStyle.BoldAndItalic : FontStyle.Bold) : (i ? FontStyle.Italic : FontStyle.Normal);
            CharacterInfo glyph;
            if (font.dynamic) font.RequestCharactersInTexture(ch.ToString(), size, style);
            return font.GetCharacterInfo(ch, out glyph, size, style) ? glyph.advance * scale : size * scale;
        }

        internal float TagWidth(string tag)
        {
            if (tag.StartsWith("<quad", StringComparison.OrdinalIgnoreCase))
            {
                Match match = Size.Match(tag);
                float value;
                return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value * scale : size * scale;
            }
            if (tag.StartsWith("<size=", StringComparison.OrdinalIgnoreCase))
            {
                Match match = Size.Match(tag);
                int value;
                if (match.Success && int.TryParse(match.Groups[1].Value, out value) && value > 0)
                { sizes.Push(size); size = value; }
            }
            else if (string.Equals(tag, "</size>", StringComparison.OrdinalIgnoreCase)) size = sizes.Count > 0 ? sizes.Pop() : baseSize;
            else if (string.Equals(tag, "<b>", StringComparison.OrdinalIgnoreCase)) bold++;
            else if (string.Equals(tag, "</b>", StringComparison.OrdinalIgnoreCase)) bold = Math.Max(0, bold - 1);
            else if (string.Equals(tag, "<i>", StringComparison.OrdinalIgnoreCase)) italic++;
            else if (string.Equals(tag, "</i>", StringComparison.OrdinalIgnoreCase)) italic = Math.Max(0, italic - 1);
            return 0;
        }
    }
}
