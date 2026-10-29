# Crawl Localization — language authoring kit

Create a language pack for Crawl and the optional CrawlOnline UI. This kit contains the plugin DLL, original English source tables, an AI skill and PowerShell tools. Both `Fonts/crawl-multilingual.bundle` (Latin/Cyrillic) and `Fonts/crawl-chinese-fonts.bundle` are included; no translated language pack is included. No compilation or Unity installation is needed for text/config edits.

## Create another language

Ask your AI to read `crawl-language-pack/SKILL.md` and specify the target language, or run this from the extracted package (Spanish example):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

Use another tag such as `ru`, `fr` or `de` as needed. The script creates `Languages/es/` with 1,488 original-English entries and `@TODO` targets. It refuses to overwrite an existing folder.

1. Back up `crawl-language-pack/catalog/` and the generated language folder before editing. Keep the English catalog unchanged.
2. Translate column 2 of `Languages/es/*translations.tsv` directly from column 1's English text. Column 3 contains English context and resource locations. Some first-column values have namespaces such as `@online:` or `@shop.il:`; see the skill for those keys.
3. Use UTF-8 raw tab-separated text. Preserve keys, placeholders, tags, escapes and significant spaces. Do not apply CSV quote escaping. Leave unfinished targets as `@TODO` to retain original game/mod English.
4. Compare the result with the backup and run the checker:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
```

Add `-RequireComplete` when all entries should be translated. The checker uses the independent English catalog, not the edited language.

## Try and share your language

Crawl for Windows requires BepInEx 5 (32-bit). With the game closed, copy `CrawlLocalization.dll` and your `Languages/es/` folder to `Crawl/BepInEx/plugins/CrawlLocalization/`. Copy the included `Fonts/` directory too (both bundles and license/notice files). Authoring tools and the catalog are not runtime files.

Set the following in the existing `Crawl/BepInEx/config/crawl.localization.cfg` (created on the plugin's first normal launch), preserving unrelated settings:

```ini
[General]
Language = es
ReloadKey = F8
```

Select your language explicitly; the plugin's default setting does not automatically select a newly created folder. Press F8 to reload config/tables, or restart. `Language = en` displays original game text.

Templates map all ten game font styles to `Fonts/crawl-multilingual.bundle`, retaining original English glyphs and adding Pixelify Sans glyphs sized for each style. Russian letters (including Ё/ё) and Spanish accents are checked. This is not an all-Unicode font; unsupported scripts still need their own fonts. If the bundle is unavailable or lacks a character, the plugin tries the original font and then installed Arial/Segoe UI; if those also fail, it keeps the original English label. Adjust `fonts.tsv` and `language.ini` if needed. The included Chinese font is an additional resource, not an automatic fallback for player names. Restart after replacing font files. Check font coverage, layout and controls in-game; text baked into images requires separate artwork changes.

For players with a compatible plugin, distribute your language folder and any referenced fonts; otherwise include the DLL too. Tell recipients the required plugin version and language setting. Diagnostic log: `Crawl/BepInEx/LogOutput.log`.
