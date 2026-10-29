# Optional combined font / 可选合并字体

Requires Crawl Localization 0.14.3 or newer. This addon combines the original
Crawl font styles, the supplied Chinese glyphs and the multilingual Latin/Cyrillic
additions. Use it when players with different writing systems share a lobby.

Close the game and extract this package into:
`Crawl/BepInEx/plugins/CrawlLocalization/`

Keep `Fonts/crawl-combined.bundle` and its license/notice files together. Restart.
The plugin detects this file and uses its ten matching styles directly; no language
mapping or configuration edits are needed. Player names are not translated.
To disable it, remove `Fonts/crawl-combined.bundle` and restart. The normal language
font is then used again. This addon contains no DLL or translations and does not
promise all Unicode characters, emoji or complex-script shaping.

---

需要 Crawl Localization 0.14.3 或更新版本。本包合并原版字形、中文和通用字体，
适合需要同时显示中文、俄文、西班牙语等玩家昵称的情况。

关闭游戏，将本包解压到上述插件目录，保留字体及许可文件，然后重启。
检测到 `Fonts/crawl-combined.bundle` 后，插件直接优先使用合并字体；
无需改语言表或配置，不翻译玩家名字。删除这一个 bundle 并重启即可恢复默认字体。
本包不含插件 DLL、语言翻译或字形覆盖表；罕见字、表情及复杂文字排版不保证支持。
