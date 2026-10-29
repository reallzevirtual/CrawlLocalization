# Crawl 中文本地化

本包仅包含简体中文语言包及原来的中文字体，支持游戏本体和可选的 CrawlOnline 模组界面翻译。语言目录只包含中文，中文显示仍使用对应的中文字体。

## 安装

需要 Windows 版 Crawl 和 BepInEx 5（32 位）。关闭游戏，将本包内容解压至：

`Crawl/BepInEx/plugins/CrawlLocalization/`

保留包内的插件文件、语言目录和完整字体目录。更新前请备份自己修改过的翻译文件。

## 设置与使用

首次正常启动后，插件会生成配置文件：

`Crawl/BepInEx/config/crawl.localization.cfg`

确认语言设置为简体中文：

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

首次安装且没有旧配置时，插件默认使用简体中文。已有配置不会被安装包自动覆盖。
将语言改为 `en` 可显示游戏原文；未翻译内容同样保留原文。

修改配置或译文后按 F8 重载；首次安装或更换字体文件后请重启游戏。
使用本包不需要安装字体、编译工具或 Unity 编辑器。

需要中俄西混排昵称时，额外安装合并字体包并重启即可；删除其中的 `crawl-combined.bundle` 并重启可恢复默认。昵称内容不变，罕见字和表情符号仍可能缺字。

图片中绘制的部分英文暂未替换。CrawlOnline 为可选模组，本包不附带该模组。

遇到问题可查看日志：`Crawl/BepInEx/LogOutput.log`。
