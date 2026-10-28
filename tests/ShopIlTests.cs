using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
// Isolated stand-ins. This test never loads or executes a Crawl method.
public class WeaponData { public void GetDisplayName() { } public void GetDescription() { } }
public class PropPurchasable { public void ShowInfoBox() { } }
public class GUIStore { public void Show() { } }
public class GUIStoreStatsDisplay { public void Show() { } }
public class GUIText { public void SetText(string text) { } public void SetWrapWidth(float width) { } }
namespace UnityEngine { public class TextMesh { public string text { get;set; } } }
namespace CrawlLocalization
{
    internal static class ShopDisplay
    {
        internal static void SetText(UnityEngine.TextMesh mesh,string source) { mesh.text=source; }
        internal static void SetGuiText(GUIText gui,string source) { gui.SetText(source); }
    }
    public sealed class ShopIlFixture
    {
        public int RandomCalls;
        public int Damage=27;
        public string Cache;
        private readonly Random random=new Random(638);
        [MethodImpl(MethodImplOptions.NoInlining)] private string Choose() { RandomCalls++;return new string[] { "Swift","Swift" }[random.Next(2)]; }
        [MethodImpl(MethodImplOptions.NoInlining)] public string Build()
        {
            if(Cache!=null) return Cache;
            StringBuilder b=new StringBuilder();b.Append(Choose());b.Append(' ');b.Append("Shortsword");
            Cache=b.ToString();return Cache;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public string Modded()
        {
            StringBuilder b=new StringBuilder();b.Append(Build());b.Append("\nMOD BONUS: kills explode");return b.ToString();
        }
    }
    public static class ShopIlTests
    {
        static int total;
        public static int CheckActual(string method,CodeInstruction[] original,bool display)
        {
            List<CodeInstruction> patched=new List<CodeInstruction>(ShopTextIlPatch.Rewrite(original,display));
            if(patched.Count!=original.Length) throw new Exception("Instruction count changed: "+method);
            int changed=0;
            for(int i=0;i<original.Length;i++)
            {
                if(ReferenceEquals(original[i],patched[i])) continue;
                MethodBase from=original[i].operand as MethodBase;MethodInfo to=patched[i].operand as MethodInfo;
                if(from==null || to==null || patched[i].opcode!=OpCodes.Call || (to.DeclaringType!=typeof(ShopText) && to.DeclaringType!=typeof(ShopDisplay))) throw new Exception("Unexpected IL replacement");
                List<Type> pop=new List<Type>();
                if(!from.IsStatic && !(from is ConstructorInfo)) pop.Add(from.DeclaringType);
                foreach(ParameterInfo p in from.GetParameters()) pop.Add(p.ParameterType);
                ParameterInfo[] input=to.GetParameters();if(input.Length!=pop.Count) throw new Exception("Stack arity mismatch");
                for(int n=0;n<input.Length;n++) if(input[n].ParameterType!=pop[n]) throw new Exception("Stack type mismatch");
                Type output=from is ConstructorInfo ? from.DeclaringType : ((MethodInfo)from).ReturnType;
                if(output!=to.ReturnType) throw new Exception("Stack result mismatch");
                changed++;
            }
            if(changed==0) throw new Exception("No supported operations found in "+method);
            total+=changed;Console.WriteLine("PASS actual IL: "+method+", "+changed+" stack-equivalent call replacements; other instructions retained");return changed;
        }
        public static void Execute(string root)
        {
            DynamicMethod labelHost=new DynamicMethod("labels",typeof(void),Type.EmptyTypes);Label label=labelHost.GetILGenerator().DefineLabel();
            CodeInstruction input=new CodeInstruction(OpCodes.Callvirt,typeof(StringBuilder).GetMethod("ToString",Type.EmptyTypes));input.labels.Add(label);input.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            CodeInstruction output=new List<CodeInstruction>(ShopTextIlPatch.Rewrite(new CodeInstruction[] { input },false))[0];
            if(output.labels.Count!=1 || !output.labels[0].Equals(label) || output.blocks.Count!=1 || input.opcode!=OpCodes.Callvirt) throw new Exception("Branch/exception metadata lost or original mutated");
            LanguagePack pack=LanguagePack.Load(root,"zh-Hans",delegate(string s) { throw new Exception(s); });
            ShopIlFixture baseline=new ShopIlFixture();string before=baseline.Build();
            Harmony harmony=new Harmony("crawl.localization.offline.shop-il");
            MethodInfo transpiler=typeof(ShopTextIlPatch).GetMethod("Transpiler",BindingFlags.NonPublic|BindingFlags.Static);
            try
            {
                harmony.Patch(typeof(ShopIlFixture).GetMethod("Build"),transpiler:new HarmonyMethod(transpiler));
                harmony.Patch(typeof(ShopIlFixture).GetMethod("Modded"),transpiler:new HarmonyMethod(transpiler));
                ShopIlFixture modified=new ShopIlFixture();string after=modified.Build();
                if(after!=before || modified.Cache!=baseline.Cache || modified.RandomCalls!=baseline.RandomCalls || modified.Damage!=baseline.Damage) throw new Exception("Original return/cache/random/game field changed");
                if(!ReferenceEquals(after,modified.Build()) || modified.RandomCalls!=1) throw new Exception("Cache path regenerated randomness");
                if(ShopText.Capture(after).Translate(pack)!="迅捷 短剑") throw new Exception("Patched method trace not translated");
                string modded=modified.Modded();if(!modded.EndsWith("MOD BONUS: kills explode") || ShopText.Capture(modded).Translate(pack)!=null) throw new Exception("Mod addition lost");
                if(ShopText.Capture(after).Translate(new LanguagePack { Id="en" })!=null || modified.RandomCalls!=1) throw new Exception("Language restore changed generator state");
                Console.WriteLine("PASS executed Harmony fixture: identical return, English cache, random call count and damage; cached path and mod additions preserved. Actual-game IL calls audited: "+total);
            }
            finally { harmony.UnpatchSelf(); }
        }
    }
}
