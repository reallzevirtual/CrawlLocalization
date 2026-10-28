using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using CrawlLocalization;
public static class ValidateShopDisplay
{
    static int checks;
    static readonly List<Font> fonts=new List<Font>();
    static void Check(bool ok,string message) { checks++;if(!ok) throw new Exception(message); }
    static TextMesh Mesh(GameObject root,string name)
    {
        GameObject obj=new GameObject(name);obj.transform.SetParent(root.transform,false);
        TextMesh mesh=obj.AddComponent<TextMesh>();mesh.fontSize=160;mesh.characterSize=1;
        Font font=Font.CreateDynamicFontFromOSFont("Arial",160);font.name="CrawlTextBodyThin";fonts.Add(font);
        mesh.font=font;mesh.GetComponent<Renderer>().sharedMaterial=font.material;return mesh;
    }
    static void Reload(Plugin plugin,string root,string id)
    { plugin.Language=LanguagePack.Load(root,id,plugin.Warn);foreach(LocalizedMenuLabel label in plugin.Labels) label.Refresh(); }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        Plugin plugin=new Plugin();Plugin.Instance=plugin;plugin.Language=LanguagePack.Load(root,"en",plugin.Warn);
        plugin.Fonts=new FontService(plugin.Warn,delegate(string s) { });
        GameObject stage=new GameObject("ShopDisplayValidation");stage.AddComponent<GUIStore>();
        try
        {
            TextMesh title=Mesh(stage,"Title"),body=Mesh(stage,"Body"),cost=Mesh(stage,"Cost"),stat=Mesh(stage,"Stat");
            global::GUIText gui=body.gameObject.AddComponent<global::GUIText>();Font titleFont=title.font,bodyFont=body.font;
            StringBuilder b=ShopText.Create();ShopText.Append(b,"Swift");ShopText.AppendChar(b,' ');ShopText.Append(b,"Shortsword");
            string source=ShopText.Finish(b);ShopDisplay.SetText(title,source);
            Check(title.text=="Swift Shortsword" && title.font==titleFont,"Initial English display/cache unchanged");
            Reload(plugin,root,"zh-Hans");
            Check(title.text=="迅捷 短剑" && title.font!=titleFont,"English-created label translates on reload");
            Check(source=="Swift Shortsword","Native rendering never changes original cache");
            Check(title.fontSize==160 && title.characterSize==1,"Original sampling and scale preserved");
            Reload(plugin,root,"en");Check(title.text==source && title.font==titleFont,"English restores text and original font");
            Reload(plugin,root,"zh-Hans");
            string unknown=ShopText.ConcatTwo(source," + MOD OVERCHARGE");ShopDisplay.SetText(title,unknown);
            Check(title.text==unknown && title.font==titleFont,"Reused title falls back with full unknown mod content");
            Reload(plugin,root,"en");Reload(plugin,root,"zh-Hans");Check(title.text==unknown,"Reload never resurrects stale recipe");
            ShopDisplay.SetText(title,source);Check(title.text=="迅捷 短剑","Known recipe recovers after unknown content");
            ShopDisplay.SetText(cost,"123");Check(cost.text=="123" && cost.GetComponent<LocalizedMenuLabel>()==null,"Numeric price has no localization/font component");
            string statSource=ShopText.ConcatArray(new object[] { "+",15," ","Strength" });ShopDisplay.SetText(stat,statSource);
            Check(stat.text=="+15 力量","Stat label keeps original sign and value");
            b=ShopText.Create();ShopText.Append(b,"\nHas ");ShopText.Append(b,"an unusually <color=#a19473>low attack speed</color>");
            ShopText.Append(b," with ");ShopText.Append(b,"abnormally <color=#a19473>high damage</color>");ShopText.AppendFormatOne(b," for a {0}","Shortsword");ShopText.Append(b,".");
            string bodySource=ShopText.Finish(b);ShopDisplay.SetGuiText(gui,bodySource);
            Check(ChineseTextWrapper.ContainsChinese(body.text) && body.text.Contains("<color=#a19473>"),"GUIText captures recipe before Unity string identity is lost");
            Check(body.font!=bodyFont,"GUIText uses bundled font");
            string translated=body.text;object[] args={translated,115f,body,null};
            bool original=(bool)typeof(MenuChineseWrapPatch).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
            Check(!original && ((string)args[3]).Replace("\n","")==translated.Replace("\n",""),"Chinese wrap keeps all characters and markup");
            body.text=(string)args[3];Check(body.GetComponent<Renderer>().bounds.size.x<=115.01f,"Body fits original shop wrap width");
            Reload(plugin,root,"en");Check(body.text==bodySource && body.font==bodyFont,"GUIText restores exact English body and font");
            Reload(plugin,root,"zh-Hans");Check(body.text==translated,"GUIText recipe survives language reload");
            string modBody=ShopText.ConcatTwo(bodySource,"\nMOD: explosive reward");ShopDisplay.SetGuiText(gui,modBody);
            Check(body.text==modBody && body.font==bodyFont,"GUIText reuse preserves all unknown mod text");
            string report="PASS: "+checks+" native shop display checks. Real Unity 5.4 TextMeshes/fonts and localization components; GUIText/game entry points are test doubles. Crawl was not started.";
            File.WriteAllText(Path.Combine(root,"artifacts/shop-display/result.txt"),report);Debug.Log("CRAWL_SHOP_DISPLAY_OK\n"+report);
        }
        finally { UnityEngine.Object.DestroyImmediate(stage);plugin.Fonts.Dispose();Plugin.Instance=null;foreach(Font font in fonts) UnityEngine.Object.DestroyImmediate(font);fonts.Clear(); }
    }
}
