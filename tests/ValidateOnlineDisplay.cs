using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;
public static class ValidateOnlineDisplay
{
    static int checks;
    static readonly List<Font> fonts=new List<Font>();
    static void Check(bool ok,string message) { checks++;if(!ok)throw new Exception(message); }
    static TextMesh Mesh(GameObject root,string name)
    {
        GameObject obj=new GameObject(name);obj.transform.SetParent(root.transform,false);
        TextMesh mesh=obj.AddComponent<TextMesh>();mesh.fontSize=160;mesh.characterSize=1;
        Font font=Font.CreateDynamicFontFromOSFont("Arial",160);font.name="CrawlTextBodyThin";fonts.Add(font);
        mesh.font=font;mesh.GetComponent<Renderer>().sharedMaterial=font.material;return mesh;
    }
    static object Invoke(Type type,string method,params object[] args) {return type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
    static void Reload(Plugin plugin,string root,string id)
    {plugin.Language=LanguagePack.Load(root,id,plugin.Warn);foreach(LocalizedMenuLabel label in plugin.Labels)label.Refresh();}
    private sealed class MirrorPuppet { public TextMesh TM; }
    private sealed class MirrorEntity { public object Puppet; public readonly string State = "untouched"; public uint Dirty = 512; }
    static void ValidateNormalMirror(string root, Plugin plugin)
    {
        Check(!plugin.Fonts.HasCombinedFont, "Ordinary mirror test runs with addon detection disabled");
        Type adapter = typeof(OnlineMirrorFonts);
        adapter.GetField("puppetField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, typeof(MirrorEntity).GetField("Puppet"));
        adapter.GetField("meshField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, typeof(MirrorPuppet).GetField("TM"));
        GameObject stage = new GameObject("NormalMirrorValidation");
        TextMesh mesh = Mesh(stage, "RemoteText");
        Font original = mesh.font;
        Renderer renderer = mesh.GetComponent<Renderer>();
        Material received = new Material(original.material);
        received.mainTexture = Texture2D.whiteTexture;
        Texture receivedAtlas = received.mainTexture;
        MirrorEntity entity = new MirrorEntity { Puppet = new MirrorPuppet { TM = mesh } };
        try
        {
            foreach (string locale in new string[] { "zh-Hans", "ru", "es" })
            {
                plugin.Language = LanguagePack.Load(root, locale, plugin.Warn);
                string text = locale == "zh-Hans" ? "开始游戏" : locale == "ru" ? "НАЧАТЬ ИГРУ" : "INICIAR PARTIDA";
                foreach (int size in new int[] { 16, 160 })
                {
                    Invoke(adapter, "Before", entity);
                    mesh.font = original; mesh.fontSize = size; mesh.characterSize = size == 16 ? 10f : 1f;
                    mesh.lineSpacing = .5f; mesh.anchor = TextAnchor.MiddleCenter;
                    mesh.text = text; renderer.sharedMaterial = received;
                    Invoke(adapter, "After", entity);
                    Check(mesh.text == text && mesh.font != original, "Already translated mirror selects local font without addon: " + locale);
                    Check(Math.Abs(mesh.fontSize * mesh.characterSize - 160f) < .001f, "Received geometry preserved: " + locale);
                    Check(renderer.sharedMaterial != received && renderer.sharedMaterial.mainTexture == mesh.font.material.mainTexture, "Local font and atlas paired after remote material write: " + locale);
                    Check(received.mainTexture == receivedAtlas && entity.State == "untouched" && entity.Dirty == 512, "Network state and shared material unchanged: " + locale);
                    for (int frame = 0; frame < 5; frame++) { Invoke(adapter, "Before", entity); Invoke(adapter, "After", entity); }
                    Check(Math.Abs(mesh.fontSize * mesh.characterSize - 160f) < .001f && mesh.lineSpacing == .5f && mesh.anchor == TextAnchor.MiddleCenter, "Incremental updates do not accumulate scale or change layout: " + locale);
                    LocalizedMenuLabel label = mesh.GetComponent<LocalizedMenuLabel>();
                    label.Refresh();
                    Check(mesh.text == text && Math.Abs(mesh.fontSize * mesh.characterSize - 160f) < .001f, "Reload retains remote text and geometry: " + locale);
                    mesh.font.RequestCharactersInTexture(text + "ABC", 240, FontStyle.Bold);
                    typeof(LocalizedMenuLabel).GetMethod("OnTextureRebuilt", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(label, new object[] { mesh.font });
                    Check(renderer.sharedMaterial.mainTexture == mesh.font.material.mainTexture, "Mirror atlas remains current after rebuild: " + locale);
                }
            }
            Invoke(adapter, "Before", entity);
            mesh.text = "READY"; renderer.sharedMaterial = received;
            Invoke(adapter, "After", entity);
            Check(mesh.text == "READY" && mesh.font != original, "ASCII player name is mapped without translation");
            plugin.Language = LanguagePack.Load(root, "en", plugin.Warn);
            mesh.GetComponent<LocalizedMenuLabel>().Refresh();
            Check(mesh.text == "READY" && mesh.font == original, "No language mapping restores the received font");
            Invoke(adapter, "Before", entity); Invoke(adapter, "After", entity);
            Check(mesh.text == "READY" && renderer.sharedMaterial.mainTexture == original.material.mainTexture, "Unmapped mirror still repairs its atlas");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(stage);
            UnityEngine.Object.DestroyImmediate(received);
            plugin.Language = LanguagePack.Load(root, "zh-Hans", plugin.Warn);
        }
    }
    static void Prompt(global::GUIText title, global::GUIText body, string source, string text)
    {
        object[] scope = { source, text, null };
        Invoke(typeof(PromptSourcePatch), "Prefix", scope);
        try
        {
            string formatted = source.ToUpper().Replace(' ', '_');
            Invoke(typeof(PausePromptPatch), "Prefix", formatted, text, title, body);
            title.SetText(formatted); body.SetText(text);
        }
        finally { Invoke(typeof(PromptSourcePatch), "Finalizer", null, scope[2]); }
    }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        Plugin plugin=new Plugin();Plugin.Instance=plugin;plugin.Language=LanguagePack.Load(root,"zh-Hans",plugin.Warn);plugin.Fonts=new FontService(plugin.Warn,delegate(string s){});
        LocalizedMenuLabel.ExternalProvider=delegate(string source) {OnlineRecipe r=OnlineText.Find(source);return r==null?null:(Func<LanguagePack,string>)delegate(LanguagePack p){return p.Id=="en"?null:r.Render(p);};};
        GameObject stage=new GameObject("OnlineValidation");stage.AddComponent<MenuPrompt>();
        try
        {
            TextMesh title=Mesh(stage,"Title"),body=Mesh(stage,"Text"),button=Mesh(stage,"Button"),panel=Mesh(stage,"Online Lobby Panel");
            global::GUIText t=title.gameObject.AddComponent<global::GUIText>(),b=body.gameObject.AddComponent<global::GUIText>();button.gameObject.AddComponent<global::GUIText>();
            title.font.name="CrawlInvLarge";
            Font originalTitle=title.font,originalBody=body.font;
            string titleSource=OnlineText.Literal("Play online"),bodySource=OnlineText.Literal("Find a game or host your own.\nHost is P1, others join as P2-P4.");
            Prompt(t,b,titleSource,bodySource);
            Check(title.text=="_联机游戏_" && body.text.Contains("房主为 P1"),"Online title/body use existing prompt before backdrop sizing");
            Check(title.font!=originalTitle && body.font!=originalBody,"Native TextMesh mapped fonts");
            object[] args={body.text,132f,body,null};Check(!(bool)Invoke(typeof(MenuChineseWrapPatch),"Prefix",args),"Online prompt uses existing Chinese wrap");
            body.text=(string)args[3];Check(body.GetComponent<Renderer>().bounds.size.x<=132.01f,"Intro body fits original prompt width");
            MenuTextMenuItem item=button.gameObject.AddComponent<MenuTextMenuItem>();MenuTextMenuItemData data=new MenuTextMenuItemData{m_text=OnlineText.Literal("Server list"),m_message="OnAction"};
            Invoke(typeof(MenuInitialisePatch),"Prefix",item,data);button.GetComponent<global::GUIText>().SetText(data.m_text);
            Check(button.text=="服务器列表" && data.m_text=="Server list" && data.m_message=="OnAction","Button display changes; source/action identifier retained");
            Reload(plugin,root,"en");Check(title.text=="PLAY_ONLINE" && body.text==bodySource && title.font==originalTitle,"English reload restores source and font");
            Reload(plugin,root,"zh-Hans");Check(title.text=="_联机游戏_" && button.text=="服务器列表","Chinese reload without reconstructing prompt");
            foreach (string source in new string[] { "Host lobby", "Server list", "Online lobby", "Close the lobby?", "Online" })
            {
                Prompt(t,b,OnlineText.Literal(source),"Unchanged body");
                Check(title.text.StartsWith("_") && title.text.EndsWith("_") && ChineseTextWrapper.ContainsChinese(title.text), "Inverse online title: " + source);
                Reload(plugin,root,"en");
                Check(title.text==source.ToUpper().Replace(' ','_'), "English title formatting restored: " + source);
                Reload(plugin,root,"zh-Hans");
            }
            foreach (string locale in new string[] { "es", "ru" })
            {
                Reload(plugin,root,locale); Prompt(t,b,OnlineText.Literal("Server list"),"Unchanged body");
                Check(title.text.StartsWith("_") && title.text.EndsWith("_") && !title.text.Contains(" ") && !ChineseTextWrapper.ContainsChinese(title.text), "Other languages preserve inverse spaces/padding: " + locale);
            }
            Reload(plugin,root,"zh-Hans");
            Prompt(t,b,"Play online","Unknown mod body");
            Check(title.text=="PLAY_ONLINE", "Unmarked mod title does not use CrawlOnline table");
            string roster=OnlineText.ConcatArray(new object[]{OnlineText.Literal("P"),1,OnlineText.Literal("  "),"Public",OnlineText.Literal("  (host)")});
            b.SetText(roster);Check(body.text=="P1  Public  (房主)","Live lobby body keeps literal-like player name");
            roster=OnlineText.ConcatArray(new object[]{OnlineText.Literal("P"),2,OnlineText.Literal("  "),"Back",OnlineText.Literal("  "),23,OnlineText.Literal("ms")});
            b.SetText(roster);Check(body.text=="P2  Back  23ms","Live body swaps provenance and preserves ping/name");
            OnlineOverlay.SetText(panel,OnlineText.ConcatTwo(OnlineText.Literal("<color=#e8d8b0ff>ONLINE LOBBY</color>\n"),roster));
            Check(panel.text.Contains("联机大厅") && panel.text.Contains("Back") && panel.text.Contains("23ms"),"Standalone live panel binds captured recipe");
            b.SetText("Custom mod message");Reload(plugin,root,"zh-Hans");Check(body.text=="Custom mod message","Reused prompt clears old online provider");
            b.SetText(roster);
            LocalizedMenuLabel bodyLabel=body.GetComponent<LocalizedMenuLabel>();bodyLabel.TextProvider=delegate(LanguagePack p){return p.Id=="en"?null:"独立弹窗";};
            bodyLabel.ResetSource("New base prompt");b.SetText("New base prompt");Check(body.text=="独立弹窗","Reusing an online label cannot clear another adapter's new provider");
            int terms=0;
            foreach(string row in LanguagePack.ReadTextLines(Path.Combine(root,"Languages/zh-Hans/crawlonline-translations.tsv")))
            {
                if(row.Length==0 || row.StartsWith("#"))continue;
                string[] cells=row.Split('\t');string key=(string)Invoke(typeof(LanguagePack),"Unescape",cells[0].Substring(8));string expected=(string)Invoke(typeof(LanguagePack),"Unescape",cells[1]);
                OnlineOverlay.SetText(panel,OnlineText.Literal(key));Check(panel.text==expected,"Native term/font: "+key);terms++;
            }
            GUIStyle style=new GUIStyle();style.font=originalBody;style.fontSize=12;style.wordWrap=true;
            object[] prepare={OnlineText.Literal("Play online"),style};string translated=(string)Invoke(typeof(OnlineOverlay),"Prepare",prepare);
            GUIStyle localized=(GUIStyle)prepare[1];Check(translated=="联机游戏" && localized.font!=null && localized.fontSize==12,"IMGUI text uses screen-pixel font size");
            Check(style.font==originalBody && style.fontSize==12 && !ReferenceEquals(style,localized),"IMGUI source style is not modified");
            GUIStyle automatic=new GUIStyle(style);automatic.fontSize=0;prepare=new object[]{OnlineText.Literal("Play online"),automatic};Invoke(typeof(OnlineOverlay),"Prepare",prepare);
            Check(((GUIStyle)prepare[1]).fontSize==12 && automatic.fontSize==0,"Default IMGUI size does not inherit system font's 32px creation size");
            Reload(plugin,root,"en");prepare=new object[]{OnlineText.Literal("Play online"),style};Check((string)Invoke(typeof(OnlineOverlay),"Prepare",prepare)=="Play online" && ReferenceEquals(prepare[1],style),"IMGUI English retains original style");
            ValidateNormalMirror(root, plugin);
            ValidateCombinedFont.Run(root, plugin);
            string report="PASS: "+checks+" native CrawlOnline display/font/reload checks, including "+terms+" scoped table entries. Unity 5.4 components and fonts are real; game GUIText entry points are test doubles. No game or network session started.";
            File.WriteAllText(Path.Combine(root,"artifacts/online-display/result.txt"),report);Debug.Log("CRAWL_ONLINE_DISPLAY_OK\n"+report);
        }
        finally {UnityEngine.Object.DestroyImmediate(stage);LocalizedMenuLabel.ExternalProvider=null;OnlineOverlay.Clear();plugin.Fonts.Dispose();Plugin.Instance=null;foreach(Font f in fonts)UnityEngine.Object.DestroyImmediate(f);fonts.Clear();}
    }
}
