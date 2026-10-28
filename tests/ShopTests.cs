using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace CrawlLocalization
{
    public static class ShopTests
    {
        private static int checks;
        private static void Check(bool ok,string message) { checks++;if(!ok) throw new Exception(message); }
        private static LanguagePack cachedPack;
        private static string cachedRoot;
        private static LanguagePack Load(string root)
        {
            if(cachedPack==null || cachedRoot!=root) { cachedPack=LanguagePack.Load(root,"zh-Hans",delegate(string s) { throw new Exception(s); });cachedRoot=root; }
            return cachedPack;
        }
        private static string Render(string root,string source) { return ShopText.Capture(source).Translate(Load(root)); }
        public static string Name(string root,string name,string prefix)
        {
            StringBuilder b=ShopText.Create();if(!string.IsNullOrEmpty(prefix)) { ShopText.Append(b,prefix);ShopText.AppendChar(b,' '); }
            ShopText.Append(b,name);return Render(root,ShopText.Finish(b));
        }
        public static string Description(string root,string source) { return Render(root,source); }
        public static string StressBody(string root,string name,string description)
        {
            StringBuilder b=ShopText.Create();ShopText.Append(b,description);ShopText.AppendChar(b,'.');
            string magic=ShopText.FormatOne("<color=#28bbcc>{0}</color>","the purifying fire");
            ShopText.AppendChar(b,'\n');ShopText.AppendFormatOne(b,"The glyph of {0} has been hastily scratched onto it's surface.",magic);
            ShopText.Append(b,"\nHas ");ShopText.Append(b,"an unusually <color=#a19473>low attack speed</color>");ShopText.Append(b," with ");ShopText.Append(b,"abnormally <color=#a19473>high damage</color>");
            ShopText.AppendFormatOne(b," for a {0}",name);ShopText.Append(b,".");
            ShopText.AppendFormatTwo(b,"\n<color=#b78f2d>{0}% chance of {1}.</color>",100,"Stealing Gold");
            return Render(root,ShopText.Finish(b));
        }
        private sealed class SideEffect { internal int Calls; public override string ToString() { Calls++;return "MOD"; } }
        public static string[] Run(string root)
        {
            checks=0;LanguagePack pack=Load(root),en=LanguagePack.Load(root,"en",delegate(string s) { throw new Exception(s); });
            StringBuilder original=new StringBuilder(),traced=ShopText.Create();
            original.Append("Swift");ShopText.Append(traced,"Swift");
            string prefix=ShopText.Finish(traced);original.Append(' ');ShopText.AppendChar(traced,' ');
            original.Append("Shortsword");ShopText.Append(traced,"Shortsword");string name=ShopText.Finish(traced);
            Check(name==original.ToString() && prefix=="Swift","Name/prefix original cache values unchanged");
            ShopTextRecipe saved=ShopText.Capture(name);Check(saved.Translate(pack)=="追风之 短剑","Original append order preserved");
            Check(ShopText.Capture(prefix).Translate(pack)=="追风之","Intermediate ToString snapshot is immutable");
            Check(saved.Translate(en)==null && saved.Translate(pack)=="追风之 短剑","Reload replays trace without generating weapon again");
            StringBuilder body=ShopText.Create();ShopText.AppendFormatTwo(body,"\n<color=#b78f2d>{0}% chance of {1}.</color>",25,"Stealing Gold");
            string source=ShopText.Finish(body);Check(source=="\n<color=#b78f2d>25% chance of Stealing Gold.</color>","Format leaves original cache exactly intact");
            Check(ShopText.Capture(source).Translate(pack).Contains("25％"),"Numbers/colors/format arguments survive");
            string modded=ShopText.ConcatTwo(source,"\nMOD BONUS: kills explode");Check(ShopText.Capture(modded).Translate(pack)==null && modded.EndsWith("MOD BONUS: kills explode"),"Untranslated mod addition falls back as a whole, never disappears");
            StringBuilder unknown=ShopText.Create();ShopText.Append(unknown,"Shortsword");unknown.Append(" + UNOBSERVED");
            Check(ShopText.Capture(ShopText.Finish(unknown)).Translate(pack)==null,"Uninstrumented builder edit invalidates trace");
            StringBuilder replaced=ShopText.Create();ShopText.Append(replaced,"Shortsword");replaced.Replace("Short","UnknownMod");
            Check(ShopText.Capture(ShopText.Finish(replaced)).Translate(pack)==null,"Replace is not mistaken for the old source");
            string equalCopy=new string(name.ToCharArray());Check(ShopText.Capture(equalCopy).Translate(pack)==null,"Equal unrelated string does not inherit provenance");
            Check(Name(root,"Unknown mod sword","Swift")==null,"Unknown component preserves original full name");
            Check(ShopText.Capture("Previously cached untraced description").Translate(pack)==null,"Pre-existing unknown cache is not regenerated");
            string trade=ShopText.ConcatThree("Shortsword","\n\nReplaces ","Bow");Check(ShopText.Capture(trade).Translate(pack)=="短剑\n\n替换 弓","Concat preserves operation order");
            string stat=ShopText.ConcatArray(new object[] { "+",15," ","Strength" });Check(stat=="+15 Strength" && ShopText.Capture(stat).Translate(pack)=="+15 力量","Stat values use original numeric conversion");
            SideEffect effect=new SideEffect();string side=ShopText.ConcatArray(new object[] {"",effect});Check(effect.Calls==1 && side=="MOD" && ShopText.Capture(side).Translate(pack)==null,"Mod object ToString runs once only");
            effect=new SideEffect();body=ShopText.Create();ShopText.AppendFormatOne(body,"{0}",effect);Check(effect.Calls==1 && ShopText.Capture(ShopText.Finish(body)).Translate(pack)==null,"Unsupported format argument is not evaluated twice");
            StringBuilder empty=ShopText.Create();ShopText.Append(empty,null);ShopText.AppendChar(empty,'\n');Check(ShopText.Finish(empty)=="\n","Null/empty fragments unchanged");
            List<string> examples=new List<string>();
            foreach(string row in File.ReadAllLines(Path.Combine(root,"Languages/zh-Hans/shop-translations.tsv")))
            {
                if(row.StartsWith("@shop.prefix:")) { string value=Name(root,"Shortsword",row.Split('\t')[0].Substring(13));Check(value!=null,"Prefix coverage"); }
                if(row.StartsWith("@shop.magic:"))
                {
                    string format=row.Split('\t')[0].Substring(12);StringBuilder b=ShopText.Create();
                    ShopText.AppendFormatOne(b,format,ShopText.FormatOne("<color=#28bbcc>{0}</color>","the purifying fire"));
                    string value=ShopText.Capture(ShopText.Finish(b)).Translate(pack);Check(value!=null && !value.Contains("{0}"),"Selected magic format coverage");examples.Add(value);
                }
            }
            string stress=StressBody(root,"Shortsword","");Check(stress!=null,"Original-operation description case");examples.Add(stress);
            Console.WriteLine("PASS: "+checks+" shop trace/cache/fallback checks");return examples.ToArray();
        }
    }
}
