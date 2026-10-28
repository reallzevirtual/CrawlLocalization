using HarmonyLib;
using UnityEngine;
namespace CrawlLocalization
{
    internal static class ShopDisplay
    {
        private static LocalizedMenuLabel Bind(TextMesh mesh,string source)
        {
            if(mesh==null || Plugin.Instance==null) return null;
            ShopTextRecipe recipe=ShopText.Capture(source);
            LocalizedMenuLabel label=mesh.GetComponent<LocalizedMenuLabel>();
            // Do not replace fonts on unchanged numeric/structural labels.
            bool hasLetters=false;
            if(source!=null) foreach(char ch in source) if(char.IsLetter(ch)) { hasLetters=true;break; }
            if(label==null && !hasLetters) return null;
            if(label==null) label=LocalizedMenuLabel.GetOrCreate(mesh.gameObject);
            label.TextProvider=delegate(LanguagePack pack) { return recipe.Translate(pack); };
            label.ResetSource(source);
            return label;
        }
        internal static void SetText(TextMesh mesh,string source)
        {
            mesh.text=source;
            LocalizedMenuLabel label=Bind(mesh,source);
            if(label!=null) label.Refresh();
        }
        internal static void SetGuiText(global::GUIText gui,string source)
        {
            Bind(gui.GetComponent<TextMesh>(),source);
            gui.SetText(source);
        }
    }
}
