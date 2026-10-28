using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CrawlLocalization;
public static class ValidateHeroStats
{
    [Serializable] public class Layout { public string path, font; public int size, anchor, alignment; public float character_size, line_spacing, scale_x; }
    [Serializable] public class Data { public string m_weaponName, m_displayName, m_displayNameHud; public int m_powerupType; public Combo[] m_combos; }
    [Serializable] public class Combo { public string m_name; }
    [Serializable] public class Record { public string cls; public Data data; }
    [Serializable] public class Catalog { public Record[] records; public Layout[] layouts; }
    static int checks;
    static List<Font> originalFonts = new List<Font>();
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    static void Invoke(Type type, string name, params object[] args) { type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,args); }
    static TextMesh Mesh(GameObject root, string name, string fontName)
    {
        GameObject obj = new GameObject(name); obj.transform.SetParent(root.transform,false);
        TextMesh mesh = obj.AddComponent<TextMesh>();
        Font font = Font.CreateDynamicFontFromOSFont("Arial",160); font.name = fontName; originalFonts.Add(font);
        mesh.font=font; mesh.fontSize=160; mesh.characterSize=0.4f; mesh.anchor=TextAnchor.MiddleLeft;
        obj.GetComponent<Renderer>().sharedMaterial=font.material;
        return mesh;
    }
    static void Names(PlayerGameData player, TextMesh single, TextMesh prefix, TextMesh name, TextMesh spell)
    { Invoke(typeof(HeroStatsNamesPatch),"Postfix",player,single,prefix,name,spell); }
    static void Artifacts(bool onlyChanged, int previous, int current, PlayerGameData player, List<GUIArtifactIcon> icons)
    { Invoke(typeof(HeroStatsArtifactsPatch),"Postfix",onlyChanged,previous,current,player,icons); }
    public static void Run()
    {
        string root=Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output=Path.Combine(root,"artifacts/hero-stats");
        Plugin plugin=new Plugin(); Plugin.Instance=plugin;
        plugin.Language=LanguagePack.Load(root,"zh-Hans",plugin.Warn);
        plugin.Fonts=new FontService(plugin.Warn,delegate(string message) { });
        GameObject panel=new GameObject("PersistentPlayerPanel");
        try
        {
            TextMesh single=Mesh(panel,"weapon","CrawlSmall"), prefix=Mesh(panel,"prefix","CrawlTextBodyTeensy"), name=Mesh(panel,"name","CrawlSmall"), spell=Mesh(panel,"spell","CrawlSmall");
            TextMesh stat=Mesh(panel,"stat","CrawlSmall"); stat.text="20";
            PlayerGameData player=new PlayerGameData();
            single.gameObject.SetActive(false); prefix.text="SWIFT"; name.text="Shortsword"; spell.text="Hyperdash";
            Names(player,single,prefix,name,spell);
            Check(prefix.text=="迅捷" && name.text=="短剑" && spell.text=="极速冲刺", "Split weapon and spell reuse existing translations");
            Check(!single.gameObject.activeSelf && stat.text=="20" && stat.GetComponent<LocalizedMenuLabel>()==null,"Original layout/number labels untouched");
            LocalizedMenuLabel label=name.GetComponent<LocalizedMenuLabel>();
            Func<LanguagePack,string> provider=label.TextProvider; int lookups=0;
            label.TextProvider=delegate(LanguagePack pack) { lookups++; return provider(pack); };
            for(int i=0;i<100;i++) { prefix.text="SWIFT"; name.text="Shortsword"; spell.text="Hyperdash"; Names(player,single,prefix,name,spell); }
            Check(name.text=="短剑" && lookups==0,"Repeated frames reuse rendered text without lookup/font work");
            single.gameObject.SetActive(true); prefix.gameObject.SetActive(false); name.gameObject.SetActive(false);
            single.text="Bow"; spell.text="Raging Bull"; Names(player,single,prefix,name,spell);
            Check(single.text=="弓" && spell.text=="狂牛" && !prefix.gameObject.activeSelf,"Changing to unprefixed weapon/spell updates labels");
            single.text="Unknown mod weapon"; spell.text="Raging Bull"; Names(player,single,prefix,name,spell);
            Check(single.text=="Unknown mod weapon" && single.font==originalFonts[0],"Unknown names restore original font/text");
            List<GUIArtifactIcon> icons=new List<GUIArtifactIcon>();
            for(int i=0;i<2;i++) { TextMesh mesh=Mesh(panel,"artifact"+i,"CrawlTextBodyThin"); GUIArtifactIcon icon=mesh.gameObject.AddComponent<GUIArtifactIcon>(); icon.m_text=mesh; icons.Add(icon); }
            player.Powerups.Add(new Powerup { Kind=ePurchasableType.Spell,m_displayName="Hyperdash" });
            player.Powerups.Add(new Powerup { Kind=ePurchasableType.Artifact,m_displayName="Vampiric Ring",m_displayNameHud="SHORT RING" });
            player.Powerups.Add(new Powerup { Kind=ePurchasableType.Artifact,m_displayName="Lucky Claw" });
            icons[0].m_text.text="SHORT RING"; icons[1].m_text.text="LUCKY CLAW";
            icons[1].gameObject.SetActive(false); icons[1].m_text.color=new Color(1,1,1,0.25f);
            float originalAlpha=icons[1].m_text.color.a;
            Artifacts(false,0,2,player,icons);
            Check(icons[0].m_text.text=="吸血戒指" && icons[1].m_text.text=="幸运之爪","Artifact alias fallback and filtered order");
            Check(!icons[1].gameObject.activeSelf && icons[1].m_text.color.a==originalAlpha,"Hidden scroll rows localized without changing visibility/alpha");
            Artifacts(true,2,2,player,icons);
            plugin.Language=LanguagePack.Load(root,"en",plugin.Warn);
            foreach(LocalizedMenuLabel item in plugin.Labels) item.Refresh();
            Check(icons[0].m_text.text=="SHORT RING" && icons[1].m_text.text=="LUCKY CLAW","Unchanged artifact frame preserves original source for reload");
            single.text="Bow"; spell.text="Raging Bull"; Names(player,single,prefix,name,spell);
            Check(single.text=="Bow" && spell.text=="Raging Bull","English while normal updates continue");
            plugin.Language=LanguagePack.Load(root,"zh-Hans",plugin.Warn);
            foreach(LocalizedMenuLabel item in plugin.Labels) item.Refresh();
            single.text="Bow"; spell.text="Raging Bull"; Names(player,single,prefix,name,spell);
            Check(single.text=="弓" && spell.text=="狂牛" && icons[0].m_text.text=="吸血戒指","Cached frames respect language reload");
            player.Powerups[1].m_displayName="Lucky Claw"; player.Powerups[1].m_displayNameHud="";
            icons[0].m_text.text="LUCKY CLAW"; icons[1].m_text.text="LUCKY CLAW";
            Artifacts(false,2,2,player,icons);
            Check(icons[0].m_text.text=="幸运之爪","Forced same-count list refresh updates reused icon");
            player.Powerups.RemoveAt(2); icons[0].m_text.text="LUCKY CLAW"; icons[1].gameObject.SetActive(false);
            Artifacts(true,2,1,player,icons);
            Check(icons[0].m_text.text=="幸运之爪" && !icons[1].gameObject.activeSelf,"Artifact count decrease handled");
            Names(null,null,null,null,null); Artifacts(true,0,0,null,null);
            Catalog catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(output,"catalog.json")));
            string geometry=Measure(panel,root,catalog);
            string report="PASS: "+checks+" HUD state/cache/reload checks using actual localization components in Unity 5.4.\n"+geometry+"\nGame/Harmony entry points are test doubles; in-game HUD placement, scrolling and equipment transitions remain user verification.";
            File.WriteAllText(Path.Combine(output,"result.txt"),report); Debug.Log("CRAWL_HERO_STATS_OK\n"+report);
        }
        finally { UnityEngine.Object.DestroyImmediate(panel); plugin.Fonts.Dispose(); Plugin.Instance=null; foreach(Font font in originalFonts) UnityEngine.Object.DestroyImmediate(font); originalFonts.Clear(); }
    }
    static string Measure(GameObject panel, string root, Catalog catalog)
    {
        List<string> weapons=new List<string>(), spells=new List<string>(), artifacts=new List<string>(), prefixes=new List<string>();
        foreach(Record record in catalog.records)
        {
            Data d=record.data;
            if(record.cls=="Weapon") { weapons.Add(d.m_weaponName); foreach(Combo combo in d.m_combos) weapons.Add(combo.m_name); }
            else if(record.cls.StartsWith("Powerup") && !string.IsNullOrEmpty(d.m_displayName))
            { if(d.m_powerupType==1) spells.Add(d.m_displayName); if(d.m_powerupType==2) artifacts.Add(d.m_displayName); }
        }
        foreach(string row in File.ReadAllLines(Path.Combine(root,"Languages/zh-Hans/shop-translations.tsv")))
            if(row.StartsWith("@shop.prefix:")) prefixes.Add(row.Split('\t')[0].Substring(13));
        int count=0; string report="";
        foreach(Layout layout in catalog.layouts)
        {
            List<string> sources=null; bool isPrefix=false;
            if(layout.path.EndsWith("/TextWeaponSplit1")) { sources=prefixes; isPrefix=true; }
            else if(layout.path.EndsWith("/TextWeaponSplit2") || layout.path.EndsWith("/TextWeaponNoSplit")) sources=weapons;
            else if(layout.path.EndsWith("/TextSpell")) sources=spells;
            else if(layout.path=="GuiStatsArtifactIcon/Text") sources=artifacts;
            if(sources==null) continue;
            TextMesh mesh=Mesh(panel,layout.path,layout.font); mesh.fontSize=layout.size; mesh.characterSize=layout.character_size; mesh.lineSpacing=layout.line_spacing;
            mesh.anchor=(TextAnchor)layout.anchor; mesh.alignment=(TextAlignment)layout.alignment; mesh.transform.localScale=new Vector3(layout.scale_x,1,1);
            float widest=0;
            foreach(string source in sources)
            {
                mesh.text=source; HeroStatsDisplay.Refresh(mesh,isPrefix ? "@shop.prefix:"+source : source,null);
                if(!ChineseTextWrapper.ContainsChinese(mesh.text)) throw new Exception("Untranslated HUD source: "+source);
                if(mesh.text.IndexOf('\n')>=0) throw new Exception("HUD name split unexpectedly");
                widest=Math.Max(widest,mesh.GetComponent<Renderer>().bounds.size.x); count++;
            }
            report+=layout.path+": max Chinese width="+widest+"\n";
        }
        return "Measured "+count+" asset-derived HUD labels at original font/size/scale; all translated and single-line.\n"+report;
    }
}
