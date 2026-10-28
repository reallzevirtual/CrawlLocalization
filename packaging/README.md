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

## Create another language

Use the separate `-Translator` ZIP. It includes the plugin DLL, an English source catalog with `@TODO` targets, and creation/checking tools. It contains no translated language packs or Chinese fonts.

Read its README or ask your AI to read `crawl-language-pack/SKILL.md`. Choose a language tag, create its template, and translate directly from the English source. No compilation is required.

Diagnostic log: `Crawl/BepInEx/LogOutput.log`.
