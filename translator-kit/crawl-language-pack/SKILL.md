---
name: crawl-language-pack
description: Create, edit, and validate a language pack for Crawl Localization 0.13.0 or newer using a released plugin package. Translate game and optional CrawlOnline UI tables, configure fonts, and preserve original English fallback without recompiling the plugin.
---

# Create a Crawl language pack

Work in an extracted Crawl Localization plugin folder containing `CrawlLocalization.dll`, `Languages/`, and any `Fonts/` assets. A source checkout is also supported. This skill and its PowerShell scripts can be used without Codex-specific tools, the source code, a .NET SDK, or Unity Editor. Use PowerShell 5.1+ or PowerShell 7. On Windows the examples below use `powershell`; `pwsh` also works.

The finished deliverable is an independent `Languages/<tag>/` folder, plus any additional font files it references. Ask for the target language/region if not specified. Use tags such as `es`, `ru`, `pt-BR`, `ja`, or `zh-Hant`; `en` is reserved for displaying the original game text. Do not invent or translate an entire language when the user only asks for a template.

## Start and activate

From the plugin root, run the initializer, changing the language/name as requested:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

In a source checkout the script is under `translator-kit/crawl-language-pack/scripts/`. The script refuses to overwrite an existing folder, copies the audited source keys and object paths, and sets every translation cell to the reserved value `@TODO`. It keeps the existing Chinese wording only in the **comment column** as optional context; Chinese translations are never used as fallback. Default font mappings are empty, using the game's original fonts followed by `Arial;Segoe UI` if glyphs are missing. Use `-FallbackFonts 'Font One;Font Two'` to change these candidates. `-CopyFontMappings` is an explicit choice to reuse the reference pack's font configuration; it is not required for a new language.

Alternatively, copy `Languages/zh-Hans` to the target folder manually. Replace **all** second-column Chinese translations with the target language or `@TODO`; leaving Chinese in a translation cell will display it. Retain first-column keys, filenames and `textmesh-paths.txt`. Adjust fonts and `language.ini` for the target language.

After editing, set the active language in the existing game's `BepInEx/config/crawl.localization.cfg`:

```ini
[General]
Language = es
ReloadKey = F8
```

Use the exact folder name. Preserve unrelated existing config sections. The plugin creates this config on its first normal launch. F8 reloads the config/tables and existing labels; restarting also works. Replacing a font binary or restoring a font at a previously missing path requires a restart because fonts are cached. The helper does not change the active game config or launch the game.

## Translate the tables

Files are **UTF-8, raw tab-separated text**, not quote-aware CSV. Each non-comment row has:

```text
original-source-or-key<TAB>target-translation<TAB>context-comment
```

`<TAB>` above means a real tab. Change column 2 only; column 3 can be improved for translators. Lines beginning with `#` are comments. Use a plain-text editor or script that splits on literal tabs. Do not run these files through CSV quoting: ASCII quotes in sources/targets are literal characters, and rows such as `@online:"?` are valid. There is no CSV header row to translate.

Rules that affect correctness:

- Preserve source keys exactly, including case, spaces and punctuation. Runtime matching ignores case and normalizes CRLF/CR to LF, but does not trim other spaces. Avoid duplicate keys, including duplicates across tables.
- `@TODO` in column 2 means unfinished: the row is ignored, and the UI retains the original English game/mod text. Missing rows/files behave the same way. The runtime never fills gaps from a different language pack. A partially translated shop recipe may fall back as a whole label to avoid broken sentences.
- Never fill a symbolic key into the translation cell as a placeholder. `@shop.*`, `@progression.*` and `@online:` are lookup identities, not text to show players.
- Empty targets are only supported for `@shop.il:` rows, where they deliberately remove a fragment (for example an extra period). Use `@TODO` for unfinished fragments instead of empty strings. Do not trim whitespace-only translations.
- Keep escape sequences `\n`, `\r`, `\t` and `\\` valid. Do not insert literal line breaks or tabs inside a field. Preserve explicit line breaks/blank lines used to leave room for illustrations, especially the tutorial's level-10 card.
- Preserve every numbered placeholder and its syntax. Ordinary `{0}`–`{9}` patterns match nonnegative integer runs only. Scoped templates may receive player names or formatted values; see their existing source and reference translation. You may reorder existing placeholders, not remove or invent them.
- Preserve rich-text tags and parameters, such as `<color=#e41944>`, `</color>` and `<size=240>`, and input markers `[A]` / `[B]`. The game chooses keyboard/controller pictures for these markers; do not translate them into literal keyboard letters.
- Keep terminology consistent across vault, store, HUD and tutorial. Translate prefix/connector fragments so the original game's composition remains readable. The plugin does not implement target-language gender, inflection or automatic word-order changes.

| Table | Content |
| --- | --- |
| `translations.tsv` | General menus, settings, standalone labels and shared terms |
| `vault-translations.tsv` | Vault entries, descriptions, achievements and statistics |
| `deity-translations.tsv` | Deities, effects, backgrounds and taunts |
| `chapter-translations.tsv` | Chapter numbers and floor names |
| `shop-translations.tsv` | Store names, prefixes, properties and prompts |
| `shop-il-translations.tsv` | Original store string fragments, connectors and format templates |
| `gameplay-translations.tsv` | Interaction prompts, currency units, portal and streak text |
| `progression-translations.tsv` | Pause prompts, evolution and boss/result text |
| `crawlonline-translations.tsv` | Optional CrawlOnline UI only; dynamic player/lobby names stay untouched |
| `tutorial-translations.tsv` | Prologue prompts, instruction and completion choices |

Some CrawlOnline sentences are split at the original string concatenation boundaries. Translate those exact pieces while preserving needed surrounding spaces. Do not merge rows or create a new whole-sentence key unless the plugin already looks it up. For `@progression.join-body:{0}:{1}:{2}:{3}:{4}`, the arguments are player number, player name, level, deity, and deity effect respectively; `leave-body` uses player number and name. Join/leave titles use player number and name.

## Language data and fonts

`textmesh-paths.txt` is a runtime binding list of audited object hierarchy suffixes; its entries are object identifiers, not translatable text. Keep it with each pack. It does not make the plugin translate every TextMesh or every mod.

`language.ini` has `Name` (a descriptive label) and `FallbackFonts` (semicolon-separated installed font family names). Language selection uses the folder/config tag, not `Name`. Only that pack's font mappings and fallback list are consulted.

`fonts.tsv` has five tab-separated columns:

```text
original-Unity-font-name<TAB>kind<TAB>relative-file<TAB>family-or-asset<TAB>scale
```

Kinds are `bundle`, `system`, or legacy `ttf`. Scale is a positive invariant decimal up to 10, normally `1`. Use `*` as a mapping key only to deliberately override all regular TextMesh fonts; it loses distinctions between original styles. For CrawlOnline's F1 overlay the optional key is `CrawlOnlineOverlay`; otherwise its own font and the selected fallback list are tried.

Examples (replace `<TAB>` with real tabs):

```text
CrawlTextBodyWide<TAB>system<TAB>-<TAB>Arial<TAB>1
CrawlOnlineOverlay<TAB>system<TAB>-<TAB>Segoe UI<TAB>1
CrawlTextBodyWide<TAB>bundle<TAB>Fonts/es-fonts.bundle<TAB>assets/fonts/body.ttf<TAB>1
```

Font paths are relative to the **plugin root**, not to the language folder, and must stay inside it. For portable custom fonts, use a Windows AssetBundle compatible with Unity 5.4.2; players do not need Unity. Raw TTF loading uses a family name and cannot guarantee file identity. A new font bundle is a separate font-authoring task, not necessary for ordinary translation when available fonts cover the target script. Verify font redistribution rights before adding third-party binaries.

Selection order for normal text is selected-pack mapping, original game font, then selected-pack fallback families. If no font covers the translated label, the plugin restores its original text/font. None of these failures triggers a lookup in the Chinese table.

Chinese-specific character wrapping, punctuation handling and small-size sampling enhancement apply only when the **selected language tag is Chinese** (`zh` or `zh-*` / `zh_*`) and the relevant text contains Han characters. Other locales do not trigger them, even if a proper name contains Chinese. Other locales use the game's normal wrapping. Japanese/Thai line breaking, Arabic shaping, RTL layout and other script-specific behavior have not been implemented or universally validated: distinguish missing font coverage from a layout/engine limitation rather than promising every language works by text edits alone. Use explicit `\n` for local layout adjustments where suitable.

`zh-CN` selects `zh-Hans`; `zh-TW` selects `zh-Hant`. An absent Traditional pack keeps original English, never Simplified Chinese.

## Validate and hand off

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
# Once the translation is intended to be complete:
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es -RequireComplete
```

The checker compares keys against the included reference catalog, checks raw TSV shape, duplicate keys, supported escapes, placeholders/tags/icons, config, font paths and binding paths. It reports translated/pending/missing counts. It does not judge translation quality, actual glyph coverage, or game geometry. The reference catalog is for authoring/validation only; the new pack can run without `zh-Hans` installed.

Before delivery, review changed text and run the checker. Let the user verify the game unless they asked you to run it. Suggested coverage: main menu/settings, long vault descriptions, assembled shop weapons, live equipment panel, tutorial with keyboard/controller icons, end prompts, and optional CrawlOnline UI. Check F8, unknown/missing text, missing fonts, and accidental Chinese remnants in **translation cells** (Chinese reference comments are not rendered). Record unresolved items instead of marking the language complete.

Image-only lettering such as PROLOGUE, some tutorial rule cards, upgrade/WRATH effects and boss banners is not changed by a language table or font mapping. Report it separately; do not add ineffective text rows or modify image assets as part of a text-only request.

Share `Languages/<tag>/` and any newly referenced `Fonts/` files for users already on a compatible plugin, or include them in a copy of the player package. Do not ship editor caches, research notes or source code. Tell recipients the minimum plugin version and the `Language = <tag>` setting. There is no need to recompile the DLL for ordinary translation, font configuration or an added language directory.
