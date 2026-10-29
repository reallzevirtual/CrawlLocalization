# Crawl Localization

用于 **Windows 版 Crawl（Unity 5.4.2）** 的 BepInEx 5 本地化插件。当前提供简体中文、西班牙语和俄语，支持通过独立语言包添加其他语言，并为 **CrawlOnline 0.4.0** 提供可选界面翻译。当前插件版本由根目录 [VERSION](VERSION) 统一管理。

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
2. 选择多语言包 `CrawlLocalization-<版本号>.zip`（全部非中文语言），或中文包 `CrawlLocalization-<版本号>-zh-Hans.zip`。关闭游戏，将所选包解压到 `Crawl/BepInEx/plugins/CrawlLocalization/`。
3. 启动游戏生成配置；多语言包需将 `Language` 设置为 `es`、`ru` 或所需语言目录名。中文包使用 `zh-Hans`。旧配置不会自动改变。

以中文包为例，安装后的结构应为：

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
            ├─ crawl-chinese-fonts.bundle
            ├─ crawl-multilingual.bundle
            └─ …字体许可与来源说明
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

## Create another language

Use `CrawlLocalization-<version>-Translator.zip`. It contains the DLL, an independent original-English catalog, `crawl-language-pack/SKILL.md`, and creation/checking scripts. It includes both Latin/Cyrillic and Chinese font bundles, with no Chinese translations or Chinese comments. The default player package includes non-Chinese language packs; Simplified Chinese has its own separate player package.

Before editing, back up the English catalog and your generated language folder. Ask an AI to read the skill and specify the target language, or run this in the extracted translator package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
```

The script creates `Languages/es/` with 1,488 original-English entries. Translate column 2 from the English in column 1; column 3 provides English context. Unfinished targets remain `@TODO` and display original English. Four input aliases retained for old manually modified Chinese game assets are intentionally excluded from new templates.

Tables are UTF-8 raw TSV, not quote-aware CSV. Preserve source keys, placeholders, tags, `[A]`/`[B]` markers, escapes and significant spaces. Do not add wrapping quotes or double existing quotes. Compare saved files with the pre-edit backup and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
```

Add `-RequireComplete` when the language should be fully translated. The checker reads the independent `catalog/` next to its scripts, so it can detect edited source keys without consulting any translated pack. Keep that catalog unchanged during translation. In a source checkout the scripts are under `translator-kit/crawl-language-pack/scripts/`.

Set `Language = es` in `BepInEx/config/crawl.localization.cfg`, then press F8 or restart. The template maps the ten game font styles to `Fonts/crawl-multilingual.bundle`; distribute both font bundles and the license/notice files. Bundled resources alone do not enable automatic font switching for online player names. Russian letters and Spanish accents are checked. Missing glyphs fall back to original fonts, configured fallback families, then original English text. Configure `fonts.tsv` and `language.ini` as needed and retain `textmesh-paths.txt`. Chinese-specific layout rules do not apply to other languages; other scripts' layout and glyph coverage still need in-game validation.

No DLL rebuild is needed. Share the language directory, required fonts and language setting. Detailed table semantics, font configuration and backup workflow are in the [language pack skill](translator-kit/crawl-language-pack/SKILL.md).

## 从源码构建与打包

构建环境需要：

- **PowerShell 7** 与 **.NET SDK**（提供 C# 编译器）。
- 本机 Crawl 游戏文件，以及其 `BepInEx/core/` 中的 BepInEx 5 和 Harmony 程序集。
- 仓库自带的 `Fonts/crawl-multilingual.bundle` 及许可说明和 `Fonts/crawl-chinese-fonts.bundle`。使用现成字体包不需要 Unity Editor。

构建使用游戏自己的 Mono / Unity 引用程序集，无需 NuGet。在项目根目录运行：

```powershell
pwsh -NoProfile -File .\build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Crawl'
```

默认流程为：**离线检查 → 编译 DLL → 打包全部非中文语言和两种字体 → 生成玩家 ZIP 与制作包 ZIP**。不会自动安装或启动游戏。

```powershell
# 全部非中文语言，目前为 es、ru；新增非中文语言目录会自动纳入
pwsh -NoProfile -File .\build.ps1

# 只收录中文语言；同时附带中文字体和通用多语言字体
pwsh -NoProfile -File .\build.ps1 -Languages zh-Hans

# 显式选择非中文语言子集
pwsh -NoProfile -File .\build.ps1 -Languages ru
```

| 输出 | 用途 |
| --- | --- |
| `artifacts/CrawlLocalization-<版本号>.zip` | 默认玩家包：全部非中文语言、中文与通用两种字体、全英文 README |
| `artifacts/CrawlLocalization-<版本号>-zh-Hans.zip` | 中文玩家包：仅中文语言，附带中文字体和通用多语言字体；全中文 README，无制作其他语言的说明 |
| `artifacts/CrawlLocalization-<版本号>-ru.zip` | 显式选择单个非中文语言时的独立玩家包 |
| `artifacts/CrawlLocalization-<版本号>-Translator.zip` | 非中文构建另行生成的英文制作包：原文目录、中文与通用两种字体和工具，无现成译文 |

暂存目录与对应玩家 ZIP 同名（不含 `.zip`），每次构建只重建所选包的暂存内容。默认包和中文包可以同时保留，不互相覆盖。中文构建不会生成或改写 Translator 包。

README 源文件分别为[英文玩家说明](packaging/README.md)、[中文玩家说明](packaging/zh-Hans/README.md)、[英文制作说明](packaging/translator/README.md)，打包时直接复制。玩家包只带运行所需的 DLL、语言文件、字体及相关许可，不带源码、docs、测试或源 TTF。

`-Languages` 支持在 PowerShell 7 终端传入数组，例如 `.\build.ps1 -Languages @('es', 'ru')`。中文与非中文混合选择会报错，应分开构建。三种发行包均附带 `crawl-chinese-fonts.bundle` 和 `crawl-multilingual.bundle`；其他语言专用字体按其映射附加。

制作包始终从独立英文 `catalog/` 打包，与玩家包的译文无关。维护者新增适配词条时需同步更新英文目录；普通翻译只改目标语言，不改目录基线。

需要构建后自动安装时，先关闭游戏：

```powershell
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录' -Install
```

`-Install` 会先把现有插件目录备份到 `artifacts/backups/`，然后复制构建结果，不启动游戏。`-SkipTests` 可跳过离线检查，常规构建建议保持默认检查。

## 发布新版本

只需修改根目录 `VERSION` 的一行版本号，例如将 `0.13.0` 改为 `0.13.1`，再正常运行 `build.ps1`。格式固定为 `主版本.次版本.修订号`，不带 `v` 前缀或预发布后缀，每段范围为 0–65534。

构建会自动使用同一版本生成 BepInEx 插件版本、四段程序集／文件版本（如 `0.13.1.0`）、暂存目录和各发行 ZIP 名称。版本源码生成在 `artifacts/build/`，不修改 `src/`，也不会进入发行包。删除 `artifacts/` 后可重新生成。

修改翻译通常递增修订号，例如 `0.13.0 → 0.13.1`。版本由维护者决定，普通构建不会自动加一；同一版本重复构建会覆盖对应暂存目录和 ZIP。发布时还需在 [CHANGELOG](CHANGELOG.md) 记录变更，并提交 `VERSION` 与相关修改。技能中的“0.13.0 或更新版本”是最低兼容要求，不随当前版本同步修改。

## 字体、检查与项目结构

随包字体保留 Crawl 的风格及原 Font 排版参数。三种包都包含中文和通用字体，但仍由语言映射选择；仅附带字体不会自动让联机昵称启用中文字体回退。字体选择顺序为：本语言映射字体 → 可显示译文的游戏原字体 → 本语言备用字体；均不可用时恢复原文和原字体。

仅在替换字体源文件或重做 bundle 时，才需要已激活的 **Unity 5.4.2f2**、Python 和 UnityPy：

```powershell
pwsh -NoProfile -File .\build-fonts.ps1 -UnityPath '你的 Unity 5.4.2f2\Editor\Unity.exe'
```

通用多语言字体独立构建：`pwsh -NoProfile -File .\build-multilingual-fonts.ps1`，需要 Python 的 fontTools、UnityPy。制作步骤、字重和覆盖范围见[通用字体说明](Fonts/Multilingual/README.md)。此命令不会改动中文字体。

旧编辑器兼容参数、字体布局恢复与验证方法见[字体工程说明](FontBundleProject/README.md)。不要用新版本 Unity 升级该工程。

| 目录 | 内容 |
| --- | --- |
| `src/` | BepInEx 插件、显示适配与语言／字体服务 |
| `Languages/` | 各语言的译文和运行配置 |
| `Fonts/`、`FontBundleProject/` | 字体源、已构建 bundle 和字体工程 |
| `translator-kit/` | 翻译技能、模板生成和校验脚本 |
| `packaging/` | 随发行包复制的简短说明 |
| `tests/`、`tools/` | 离线检查、独立 Unity 验证和资源导出 |
| `docs/` | 图片文字清单与维护检查指南 |
| `artifacts/` | 构建结果、检查日志和生成文件，不提交 Git |

日常离线检查可运行 `pwsh -NoProfile -File .\tests\test.ps1`。其他检查按改动选择，见[检查与资源导出](docs/testing.md)。游戏内的位置、动画、输入及模组组合仍需实际验证。

## 常见问题

- **没有翻译：** 检查 DLL 路径、配置的语言目录及表格第一列；部分图片文字本来就未替换。
- **方框、英文回退或字体没更新：** 查看字体文件、映射与备用字体，替换字体后重启。新增汉字也应检查字形覆盖。
- **修改表格后报错或漏译：** 对照备份检查引号、制表符、占位符与转义，再运行校验脚本。
- **需要诊断信息：** 查看 `BepInEx/LogOutput.log` 中的插件版本、语言、字体加载或回退警告。`Loaded embedded font` 表示加载包内字体，`Created font by family (file identity unverified)` 表示按系统家族名称创建字体。
