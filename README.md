# Crawl Localization

BepInEx 5 中文本地化插件，版本 **0.3.1**，适配 Windows Crawl / Unity 5.4.2。当前只维护简体中文；保留恢复游戏原文的选项。包含菜单按钮及经过核对的独立 TextMesh 标题、提示文字，共 206 条译文/数字模板、67 个 TextMesh 路径。

## 构建与安装

需要 PowerShell 7、.NET SDK 和安装了 BepInEx 5 的 Crawl。引用游戏自己的 Mono 3.5 / Unity 程序集，无需 NuGet。已有字体 bundle 时，插件构建无需 Unity Editor。

```powershell
pwsh -NoProfile -File .\build.ps1
# 使用其他游戏位置：
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录'
# 如需自动安装，先关闭游戏；脚本会备份原插件目录：
pwsh -NoProfile -File .\build.ps1 -Install
```

输出 `artifacts/CrawlLocalization-0.3.1.zip`。关闭游戏，将 ZIP 内容解压至 `BepInEx/plugins/CrawlLocalization/`，然后重新启动。更新时覆盖 DLL、Languages 和 Fonts。新版不再分发繁体包，旧 `Languages/zh-Hant` 目录可删除；保留也不会被旧配置继续选中。不自动启动游戏。

## 配置与重载

配置文件：`BepInEx/config/crawl.localization.cfg`。

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

`zh-Hans` 或 `zh-CN` 使用简体中文；旧 `zh-Hant`、`zh-TW` 设置自动按简体中文处理。`en` 恢复游戏提供的原文与原字体，不撤销此前手动改过的游戏资源或 DLL。

F8 重载配置、译文及路径清单，刷新已创建的文字。`ReloadKey = None` 关闭快捷键。修改同路径字体二进制或恢复缺失字体后必须重启，F8 不清空字体缓存。

## 译文表及位置注释

`Languages/zh-Hans/translations.tsv` 是 UTF-8 TSV，现在有三列：

```text
原文<TAB>中文<TAB>位置注释
START GAME<TAB>开始游戏<TAB>主菜单／入口按钮
Note: Many keyboards do not\nsupport multiple players<TAB>注意：许多键盘不支持\n多名玩家同时操作<TAB>键盘设置／TextInfo
```

`<TAB>` 代表实际制表符，不是文件中的字面字符。第三列仅用于阅读，不参与匹配；兼容原来的两列文件。独立 TextMesh 的注释包含资源文件、PathID 和对象路径；PathID 仅是调查线索，不是运行时绑定条件。

原文匹配忽略大小写，并统一 CRLF、LF、CR 换行；不吞掉其他空格。支持 `\n`、`\r`、`\t`、`\\` 转义。`{0}` 只匹配连续非负整数，译文必须保留它，不能用来吞掉玩家名。重复键保留第一条并报警，格式错误给出文件和行号。缺译文保留原文。

## 菜单 TextMesh 如何处理

原来的菜单按钮仍在 `MenuTextMenuItem` 初始化和游戏 `GUIText.SetText` 显示入口本地化，保留内部原文、消息标识及回调。

像键盘设置 `TextInfo` 这样的文字直接保存在 TextMesh 上，不经过按钮入口。新增 `textmesh-paths.txt` 列出核对过的对象路径；插件在场景载入、菜单状态切换和 GUIText 启用时查找这些文字，包含未激活子对象，但不修改资源中的 prefab。

路径使用层级后缀匹配，忽略实例名 `(Clone)`；`MenuJoinPlayer1` 到 `MenuJoinPlayer4` 统一记成 `MenuJoinPlayer*`，其余路径不是任意通配符。绑定后复用字体替换和回退服务。直接写 TextMesh.text 的动态指令由已绑定标签检查变化，变化后才翻译；不每帧扫描全场景。经过 GUIText 的文字保留最终自动换行/图标展开结果，避免重复翻译。

增加独立文字时，在译文表添加原文和译文，再将核对过的对象路径添加到路径清单。不会自动翻译所有 MonoBehaviour 或所有 TextMesh；玩家名字、账号名、制作人员姓名、网址、编辑器占位文字和图标字符不作为通用替换目标。

已增加：键盘提示、街机模式说明、菜单标题、操作重绑的静态和动态提示、玩家加入的电脑说明、加载/保存提示、宝库标题及操作提示、暂停菜单游戏提示、制作人员页面的职能标题。物品/神祇完整描述、宝库动态统计、所有弹窗正文、CrawlOnline F1 IMGUI 和 ex2D 仍未全面覆盖。

## 重玩教程开关

CrawlOnline 的 `Patch_SystemTutorial_GetShouldStartTutorial` 在 `Unlocks.Enabled` 时强制返回 false。原菜单用这个“当前是否允许启动教程”的结果显示开关，导致已保存的开关开启后仍显示关。

新版只修正菜单显示来源：读取实际保存的教程开关。关闭显示“重玩教程：关”；开启且允许启动显示“重玩教程：开”；开启但被当前模式限制时显示“重玩教程：开（当前模式禁用）”。保留 CrawlOnline 和原游戏的教程限制，不把显示为开当成联机模式能运行教程。

## 字体及排版

`Fonts/crawl-fonts.bundle` 由 Unity 5.4.2f2 / Windows x86 构建，包含十个合并中文字体。内嵌 TTF 与根目录源文件逐字节相同，保留原游戏 Font 的 ascent、descent、line spacing；0.3.1 继续使用 0.2.2 修正后的字体包。

`fonts.tsv` 五列：原字体名、类型、相对文件路径、资源/家族名、倍率。默认按完整资源路径 `LoadAsset<Font>`。支持 `bundle`、兼容入口 `ttf` 和 `system`；TTF 按家族创建不能保证实际文件来源，推荐 bundle。

回退顺序：映射字体 → 能显示完整译文的原字体 → `language.ini` 中备用字体 → 原文和原字体。保留原字号、样式、材质 shader、字符缩放和图集同步。TextMesh 的坐标、锚点、对齐、行距不作补偿偏移。

需要更换源字体时：

```powershell
pwsh -NoProfile -File .\build-fonts.ps1
python .\tests\check_bundle.py
pwsh -NoProfile -File .\build.ps1
```

需要已激活的 Unity 5.4.2f2、Python + UnityPy。构建先完成独立导入/编译，再打包、恢复 Font 布局，最后由新的编辑器进程加载验证。详见 `FontBundleProject/README.md`。脚本为旧 OpenSSL 闪退设置进程级 `OPENSSL_ia32cap=0:0`；手工打开工程用 `open-unity54.cmd`。不要用新 Unity 升级此字体工程。

## 小字号中文模糊修复（0.3.1）

电脑玩家说明原本使用 `fontSize=16`、`characterSize=10`。合并字体中的汉字在这个字号只有约 5 像素高，先丢失笔画再放大，导致中文模糊；英文像素字形仍能显示清楚。

包含汉字的低字号（小于 64）标签现在以整数倍率提高字形生成字号到至少 160，同时按反比调整字符缩放。例如 16/10 改为 160/1；已有 160 字号的菜单项和纯英文字体采样保持原值，恢复原文时恢复原字号与缩放。映射字体和已有中文的原字体回退都使用此处理，不改源 TTF 或字体 bundle。

Unity 5.4 独立渲染复现并确认：示例中文的总高度、垂直中心不变，字形清晰；文字宽度会因字形取整精度改善略有变化。运行 `pwsh -NoProfile -File .\tests\check-text-sampling.ps1` 可生成对比图和尺寸记录，位于 `artifacts/text-sampling/`。这项检查不启动 Crawl。

## 检查和调查

```powershell
pwsh -NoProfile -File .\tests\test.ps1
python .\tests\check_fonts.py
python .\tests\check_bundle.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl'
# 导出主菜单、选项、暂停菜单所用资源的 TextMesh 线索；不启动游戏：
python .\tools\export_menu_texts.py --game-path '你的 Crawl 目录' --output artifacts/menu-textmeshes.json
```

离线检查覆盖三列/两列译文、换行兼容、数字模板、教程保存状态与模式限制、对象路径范围、字体映射和回退；字体检查确认新增译文的字形及原游戏布局。它们不能代替游戏内视觉与交互验证。

用户验证建议：连续切换重玩教程；打开键盘/街机设置；走完全部按键重绑提示；打开暂停菜单提示及制作人员页；F8 切换中文/原文；检查字体位置和鼠标选项对应关系。

日志在 `BepInEx/LogOutput.log`。`Loaded embedded font` 是包内字体；`Created font by family (file identity unverified)` 是按家族创建；缺字会触发回退。关闭游戏后移走插件目录可卸载。
