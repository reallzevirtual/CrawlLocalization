using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    // Display-only adapter for every language pack, independent of the optional addon.
    // Never change Entity.State, Dirty, FontId or text.
    internal static class OnlineMirrorFonts
    {
        private static FieldInfo puppetField, meshField;
        internal static void Install(Harmony harmony, Action<string> warn)
        {
            Type mirror = AccessTools.TypeByName("CrawlOnline.Client.SceneMirror");
            if (mirror == null) return;
            Type entity = mirror.GetNestedType("Entity", BindingFlags.Public | BindingFlags.NonPublic);
            Type puppet = mirror.GetNestedType("RendererPuppet", BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo method = AccessTools.Method(mirror, "ApplyRenderer");
            puppetField = entity == null ? null : AccessTools.Field(entity, "Puppet");
            meshField = puppet == null ? null : AccessTools.Field(puppet, "TM");
            if (method == null || puppetField == null || meshField == null)
            { warn("CrawlOnline mirror font fallback unavailable: unsupported renderer structure."); return; }
            try { harmony.Patch(method, prefix: new HarmonyMethod(typeof(OnlineMirrorFonts), "Before"), postfix: new HarmonyMethod(typeof(OnlineMirrorFonts), "After")); }
            catch (Exception ex) { warn("CrawlOnline mirror font fallback unavailable: " + ex.Message); }
        }
        private static TextMesh Mesh(object entity)
        {
            if (entity == null || Plugin.Instance == null) return null;
            object puppet = puppetField.GetValue(entity);
            return puppet == null || !meshField.DeclaringType.IsInstanceOfType(puppet) ? null : meshField.GetValue(puppet) as TextMesh;
        }
        private static void Before(object __0)
        {
            try
            {
                TextMesh mesh = Mesh(__0);
                if (mesh == null) return;
                LocalizedMenuLabel label = mesh.GetComponent<LocalizedMenuLabel>();
                if (label != null) label.Restore(false);
            }
            catch (Exception ex) { if (Plugin.Instance != null) Plugin.Instance.Warn("Mirror font restore failed: " + ex.Message); }
        }
        private static void After(object __0)
        {
            try
            {
                TextMesh mesh = Mesh(__0);
                if (mesh == null) return;
                LocalizedMenuLabel.GetOrCreate(mesh.gameObject, true).CaptureMirrorSource();
            }
            catch (Exception ex) { if (Plugin.Instance != null) Plugin.Instance.Warn("Mirror font fallback failed: " + ex.Message); }
        }
    }
}
