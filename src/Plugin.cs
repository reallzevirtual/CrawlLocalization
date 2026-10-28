using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CrawlLocalization
{
    [BepInPlugin(Id, "Crawl Localization", "0.2.2")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "crawl.localization";
        internal static Plugin Instance;
        internal LanguagePack Language;
        internal FontService Fonts;
        internal readonly HashSet<LocalizedMenuLabel> Labels = new HashSet<LocalizedMenuLabel>();
        private readonly HashSet<string> warnings = new HashSet<string>();
        private ConfigEntry<string> language;
        private ConfigEntry<KeyCode> reloadKey;
        private Harmony harmony;
        private string folder;

        private void Awake()
        {
            Instance = this;
            folder = Path.GetDirectoryName(Info.Location);
            language = Config.Bind("General", "Language", "zh-Hans", "Language folder: zh-Hans, zh-Hant, en. Aliases: zh-CN, zh-TW.");
            reloadKey = Config.Bind("General", "ReloadKey", KeyCode.F8, "Reload configuration and language tables. Restart to replace font files.");
            Fonts = new FontService(Warn, delegate(string message) { Logger.LogInfo(message); });
            LoadLanguage();
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
        }

        private void Update()
        {
            if (reloadKey.Value == KeyCode.None || !Input.GetKeyDown(reloadKey.Value)) return;
            try
            {
                Config.Reload();
                LoadLanguage();
                foreach (LocalizedMenuLabel label in new List<LocalizedMenuLabel>(Labels))
                    if (label != null) label.Refresh();
            }
            catch (Exception ex) { Warn("Reload failed: " + ex.Message); }
        }

        private void LoadLanguage()
        {
            LanguagePack next = LanguagePack.Load(folder, language.Value, Warn);
            Language = next;
            Logger.LogInfo("Language=" + next.Id + "; menu translations=" + next.Count + "; font mappings=" + next.Fonts.Count);
        }

        internal void Warn(string message)
        {
            if (warnings.Add(message)) Logger.LogWarning(message);
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            foreach (LocalizedMenuLabel label in new List<LocalizedMenuLabel>(Labels))
                if (label != null) { label.Restore(true); Destroy(label); }
            if (Fonts != null) Fonts.Dispose();
            Instance = null;
        }
    }
}
