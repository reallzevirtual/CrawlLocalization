using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;
public static class ValidateGameplay
{
    [Serializable] public class Layout { public string source, path, text, font; public string[] components; public int size, anchor, alignment; public float character_size, line_spacing, scale_x; }
    [Serializable] public class ActionRecord { public string source, text; }
    [Serializable] public class Catalog { public Layout[] meshes; public ActionRecord[] actions; }
    static int checks;
    static readonly List<Font> fonts = new List<Font>();
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static object Invoke(Type type, string method, params object[] args) { return type.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args); }
    static TextMesh Mesh(GameObject root,string path,string text,string fontName)
    {
        GameObject current=root;
        foreach(string name in path.Split('/')) { GameObject next=new GameObject(name); next.transform.SetParent(current.transform,false); current=next; }
        TextMesh mesh=current.AddComponent<TextMesh>(); mesh.text=text; mesh.fontSize=160; mesh.characterSize=1;
        Font font=Font.CreateDynamicFontFromOSFont("Arial",160); font.name=fontName; fonts.Add(font); mesh.font=font;
        mesh.GetComponent<Renderer>().sharedMaterial=font.material;
        return mesh;
    }
    static void Action(TextMesh mesh,string text) { mesh.text=text; Invoke(typeof(GameplayActionPatch),"Postfix",text,mesh); }
    static void Reload(Plugin plugin,string root,string language)
    {
        plugin.Language=LanguagePack.Load(root,language,plugin.Warn);
        foreach(LocalizedMenuLabel label in plugin.Labels) label.Refresh();
    }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output=Path.Combine(root,"artifacts/gameplay");
        Plugin plugin=new Plugin(); Plugin.Instance=plugin;
        plugin.Language=LanguagePack.Load(root,"zh-Hans",plugin.Warn);
        plugin.Fonts=new FontService(plugin.Warn,delegate(string message) { });
        GameObject stage=new GameObject("GameplayValidation");
        try
        {
            Catalog catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output,"catalog.json")));
            TextMesh action=Mesh(stage,"ActionIndicator/Text","","CrawlTextBodyThin");
            Action(action,"BUY POTION"); Check(action.text=="购买药水","Existing shop prompt regression");
            Action(action,"DEFILE"); Check(action.text=="亵渎","Reused interaction changes prompt");
            Action(action,"Unknown mod action"); Reload(plugin,root,"zh-Hans");
            Check(action.text=="Unknown mod action" && action.font==fonts[0],"Unknown action restores source/font after reload");
            Action(action,""); Reload(plugin,root,"zh-Hans"); Check(action.text=="","Blank action never replays previous text");
            Action(action,"WISH"); Reload(plugin,root,"en"); Check(action.text=="WISH","English restore keeps latest action source");
            Action(action,"WISH"); Check(action.text=="WISH","Repeated input respects English cache");
            Reload(plugin,root,"zh-Hans"); Action(action,"WISH"); Check(action.text=="许愿","Repeated input respects Chinese reload");
            TextMesh unknown=Mesh(stage,"PlayerName","DEFILE","CrawlTextBodyThin"); MenuTextMeshes.Scan(stage);
            Check(unknown.text=="DEFILE" && unknown.GetComponent<LocalizedMenuLabel>()==null,"Unlisted player-name object excluded");
            int actions=0;
            foreach(ActionRecord record in catalog.actions)
            {
                if(string.IsNullOrEmpty(record.text)) continue;
                Action(action,record.text); Check(ChineseTextWrapper.ContainsChinese(action.text),"Untranslated action: "+record.source+" "+record.text); actions++;
            }
            GameObject player=new GameObject("GuiPlayer"); player.transform.SetParent(stage.transform,false);
            GUIPlayer gui=player.AddComponent<GUIPlayer>();
            GameObject reward=new GameObject("GoldTally"); reward.transform.SetParent(player.transform,false);
            MenuFlashEffect flash=reward.AddComponent<MenuFlashEffect>();
            TextMesh title=Mesh(reward,"TextGold","REWARD","CrawlTextBodyWide");
            TextMesh unit=Mesh(reward,"TextGold","gold","CrawlLarge");
            TextMesh number=Mesh(reward,"TextNumber","030","CrawlLarge");
            TextMesh plus=Mesh(reward,"Text+","+","CrawlLarge");
            reward.SetActive(false); Invoke(typeof(GameplayRewardPatch),"Postfix",gui,flash);
            Check(title.text=="奖励" && unit.text=="金" && !reward.activeSelf,"Inactive same-name reward labels translated independently");
            Check(number.text=="030" && plus.text=="+" && number.GetComponent<LocalizedMenuLabel>()==null && plus.GetComponent<LocalizedMenuLabel>()==null,"Reward numeric value and plus unchanged");
            string value; Check(plugin.Language.TryTranslate("gold",out value) && value=="金币","Scoped reward unit leaves global gold unchanged");
            Reload(plugin,root,"en"); Check(title.text=="REWARD" && unit.text=="gold","Scoped key never leaks into English display");
            Reload(plugin,root,"zh-Hans"); Check(unit.text=="金","Reward restored after reload");
            TextMesh portal=Mesh(stage,"PropPortal/TextInactiveMessage","Level 10 Required","CrawlLarge");
            PropPortal prop=portal.transform.parent.gameObject.AddComponent<PropPortal>(); portal.gameObject.SetActive(false);
            Invoke(typeof(GameplayStaticTextPatch),"Postfix",prop);
            Check(portal.text=="需要 10 级" && !portal.gameObject.activeSelf,"Portal text ready before original game enables object");
            TextMesh streak=Mesh(stage,"GUIGame/KillStreak","Streak","CrawlLarge");
            foreach(string n in new string[] { "1","10","99","001" })
            {
                streak.text=n+"x Streak"; Invoke(typeof(GameplayStreakPatch),"Postfix",streak.gameObject);
                Check(streak.text==n+"x 连杀","Streak preserves digits and x");
                Reload(plugin,root,"en"); Check(streak.text==n+"x Streak","Streak source survives reload"); Reload(plugin,root,"zh-Hans");
            }
            int targets=0;
            foreach(MethodBase method in (IEnumerable<MethodBase>)Invoke(typeof(GameplayStaticTextPatch),"TargetMethods")) { Check(method!=null,"Start hook resolves"); targets++; }
            Check(targets==5,"All intended lifecycle hooks");
            int layouts=0; float widest=0;
            foreach(Layout layout in catalog.meshes)
            {
                bool rewardUnit=layout.source=="sharedassets4.assets:237";
                bool rewardTitle=layout.source=="sharedassets4.assets:235";
                if(!rewardUnit && !rewardTitle && !MenuTextPolicy.MatchesPath(plugin.Language.TextMeshPaths,layout.path)) continue;
                // This catalog also includes older menu/HUD paths; restrict to this feature's audited roots.
                if(!rewardUnit && !rewardTitle && !(layout.path.StartsWith("Prop") || layout.path.StartsWith("Signs") || layout.path.StartsWith("GUIGame/") || layout.path=="GuiPlayer/Gold/GoldDamage")) continue;
                Check(Array.IndexOf(layout.components,"MenuTextMenuItem")<0,"Audited static label is eligible for path binding: "+layout.path);
                TextMesh mesh=Mesh(stage,layout.path,layout.text,layout.font);
                mesh.fontSize=layout.size; mesh.characterSize=layout.character_size; mesh.lineSpacing=layout.line_spacing;
                mesh.anchor=(TextAnchor)layout.anchor; mesh.alignment=(TextAlignment)layout.alignment; mesh.transform.localScale=new Vector3(layout.scale_x,1,1);
                if(rewardUnit || rewardTitle) LocalizedMenuLabel.GetOrCreate(mesh.gameObject).RefreshDirect(rewardUnit ? "@gameplay.reward-unit:gold" : layout.text,null);
                else MenuTextMeshes.Bind(mesh.gameObject,true);
                Check(ChineseTextWrapper.ContainsChinese(mesh.text),"Static path not translated: "+layout.path);
                Check(mesh.font.name==layout.font,"Expected bundled font identity: "+layout.path);
                float width=mesh.GetComponent<Renderer>().bounds.size.x;
                Check(width>0 && !float.IsNaN(width),"Valid rendered geometry: "+layout.path);
                widest=Math.Max(widest,width); layouts++;
            }
            Check(layouts==21,"Expected 19 audited static paths plus two scoped reward labels");
            Invoke(typeof(GameplayActionPatch),"Postfix","",null);
            Invoke(typeof(GameplayStreakPatch),"Postfix",new object[] { null });
            string report="PASS: "+checks+" gameplay checks; "+actions+" nonempty serialized action records; "+layouts+" static TextMesh cases using original asset font/size/scale. Maximum Chinese width="+widest+".\nActual localization components and font bundle run in Unity 5.4. Game/Harmony entry points are test doubles. No panel-width limit inferred; live layout, timing and interactions remain user verification.";
            File.WriteAllText(Path.Combine(output,"result.txt"),report); Debug.Log("CRAWL_GAMEPLAY_OK\n"+report);
        }
        finally { UnityEngine.Object.DestroyImmediate(stage); plugin.Fonts.Dispose(); Plugin.Instance=null; foreach(Font font in fonts) UnityEngine.Object.DestroyImmediate(font); fonts.Clear(); }
    }
}
