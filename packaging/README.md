# Crawl Localization

Crawl 的本地化插件，默认简体中文，支持独立语言包和可选 CrawlOnline 界面翻译。
A localization plugin for Crawl with Simplified Chinese included, independent language packs, and optional CrawlOnline UI translation.

## 安装 / Install

- 需要 Windows 版 Crawl 和 BepInEx 5（32 位）。关闭游戏，将本包内容解压至 `Crawl/BepInEx/plugins/CrawlLocalization/`，再启动游戏。
- Requires Crawl for Windows and BepInEx 5 (32-bit). Close the game, extract this package into `Crawl/BepInEx/plugins/CrawlLocalization/`, then launch it.
- 配置文件 / Configuration: `Crawl/BepInEx/config/crawl.localization.cfg`，首次正常启动后生成 / created on the first normal launch.

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

`Language = en` 恢复游戏原文。修改配置或译文后按 F8；替换字体文件后重启游戏。更新前备份自己改过的语言文件。
Use `en` for original game text. Press F8 after editing configuration or translations; restart after replacing fonts. Back up your custom language files before updating.

## 制作其他语言 / Create another language

文件名含 `-Translator` 的制作包额外包含 `crawl-language-pack/`。普通玩家包不包含制作工具。
The `-Translator` package includes `crawl-language-pack/`; the normal player package does not include these tools.

让 AI 阅读 `crawl-language-pack/SKILL.md`，并告诉它目标语言；或在制作包解压目录运行以下命令，以西班牙语为例：
Ask your AI to read `crawl-language-pack/SKILL.md` and specify your target language, or run these commands from the extracted translator package (Spanish example):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
```

编辑前备份原文件；修改 `Languages/es/` 中 TSV 的第二列，保留原文键、占位符和标签，不要使用 CSV 引号转义。完成后对照备份并运行校验，再设置 `Language = es`。将 `es` 换成 `ru`、`fr` 等可创建其他语言，无需编译代码。
Back up the original files before editing. Translate column 2 of the TSV files in `Languages/es/`; preserve source keys, placeholders and tags, without CSV quote escaping. Compare against your backup and run the checker, then set `Language = es`. Use another language tag such as `ru` or `fr` as needed. No compilation is required.

`@TODO` 和缺译内容保留原游戏／模组英文，不回退中文。字体与排版需要在游戏内检查；图片里的文字不受译文表影响。
`@TODO` and missing translations retain original game/mod English, never Chinese fallback. Check fonts and layout in-game; text baked into images is not changed by translation tables.

问题日志 / Diagnostic log: `Crawl/BepInEx/LogOutput.log`.
