using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    [HarmonyPatch]
    internal static class ShopTextIlPatch
    {
        private static readonly Dictionary<MethodBase,MethodInfo> replacements=CreateMap();
        private static void Map(Dictionary<MethodBase,MethodInfo> map,Type type,string name,Type[] args,string replacement)
        {
            MethodBase original=name==".ctor" ? (MethodBase)type.GetConstructor(args) : type.GetMethod(name,args);
            map.Add(original,AccessTools.Method(typeof(ShopText),replacement));
        }
        private static Dictionary<MethodBase,MethodInfo> CreateMap()
        {
            Dictionary<MethodBase,MethodInfo> map=new Dictionary<MethodBase,MethodInfo>();
            Map(map,typeof(StringBuilder),".ctor",Type.EmptyTypes,"Create");
            Map(map,typeof(StringBuilder),"Append",new Type[] { typeof(string) },"Append");
            Map(map,typeof(StringBuilder),"Append",new Type[] { typeof(char) },"AppendChar");
            Map(map,typeof(StringBuilder),"AppendFormat",new Type[] { typeof(string),typeof(object) },"AppendFormatOne");
            Map(map,typeof(StringBuilder),"AppendFormat",new Type[] { typeof(string),typeof(object),typeof(object) },"AppendFormatTwo");
            Map(map,typeof(StringBuilder),"AppendFormat",new Type[] { typeof(string),typeof(object[]) },"AppendFormatMany");
            Map(map,typeof(StringBuilder),"ToString",Type.EmptyTypes,"Finish");
            Map(map,typeof(object),"ToString",Type.EmptyTypes,"ObjectString");
            Map(map,typeof(string),"Format",new Type[] { typeof(string),typeof(object) },"FormatOne");
            Map(map,typeof(string),"Concat",new Type[] { typeof(string),typeof(string) },"ConcatTwo");
            Map(map,typeof(string),"Concat",new Type[] { typeof(string),typeof(string),typeof(string) },"ConcatThree");
            Map(map,typeof(string),"Concat",new Type[] { typeof(object),typeof(object),typeof(object) },"ConcatObjects");
            Map(map,typeof(string),"Concat",new Type[] { typeof(object[]) },"ConcatArray");
            return map;
        }
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(WeaponData),"GetDisplayName");
            yield return AccessTools.Method(typeof(WeaponData),"GetDescription");
            yield return AccessTools.Method(typeof(PropPurchasable),"ShowInfoBox");
            yield return AccessTools.Method(typeof(GUIStore),"Show");
            yield return AccessTools.Method(typeof(GUIStoreStatsDisplay),"Show");
        }
        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,bool display)
        {
            foreach(CodeInstruction instruction in instructions)
            {
                MethodBase method=instruction.operand as MethodBase;MethodInfo replacement=null;
                if(method!=null && (instruction.opcode==OpCodes.Call || instruction.opcode==OpCodes.Callvirt || instruction.opcode==OpCodes.Newobj))
                {
                    replacements.TryGetValue(method,out replacement);
                    if(display && method.Equals(AccessTools.PropertySetter(typeof(TextMesh),"text"))) replacement=AccessTools.Method(typeof(ShopDisplay),"SetText");
                    if(display && method.Equals(AccessTools.Method(typeof(global::GUIText),"SetText"))) replacement=AccessTools.Method(typeof(ShopDisplay),"SetGuiText");
                }
                if(replacement==null) { yield return instruction;continue; }
                // Same stack signature; retain every original label and exception boundary.
                CodeInstruction changed=new CodeInstruction(instruction);changed.opcode=OpCodes.Call;changed.operand=replacement;
                yield return changed;
            }
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
        { return Rewrite(instructions,__originalMethod.DeclaringType==typeof(GUIStore) || __originalMethod.DeclaringType==typeof(GUIStoreStatsDisplay)); }
    }
}
