using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CrawlLocalization
{
    // Observe the existing random choice, returning the identical format to the game.
    // Weak keys avoid retaining weapons that have left the game.
    internal static class ShopMagicChoice
    {
        private sealed class Entry { internal WeakReference Weapon; internal string Format; }
        private static readonly List<Entry> choices = new List<Entry>();
        internal static string Record(string format, WeaponData weapon)
        {
            for (int i = choices.Count - 1; i >= 0; i--)
            {
                object target = choices[i].Weapon.Target;
                if (target == null) choices.RemoveAt(i);
                else if (ReferenceEquals(target, weapon)) { choices[i].Format = format; return format; }
            }
            choices.Add(new Entry { Weapon = new WeakReference(weapon), Format = format });
            return format;
        }
        internal static string Get(WeaponData weapon)
        {
            foreach (Entry entry in choices) if (ReferenceEquals(entry.Weapon.Target, weapon)) return entry.Format;
            return null;
        }
    }

    [HarmonyPatch(typeof(WeaponData), "GetDescription")]
    internal static class ShopMagicChoicePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            FieldInfo formats = AccessTools.Field(typeof(Constants), "m_weaponMagicDescription");
            int selected = -1, matches = 0;
            for (int i = 0; i < code.Count; i++)
            {
                if (!code[i].LoadsField(formats)) continue;
                for (int j = i + 1; j < Math.Min(i + 16, code.Count); j++)
                {
                    if (code[j].opcode != OpCodes.Ldelem_Ref) continue;
                    if (selected != j) { selected = j; matches++; }
                    break;
                }
            }
            if (matches != 1)
            {
                if (Plugin.Instance != null) Plugin.Instance.Warn("Cannot observe weapon magic format; preserving original descriptions where needed.");
                return code;
            }
            code.InsertRange(selected + 1, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShopMagicChoice), "Record")) });
            return code;
        }
    }
}
