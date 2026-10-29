using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;

// Real Unity 5.4 TextMeshes/fonts; a small puppet fixture models the renderer's
// assignment order. No game, Steam or network session is launched.
public static class ValidateFontFallback
{
    private sealed class Puppet { public TextMesh TM; }
    private sealed class Entity { public object Puppet; public readonly string State = "unchanged network text"; public uint Dirty = 512; }
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static object Invoke(Type type, string method, params object[] args)
    { return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args); }
    internal static void Run(string root, Plugin plugin)
    {
        FontService previous = plugin.Fonts;
        LanguagePack previousLanguage = plugin.Language;
        GameObject stage = new GameObject("FontFallbackValidation");
        using (FontService service = new FontService(delegate(string s) { Debug.LogWarning(s); }, delegate(string s) { }, root))
        {
            plugin.Fonts = service;
            // Reuse the previously loaded Chinese bundle through the existing service:
            // unload it first to avoid Unity loading two copies of the same bundle.
            previous.Dispose();
            string[] names = { "CrawlLarge", "CrawlSmall", "CrawlMedium", "CrawlTiny", "CrawlInvLarge", "CrawlTextBodyThin", "CrawlTextBodyWide", "CrawlTextBodyTeensy", "04B_03-White", "04B_04-Black" };
            foreach (string locale in new string[] { "ru", "es" })
            {
                plugin.Language = LanguagePack.Load(root, locale, plugin.Warn);
                foreach (string name in names)
                {
                    GameObject go = new GameObject(name); go.transform.SetParent(stage.transform);
                    TextMesh mesh = go.AddComponent<TextMesh>();
                    Font reference = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/" + name + ".ttf");
                    Check(reference != null, "Chinese input font exists: " + name);
                    FontChoice primary, fallback;
                    string latin = "Jugador Ñandú Игрок Ёж";
                    string mixed = "Jugador Ñandú Игрок Ёж 中文张三";
                    Check(service.TryChoose(plugin.Language, reference, 16, FontStyle.Normal, latin, out primary), "Primary native coverage: " + name);
                    Check(service.TryChoose(plugin.Language, reference, 16, FontStyle.Normal, mixed, out fallback), "Mixed native coverage: " + name);
                    Debug.Log("FALLBACK_FONT " + name + " primary=" + primary.Font.GetInstanceID() + " Han=" + primary.Font.HasCharacter('中') + " fallback=" + fallback.Font.GetInstanceID());
                    Check(primary.Font != fallback.Font, "Fallback changes font only when Han is needed: " + name);
                    Check(primary.Size == 16 && fallback.Size == 160 && Math.Abs(fallback.Size * fallback.Scale - 16) < .001f, "Sampling scale preserves geometry: " + name);
                    mesh.font = fallback.Font; mesh.fontSize = fallback.Size; mesh.characterSize = 10 * fallback.Scale; mesh.text = mixed;
                    mesh.GetComponent<Renderer>().sharedMaterial = fallback.Font.material;
                    Check(mesh.GetComponent<Renderer>().bounds.size.x > 0 && mesh.GetComponent<Renderer>().bounds.size.y > 0, "Mixed native mesh generated: " + name);
                    CharacterInfo before, after;
                    reference.RequestCharactersInTexture("A中",160,FontStyle.Normal);fallback.Font.RequestCharactersInTexture("A中",160,FontStyle.Normal);
                    foreach (char c in "A中")
                    {
                        Check(reference.GetCharacterInfo(c,out before,160) && fallback.Font.GetCharacterInfo(c,out after,160), "Existing glyph readable");
                        // Font importer baseline can differ; serialized layout is checked separately.
                        reference.GetCharacterInfo(c,out before,160);fallback.Font.GetCharacterInfo(c,out after,160);
                        Check(before.advance==after.advance && before.glyphWidth==after.glyphWidth && before.glyphHeight==after.glyphHeight, "Original glyph size/advance retained: " + name);
                    }
                }
            }
            plugin.Language = LanguagePack.Load(root,"ru",plugin.Warn);
            FontChoice ascii;
            Font original = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/CrawlSmall.ttf");
            service.TryChoose(plugin.Language,original,16,FontStyle.Normal,"READY",out ascii);
            GameObject obj = new GameObject("PlayerPlatformName");obj.transform.SetParent(stage.transform);
            TextMesh player = obj.AddComponent<TextMesh>();player.font=ascii.Font;player.fontSize=16;player.characterSize=10;
            Renderer renderer=player.GetComponent<Renderer>();renderer.sharedMaterial=ascii.Font.material;
            Material shared=renderer.sharedMaterial;Texture atlas=shared.mainTexture;
            player.text="中文Яñ";
            LocalizedMenuLabel label=MenuTextMeshes.Bind(obj,true);
            Check(label.FontOnly && player.text=="中文Яñ" && player.font!=ascii.Font,"Unlisted player name gets font-only fallback");
            Check(shared.mainTexture==atlas && renderer.sharedMaterial!=shared,"Shared source atlas remains unchanged");
            player.text="READY";label.RefreshChangedText();
            Check(player.text=="READY" && player.font==ascii.Font && player.fontSize==16 && player.characterSize==10 && renderer.sharedMaterial==shared,"Literal-like name is preserved and original rendering restored");
            foreach(string locale in new string[]{"ru","es","en","zh-Hans"})
            {
                plugin.Language=LanguagePack.Load(root,locale,plugin.Warn);label.ResetSource("中文Яñ");label.Refresh();
                Check(player.text=="中文Яñ" && player.font!=ascii.Font,"Name preserved through language reload: "+locale);
            }
            plugin.Language=LanguagePack.Load(root,"ru",plugin.Warn);
            Type adapter=typeof(OnlineMirrorFonts);
            adapter.GetField("puppetField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,typeof(Entity).GetField("Puppet"));
            adapter.GetField("meshField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,typeof(Puppet).GetField("TM"));
            Entity entity=new Entity{Puppet=new Puppet{TM=player}};
            Invoke(adapter,"Before",entity);
            Check(player.font==ascii.Font && player.characterSize==10,"Mirror starts from unmodified parameters");
            player.text="中文Яñ";renderer.sharedMaterial=shared;
            Invoke(adapter,"After",entity);
            Check(player.text=="中文Яñ" && player.font!=ascii.Font && renderer.sharedMaterial.mainTexture==player.font.material.mainTexture,"Mirror final material uses fallback atlas");
            Check(entity.State=="unchanged network text" && entity.Dirty==512,"Font adapter leaves protocol state and dirty flags intact");
            for(int i=0;i<3;i++){Invoke(adapter,"Before",entity);Invoke(adapter,"After",entity);}
            Check(Math.Abs(player.characterSize-1)<.001f && player.fontSize==160,"Repeated mirror updates do not accumulate scaling");
            Material active=renderer.sharedMaterial;
            player.font.RequestCharactersInTexture("更多字符的混排测试АбвгÑ",240,FontStyle.Bold);
            typeof(LocalizedMenuLabel).GetMethod("OnTextureRebuilt",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(label,new object[]{player.font});
            Check(renderer.sharedMaterial==active && active.mainTexture==player.font.material.mainTexture,"Atlas rebuild retains private material and refreshes texture");
            Invoke(adapter,"Before",entity);player.text="READY";Invoke(adapter,"After",entity);
            Check(player.text=="READY" && player.font==ascii.Font && renderer.sharedMaterial==shared,"Mirror restores ASCII font without translating a name");
            Invoke(adapter,"Before",entity);
            Material mismatched=new Material(shared);mismatched.mainTexture=Texture2D.whiteTexture;
            renderer.sharedMaterial=mismatched;
            Invoke(adapter,"After",entity);
            Check(player.font==ascii.Font && renderer.sharedMaterial!=mismatched && renderer.sharedMaterial.mainTexture==ascii.Font.material.mainTexture && mismatched.mainTexture==Texture2D.whiteTexture,"Mirror fixes a mismatched atlas even when the selected Font object is unchanged");
            Invoke(adapter,"Before",entity);renderer.sharedMaterial=shared;Invoke(adapter,"After",entity);
            UnityEngine.Object.DestroyImmediate(mismatched);
            GUIStyle style=new GUIStyle();style.font=ascii.Font;style.fontSize=12;
            object[] args={"中文Яñ",style};
            Check((string)Invoke(typeof(OnlineOverlay),"Prepare",args)=="中文Яñ" && ((GUIStyle)args[1]).font!=ascii.Font && ((GUIStyle)args[1]).fontSize==12,"IMGUI raw mixed names get pixel-sized fallback");
            UnityEngine.Object.DestroyImmediate(stage);
        }
        plugin.Fonts=previous;plugin.Language=previousLanguage;
        File.WriteAllText(Path.Combine(root,"artifacts/online-display/font-fallback.txt"),"PASS: "+checks+" native fallback checks (10 styles, ru/es mixed names, language reload, original glyph sizes, mirror material/order/state, IMGUI). Mirror calls use a fixture; no network session was started.");
        Debug.Log("CRAWL_FONT_FALLBACK_OK: "+checks);
    }
}
