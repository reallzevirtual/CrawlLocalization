# Crawl Localization

A **BepInEx 5** localization plugin for **Crawl on Windows (Unity 5.4.2)**. Available language packs include Simplified Chinese, Spanish, and Russian. Additional languages can be created by editing external translation tables and font mappings, without rebuilding the plugin.

The plugin also adapts the menus and text rendering of **CrawlOnline 0.4.0**. **During online play, mirrored game text follows the host's language. Different clients cannot currently select independent languages for that text.** See [CrawlOnline limitations](#crawlonline-limitations) before organizing a session with players using different languages.

The current version is defined in [VERSION](VERSION). The plugin patches display code in memory; installation does not require editing game assets or DLLs on disk. Game identifiers, save data, random choices, and interaction callbacks are preserved.

[Changelog](CHANGELOG.md) · [Language pack guide / AI skill](translator-kit/crawl-language-pack/SKILL.md) · [Image-based text inventory](docs/image-text.md) · [Testing and extraction](docs/testing.md)

## Supported content

- Main menu, settings, keyboard bindings, Vault entries, and achievements.
- God selection, challenges, opening tips, chapter numbers, and floor names.
- Shop weapons, abilities, relics, potions, purchase prompts, and the equipment panel.
- Gameplay interactions, pause confirmations, monster upgrades, boss introductions, and results.
- Prologue tutorial messages and control prompts.
- CrawlOnline menus, server list, lobby, and F1 overlay when the mod is installed.

Random weapon names and descriptions retain the game's composition order and translate the individual fragments. Unknown or unsupported content falls back to its source text. Player names, room names, addresses, version numbers, and game values are not translated as ordinary catalog entries.

Coverage is limited to adapted display paths; the plugin does not automatically translate every TextMesh, MonoBehaviour, or third-party mod. Text baked into images, including `PROLOGUE`, `SALVATION`, and some combat feedback, remains unchanged. See the [image-text inventory](docs/image-text.md). Updates to CrawlOnline or mods that change the same methods may require additional compatibility work.

## Install or update

Requires Crawl for Windows and **BepInEx 5 (32-bit)**. Players do not need a .NET SDK, Python, or Unity Editor.

1. Install BepInEx and confirm that it loads successfully.
2. Close the game. Choose the default player ZIP for non-Chinese languages, or the separate `-zh-Hans.zip` for Simplified Chinese.
3. Extract the package into `Crawl/BepInEx/plugins/CrawlLocalization/`.
4. Launch the game to create the configuration, then select an installed language as described below. Existing configuration is not overwritten automatically.

For example, the default player package contains:

```text
Crawl/
└─ BepInEx/
   └─ plugins/
      └─ CrawlLocalization/
         ├─ README.md
         ├─ CrawlLocalization.dll
         ├─ Languages/
         │  ├─ es/
         │  └─ ru/
         └─ Fonts/
            ├─ crawl-multilingual.bundle
            └─ (font license and notice files)
```

The Chinese package instead contains `Languages/zh-Hans/` and `Fonts/crawl-chinese-fonts.bundle`. Each language folder includes translation tables, `language.ini`, `fonts.tsv`, and `textmesh-paths.txt`; keep these runtime files together.

Back up any edited language files before updating. Replace the DLL, language files, and required fonts, then restart. Avoid duplicate plugin DLLs or an extra nested extraction directory. CrawlOnline is optional and is not included in these packages.

To uninstall, close the game and remove the `CrawlLocalization` plugin folder. You may also remove `BepInEx/config/crawl.localization.cfg`.

## Configuration

Configuration file: `Crawl/BepInEx/config/crawl.localization.cfg`.

```ini
[General]
Language = es
ReloadKey = F8
```

| Value | Behavior |
| --- | --- |
| `es`, `ru`, or another language tag | Load the matching folder under `Languages/` |
| `zh-Hans` or `zh-CN` | Load the Simplified Chinese pack |
| `zh-Hant` or `zh-TW` | Load a separately installed Traditional Chinese pack; not included by default |
| `en` | Keep original game/mod text locally; see the online limitation below |

A fresh configuration defaults to `zh-Hans`; users of the non-Chinese package must select their language explicitly. Language packs are independent: missing entries, `@TODO` targets, or missing packs retain the original game/mod English instead of falling back to Chinese. This local fallback does not convert translated text received from an online host back to English. Original-text mode also cannot undo manually modified game assets.

Press **F8** after editing the configuration or translation tables to reload bound labels. Set `ReloadKey = None` to disable the shortcut. Restart after installing or replacing font files.

The replay-tutorial option may indicate that it is enabled but unavailable in the current mode. This reflects the existing game/CrawlOnline tutorial restrictions.

## CrawlOnline limitations

**The host's language determines the game text seen by connected clients.** CrawlOnline 0.4.0 captures the host's rendered scene, including final TextMesh strings and layout parameters, and reconstructs it on each client. This applies to mirrored game menus, shop descriptions, prompts, and similar scene text, not just player names.

For example, a Spanish-language host sends Spanish weapon names and descriptions. A client configured for Chinese still receives and displays those Spanish strings. The client's own connection UI and F1 overlay can use its local language, but the mirrored game text is not translated again.

Since 0.14.4, clients adapt mirrored text to their current language pack's fonts and local font materials without requiring the combined-font addon. Received text and layout are retained, and language-pack scaling is not applied a second time. For a session using the same supported language, each player should install the current plugin and the corresponding language/font pack.

### Recommendation for players using different languages

**For now, use the original English game text when playing together.** Set `Language = en` on the host and every client and restart, or disable this localization plugin for that session. Use unmodified English game assets. Changing only a client's language to English cannot override translated strings sent by the host.

The optional combined-font addon can display a wider range of characters, such as mixed Chinese, Latin, and Cyrillic names. **It does not translate between languages or enable independent client languages.**

### Planned development

A separate development branch is planned for localization specifically supporting players who use different languages in the same CrawlOnline session. The intended approach is to provide original text or translation identifiers and dynamic parameters alongside the display information, allowing each supported client to translate and lay out text in its own language while preserving player names.

This is planned work, not a feature of the current release. Protocol compatibility, clients without localization support, and missing translations will need explicit fallback behavior. No release date is committed.

## Create another language

Use `CrawlLocalization-<version>-Translator.zip`. It includes the DLL, an independent original-English catalog, the multilingual font bundle, `crawl-language-pack/SKILL.md`, and creation/checking scripts. It contains no Chinese translations or Chinese reference comments.

Back up the English catalog and your generated language folder before editing. Ask an AI to read the skill and specify the target language, or run this inside the extracted translator package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

The script creates `Languages/es/` with all entries from the canonical English catalog. Translate column 2 from the English in column 1; column 3 provides English context. Leave unfinished targets as `@TODO`. Four legacy input aliases for manually modified Chinese assets are intentionally excluded from new templates.

Tables are **UTF-8 raw TSV**, not quote-aware CSV. Preserve source keys, placeholders, tags, `[A]`/`[B]` markers, escapes, and significant spaces. Do not add wrapping quotes or double existing quotes. Compare the result with your backup and validate it:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
```

Add `-RequireComplete` when the pack should be fully translated. The checker uses the independent `catalog/` beside its scripts; keep this English baseline unchanged during translation. In a source checkout, the scripts are under `translator-kit/crawl-language-pack/scripts/`.

Set `Language = es`, then press F8 or restart. The template maps the ten game font styles to `Fonts/crawl-multilingual.bundle`. Russian letters and Spanish accents have been checked; other scripts and layouts require suitable fonts and in-game verification. Chinese-specific wrapping and punctuation rules do not apply to other languages.

No DLL rebuild is needed to add translations. Distribute the language directory, required fonts and notices, and instructions for selecting the language. Retain `language.ini`, `fonts.tsv`, and `textmesh-paths.txt`. Detailed formats, backup procedures, and font configuration are in the [language pack skill](translator-kit/crawl-language-pack/SKILL.md).

## Build and package

Requirements:

- **PowerShell 7** and a **.NET SDK** providing the C# compiler.
- A local Crawl installation and the BepInEx 5 / Harmony assemblies under its `BepInEx/core/` directory.
- The prebuilt font bundles and associated notices included in this repository. Unity Editor is not required to package them.

Builds use the game's Mono/Unity reference assemblies and do not require NuGet. Run from the repository root, supplying your game path as needed:

```powershell
# All non-Chinese packs (currently es and ru), plus the translator kit.
pwsh -NoProfile -File .\build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Crawl'

# Simplified Chinese only, with the Chinese font bundle.
pwsh -NoProfile -File .\build.ps1 -Languages zh-Hans

# A selected non-Chinese pack, plus the translator kit.
pwsh -NoProfile -File .\build.ps1 -Languages ru

# Package the existing optional combined font separately.
pwsh -NoProfile -File .\package-combined-fonts.ps1
```

The normal build runs offline checks, compiles the DLL, and creates ZIPs. It does not install the plugin or launch the game.

| Output under `artifacts/` | Contents |
| --- | --- |
| `CrawlLocalization-<version>.zip` | All non-Chinese language packs, their mapped fonts, and an English player README |
| `CrawlLocalization-<version>-zh-Hans.zip` | Simplified Chinese only, the Chinese font bundle, and a Chinese player README |
| `CrawlLocalization-<version>-ru.zip` | An explicitly selected language pack; the suffix reflects the selection |
| `CrawlLocalization-<version>-Translator.zip` | English source catalog, authoring tools, DLL, and multilingual font bundle; no translated packs |
| `CrawlLocalization-<version>-CombinedFonts.zip` | Optional combined font, notices, and instructions; no DLL or translations |

Normal player builds include the fonts referenced by the selected language mappings. The current Chinese package contains only the Chinese bundle; the current non-Chinese package and translator kit contain only the multilingual bundle. The combined addon is never included automatically.

To select multiple non-Chinese packs in PowerShell 7, use `.\build.ps1 -Languages @('es', 'ru')`. Chinese and non-Chinese packs must be built separately. A Chinese build does not generate or replace the translator kit.

Staging folders have the same names as their player ZIPs without the extension. Player packages exclude source code, `docs/`, tests, and source TTFs. Package README files are copied from [English player instructions](packaging/README.md), [Chinese player instructions](packaging/zh-Hans/README.md), and [English authoring instructions](packaging/translator/README.md).

Use `-Install` only when you want to install the build into the specified game directory, with the game closed. It backs up the existing plugin folder under `artifacts/backups/` before copying files and does not launch the game. `-SkipTests` skips offline checks; normal builds should keep them enabled.

## Versioning and maintenance

Edit the single version line in [VERSION](VERSION), then build normally. Use `major.minor.patch`, without a `v` prefix or prerelease suffix; each component must be between 0 and 65534. Translation corrections generally increment the patch version.

The build derives the BepInEx plugin version, four-part assembly/file version, staging directory names, and ZIP names from that value. Generated version source stays under `artifacts/build/`. Builds do not increment the version automatically; rebuilding a version overwrites its corresponding outputs. Record release changes in [CHANGELOG.md](CHANGELOG.md) and commit them with `VERSION` and the related changes.

Maintainers add supported source keys and English context to `translator-kit/crawl-language-pack/catalog/*translations.tsv`, keeping **every target cell `@TODO`**, and update runtime adapters where necessary. This is the single source catalog; do not regenerate it from Chinese or another translation. Translators edit their target pack, not this baseline.

Synchronize existing languages from the repository root:

```powershell
# Preview missing keys and report extra/duplicate/misplaced keys across all languages.
powershell -NoProfile -ExecutionPolicy Bypass -File .\translator-kit\crawl-language-pack\scripts\sync-languages.ps1 -PluginRoot .
# Back up affected language folders and the catalog, then append missing @TODO rows.
powershell -NoProfile -ExecutionPolicy Bypass -File .\translator-kit\crawl-language-pack\scripts\sync-languages.ps1 -PluginRoot . -Apply
# Read-only check; fails on missing keys or conflicts. Pending @TODO rows are allowed.
powershell -NoProfile -ExecutionPolicy Bypass -File .\translator-kit\crawl-language-pack\scripts\sync-languages.ps1 -PluginRoot . -Check
```

Use `-Languages es` to select one language; omit it for all initialized languages. Existing translations, comments, literal quotes, whitespace and deliberate empty targets remain untouched. Missing tables are created; obsolete or unknown rows are reported, never deleted or automatically promoted into the catalog. Resolve conflicting keys before applying any additions. A key rename/removal requires a reviewed migration, not automatic matching by row position.

Backups go to `authoring-backups/sync-<timestamp>-<id>/` by default; `-BackupRoot` can select another durable location outside the catalog and `Languages/`. They contain the pre-sync catalog and affected language folders. No-change runs create no backup. After synchronization, use `check-language.ps1` for translation/placeholder/font validation, and `-RequireComplete` when no `@TODO` is acceptable. Standard tests check catalog alignment for all languages; builds check selected languages even with `-SkipTests`. Neither check modifies files. `artifacts/` contains reproducible outputs and is excluded from Git.

## Fonts

For ordinary local translations, selection proceeds through the language's font mapping, the game's original font, and configured system fallback families. If no suitable font can render the translated label, the original text is retained. There is no automatic fallback between the Chinese and multilingual bundles, and no `.coverage.tsv` metadata is required.

For mixed-script names, install the separate `-CombinedFonts.zip` into the plugin directory and restart. When `Fonts/crawl-combined.bundle` is present, the plugin prefers its matching font styles. Remove that bundle and restart to restore normal font selection. This changes character coverage, not the online language limitation described above.

The combined fonts preserve the existing glyphs and layout metrics while adding Chinese, Latin, and Cyrillic coverage. Coverage is not guaranteed for every rare character, emoji, or complex script. Higher-resolution Han rendering uses compensating geometry scaling; it does not enable Chinese wrapping rules for other languages.

Rebuilding font bundles requires an activated **Unity 5.4.2f2** editor, Python, UnityPy, and fontTools as required by the scripts:

```powershell
# Rebuild the standard Chinese bundle.
pwsh -NoProfile -File .\build-fonts.ps1 -UnityPath 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe'

# Rebuild the optional combined bundle.
pwsh -NoProfile -File .\build-fonts.ps1 -Combined

# Rebuild the multilingual bundle independently.
pwsh -NoProfile -File .\build-multilingual-fonts.ps1
```

See the [multilingual font notes](Fonts/Multilingual/README.md) and [font project instructions](FontBundleProject/README.md) for generation, layout preservation, and old-editor compatibility. Do not upgrade the font project with a newer Unity version.

## Project layout and verification

| Directory | Purpose |
| --- | --- |
| `src/` | Plugin, display adapters, translation and font services |
| `Languages/` | Language tables and runtime configuration |
| `Fonts/`, `FontBundleProject/` | Font sources, bundles, and Unity font project |
| `translator-kit/` | English catalog, AI skill, creation, synchronization and validation scripts |
| `packaging/` | Instructions copied into release packages |
| `tests/`, `tools/` | Offline checks, isolated Unity validation, and asset extraction |
| `docs/` | Image-text inventory and maintenance/testing notes |
| `artifacts/` | Ignored build outputs, logs, and generated files |

Run routine offline checks with:

```powershell
pwsh -NoProfile -File .\tests\test.ps1
```

Choose additional checks based on the change; see [testing and extraction](docs/testing.md). Isolated Unity tests do not replace real gameplay, interaction, animation, or multiplayer testing.

## Troubleshooting

- **No translations:** check the DLL location, selected language folder, and source keys. Some image-based text is intentionally unchanged.
- **Unexpected language online:** mirrored game text follows the host. Selecting another language on the client does not translate it again.
- **Large text on a client:** ensure the client has version 0.14.4 or later and its required font bundle, then restart. Normal same-language sessions do not require the combined addon.
- **Missing glyphs or unchanged fonts:** check font files and mappings, restart after replacements, and verify glyph coverage for newly added characters.
- **Broken tables or missing entries after editing:** compare with the backup, check tabs, quotes, placeholders, and escapes, then run the language checker.
- **Diagnostics:** inspect `BepInEx/LogOutput.log` for the plugin version, selected language, and font warnings. `Loaded embedded font` identifies a bundled font; `Created font by family (file identity unverified)` identifies a system-family lookup.
