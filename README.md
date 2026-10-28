# Crawl Localization

BepInEx 5 菜单本地化插件，版本 0.2.2，面向当前 Windows 版 Crawl / Unity 5.4.2。默认简体中文，支持繁体中文及恢复游戏原文。语言包、译文和字体可以独立更换；默认字体由 AssetBundle 内嵌提供，并保留原游戏的排版参数。

## 构建与安装

需要 PowerShell 7、.NET SDK，以及已安装 BepInEx 5 的 Crawl。编译引用游戏自己的 Mono 3.5 / Unity 程序集，不需要下载 NuGet 包。已有字体 bundle 时，编译插件无需 Unity Editor；重新制作字体包需要 Unity 5.4.2f2 和 Python + UnityPy。

```powershell
pwsh -NoProfile -File .\build.ps1
pwsh -NoProfile -File .\build.ps1 -Install
# 游戏安装在其它位置时：
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录' -Install
```

构建默认运行语言包与配置测试。输出在 `artifacts/CrawlLocalization-0.2.2/`；将该目录的内容复制到 `BepInEx/plugins/CrawlLocalization/` 即可安装。也可把 `artifacts/CrawlLocalization-0.2.2.zip` 解压到该位置。`-Install` 自动完成复制，要求游戏已关闭，并将原插件目录备份到 `artifacts/backups/`。不会启动游戏；不会修改游戏资源、DLL 或 BepInEx 配置文件。安装会更新插件内置语言包，手工改过的旧文件可从备份取回。

从 0.2.0 升级时应一并更新 DLL、两种语言的 `fonts.tsv` 和 `Fonts/crawl-fonts.bundle`，然后重启游戏。旧 TTF 留在插件目录不会影响新版；新版默认不再按系统字体家族查找它们。

从 0.2.1 升级到 0.2.2 时务必替换 `Fonts/crawl-fonts.bundle`，只替换 DLL 无法修复文字下移。关闭游戏再覆盖新版包；F8 不会重载已缓存的字体二进制文件。

## 选择语言

首次启动后，编辑 `BepInEx/config/crawl.localization.cfg`：

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

| Language | 效果 |
| --- | --- |
| `zh-Hans` 或 `zh-CN` | 简体中文（默认） |
| `zh-Hant` 或 `zh-TW` | 繁體中文 |
| `en` | 游戏原文、原字体 |

修改后按 F8 重读配置和语言包，已创建的菜单项也会刷新。也可重启游戏。`ReloadKey = None` 可禁用快捷键。缺失译文或语言包时保留游戏原文；不从另一种中文语言包补译。

0.1.0 的 `Translations/zh-CN.tsv` 与 `[Font] Family` 设置不再使用；为保留你的旧修改，升级不删除它们。自定义旧译文请迁入新语言包。`en` 恢复的是游戏实际提供的原文；当前游戏 DLL 中已有的手动中文修改不会被本插件撤销。

## 当前覆盖范围

两种语言各有 105 条精确译文或数字模板，包含主菜单、宝库分类、选项菜单、暂停菜单、玩家与按键设置，以及 CrawlOnline 的 ONLINE / ONLINE LOBBY 入口和常见菜单按钮。音量、显示安全区、电脑人数等数字更新和开关状态也有译文。

这是菜单本地化基础版，并非全游戏汉化。当前只处理 `MenuTextMenuItem` 菜单组件的文字；不包含宝库物品描述、普通场景 TextMesh、提示框标题与正文、CrawlOnline F1 的 IMGUI 窗口、玩家房间名、ex2D。其它 Mod 通过同一菜单系统加入的按钮，只需补译文表即可匹配；其它渲染路径需要新增适配代码。

## 目录与译文格式

```text
CrawlLocalization/
  CrawlLocalization.dll
  Languages/
    zh-Hans/
      language.ini
      translations.tsv
      fonts.tsv
    zh-Hant/
      language.ini
      translations.tsv
      fonts.tsv
  Fonts/
    crawl-fonts.bundle
```

新增语言时复制一个语言目录，改目录名，在配置中使用该名字即可。每种语言独立设置字体映射和备用字体。

`translations.tsv` 使用 UTF-8，每行两列，以真正的制表符 Tab 分隔：

```text
START GAME<TAB>开始游戏
Bots: {0}<TAB>电脑玩家：{0}
```

上面 `<TAB>` 是说明标记，实际文件应使用 Tab。原文不区分大小写，重复原文会报警并保留第一条。`{0}` 只匹配一个连续非负整数，不会吞掉玩家名或任意文字。译文需要保留 `{0}`。支持 `\n`、`\t`、`\\` 转义。以 `#` 开头的整行是注释；格式错误会在日志中给出文件名和行号并跳过该行。

## 字体替换与回退

已将你在 `D:\Work\else\crawlfont` 提供的十个合并字体打包为 `Fonts/crawl-fonts.bundle`。使用 Unity 5.4.2f2 / Windows x86 构建，包内 TTF 与源文件逐字节相同。简繁语言包目前共用这个包，但每种语言都能指定不同文件。字体只用于匹配到译文的菜单，未做全局字体替换。项目根目录的原始 TTF 保留用于重新构建，默认发布包只携带 bundle。

`fonts.tsv` 每行五列：

```text
原Unity字体名<TAB>加载类型<TAB>文件路径<TAB>内部字体家族名或bundle资源名<TAB>缩放
CrawlSmall<TAB>bundle<TAB>Fonts/crawl-fonts.bundle<TAB>assets/fonts/crawlsmall.ttf<TAB>1
```

第一列是运行时 `Font.name`，例如 `CrawlSmall`，不依赖 assets 文件名或 PathID。也可用 `*` 指定未单独映射的原字体。最后一列为大于 0、最多 10 的倍率，通常为 1。

支持三种加载类型：

| 类型 | 第三列 | 第四列 | 用途 |
| --- | --- | --- | --- |
| `bundle` | 兼容 AssetBundle 的相对路径 | bundle 内 Font 资源名或完整资源路径 | 当前默认方案，直接使用内嵌字体 |
| `ttf` | 插件目录下的 TTF/OTF 相对路径 | 字体内部 family 名 | 保留的 Windows 兼容入口，不能保证实际字体来源 |
| `system` | `-` | 已安装字体名称 | 例如 Microsoft YaHei |

TTF 模式用 Windows `AddFontResourceEx(FR_PRIVATE)` 将文件注册到当前游戏进程，再由 Unity 按 family 名创建动态字体；不会永久安装字体，插件销毁时释放注册。文件名与内部 family 名并不一定相同，例如两个 `04B_*.ttf` 的 family 都是 `04b03`。需要不同字体同时存在时，应使用不同的内部 family 名，或用 bundle 按资源名区分。

`Loaded embedded font: bundle|...` 表示成功取得包内 Font 资源。旧 TTF/系统模式日志现在明确标记 `Created font by family (file identity unverified)`，避免把按名称创建字体误当成确定加载了指定文件。`Mapped font unavailable or missing glyphs` 表示已触发备用流程。

字体包使用 Dynamic 模式并包含字体数据；原字号和渲染模式沿用 Crawl 的对应字体。不要用 Unity 2022/6000 重建这个旧版工程。替换源 TTF 后，在项目目录执行：

```powershell
pwsh -NoProfile -File .\build-fonts.ps1
python .\tests\check_bundle.py
pwsh -NoProfile -File .\build.ps1
```

构建脚本只有在字体包生成、重新加载和字形检查通过后，才会更新简繁字体映射。详见 `FontBundleProject/README.md`。玩家使用插件不需要安装 Unity。

0.2.2 额外保留 `m_Ascent`、`m_Descent`、`m_LineSpacing`。例如 CrawlSmall 的上升高度为 10.65625、行距为 14.09375；0.2.1 重新导入合并字体后曾变为 15.09375 和 18.53125，导致与手动替换字体不同的基线位置。现在先正常打包，再按 `FontBundleProject/font-layout.tsv` 恢复这三个序列化字段，最后在独立 Unity 进程中验证最终文件。不会修改 TTF 字节、菜单坐标或鼠标命中逻辑。

本机 Unity 5.4.2f2 曾在旧 OpenSSL 的 `sha1_block_data_order` 中闪退。构建脚本已对子进程加入 `OPENSSL_ia32cap=0:0` 兼容参数；手动编辑工程可双击项目根目录 `open-unity54.cmd`。这些脚本不修改系统环境变量，直接打开原 Unity.exe 仍可能闪退。

每个菜单项按下面顺序选择能完整显示译文的字体：

1. 当前语言 `fonts.tsv` 指定的字体。
2. 游戏原字体（如果已经包含所需字形）。
3. `language.ini` 的 `FallbackFonts` 列表，从左到右尝试。
4. 都不可用时恢复这个菜单项的原文、原字体和原字号，避免显示方框或空白。

例如：

```ini
Name=繁體中文
FallbackFonts=Microsoft JhengHei;Microsoft YaHei;PMingLiU
```

映射 TTF/bundle 保留原字号和样式，再应用倍率；系统备用字体按原字体字高适配。替换材质保留 Crawl 原着色器和属性，并同步动态字体图集。若需严格检查自定义字体，可暂时将 `FallbackFonts=` 留空；失败时会回到原字体或原文。

F8 可重读字体映射；字体对象按文件路径与内部名称缓存。替换同一路径下的字体二进制文件、补回曾加载失败的字体、修改字体 family 后，需重启游戏。

## 代码组织与边界

- `src/Plugin.cs`：配置、生命周期、F8 重载。
- `src/LanguagePack.cs`：不依赖 Unity 的译文/字体映射解析与数字匹配。
- `src/FontService.cs`：TTF、bundle、系统字体加载，字形检查与回退。
- `src/MenuPatches.cs`：菜单生命周期适配、原文记录、字体与材质还原。
- `FontBundleProject/`、`build-fonts.ps1`：Unity 5.4 字体导入、构建、包内加载验证。

Harmony 在菜单初始化时记录原文和字体，在菜单的 `GUIText.SetText` 输入处替换显示文字。游戏自己的 `m_text`、`GetText()`、`m_message` 与点击回调保持原值。按钮覆写与闪烁效果、隐藏菜单的延迟赋值另行处理；玩家名称与字母选择按回调排除。没有逐帧遍历场景，也没有对所有 MonoBehaviour 做反射改写。

## 验证与排错

```powershell
pwsh -NoProfile -File .\tests\test.ps1
# 可选：需要 Python 与 fontTools，检查两种语言所有译文的字形覆盖
python .\tests\check_fonts.py
# 需要 UnityPy 与 fontTools，检查包内字体原始字节、版本、平台和资源映射
python .\tests\check_bundle.py
# 可选：同时比对原游戏 Font 的全部标量排版/样式字段
python .\tests\check_bundle.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl'
```

64 项离线检查覆盖语言切换数据、别名、数字模板、缺失文件、坏行、重复项、路径和字体映射，以及 bundle 加载缓存和回退。Unity 在独立进程中重新加载十个字体，检查布局字段及每个字体的 229 个所需字形；二进制检查核对内嵌 TTF 与源文件一致、20 组语言/字体映射有效。布局回归检查会拒绝 0.2.1 的旧包。这些检查不能代替 Crawl 内的实际绘制和按钮尺寸验证。

游戏内建议依次检查：主菜单与鼠标/手柄选中效果；F8 简繁及原文切换；打开设置后改变音量/开关；进入宝库、暂停和联机菜单。测试回退时先关闭游戏，临时移动 Fonts 目录后重启；若同时清空备用列表，应保留原文。最后恢复文件与设置。

日志：`BepInEx/LogOutput.log`，搜索 `Crawl Localization`、`Loaded embedded font` 或 `Font load failed`。关闭游戏后移走 `BepInEx/plugins/CrawlLocalization` 即可卸载。
