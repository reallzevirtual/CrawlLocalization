---
name: crawl-language-pack
description: Create, edit, and validate a language pack for Crawl Localization 0.13.0 or newer using a released plugin package. Translate game and optional CrawlOnline UI tables, configure fonts, and preserve original English fallback without recompiling the plugin.
---

# Create a Crawl language pack

Work in an extracted Translator package containing `CrawlLocalization.dll`, `README.md`, and `crawl-language-pack/`. The kit includes an independent English `catalog/`; the initial package has no translated language folder and includes only `Fonts/crawl-multilingual.bundle` (Latin/Cyrillic). The initializer creates `Languages/<tag>/`. A source checkout is also supported. This skill and its PowerShell scripts can be used without Codex-specific tools, the source code, a .NET SDK, or Unity Editor. Use PowerShell 5.1+ or PowerShell 7. On Windows the examples below use `powershell`; `pwsh` also works.

The finished deliverable is an independent `Languages/<tag>/` folder, plus any additional font files it references. Ask for the target language/region if not specified. Use tags such as `es`, `ru`, `pt-BR`, `ja`, or `zh-Hant`; `en` is reserved for displaying the original game text. Do not invent or translate an entire language when the user only asks for a template.

## Start and activate

From the plugin root, run the initializer, changing the language/name as requested:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

In a source checkout the script is under `translator-kit/crawl-language-pack/scripts/`. It reads only the `catalog/` beside the scripts, copies original English source keys and English context, and sets every target to `@TODO`. Translate from the original English, never from another language's translation. Default font mappings use `Fonts/crawl-multilingual.bundle` for the ten original styles. Keep the mapped font bundle and its license/notice files when sharing a pack. The optional CombinedFonts addon (plugin 0.14.3+) supplies Chinese, Latin and Cyrillic glyphs in one separate bundle. Installing `Fonts/crawl-combined.bundle` and restarting selects it directly for the ten original styles, including raw player names and optional CrawlOnline displays. Removing it and restarting restores the default language fonts. Names are never translated. Normal packages and this authoring kit do not include the addon or coverage tables. Russian letters and Spanish accents are checked; other scripts may need other fonts. Use precomposed (NFC) accented letters; complex shaping/combining-mark positioning is not added by this bundle. Existing game-specific symbol slots are preserved. Use `-FallbackFonts 'Font One;Font Two'` for other candidates. The helper refuses to overwrite an existing language folder.

The independent English catalog is the single source of supported keys; every target cell must be `@TODO`. Four legacy aliases for manually modified game assets belong only to the existing Simplified Chinese pack and are not translation sources for new languages. Do not add them to a new template. Treat `catalog/` as read-only while translating; new game source keys require a separate maintainer change to this version-controlled catalog and the relevant runtime adapter.

After editing, set the active language in the existing game's `BepInEx/config/crawl.localization.cfg`:

```ini
[General]
Language = es
ReloadKey = F8
```

Use the exact folder name. Preserve unrelated existing config sections. The plugin creates this config on its first normal launch. F8 reloads the config/tables and existing labels; restarting also works. Replacing a font binary or restoring a font at a previously missing path requires a restart because fonts are cached. The helper does not change the active game config or launch the game.

## Update an existing language to a newer catalog

Use the canonical catalog and scripts from the same updated kit/plugin release. Preview changes before synchronizing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\sync-languages.ps1 -PluginRoot . -Languages es
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\sync-languages.ps1 -PluginRoot . -Languages es -Apply
```

Omit `-Languages` to check/update all initialized language folders. `-Apply` automatically backs up the catalog and affected complete language folders to a unique `authoring-backups/sync-.../` directory (or `-BackupRoot`), then appends only missing rows with `@TODO`. Existing target cells, comments, spacing, quotes and deliberate empty translations are preserved. Missing tables are created. Repeating a completed sync changes nothing.

Extra, duplicate or misplaced keys block all planned additions. Do not fix an unknown key by automatically adding it to the master: it may be a damaged source or a typo. A maintainer must confirm new runtime-supported sources, add them to the canonical table with English context and `@TODO`, then synchronize. Do not delete removed keys or infer renames; flag them for a reviewed migration. The four Simplified Chinese legacy aliases remain local exceptions and are never propagated.

`sync-languages.ps1 -Check` is a read-only alignment check; missing keys/conflicts fail, pending rows are allowed. Follow synchronization with `check-language.ps1` for translation syntax, placeholders, padding, config and fonts. The standard checker also validates that all catalog targets remain `@TODO`. Sync does not translate text, install files, or change game configuration. When comparing against the backup, distinguish the reported newly added keys from accidental source edits.

## Back up before translation

Before the AI or user edits any existing language files, make a byte-for-byte backup of the entire target language folder and the bundled English `catalog/`. For a new language, back up the reference before initialization and the generated target immediately after initialization, before handing it to the translator. Keep filenames and relative paths intact.

Use a unique, dated backup directory in a durable project/authoring location outside the live `Languages/` tree; do not use Temp or put backups inside a release package. Never overwrite an earlier backup. Record its absolute path and the plugin/catalog version in the handoff so another AI can find it when the user finishes. Keep it unchanged until comparison and handoff are complete; do not delete it without the user's request. A Git revision can provide additional evidence but does not replace the requested file backup.

If the user returns files that were already edited and no prior backup exists, preserve those received files before repairs, then obtain the original keys from a known matching release or Git revision. Do not describe a post-edit copy as a pre-translation baseline. If no trustworthy baseline is available, report that source-key integrity could not be verified.

## Translate the tables

Files are **UTF-8, raw tab-separated text**, not quote-aware CSV. Each non-comment row has:

```text
original-source-or-key<TAB>target-translation<TAB>context-comment
```

`<TAB>` above means a real tab. Change column 2 only; column 3 can be improved for translators. Lines beginning with `#` are comments. Use a plain-text editor or script that splits on literal tabs. Do not run these files through CSV quoting: ASCII quotes in sources/targets are literal characters, and rows such as `@online:"?` are valid. There is no CSV header row to translate.

Spreadsheet software can silently apply CSV quoting to these raw TSV files. For example, `<color="#b02829">Kill</color> beasts\n` must not become `"<color=""#b02829"">Kill</color> beasts\n"`. It may also remove literal quotation marks from a weapon description. Both change the source key and break lookup; doubled quotes in a translated color tag may break rendering. Inspect the saved raw file, not just the spreadsheet application's cell display.

Rules that affect correctness:

- Inverse-font titles use `_` as black masking/padding. Preserve outer underscores; do not replace them with hyphens. Use underscores for spaces within such titles. `@online:title:` and `@progression.prompt-title:` keys affect titles only.
- Preserve source keys exactly, including case, spaces and punctuation. Runtime matching ignores case and normalizes CRLF/CR to LF, but does not trim other spaces. Avoid duplicate keys, including duplicates across tables.
- `@TODO` in column 2 means unfinished: the row is ignored, and the UI retains the original English game/mod text. Missing rows/files behave the same way. The runtime never fills gaps from a different language pack. A partially translated shop recipe may fall back as a whole label to avoid broken sentences.
- Never fill a symbolic key into the translation cell as a placeholder. `@shop.*`, `@progression.*` and `@online:` are lookup identities, not text to show players.
- Empty targets are only supported for `@shop.il:` rows, where they deliberately remove a fragment (for example an extra period). Use `@TODO` for unfinished fragments instead of empty strings. Do not trim whitespace-only translations.
- Keep escape sequences `\n`, `\r`, `\t` and `\\` valid. Do not insert literal line breaks or tabs inside a field. Preserve explicit line breaks/blank lines used to leave room for illustrations, especially the tutorial's level-10 card.
- Preserve every numbered placeholder and its syntax. Ordinary `{0}`–`{9}` patterns match nonnegative integer runs only. Scoped templates may receive player names or formatted values; use the original English source and the argument descriptions below. You may reorder existing placeholders, not remove or invent them.
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

For symbolic `@progression.*` keys, column 3 includes the original English sentence template and argument meanings; translate that English template rather than the lookup identity.

Some CrawlOnline sentences are split at the original string concatenation boundaries. Translate those exact pieces while preserving needed surrounding spaces. Do not merge rows or create a new whole-sentence key unless the plugin already looks it up. For `@progression.join-body:{0}:{1}:{2}:{3}:{4}`, the arguments are player number, player name, level, deity, and deity effect respectively; `leave-body` uses player number and name. Join/leave titles use player number and name.

## Language data and fonts

`textmesh-paths.txt` is a runtime binding list of audited object hierarchy suffixes; its entries are object identifiers, not translatable text. Keep it with each pack. It does not make the plugin translate every TextMesh or every mod.

`language.ini` has `Name` (a descriptive label) and `FallbackFonts` (semicolon-separated installed font family names). Language selection uses the folder/config tag, not `Name`. Only that pack supplies translation mappings and configured system families; no other language font is loaded automatically. The separately installed combined addon overrides the ten original styles.

`fonts.tsv` has five tab-separated columns:

```text
original-Unity-font-name<TAB>kind<TAB>relative-file<TAB>family-or-asset<TAB>scale
```

Kinds are `bundle`, `system`, or legacy `ttf`. Scale is a positive invariant decimal up to 10, normally `1`. Use `*` as a mapping key only to deliberately override all regular TextMesh fonts; it loses distinctions between original styles. For CrawlOnline's F1 overlay the optional key is `CrawlOnlineOverlay`; otherwise its own font and the selected fallback list are used. With the optional addon installed, CrawlTextBodyThin from the combined bundle is used first.

Examples (replace `<TAB>` with real tabs):

```text
CrawlTextBodyWide<TAB>system<TAB>-<TAB>Arial<TAB>1
CrawlOnlineOverlay<TAB>system<TAB>-<TAB>Segoe UI<TAB>1
CrawlTextBodyWide<TAB>bundle<TAB>Fonts/es-fonts.bundle<TAB>assets/fonts/body.ttf<TAB>1
```

Font paths are relative to the **plugin root**, not to the language folder, and must stay inside it. For portable custom fonts, use a Windows AssetBundle compatible with Unity 5.4.2; players do not need Unity. Raw TTF loading uses a family name and cannot guarantee file identity. A new font bundle is a separate font-authoring task, not necessary for ordinary translation when available fonts cover the target script. Verify font redistribution rights before adding third-party binaries.

Normal selection order is selected-pack mapping, original game font, then selected-pack fallback families. The optional combined addon is tried first only when its file is installed. Without it, there is no automatic cross-bundle or raw-name font switching. If no font covers the translation, the original text/font is retained. No failure looks up another language's translation table.

Chinese-specific character wrapping and punctuation handling apply only when the **selected language tag is Chinese** (`zh` or `zh-*` / `zh_*`) and the relevant text contains Han characters. Other locales do not trigger these layout rules, even if a proper name contains Chinese. Font sampling for Han glyphs in the optional combined font is independent of those rules. Other locales use the game's normal wrapping. Japanese/Thai line breaking, Arabic shaping, RTL layout and other script-specific behavior have not been implemented or universally validated: distinguish missing font coverage from a layout/engine limitation rather than promising every language works by text edits alone. Use explicit `\n` for local layout adjustments where suitable.

`zh-CN` selects `zh-Hans`; `zh-TW` selects `zh-Hant`. An absent Traditional pack keeps original English, never Simplified Chinese.

## Validate and hand off

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
# Once the translation is intended to be complete:
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es -RequireComplete
```

The checker compares keys against the included reference catalog, checks raw TSV shape, duplicate keys, supported escapes, placeholders/tags/icons, config, font paths and binding paths. It reports translated/pending/missing counts. It does not judge translation quality, actual glyph coverage, or game geometry. The English catalog is for authoring/validation only; finished language packs do not require it at runtime or depend on any other language pack.

When the user finishes translating, compare the saved files with the immutable pre-translation backup **before** declaring success or committing:

- Read UTF-8 and split on literal tabs, without CSV quote decoding or trimming fields. Check table filenames, row counts, column counts and the exact source-key multiset per table. Report missing, added, duplicated or changed keys; do not mistake harmless row reordering for lost entries or silently align unrelated rows by position.
- Review changes to target cells and comments separately. Preserve the user's wording. Check placeholders, input markers, rich-text tags, escape sequences and significant leading/trailing spaces, including deliberate empty `@shop.il:` values. Compare config and object-binding changes against what the task authorized.
- Look for added outer quotes, doubled inner quotes and removed literal quotes in both source and target fields. Repair source keys only against the trusted baseline; undo target quoting only when the export damage is clear. Never strip or CSV-decode every quoted field: some keys intentionally contain literal quotes. Preserve an unrepaired copy and report ambiguous changes instead of guessing.
- Run `check-language.ps1` as well as the backup comparison, then report the checks and repairs. The checker uses the independent English `catalog/` beside the scripts, not the language being edited. It can therefore detect damaged source keys even when checking the maintained Chinese pack. Keep the catalog unchanged; if someone edits both the catalog and the target, compare them against the trusted baseline too. The checker does not automatically read your backups.

Before delivery, review changed text and run the checker. Let the user verify the game unless they asked you to run it. Suggested coverage: main menu/settings, long vault descriptions, assembled shop weapons, live equipment panel, tutorial with keyboard/controller icons, end prompts, and optional CrawlOnline UI. Check F8, unknown/missing text, missing fonts, and accidental leftover `@TODO` targets. Record unresolved items instead of marking the language complete.

Image-only lettering such as PROLOGUE, some tutorial rule cards, upgrade/WRATH effects and boss banners is not changed by a language table or font mapping. Report it separately; do not add ineffective text rows or modify image assets as part of a text-only request.

Share `Languages/<tag>/` and any newly referenced `Fonts/` files for users already on a compatible plugin, or package them with the supplied DLL. Do not ship editor caches, research notes or source code. Tell recipients the minimum plugin version and the `Language = <tag>` setting. There is no need to recompile the DLL for ordinary translation, font configuration or an added language directory.
