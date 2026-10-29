using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    internal static class OnlineMirrorFonts { internal static void Install(Harmony h, Action<string> w) { } }
    internal static class LocalizedMenuLabel { internal static Func<string,Func<LanguagePack,string>> ExternalProvider; }
    internal static class OnlineOverlay
    {
        internal static void Label(string s,params GUILayoutOption[] o) { }
        internal static void Label(string s,GUIStyle st,params GUILayoutOption[] o) { }
        internal static bool Button(string s,params GUILayoutOption[] o) { return false; }
        internal static bool Toggle(bool v,string s,params GUILayoutOption[] o) { return v; }
        internal static bool Toggle(bool v,string s,GUIStyle st,params GUILayoutOption[] o) { return v; }
        internal static void GuiLabel(Rect r,string s,GUIStyle st) { }
        internal static Rect Window(int id,Rect r,GUI.WindowFunction fn,string s,params GUILayoutOption[] o) { return r; }
        internal static void SetText(TextMesh m,string s) { }
    }
    public static class OnlineFixture
    {
        public static string Cache;
        public static int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)] public static string Build(string userName)
        {
            Calls++;
            StringBuilder b=new StringBuilder();b.Append("Who can join \"");b.Append(userName);b.Append("\"?");
            Cache=b.ToString();return Cache;
        }
    }
    public static class OnlineIlTests
    {
        public static string[] MenuMethods { get { return OnlineIntegration.MenuMethods; } }
        public static string[] OverlayMethods { get { return OnlineIntegration.OverlayMethods; } }
        public static void Check(string name,CodeInstruction[] source)
        {
            List<CodeInstruction> result=new List<CodeInstruction>(OnlineIntegration.Transpiler(source));int n=0,markers=0,calls=0,sinks=0;
            foreach(CodeInstruction from in source)
            {
                CodeInstruction to=result[n++];MethodBase m=from.operand as MethodBase;
                if(from.opcode!=to.opcode || !object.Equals(from.operand,to.operand))
                {
                    MethodInfo wrap=to.operand as MethodInfo;if(m==null || wrap==null || to.opcode!=OpCodes.Call)throw new Exception("Unexpected instruction edit: "+name);
                    List<Type> inputs=new List<Type>();if(!m.IsStatic && !(m is ConstructorInfo)) inputs.Add(m.DeclaringType);
                    foreach(ParameterInfo p in m.GetParameters())inputs.Add(p.ParameterType);
                    ParameterInfo[] actual=wrap.GetParameters();if(inputs.Count!=actual.Length)throw new Exception("Stack count");
                    for(int i=0;i<actual.Length;i++)if(inputs[i]!=actual[i].ParameterType)throw new Exception("Stack type");
                    Type output=m is ConstructorInfo ? m.DeclaringType : ((MethodInfo)m).ReturnType;
                    if(output!=wrap.ReturnType)throw new Exception("Stack return");calls++;
                    if(wrap.DeclaringType==typeof(OnlineOverlay))sinks++;
                }
                else if(m!=null && (m.DeclaringType==typeof(GUILayout) || m.DeclaringType==typeof(GUI)) && (m.Name=="Label" || m.Name=="Button" || m.Name=="Toggle" || m.Name=="Window"))
                    throw new Exception("Uncovered GUI text sink: "+m);
                while(n<result.Count && result[n].operand is MethodInfo && ((MethodInfo)result[n].operand).DeclaringType==typeof(OnlineText) && (((MethodInfo)result[n].operand).Name=="Literal" || ((MethodInfo)result[n].operand).Name=="Message" || ((MethodInfo)result[n].operand).Name=="Roster")) { markers++;n++; }
            }
            if(n!=result.Count)throw new Exception("Instruction alignment");
            Console.WriteLine("PASS IL "+name+": "+calls+" equivalent calls, "+markers+" source markers, "+sinks+" display sinks");
        }
        public static void Execute(string root)
        {
            LanguagePack zh=LanguagePack.Load(root,"zh-Hans",delegate(string s){throw new Exception(s);});
            string before=OnlineFixture.Build("Public");int beforeCalls=OnlineFixture.Calls;
            Harmony h=new Harmony("crawl.localization.offline.online");
            try
            {
                h.Patch(typeof(OnlineFixture).GetMethod("Build"),transpiler:new HarmonyMethod(typeof(OnlineIntegration),"Transpiler"));
                string after=OnlineFixture.Build("Public");
                if(after!=before || OnlineFixture.Cache!=before || OnlineFixture.Calls!=beforeCalls+1)throw new Exception("Fixture state altered");
                if(OnlineText.Find(after)==null || OnlineText.Find(after).Render(zh)!="谁可以加入 \"Public\"？")throw new Exception("Fixture translation/name protection failed");
                Console.WriteLine("PASS executed Harmony UI fixture: original output/cache/call count preserved; user name untouched.");
            }
            finally { h.UnpatchSelf(); }
        }
    }
}
