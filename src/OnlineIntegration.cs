using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    // Optional adapter: the base plugin has no assembly reference to CrawlOnline.
    internal static class OnlineIntegration
    {
        internal static readonly string[] MenuMethods = { "ItemText", "ShowPlayOnline", "ShowHostType", "ShowServerList", "ShowServerListResults", "JoinEntry", "ShowLobby", "ConfirmCloseLobby", "ShowError", "LobbyText", "UpdatePanel" };
        internal static readonly string[] OverlayMethods = { "Draw", "StatusLine", "DrawWindow", "DrawIdle", "DrawHost", "DrawClient" };
        internal static void Install(Harmony harmony, Action<string> warn)
        {
            Type menu = AccessTools.TypeByName("CrawlOnline.UI.OnlineMenu");
            if (menu == null) return;
            LocalizedMenuLabel.ExternalProvider = Provider;
            Type overlay = AccessTools.TypeByName("CrawlOnline.UI.Overlay");
            foreach (Type type in new Type[] { menu, overlay })
            {
                if (type == null) continue;
                foreach (string name in type == menu ? MenuMethods : OverlayMethods)
                {
                    MethodInfo method = AccessTools.Method(type, name);
                    if (method == null) { warn("CrawlOnline UI method unavailable: " + type.Name + "." + name); continue; }
                    try { harmony.Patch(method, transpiler: new HarmonyMethod(typeof(OnlineIntegration), "Transpiler")); }
                    catch (Exception ex) { warn("CrawlOnline localization unavailable for " + name + ": " + ex.Message); }
                }
            }
            MethodInfo error = AccessTools.Method(menu, "ShowError");
            if (error != null)
            {
                try { harmony.Patch(error, prefix: new HarmonyMethod(typeof(OnlineIntegration), "ErrorPrefix")); }
                catch (Exception ex) { warn("CrawlOnline error-message localization unavailable: " + ex.Message); }
            }
        }
        internal static Func<LanguagePack,string> Provider(string source)
        {
            OnlineRecipe recipe = OnlineText.Find(source);
            return recipe == null ? null : (Func<LanguagePack,string>)delegate(LanguagePack p) { return p.Id == "en" ? null : recipe.Render(p); };
        }
        private static void ErrorPrefix(ref string message) { message = OnlineText.Message(message); }
        private static MethodInfo Wrapper(MethodBase method)
        {
            Type type = method.DeclaringType;
            string name = null;
            ParameterInfo[] p = method.GetParameters();
            if (type == typeof(StringBuilder))
            {
                if (method is ConstructorInfo && p.Length == 0) name = "Create";
                if (method.Name == "Append" && p.Length == 1) name = p[0].ParameterType == typeof(string) ? "Append" : p[0].ParameterType == typeof(char) ? "AppendChar" : null;
                if (method.Name == "ToString" && p.Length == 0) name = "Finish";
            }
            if (type == typeof(object) && method.Name == "ToString" && p.Length == 0) name = "ObjectString";
            if (type == typeof(string) && method.Name == "Concat")
            {
                if (p.Length == 1) name = p[0].ParameterType == typeof(string[]) ? "ConcatStrings" : p[0].ParameterType == typeof(object[]) ? "ConcatArray" : null;
                else if (p[0].ParameterType == typeof(string)) name = p.Length == 2 ? "ConcatTwo" : p.Length == 3 ? "ConcatThree" : p.Length == 4 ? "ConcatFour" : null;
                else if (p.Length == 3) name = "ConcatObjects";
            }
            if (name != null) return AccessTools.Method(typeof(OnlineText), name);
            if (type == typeof(TextMesh) && method.Name == "set_text") return AccessTools.Method(typeof(OnlineOverlay), "SetText");
            if (type == typeof(GUILayout) || type == typeof(GUI))
            {
                Type[] args = new Type[p.Length]; for (int i=0;i<p.Length;i++) args[i]=p[i].ParameterType;
                return typeof(OnlineOverlay).GetMethod((type == typeof(GUI) ? "Gui" : "") + method.Name, BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic, null, args, null);
            }
            return null;
        }
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction original in instructions)
            {
                CodeInstruction code = new CodeInstruction(original);
                MethodBase method = code.operand as MethodBase;
                if (method != null && (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt || code.opcode == OpCodes.Newobj))
                {
                    MethodInfo replacement = Wrapper(method);
                    if (replacement != null) { code.opcode = OpCodes.Call; code.operand = replacement; }
                }
                yield return code;
                string marker = null;
                if (original.opcode == OpCodes.Ldstr) marker = "Literal";
                FieldInfo field = original.operand as FieldInfo;
                if (field != null && field.FieldType == typeof(string) && (original.opcode == OpCodes.Ldfld || original.opcode == OpCodes.Ldsfld))
                {
                    string owner = field.DeclaringType.FullName;
                    if ((owner == "CrawlOnline.Online" && (field.Name == "LastError" || field.Name == "Status")) || (owner == "CrawlOnline.Net.LobbyBrowser" && field.Name == "Error")) marker = "Message";
                    if (owner == "CrawlOnline.Net.LobbyEntry" && field.Name == "State") marker = "Literal";
                }
                if (method != null && method.DeclaringType.FullName == "CrawlOnline.Net.SteamLobby" && method.Name == "TypeName") marker = "Literal";
                if (method != null && method.DeclaringType.FullName == "CrawlOnline.Net.LobbyBrowser" && method.Name == "get_Error") marker = "Message";
                if (method != null && method.DeclaringType == typeof(List<string>.Enumerator) && method.Name == "get_Current") marker = "Roster";
                if (marker != null) yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OnlineText), marker));
            }
        }
    }
}
