using System;
using System.Text;
namespace CrawlLocalization
{
    public static class OnlineTests
    {
        static int checks;
        static void Check(bool ok,string message) { checks++;if(!ok) throw new Exception(message); }
        static string Render(string source,LanguagePack p) { OnlineRecipe r=OnlineText.Find(source);return r==null ? source : r.Render(p); }
        public static void Run(string root)
        {
            checks=0;
            LanguagePack zh=LanguagePack.Load(root,"zh-Hans",delegate(string s){throw new Exception(s);});
            LanguagePack en=LanguagePack.Load(root,"en",delegate(string s){throw new Exception(s);});
            string text=OnlineText.Literal("Play online");Check(text=="Play online" && Render(text,zh)=="联机游戏","Original literal kept, translated only when rendering");
            Check(OnlineText.Find("Play online")==null,"Interned ordinary strings are not marked");
            string ignored;Check(!zh.TryTranslate("Public",out ignored),"Online-only keys do not leak into base dictionary");
            foreach(string name in new string[]{"Public","Back","ONLINE","waiting...","Alice\nP2  waiting...","玩家 (host)"})
            {
                text=OnlineText.ConcatThree(OnlineText.Literal("Who can join \""),name,OnlineText.Literal("\"?"));
                Check(text=="Who can join \""+name+"\"?" && Render(text,zh)=="谁可以加入 \""+name+"\"？","User lobby name opaque: "+name);
                Check(Render(text,en)==text,"English original preserved: "+name);
                text=OnlineText.ConcatTwo(name,OnlineText.Literal(" FULL"));
                Check(Render(text,zh)==name+" 已满","Only server suffix translated");
            }
            text=OnlineText.ConcatArray(new object[]{OnlineText.Literal("P"),1,OnlineText.Literal("  "),"Public",OnlineText.Literal("  (host)")});
            Check(Render(text,zh)=="P1  Public  (房主)","Host name preserved");
            string slot=OnlineText.ConcatArray(new object[]{OnlineText.Literal("P"),2,OnlineText.Literal("  "),OnlineText.Literal("waiting...")});
            StringBuilder b=OnlineText.Create();OnlineText.Append(b,text);OnlineText.AppendChar(b,'\n');OnlineText.Append(b,slot);text=OnlineText.Finish(b);
            Check(Render(text,zh)=="P1  Public  (房主)\nP2  等待加入...","Multiline roster composes recorded children");
            Check(Render(text,en)==text && Render(text,zh).Contains("房主"),"Reload reuses immutable records");
            text=OnlineText.ConcatArray(new object[]{5,OnlineText.Literal(" games"),OnlineText.Literal("  -  page "),2,OnlineText.Literal("/"),3,OnlineText.Literal("\n* = friend")});
            Check(Render(text,zh)=="5 个房间  -  页码 2/3\n* 表示好友","Server counts/pages retain numbers");
            text=OnlineText.ConcatThree(OnlineText.Literal("This game runs Crawl Online "),"0.5.0",OnlineText.Literal(", you have 0.4.0. Everybody needs the same version."));
            Check(Render(text,zh).Contains("0.5.0") && Render(text,zh).Contains("相同版本"),"Version argument preserved");
            foreach(string s in new string[]{"Play online","Server list","Find a game or host your own.\nHost is P1, others join as P2-P4.","Searching for games...","No games found.","Everybody will be disconnected.","No games found. Host one or ask a friend to invite you.","Steam is not available","Could not reach the host (timeout)","Host stopped responding","Invalid port"})
                Check(Render(OnlineText.Literal(s),zh)!=s,"Required UI term translated: "+s);
            foreach(string s in new string[]{"Hosting (Steam, Public lobby)","Hosting (UDP port 27015)","Joining 127.0.0.1:27015...","Host failed: Untranslated socket detail","Host closed the session: kicked by host","Rejected: Session is full"})
            {
                text=OnlineText.Message(s);Check(text==s && Render(text,en)==s && Render(text,zh)!=s,"Scoped status/error: "+s);
            }
            text=OnlineText.Message("Host failed: Public");Check(Render(text,zh)=="创建房间失败：Public","Exception detail not translated as UI words");
            text=OnlineText.Message("Unknown custom diagnostic");Check(Render(text,zh)==text,"Unknown diagnostic intact");
            b=OnlineText.Create();OnlineText.Append(b,OnlineText.Literal("Public"));b.Append(" MOD");text=OnlineText.Finish(b);Check(OnlineText.Find(text)==null,"Unobserved builder changes invalidate record");
            text=OnlineText.Roster("Public\nBack  23 ms  (2 controllers)");Check(Render(text,zh)=="Public\nBack  23 ms  (2 个手柄)","Roster numeric suffix preserves arbitrary name prefix");
            text=OnlineText.Message("Rejected: Crawl Online version mismatch: host has 0.4.0, you have 0.5.0. Use the same version.");Check(text.Contains("0.4.0") && Render(text,zh)=="连接被拒绝：Crawl Online 版本不一致：房主使用 0.4.0，你使用 0.5.0。请使用相同版本。","Version error values are preserved");
            foreach(string row in LanguagePack.ReadTextLines(System.IO.Path.Combine(root,"Languages/zh-Hans/crawlonline-translations.tsv")))
            { if(row.Length==0 || row.StartsWith("#"))continue;Check(row.StartsWith("@online:"),"All mod entries scoped"); }
            Console.WriteLine("PASS: "+checks+" CrawlOnline text/name/cache checks.");
        }
    }
}
