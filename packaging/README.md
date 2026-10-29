# Crawl Localization

Local display translation for Crawl, with optional CrawlOnline UI support.
This package includes the available non-Chinese language packs and
`crawl-multilingual.bundle` (Latin/Cyrillic).
The Chinese translation pack is distributed separately.

## Install

Requires Crawl for Windows and BepInEx 5 (32-bit).
Close the game and extract this package into:

`Crawl/BepInEx/plugins/CrawlLocalization/`

Keep `CrawlLocalization.dll`, `Languages/` and `Fonts/` together, including the
font license and notice files. Back up your edited language files before updating.

## Select a language

The plugin creates `Crawl/BepInEx/config/crawl.localization.cfg` on its first
normal launch. Set the language to a folder included under `Languages/`:

```ini
[General]
Language = es
ReloadKey = F8
```

Use `es` for Spanish, `ru` for Russian, or `en` for original game text.
Select your language explicitly; installing the package does not change an
existing configuration. Missing packs or translations keep the original English.

Press F8 after editing the configuration or translations. Restart after installing
or replacing font files. No system font installation or Unity Editor is needed.
Text baked into images is not translated by these language tables.
For mixed Chinese/Latin/Cyrillic player names, install the separate optional
`-CombinedFonts.zip` addon and restart. Remove its `crawl-combined.bundle` to
restore normal fonts. Player names remain unchanged.

## Create another language

Use the separate `-Translator.zip` package. It contains an original-English
catalog, template/checking scripts, the multilingual font bundle and an AI skill.
Read its README or ask your AI to read `crawl-language-pack/SKILL.md`.

Diagnostic log: `Crawl/BepInEx/LogOutput.log`.
