using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;
public static class ValidateProgression
{
    [Serializable] public class Layout { public string source,path,text,font; public string[] components; public int size,anchor,alignment; public float character_size,line_spacing,scale_x,width; }
    [Serializable] public class Data { public string m_displayText,m_stringLocked,m_stringCantAfford,m_bossKillerNames,m_displayName; }
    [Serializable] public class Record { public string source,cls; public Data data; }
    [Serializable] public class Catalog { public Layout[] layouts; public Record[] records; }
    static int checks;
    static readonly List<Font> fonts=new List<Font>();
    static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    static object Invoke(Type type,string method,params object[] args) { return type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args); }
    static TextMesh Mesh(GameObject root,string path,string text,string fontName)
    {
        GameObject current=root;
        foreach(string name in path.Split('/')) { GameObject next=new GameObject(name); next.transform.SetParent(current.transform,false); current=next; }
        TextMesh mesh=current.AddComponent<TextMesh>();mesh.text=text;mesh.fontSize=160;mesh.characterSize=1;
        Font font=Font.CreateDynamicFontFromOSFont("Arial",160);font.name=fontName;fonts.Add(font);mesh.font=font;mesh.GetComponent<Renderer>().sharedMaterial=font.material;
        return mesh;
    }
    static void Reload(Plugin plugin,string root,string id) { plugin.Language=LanguagePack.Load(root,id,plugin.Warn);foreach(LocalizedMenuLabel label in plugin.Labels) label.Refresh(); }
    static void Prompt(TextMesh title,TextMesh body,string sourceTitle,string sourceBody)
    {
        global::GUIText t=title.GetComponent<global::GUIText>(), b=body.GetComponent<global::GUIText>();
        Invoke(typeof(PausePromptPatch),"Prefix",sourceTitle,sourceBody,t,b); t.SetText(sourceTitle);b.SetText(sourceBody);
    }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output=Path.Combine(root,"artifacts/progression");
        Plugin plugin=new Plugin();Plugin.Instance=plugin;plugin.Language=LanguagePack.Load(root,"zh-Hans",plugin.Warn);plugin.Fonts=new FontService(plugin.Warn,delegate(string s) { });
        GameObject stage=new GameObject("ProgressionValidation");
        try
        {
            Catalog catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output,"catalog.json")));
            GameObject prompt=new GameObject("MenuPrompt");prompt.transform.SetParent(stage.transform,false);prompt.AddComponent<MenuPrompt>();
            TextMesh title=Mesh(prompt,"TextTitle","","CrawlInvLarge"),body=Mesh(prompt,"Text","","04B_03-White");
            title.gameObject.AddComponent<global::GUIText>();body.gameObject.AddComponent<global::GUIText>();
            foreach(Layout layout in catalog.layouts) if(layout.path=="MenuPrompt/Text") { body.fontSize=layout.size;body.characterSize=layout.character_size;body.lineSpacing=layout.line_spacing; }
            Prompt(title,body,"_End the game?_","All progress will be lost, are you sure you want to quit?");
            Check(title.text=="结束本局游戏？" && body.text.Contains("本局进度"),"Quit text translated before original backdrop sizing");
            for(int i=0;i<4;i++)
            {
                SystemPlayers.Players[i]=new PlayerData { Name=i==1 ? "READY" : "P"+i };
                SystemPlayers.Players[i].GameData.Deity.Text="Monsters\nare\nStronger";
                PausePlayerText data=new PausePlayerText(i+1,SystemPlayers.Players[i].Name,"10","Teok","Monsters\nare\nStronger");
                foreach(bool joined in new bool[] { true,false })
                {
                    Prompt(title,body,data.OriginalTitle(joined),data.OriginalBody(joined));
                    Check(title.text.Contains(joined ? "已加入" : "已退出") && body.text.Contains(data.Name),"Player prompt/source snapshot");
                    if(joined) Check(body.text.Contains("特奥克") && body.text.Contains("怪物\n更加强大") && body.text.Contains("10"),"Reuse deity terms and level");
                    SystemPlayers.Players[i].Name="CHANGED";
                    Reload(plugin,root,"en");Check(title.text==data.OriginalTitle(joined) && body.text==data.OriginalBody(joined),"English restores original snapshot, not changed player data");
                    Reload(plugin,root,"zh-Hans");Check(body.text.Contains(data.Name),"Chinese reload keeps snapshot");SystemPlayers.Players[i].Name=data.Name;
                }
            }
            SystemPlayers.Players[1].GameData.Deity=null;
            PausePlayerText withoutDeity=new PausePlayerText(2,"READY","10","","");
            Prompt(title,body,withoutDeity.OriginalTitle(false),withoutDeity.OriginalBody(false));
            Check(body.text.Contains("电脑接管"),"Leaving does not require deity data");
            SystemPlayers.Players[1].GameData.Deity=new Deity { Text="Monsters\nare\nStronger" };
            PausePlayerText last=new PausePlayerText(2,"READY","10","Teok","Monsters\nare\nStronger");
            Prompt(title,body,last.OriginalTitle(false),"Custom mod notification for READY");
            Reload(plugin,root,"zh-Hans");Check(body.text=="Custom mod notification for READY" && title.text==last.OriginalTitle(false),"Modified mod prompt not parsed or overwritten");
            Prompt(title,body,"_End the game?_","All progress will be lost, are you sure you want to quit?");
            object[] wrapArgs={body.text,132f,body,null};
            bool original=(bool)Invoke(typeof(MenuChineseWrapPatch),"Prefix",wrapArgs);
            Check(!original && ((string)wrapArgs[3]).Replace("\n","")==body.text,"Prompt opts into Chinese wrapping without dropping characters");
            body.text=(string)wrapArgs[3];Check(body.GetComponent<Renderer>().bounds.size.x<=132.01f,"Prompt width fits original asset width");
            TextMesh description=Mesh(stage,"MenuEvolvePlayer/Menu/TextButtonDescription","UPGRADE\nMONSTER","CrawlTextBodyWide");
            MenuEvolvePlayer evolve=description.transform.parent.parent.gameObject.AddComponent<MenuEvolvePlayer>();
            Invoke(typeof(EvolutionBindPatch),"Postfix",evolve);
            foreach(string source in new string[] {"NOT ENOUGH\nWRATH","PEAK LEVEL\nREACHED","UPGRADE\nMONSTER","RETURN TO\nDUNGEON"})
            {
                description.text=source;Invoke(typeof(EvolutionDescriptionPatch),"Postfix",description);
                Check(ChineseTextWrapper.ContainsChinese(description.text),"Evolution selection updates immediately");
                Invoke(typeof(EvolutionDescriptionPatch),"Postfix",description);Reload(plugin,root,"en");Check(description.text==source,"No-write selection call does not capture Chinese as source");Reload(plugin,root,"zh-Hans");
            }
            TextMesh cant=Mesh(stage,"MenuEvolvePlayer/Evolve/Wrath/TextCantBuy","not enough","CrawlTextBodyThin");MenuTextMeshes.Bind(cant.gameObject,true);cant.gameObject.SetActive(false);
            cant.text="locked";cant.GetComponent<LocalizedMenuLabel>().RefreshChangedText();Check(cant.text=="未解锁" && !cant.gameObject.activeSelf,"Inactive evolution warning updated without changing visibility");
            foreach(bool won in new bool[] {true,false})
            {
                GameObject panel=new GameObject(won ? "MenuWin" : "MenuLose");panel.transform.SetParent(stage.transform,false);MenuWin win=panel.AddComponent<MenuWin>();
                string message=won ? "ESCAPED\r\nALIVE!" : "LOST\r\nFOREVER";
                TextMesh result=Mesh(panel,"ContainerGameOver/MenuWinTextGameOver",message,"CrawlLarge");
                TextMesh rank=Mesh(panel,"ContainerScores/P1/Title",won ? "BOSS KILLER" : "CONSUMED","CrawlTextBodyTeensy");
                TextMesh player=Mesh(panel,"ContainerScores/P1/Name","BOSS KILLER","CrawlSmall");
                MenuTextMeshes.Scan(panel); // Scene callback can run before MenuWin.Start.
                if(won) rank.text="CHAOS BRINGER\r";
                Invoke(typeof(ResultsTextPatch),"Postfix",win,won,result,rank);
                Check(result.text.Contains(won ? "逃出生天" : "迷失") && rank.text==(won ? "混沌使者" : "被吞噬"),"Win/loss Start after initial scan");
                Check(player.text=="BOSS KILLER" && player.GetComponent<LocalizedMenuLabel>()==null,"Winner name never treated as a title");
                Reload(plugin,root,"en");Check(result.text==message && rank.text==(won ? "CHAOS BRINGER\r" : "CONSUMED"),"Results restore line endings and selected original title");Reload(plugin,root,"zh-Hans");
            }
            TextMesh boss=Mesh(stage,"EffectBossName(Clone)","- Challenge KOUROK -","CrawlLarge");boss.gameObject.AddComponent<global::GUIText>();MenuTextMenuItem item=boss.gameObject.AddComponent<MenuTextMenuItem>();
            Check(MenuTextMeshes.Bind(boss.gameObject,true)==null,"Reproduce generic MenuTextMenuItem exclusion");Invoke(typeof(BossEntranceTextPatch),"Postfix",item);
            Check(boss.text.Contains("库洛克"),"Boss entrance forced binding");
            boss.text="- Challenge GHIDRAAK -";boss.GetComponent<LocalizedMenuLabel>().RefreshChangedText();Check(boss.text.Contains("吉德拉克"),"Pooled entrance label changes boss");
            Reload(plugin,root,"en");Check(boss.text=="- Challenge GHIDRAAK -","Boss source restored");Reload(plugin,root,"zh-Hans");
            boss.text="Unknown mod boss";boss.GetComponent<LocalizedMenuLabel>().RefreshChangedText();Check(boss.text=="Unknown mod boss","Unknown boss fallback");
            int layouts=0,cases=0;float widest=0;
            foreach(Layout layout in catalog.layouts)
            {
                if(!MenuTextPolicy.MatchesPath(plugin.Language.TextMeshPaths,layout.path)) continue;
                TextMesh mesh=Mesh(stage,layout.path,layout.text,layout.font);mesh.fontSize=layout.size;mesh.characterSize=layout.character_size;mesh.lineSpacing=layout.line_spacing;mesh.anchor=(TextAnchor)layout.anchor;mesh.alignment=(TextAlignment)layout.alignment;mesh.transform.localScale=new Vector3(layout.scale_x,1,1);
                Check(Array.IndexOf(layout.components,"MenuTextMenuItem")<0,"Static binding eligibility");MenuTextMeshes.Bind(mesh.gameObject,true);
                Check(ChineseTextWrapper.ContainsChinese(mesh.text),"Static translation: "+layout.path);
                Check(mesh.font.name==layout.font && mesh.GetComponent<Renderer>().bounds.size.x>0,"Bundle font/geometry: "+layout.path);layouts++;
                widest=Math.Max(widest,mesh.GetComponent<Renderer>().bounds.size.x);
            }
            foreach(Record record in catalog.records)
            {
                List<string> sources=new List<string>();Data data=record.data;
                if(data.m_displayText!=null) sources.Add(data.m_displayText);
                if(data.m_displayName!=null) sources.Add(data.m_displayName);
                if(data.m_stringLocked!=null) { sources.Add(data.m_stringLocked);sources.Add(data.m_stringCantAfford); }
                if(data.m_bossKillerNames!=null) foreach(string name in data.m_bossKillerNames.Split('\n')) if(name.Trim().Length>0) sources.Add(name.Trim());
                foreach(string source in sources) { string value;Check(plugin.Language.TryTranslate(source,out value) && ChineseTextWrapper.ContainsChinese(value),"Catalog term: "+source);cases++; }
            }
            string report="PASS: "+checks+" progression checks; "+layouts+" asset-derived static layouts and "+cases+" component text cases. Maximum static Chinese width="+widest+".\nActual localization components and bundled fonts run in Unity 5.4; game/Harmony entry points are test doubles. In-game controls, placement, flashes and save/callback behavior are not executed.";
            File.WriteAllText(Path.Combine(output,"result.txt"),report);Debug.Log("CRAWL_PROGRESSION_OK\n"+report);
        }
        finally { UnityEngine.Object.DestroyImmediate(stage);plugin.Fonts.Dispose();Plugin.Instance=null;foreach(Font font in fonts) UnityEngine.Object.DestroyImmediate(font);fonts.Clear(); }
    }
}
