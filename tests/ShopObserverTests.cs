using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
// These minimal types prevent loading or executing game code during observer tests.
public class WeaponData { }
public class Constants { public string[] m_weaponMagicDescription; }
namespace CrawlLocalization
{
    internal class Plugin { internal static Plugin Instance; internal void Warn(string message) { } }
    public static class ShopObserverTests
    {
        public static void Run(CodeInstruction[] actual)
        {
            MethodInfo transpile = typeof(ShopMagicChoicePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            List<CodeInstruction> result = new List<CodeInstruction>((IEnumerable<CodeInstruction>)transpile.Invoke(null, new object[] { actual }));
            if (result.Count != actual.Length + 2) throw new Exception("Expected exactly two observer instructions on actual game IL");
            int added = -1;
            for (int i = 0; i < actual.Length; i++)
            {
                if (ReferenceEquals(actual[i], result[i])) continue;
                added = i; break;
            }
            if (added < 1 || actual[added-1].opcode != OpCodes.Ldelem_Ref || result[added].opcode != OpCodes.Ldarg_0 || result[added+1].opcode != OpCodes.Call || ((MethodInfo)result[added+1].operand).Name != "Record") throw new Exception("Observer is not immediately after chosen format");
            for (int i = 0; i < actual.Length; i++)
                if (!ReferenceEquals(actual[i], result[i < added ? i : i+2])) throw new Exception("Original instruction changed");
            foreach (CodeInstruction[] unmatched in new CodeInstruction[][] { new CodeInstruction[0], new CodeInstruction[] { new CodeInstruction(OpCodes.Nop) } })
            {
                List<CodeInstruction> untouched = new List<CodeInstruction>((IEnumerable<CodeInstruction>)transpile.Invoke(null,new object[] { unmatched }));
                if (untouched.Count != unmatched.Length) throw new Exception("Unmatched IL was changed");
            }
            WeaponData a = new WeaponData(), b = new WeaponData();
            string first = new string(new char[] { 'a', '{', '0', '}' });
            if (!ReferenceEquals(first, ShopMagicChoice.Record(first,a)) || ShopMagicChoice.Get(a) != first || ShopMagicChoice.Get(b) != null) throw new Exception("Choice observation changed return or crossed weapons");
            ShopMagicChoice.Record("second",b); ShopMagicChoice.Record("updated",a);
            if (ShopMagicChoice.Get(a) != "updated" || ShopMagicChoice.Get(b) != "second") throw new Exception("Choice update leaked");
            Console.WriteLine("PASS: actual WeaponData.GetDescription IL retained in order, including random calls/cache writes; only identity observer inserted. Unmatched IL and independent choice records verified.");
        }
    }
}
