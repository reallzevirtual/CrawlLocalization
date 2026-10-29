// Test doubles only. These are never compiled into the game plugin.
// Exercise FontService fallback decisions without invoking Unity or registering fonts.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static void DontDestroyOnLoad(Object value) { }
        public static void Destroy(Object value) { value.Destroyed = true; }
    }
    public class Material { }
    public enum FontStyle { Normal, Bold }
    public struct CharacterInfo { public int glyphWidth, glyphHeight, maxY, minY; }
    public static class Mathf { public static float Max(float a, float b) { return Math.Max(a, b); } }
    public class Font : Object
    {
        public static readonly Dictionary<string, Font> Installed = new Dictionary<string, Font>();
        public string name;
        public bool dynamic = true;
        public bool FailRequests;
        public Material material = new Material();
        public string Characters;
        public Font(string name, string characters) { this.name = name; Characters = characters; }
        public static string[] GetOSInstalledFontNames() { return new List<string>(Installed.Keys).ToArray(); }
        public static Font CreateDynamicFontFromOSFont(string name, int size) { return Installed[name]; }
        public void RequestCharactersInTexture(string text, int size, FontStyle style)
        {
            if (FailRequests) throw new InvalidOperationException("Simulated native glyph failure");
        }
        public bool HasCharacter(char ch) { return Characters.IndexOf(ch) >= 0; }
        public bool GetCharacterInfo(char ch, out CharacterInfo glyph, int size, FontStyle style)
        {
            glyph = new CharacterInfo { glyphWidth = 8, glyphHeight = 8, minY = 0, maxY = 8 };
            return Characters.IndexOf(ch) >= 0;
        }
    }
    public class AssetBundle : Object
    {
        public static readonly Dictionary<string, AssetBundle> Files = new Dictionary<string, AssetBundle>();
        public static int LoadCount;
        public readonly Dictionary<string, Font> Assets = new Dictionary<string, Font>();
        public static AssetBundle LoadFromFile(string file)
        {
            LoadCount++;
            AssetBundle bundle;
            return Files.TryGetValue(file, out bundle) ? bundle : null;
        }
        public T LoadAsset<T>(string name) where T : class
        {
            Font font;
            return Assets.TryGetValue(name, out font) ? font as T : null;
        }
        public void Unload(bool all) { Destroyed = true; }
    }
}
