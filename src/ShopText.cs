using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CrawlLocalization
{
    // Object identity matters: an unrelated mod string with equal text must not inherit a recipe.
    internal sealed class WeakIdentityMap<T> where T : class
    {
        private sealed class Entry { internal WeakReference Key; internal T Value; }
        private readonly Dictionary<int,List<Entry>> buckets=new Dictionary<int,List<Entry>>();
        private int writes;
        internal T Get(object key)
        {
            if (key==null) return null;
            lock(buckets)
            {
                List<Entry> entries;
                if (!buckets.TryGetValue(RuntimeHelpers.GetHashCode(key),out entries)) return null;
                foreach(Entry entry in entries) if (ReferenceEquals(entry.Key.Target,key)) return entry.Value;
                return null;
            }
        }
        internal void Set(object key,T value)
        {
            if (key==null) return;
            lock(buckets)
            {
                if (++writes%128==0)
                {
                    foreach(int hash in new List<int>(buckets.Keys))
                    {
                        buckets[hash].RemoveAll(delegate(Entry e) { return !e.Key.IsAlive; });
                        if(buckets[hash].Count==0) buckets.Remove(hash);
                    }
                }
                int id=RuntimeHelpers.GetHashCode(key);List<Entry> entries;
                if(!buckets.TryGetValue(id,out entries)) { entries=new List<Entry>();buckets.Add(id,entries); }
                foreach(Entry entry in entries) if(ReferenceEquals(entry.Key.Target,key)) { entry.Value=value;return; }
                entries.Add(new Entry { Key=new WeakReference(key),Value=value });
            }
        }
    }

    // Immutable trace of actual string operations, not a second implementation of weapon rules.
    internal sealed class ShopTextRecipe
    {
        internal readonly string Source;
        private readonly ShopTextRecipe[] parts;
        private readonly string format;
        private readonly object[] arguments;
        private readonly CultureInfo culture;
        private static readonly Regex Structural=new Regex(@"</?(?:b|i|size|color|material)(?:=[^>]*)?>|\{[0-9]+(?:[^}]*)?\}");
        internal ShopTextRecipe(string source,ShopTextRecipe[] parts,string format,object[] arguments)
        {
            // Never retain the weak-map key through its value (including identity Concat results).
            Source=new string((source ?? "").ToCharArray());this.parts=parts;this.format=format==null ? null : new string(format.ToCharArray());this.arguments=arguments;
            culture=CultureInfo.CurrentCulture;
        }
        private static string Term(LanguagePack pack,string source,bool format)
        {
            string value;
            if(pack.TryTranslate("@shop.il:"+source,out value) || pack.TryTranslate(source,out value)) return value;
            foreach(string prefix in new string[] { "@shop.prefix:","@shop.magic-name:","@shop.status:","@shop.magic:","@shop.stat:" })
                if(pack.TryTranslate(prefix+source,out value)) return value;
            if(!format && !source.EndsWith(".",StringComparison.Ordinal) && pack.TryTranslate(source+".",out value)) return value;
            string plain=Structural.Replace(source,"");
            foreach(char ch in plain) if(char.IsLetter(ch)) return null;
            return source;
        }
        internal string Translate(LanguagePack pack)
        {
            if(pack.Id=="en") return null;
            try { return Render(pack); } catch(FormatException) { return null; }
        }
        private string Render(LanguagePack pack)
        {
            string exact;
            if(pack.TryTranslate(Source,out exact)) return exact;
            if(parts!=null)
            {
                StringBuilder result=new StringBuilder();
                foreach(ShopTextRecipe part in parts) { string text=part.Render(pack);if(text==null) return null;result.Append(text); }
                return result.ToString();
            }
            if(format!=null)
            {
                string translated=Term(pack,format,true);if(translated==null) return null;
                object[] values=new object[arguments.Length];
                for(int i=0;i<values.Length;i++)
                {
                    ShopTextRecipe node=arguments[i] as ShopTextRecipe;
                    values[i]=node==null ? arguments[i] : node.Render(pack);
                    if(node!=null && values[i]==null) return null;
                }
                return string.Format(culture,translated,values);
            }
            return Term(pack,Source,false);
        }
    }

    internal static class ShopText
    {
        private sealed class BuilderTrace { internal readonly List<ShopTextRecipe> Parts=new List<ShopTextRecipe>(); internal bool Invalid; }
        private static readonly WeakIdentityMap<BuilderTrace> builders=new WeakIdentityMap<BuilderTrace>();
        private static readonly WeakIdentityMap<ShopTextRecipe> strings=new WeakIdentityMap<ShopTextRecipe>();
        internal static ShopTextRecipe Capture(string source)
        { return strings.Get(source) ?? new ShopTextRecipe(source,null,null,null); }
        private static ShopTextRecipe Sequence(string source,ShopTextRecipe[] parts)
        {
            StringBuilder original=new StringBuilder();foreach(ShopTextRecipe part in parts) original.Append(part.Source);
            // Missing/unsupported operations from another patch invalidate the whole trace.
            return original.ToString()==source ? new ShopTextRecipe(source,parts,null,null) : null;
        }
        private static void Remember(string source,ShopTextRecipe recipe)
        { if(!string.IsNullOrEmpty(source) && recipe!=null) strings.Set(source,recipe); }
        private static ShopTextRecipe FormatRecipe(string source,string format,object[] args)
        {
            object[] snapshot=new object[args.Length];
            for(int i=0;i<args.Length;i++)
            {
                object arg=args[i];
                if(arg is string) snapshot[i]=Capture((string)arg);
                else if(arg==null || arg.GetType().IsPrimitive || arg is decimal) snapshot[i]=arg;
                else return null; // Never invoke an arbitrary mod object's ToString again.
            }
            return new ShopTextRecipe(source,null,format,snapshot);
        }
        private static void Add(StringBuilder builder,ShopTextRecipe node)
        {
            BuilderTrace trace=builders.Get(builder);
            if(trace==null) return;
            if(node==null) trace.Invalid=true;else trace.Parts.Add(node);
        }
        internal static StringBuilder Create()
        { StringBuilder builder=new StringBuilder();builders.Set(builder,new BuilderTrace());return builder; }
        internal static StringBuilder Append(StringBuilder builder,string value)
        { builder.Append(value);try { Add(builder,Capture(value)); } catch(Exception) { Invalidate(builder); } return builder; }
        internal static StringBuilder AppendChar(StringBuilder builder,char value)
        { builder.Append(value);try { Add(builder,Capture(value.ToString())); } catch(Exception) { Invalidate(builder); } return builder; }
        private static void Invalidate(StringBuilder builder) { BuilderTrace trace=builders.Get(builder);if(trace!=null) trace.Invalid=true; }
        internal static StringBuilder AppendFormatOne(StringBuilder builder,string format,object arg)
        { return AppendFormatMany(builder,format,new object[] { arg }); }
        internal static StringBuilder AppendFormatTwo(StringBuilder builder,string format,object a,object b)
        { return AppendFormatMany(builder,format,new object[] { a,b }); }
        internal static StringBuilder AppendFormatMany(StringBuilder builder,string format,object[] args)
        {
            int start=builder.Length;builder.AppendFormat(format,args);
            try { Add(builder,FormatRecipe(builder.ToString(start,builder.Length-start),format,args)); } catch(Exception) { Invalidate(builder); }
            return builder;
        }
        internal static string Finish(StringBuilder builder)
        {
            string source=builder.ToString();
            RecordFinished(builder,source);
            return source;
        }
        internal static string ObjectString(object value)
        {
            string source=value.ToString();
            StringBuilder builder=value as StringBuilder;
            if(builder!=null) RecordFinished(builder,source);
            return source;
        }
        private static void RecordFinished(StringBuilder builder,string source)
        { try { BuilderTrace trace=builders.Get(builder);if(trace!=null && !trace.Invalid) Remember(source,Sequence(source,trace.Parts.ToArray())); } catch(Exception) { } }
        internal static string FormatOne(string format,object arg)
        {
            string source=string.Format(format,arg);
            try { Remember(source,FormatRecipe(source,format,new object[] { arg })); } catch(Exception) { }
            return source;
        }
        internal static string ConcatTwo(string a,string b) { return Join(string.Concat(a,b),new object[] { a,b }); }
        internal static string ConcatThree(string a,string b,string c) { return Join(string.Concat(a,b,c),new object[] { a,b,c }); }
        internal static string ConcatObjects(object a,object b,object c) { return Join(string.Concat(a,b,c),new object[] { a,b,c }); }
        internal static string ConcatArray(object[] args) { return Join(string.Concat(args),args); }
        private static string Join(string source,object[] args)
        {
            try
            {
                ShopTextRecipe[] nodes=new ShopTextRecipe[args.Length];
                for(int i=0;i<args.Length;i++)
                {
                    object arg=args[i];
                    if(arg!=null && !(arg is string) && !arg.GetType().IsPrimitive && !(arg is decimal)) return source;
                    nodes[i]=Capture(arg==null ? "" : Convert.ToString(arg,CultureInfo.CurrentCulture));
                }
                Remember(source,Sequence(source,nodes));
            }
            catch(Exception) { }
            return source;
        }
    }
}
