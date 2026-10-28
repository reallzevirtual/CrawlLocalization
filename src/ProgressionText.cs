using System;
using System.Globalization;
namespace CrawlLocalization
{
    // Snapshot the original source parts. Player names are arguments, never lookup keys.
    internal sealed class PausePlayerText
    {
        internal readonly string Number, Name, Level, DeityName, DeityText;
        internal PausePlayerText(int number, string name, string level, string deityName, string deityText)
        { Number=number.ToString(CultureInfo.InvariantCulture); Name=name; Level=level; DeityName=deityName; DeityText=deityText; }
        internal string OriginalTitle(bool joined) { return "_P"+Number+"_'"+Name.ToUpper()+"'_DROPPED_"+(joined ? "IN_" : "OUT_"); }
        internal string OriginalBody(bool joined)
        {
            return joined ? "Joined as Player "+Number+" with alias: '"+Name+"'\n\nHero level: "+Level+"\nWorships: "+DeityName+"\n("+DeityText+")" : "Player "+Number+" ("+Name+") will be replaced with a bot.";
        }
        internal bool Matches(string title,string body,bool joined) { return title==OriginalTitle(joined) && body==OriginalBody(joined); }
        internal string Title(LanguagePack pack,bool joined)
        { return Format(pack,joined ? "@progression.join-title:{0}:{1}" : "@progression.leave-title:{0}:{1}",Number,Name.ToUpper()); }
        internal string Body(LanguagePack pack,bool joined)
        {
            return joined ? Format(pack,"@progression.join-body:{0}:{1}:{2}:{3}:{4}",Number,Name,Level,Term(pack,DeityName),Term(pack,DeityText)) : Format(pack,"@progression.leave-body:{0}:{1}",Number,Name);
        }
        private static string Term(LanguagePack pack,string source) { string value; return pack.TryTranslate(source,out value) ? value : source; }
        private static string Format(LanguagePack pack,string key,params object[] args)
        {
            string format;
            if (!pack.TryTranslate(key,out format)) return null;
            try { return string.Format(CultureInfo.InvariantCulture,format,args); }
            catch (FormatException) { return null; }
        }
    }
}
