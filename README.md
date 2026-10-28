# Crawl Localization

用于 **Windows 版 Crawl（Unity 5.4.2）** 的 BepInEx 5 本地化插件。当前维护简体中文，支持通过独立语言包添加其他语言，并为 **CrawlOnline 0.4.0** 提供可选界面翻译。当前插件版本由根目录 [VERSION](VERSION) 统一管理。

本地化在本机显示层生效。插件使用外置译文表和字体资源，在内存中适配显示入口；保留游戏的原始标识、存档数据、随机选择及交互回调，不需要手动修改游戏 assets 或磁盘上的 DLL。

[更新记录](CHANGELOG.md) · [翻译技能与语言包规范](translator-kit/crawl-language-pack/SKILL.md) · [图片文字清单](docs/image-text.md) · [检查与资源导出](docs/testing.md)

## 功能与范围

- 主菜单、设置、键盘重绑、宝库图鉴和成就。
- 邪神选择、挑衅试炼、开局提示、章节编号及随机楼层名称。
- 商店武器、技能、遗物、药水及购买提示；常驻玩家装备面板。
- 游戏内交互、暂停确认、怪物升级、首领登场和结算文字。
- 序章教程消息和操作提示。
- 安装 CrawlOnline 时的联机菜单、服务器列表、大厅和 F1 面板。

商店随机名称与描述沿用游戏的拼接顺序，翻译实际使用的文本片段。未知内容或缺少所需片段时保留原文。玩家名、房间名、地址、版本号和游戏数值不作为普通词条翻译。

插件只处理已适配的文字入口，不自动覆盖所有 TextMesh、MonoBehaviour 或其他模组。`PROLOGUE`、`SALVATION`、部分升级／治疗／怒气反馈等属于图片文字，目前保留原图，详见[清单](docs/image-text.md)。CrawlOnline 更新或其他模组改写相同入口时，可能需要追加适配。

## 安装与更新

玩家需要 Windows 版 Crawl，以及与游戏匹配的 **BepInEx 5（32 位）**。使用发行包不需要 .NET SDK、Python 或 Unity Editor。

1. 安装并确认 BepInEx 能正常加载。
2. 关闭游戏，将普通玩家包 `CrawlLocalization-<版本号>.zip` 的内容解压到 `Crawl/BepInEx/plugins/CrawlLocalization/`。
3. 启动游戏。默认使用简体中文，插件生成配置文件。

安装后的结构应为：

```text
Crawl/
└─ BepInEx/
   └─ plugins/
      └─ CrawlLocalization/
         ├─ CrawlLocalization.dll
         ├─ Languages/
         │  └─ zh-Hans/
         │     ├─ language.ini
         │     ├─ fonts.tsv
         │     ├─ textmesh-paths.txt
         │     └─ …translations.tsv
         └─ Fonts/
            └─ crawl-fonts.bundle
```

更新前备份自己改过的语言包，再覆盖 DLL、语言数据和字体文件，重启游戏。避免多放一份旧 DLL，或多套一层解压目录。CrawlOnline 是可选模组；本地化包不包含它，也不要求安装它。

卸载时关闭游戏并移走 `BepInEx/plugins/CrawlLocalization/`。配置文件可以保留，或删除 `BepInEx/config/crawl.localization.cfg`。

## 配置与使用

配置位置：`Crawl/BepInEx/config/crawl.localization.cfg`。

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

| 设置值 | 行为 |
| --- | --- |
| `zh-Hans`、`zh-CN` | 使用简体中文包 |
| `zh-Hant`、`zh-TW` | 使用独立繁体包，需要自行安装；默认发行包不包含 |
| `es`、`ru` 等 | 读取 `Languages/` 下对应目录 |
| `en` | 使用游戏原文和原字体 |

语言包互相独立。缺失词条、待译项或未安装的语言包保留原游戏／模组英文，不回退中文。原文模式不会撤销此前手动修改过的游戏资源。

编辑配置或译文后按 **F8**，可重载并刷新已绑定文字；`ReloadKey = None` 可关闭快捷键。替换字体文件或补回先前缺失的字体文件后，需要重启游戏。

重玩教程开关可能显示“开（当前模式禁用）”：表示设置已保存，但当前模式限制了教程启动。本插件保留原游戏和 CrawlOnline 的教程限制。

## 修改译文与制作其他语言

普通玩家包只含运行文件。需要制作语言包时，使用 `CrawlLocalization-<版本号>-Translator.zip`，其中额外包含 `crawl-language-pack/SKILL.md` 和创建、校验脚本。可直接让 AI 阅读该技能。

**编辑前备份原始文件，翻译完成后对照备份检查。** 备份放在不会随临时文件清理的位置，保留原始英文键和文件结构。

在解压后的翻译制作包根目录运行以下命令，以西班牙语为例：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

脚本生成 `Languages/es/`。新模板第二列为 `@TODO`，第三列的中文只是参考注释；默认不复制中文字体映射。修改第二列译文后，设置 `Language = es`，按 F8 或重启。无需重新编译插件。创建工具会拒绝覆盖已有语言目录。

译文文件是 **UTF-8 原始 TSV**：

```text
原文或查找键<TAB>译文<TAB>位置注释
START GAME<TAB>开始游戏<TAB>主菜单／入口按钮
```

`<TAB>` 指实际制表符。保持第一列不变，保留 `{0}` 等占位符、颜色标签、`[A]`／`[B]` 图标标记及 `\n` 等转义。`@TODO` 表示待译；只有 `@shop.il:` 允许空译文来主动省略片段。

**不要按 CSV 引号规则保存。** 引号是文本的一部分，将 `"` 改为 `""`、添加外层引号或删掉原文引号都会影响匹配。表格软件中看起来正常，也应检查保存后的原始文件。

完成后校验：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
# 要求全部翻译完成时加上：
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es -RequireComplete
```

源码目录下，脚本位于 `translator-kit/crawl-language-pack/scripts/`。校验检查结构、重复键、占位符、标签和字体配置；它使用当前 `zh-Hans` 目录作为参考，因此检查中文自身时仍须与编辑前备份或可信 Git 版本比对。脚本通过不代表译文质量、字形和游戏内布局已经验证。

语言目录中的 `language.ini` 配置备用字体，`fonts.tsv` 配置替换映射，`textmesh-paths.txt` 是显示对象绑定清单；这些都是运行数据，不应随意删除。各语言只使用自己的字体配置。中文专用换行和小字号采样不会在其他语言触发；特殊文字系统的排版能力需另外验证。

各表用途、字体格式、术语与拼接约束详见[语言包规范](translator-kit/crawl-language-pack/SKILL.md)。发给玩家时提供 `Languages/<语言>/` 和它引用的字体，并告知配置值。

## 从源码构建与打包

构建环境需要：

- **PowerShell 7** 与 **.NET SDK**（提供 C# 编译器）。
- 本机 Crawl 游戏文件，以及其 `BepInEx/core/` 中的 BepInEx 5 和 Harmony 程序集。
- 仓库自带的 `Fonts/crawl-fonts.bundle`。使用现成字体包构建 DLL 不需要 Unity Editor。

构建使用游戏自己的 Mono / Unity 引用程序集，无需 NuGet。在项目根目录运行：

```powershell
pwsh -NoProfile -File .\build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Crawl'
```

默认流程为：**离线检查 → 编译 DLL → 整理运行文件 → 生成两个 ZIP**。不会自动安装或启动游戏。

| 输出 | 用途 |
| --- | --- |
| `artifacts/CrawlLocalization-<版本号>/` | 运行文件暂存目录，每次构建会重建其内容 |
| `artifacts/CrawlLocalization-<版本号>.zip` | 普通玩家包 |
| `artifacts/CrawlLocalization-<版本号>-Translator.zip` | 运行文件 + 翻译技能与脚本 |

玩家包只包含 DLL、所选语言包的运行数据和引用字体，不带 README、CHANGELOG、docs、源码、测试或字体源 TTF。译者工具只进入 Translator 包。

如果已经在源码的 `Languages/` 下准备了其他语言，在 **PowerShell 7 终端**中按数组选择：

```powershell
.\build.ps1 -GamePath '你的 Crawl 目录' -Languages @('zh-Hans', 'es')
```

默认只打包 `zh-Hans`。译者工具依赖该目录作原文参考，分发制作包时应保留它；新增语言运行时不依赖它。

需要构建后自动安装时，先关闭游戏：

```powershell
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录' -Install
```

`-Install` 会先把现有插件目录备份到 `artifacts/backups/`，然后复制构建结果，不启动游戏。`-SkipTests` 可跳过离线检查，常规构建建议保持默认检查。

## 发布新版本

只需修改根目录 `VERSION` 的一行版本号，例如将 `0.13.0` 改为 `0.13.1`，再正常运行 `build.ps1`。格式固定为 `主版本.次版本.修订号`，不带 `v` 前缀或预发布后缀，每段范围为 0–65534。

构建会自动使用同一版本生成 BepInEx 插件版本、四段程序集／文件版本（如 `0.13.1.0`）、暂存目录和两个 ZIP 名称。版本源码生成在 `artifacts/build/`，不修改 `src/`，也不会进入发行包。删除 `artifacts/` 后可重新生成。

修改翻译通常递增修订号，例如 `0.13.0 → 0.13.1`。版本由维护者决定，普通构建不会自动加一；同一版本重复构建会覆盖对应暂存目录和 ZIP。发布时还需在 [CHANGELOG](CHANGELOG.md) 记录变更，并提交 `VERSION` 与相关修改。技能中的“0.13.0 或更新版本”是最低兼容要求，不随当前版本同步修改。

## 字体、检查与项目结构

随包字体保留 Crawl 的风格及原 Font 排版参数。字体选择顺序为：本语言映射字体 → 可显示译文的游戏原字体 → 本语言备用字体；均不可用时恢复原文和原字体。

仅在替换字体源文件或重做 bundle 时，才需要已激活的 **Unity 5.4.2f2**、Python 和 UnityPy：

```powershell
pwsh -NoProfile -File .\build-fonts.ps1 -UnityPath '你的 Unity 5.4.2f2\Editor\Unity.exe'
```

旧编辑器兼容参数、字体布局恢复与验证方法见[字体工程说明](FontBundleProject/README.md)。不要用新版本 Unity 升级该工程。

| 目录 | 内容 |
| --- | --- |
| `src/` | BepInEx 插件、显示适配与语言／字体服务 |
| `Languages/` | 各语言的译文和运行配置 |
| `Fonts/`、`FontBundleProject/` | 字体源、已构建 bundle 和字体工程 |
| `translator-kit/` | 翻译技能、模板生成和校验脚本 |
| `tests/`、`tools/` | 离线检查、独立 Unity 验证和资源导出 |
| `docs/` | 图片文字清单与维护检查指南 |
| `artifacts/` | 构建结果、检查日志和生成文件，不提交 Git |

日常离线检查可运行 `pwsh -NoProfile -File .\tests\test.ps1`。其他检查按改动选择，见[检查与资源导出](docs/testing.md)。游戏内的位置、动画、输入及模组组合仍需实际验证。

## 常见问题

- **没有翻译：** 检查 DLL 路径、配置的语言目录及表格第一列；部分图片文字本来就未替换。
- **方框、英文回退或字体没更新：** 查看字体文件、映射与备用字体，替换字体后重启。新增汉字也应检查字形覆盖。
- **修改表格后报错或漏译：** 对照备份检查引号、制表符、占位符与转义，再运行校验脚本。
- **需要诊断信息：** 查看 `BepInEx/LogOutput.log` 中的插件版本、语言、字体加载或回退警告。`Loaded embedded font` 表示加载包内字体，`Created font by family (file identity unverified)` 表示按系统家族名称创建字体。
