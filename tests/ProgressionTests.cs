using System;
namespace CrawlLocalization
{
    public static class ProgressionTests
    {
        public static void Run(string root)
        {
            int checks=0;
            Action<bool,string> check=delegate(bool ok,string message) { checks++; if (!ok) throw new Exception(message); };
            LanguagePack zh=LanguagePack.Load(root,"zh-Hans",delegate(string s) { throw new Exception(s); });
            LanguagePack en=LanguagePack.Load(root,"en",delegate(string s) { throw new Exception(s); });
            foreach(string name in new string[] { "ABC","READY","O'NEIL","{0}","字(test)" })
            for(int id=1;id<=4;id++)
            {
                PausePlayerText data=new PausePlayerText(id,name,"10","Teok","Unknown mod deity effect");
                foreach(bool joined in new bool[] { false,true })
                {
                    string title=data.OriginalTitle(joined),body=data.OriginalBody(joined);
                    check(data.Matches(title,body,joined),"Matches exact source composition");
                    check(!data.Matches(title,body+" modified",joined),"Unknown mod text must not be overwritten");
                    check(data.Body(zh,joined).Contains(name),"Player name preserved literally");
                    check(data.Title(zh,joined).Contains(name.ToUpper()),"Title keeps game casing");
                    check(data.Body(en,joined)==null && data.Title(en,joined)==null,"English falls back to original");
                    if(joined) check(data.Body(zh,true).Contains("Unknown mod deity effect"),"Unknown deity effect remains readable");
                }
            }
            string translated;
            foreach(string source in new string[] { "UPGRADE\nMONSTER","NOT ENOUGH\nWRATH","PEAK LEVEL\nREACHED","not enough","BACK","locked","WRATH","ESCAPED\r\nALIVE!","LOST\r\nFOREVER","- Challenge KOUROK -","- Challenge GHIDRAAK -","- Challenge Tezekcal -","- final struggle -" })
                check(zh.TryTranslate(source,out translated) && ChineseTextWrapper.ContainsChinese(translated),"Translate "+source);
            check(!zh.TryTranslate("_P2_'READY'_DROPPED_OUT_",out translated),"No general player-name regex registered");
            Console.WriteLine("PASS: "+checks+" pause/evolution/result composition checks");
        }
    }
}
