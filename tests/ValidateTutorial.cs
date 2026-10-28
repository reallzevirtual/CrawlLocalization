using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using CrawlLocalization;

// Real Unity TextMeshes/fonts, isolated game entry-point stand-ins. No Crawl session.
public class GuiTutorialMessageBox : MonoBehaviour { }
public static class ValidateTutorial
{
    [Serializable] public class Layout { public string path,font; public int size; public float character_size,line_spacing,width,scale_x; }
    [Serializable] public class Catalog { public Layout[] layouts; }
    static int checks;
    static void Check(bool value,string message) { checks++; if(!value)throw new Exception(message); }
    static object Invoke(Type type,string name,params object[] args) { return type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args); }
    static string Decode(string cell) { return (string)Invoke(typeof(LanguagePack),"Unescape",cell); }
    static void Reload(Plugin p,string root,string id) {p.Language=LanguagePack.Load(root,id,p.Warn);foreach(LocalizedMenuLabel label in p.Labels)label.Refresh();}
    static TextMesh Mesh(GameObject parent,Layout layout,List<Font> fonts)
    {
        GameObject go=new GameObject(layout.path);go.transform.SetParent(parent.transform,false);
        TextMesh m=go.AddComponent<TextMesh>();m.fontSize=layout.size;m.characterSize=layout.character_size;m.lineSpacing=layout.line_spacing;
        m.transform.localScale=Vector3.one*layout.scale_x;m.richText=true;
        Font f=Font.CreateDynamicFontFromOSFont("Arial",layout.size);f.name=layout.font;fonts.Add(f);m.font=f;m.GetComponent<Renderer>().sharedMaterial=f.material;
        go.AddComponent<global::GUIText>();return m;
    }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        Plugin p=new Plugin();Plugin.Instance=p;p.Language=LanguagePack.Load(root,"zh-Hans",p.Warn);p.Fonts=new FontService(p.Warn,delegate(string s){});
        Catalog c=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(root,"artifacts/tutorial/catalog.json")));
        GameObject stage=new GameObject("TutorialValidation");stage.AddComponent<GuiTutorialMessageBox>();List<Font> fonts=new List<Font>();
        try
        {
            Layout bodyLayout=Array.Find(c.layouts,delegate(Layout l){return l.path=="GuiMessageBox/Text";});
            Layout topLayout=Array.Find(c.layouts,delegate(Layout l){return l.path=="GuiMessageBoxTop/Text";});
            Layout buttonLayout=Array.Find(c.layouts,delegate(Layout l){return l.path=="GuiMessageBox/Button/TextButton";});
            TextMesh body=Mesh(stage,bodyLayout,fonts),top=Mesh(stage,topLayout,fonts),button=Mesh(stage,buttonLayout,fonts);
            global::GUIText gui=body.GetComponent<global::GUIText>();Font original=body.font;
            Invoke(typeof(TutorialMessagePatch),"Prefix",gui);
            Invoke(typeof(TutorialMessagePatch),"Prefix",top.GetComponent<global::GUIText>());
            Invoke(typeof(TutorialButtonPatch),"Prefix",button.GetComponent<global::GUIText>());
            string source="<color=#e41944>ATTACK</color> by pressing [A]";
            gui.SetText(source);
            Check(body.text=="按 [A] <color=#e41944>攻击</color>","Translate before the game's icon expansion and box layout");
            Check(body.font!=original && body.characterSize==bodyLayout.character_size && body.fontSize==bodyLayout.size && body.lineSpacing==bodyLayout.line_spacing,"Mapped font preserves native layout parameters");
            Reload(p,root,"en");Check(body.text==source && body.font==original,"English restores current source and font");
            Reload(p,root,"zh-Hans");Check(body.text.Contains("攻击"),"F8 language reload refreshes tutorial message");
            gui.SetText("Future mod tutorial [A]");Reload(p,root,"zh-Hans");Check(body.text=="Future mod tutorial [A]" && body.font==original,"Unknown tutorial message falls back without stale translation");
            button.GetComponent<global::GUIText>().SetText("OK");Check(button.text=="确定" && button.font.name!="Arial","Tutorial confirmation uses existing translation/font mapping");
            stage.SetActive(false);gui.SetText(source);stage.SetActive(true);Check(body.text.Contains("攻击"),"Inactive binding remains usable");
            int rows=0,cases=0;
            foreach(string row in LanguagePack.ReadTextLines(Path.Combine(root,"Languages/zh-Hans/tutorial-translations.tsv")))
            {
                if(row.Length==0 || row.StartsWith("#"))continue;
                string[] cols=row.Split('\t');string english=Decode(cols[0]),zh=Decode(cols[1]);rows++;
                gui.SetText(english);Check(body.text==zh,"Exact tutorial lookup: "+english);
                Check(Regex.Matches(english,@"\[[AB]\]").Count==Regex.Matches(zh,@"\[[AB]\]").Count,"Input markers retained");
                foreach(TextMesh m in new TextMesh[]{body,top})
                {
                    m.GetComponent<global::GUIText>().SetText(english);
                    string expanded=Regex.Replace(m.text,@"\[[AB]\]","<quad material=0 size="+(m.fontSize*0.5f).ToString(System.Globalization.CultureInfo.InvariantCulture)+" x=0 y=0 width=1 height=1 />");
                    foreach(float width in m==body ? new float[]{52,59,65,70,80} : new float[]{topLayout.width})
                    {
                        object[] args={expanded,width,m,null};Check(!(bool)Invoke(typeof(TutorialWrapPatch),"Prefix",args),"Scoped Chinese wrap selected");
                        string wrapped=(string)args[3];m.text=wrapped;
                        Check(m.GetComponent<Renderer>().bounds.size.x<=width+0.1f,"Native tutorial bounds exceed "+width+": "+english+" => "+m.GetComponent<Renderer>().bounds.size.x);
                        Check(Regex.Replace(wrapped,@"\s","")==Regex.Replace(expanded,@"\s",""),"Wrap retains all glyphs and intact rich tags");cases++;
                    }
                }
            }
            gui.SetText("But you must be\n\n\n\nto open THE PORTAL");Check(body.text.Contains("\n\n\n\n"),"Level 10 picture's four-line gap retained");
            gui.SetText("<size=240>When one HERO levels up...</size>");TutorialTextMetrics metrics=new TutorialTextMetrics(body);
            float normal=metrics.Advance('英');metrics.TagWidth("<size=240>");float large=metrics.Advance('英');metrics.TagWidth("</size>");
            Check(large>normal*1.3f && Mathf.Abs(metrics.Advance('英')-normal)<0.001f,"Size tags change advance then restore the base size");
            string quad="<quad material=0 size=80 x=0 y=0 width=1 height=1 />";
            Check(Mathf.Abs(metrics.TagWidth(quad)-4.8f)<0.01f,"Inline quad uses its physical size, not zero width or atlas UV width");
            LanguagePack savedPack=p.Language;
            GameObject menuRoot=new GameObject("LanguageScopeMenu");menuRoot.AddComponent<MenuPrompt>();TextMesh menuMesh=Mesh(menuRoot,bodyLayout,fonts);TutorialMessagePatch.Bind(menuMesh.GetComponent<global::GUIText>());
            foreach(string id in new string[]{"es","ru","ja","ko","en","zh-Hans","zh-Hant"})
            {
                p.Language=new LanguagePack{Id=id};bool chinese=id.StartsWith("zh-");
                object[] tutorialScope={"姓名中文",80f,body,null};Check((bool)Invoke(typeof(TutorialWrapPatch),"Prefix",tutorialScope)==!chinese,"Tutorial Chinese rules scoped to language: "+id);
                object[] menuScope={"姓名中文",80f,menuMesh,null};Check((bool)Invoke(typeof(MenuChineseWrapPatch),"Prefix",menuScope)==!chinese,"Menu Chinese rules scoped to language: "+id);
            }
            p.Language=savedPack;UnityEngine.Object.DestroyImmediate(menuRoot);
            object[] fallback={"Original English",80f,body,null};Check((bool)Invoke(typeof(TutorialWrapPatch),"Prefix",fallback),"Non-Chinese wrapping remains original");
            GameObject outside=new GameObject("OutsideTutorial");TextMesh other=Mesh(outside,bodyLayout,fonts);TutorialMessagePatch.Bind(other.GetComponent<global::GUIText>());
            fallback=new object[]{"中文",80f,other,null};Check((bool)Invoke(typeof(TutorialWrapPatch),"Prefix",fallback),"Unrelated UI is outside the tutorial wrap scope");UnityEngine.Object.DestroyImmediate(outside);
            // Simulate translation removal during F8: current English source is retained.
            p.Language=new LanguagePack { Id="missing" };body.GetComponent<LocalizedMenuLabel>().Refresh();Check(body.text=="<size=240>When one HERO levels up...</size>" && body.font==original,"Missing entry restores source and font");
            string report="PASS: "+checks+" tutorial display/font/reload checks; "+rows+" entries, "+cases+" native Unity rich-text layout cases. Game entry points are stand-ins; no game session was launched.";
            File.WriteAllText(Path.Combine(root,"artifacts/tutorial/result.txt"),report);Debug.Log("CRAWL_TUTORIAL_OK\n"+report);
        }
        finally {UnityEngine.Object.DestroyImmediate(stage);p.Fonts.Dispose();foreach(Font f in fonts)UnityEngine.Object.DestroyImmediate(f);Plugin.Instance=null;}
    }
}
