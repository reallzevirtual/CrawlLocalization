# Crawl Localization

BepInEx 5 中文本地化插件，版本 **0.5.1**，适配 Windows Crawl / Unity 5.4.2。当前只维护简体中文；保留恢复游戏原文的选项。包含菜单按钮及经过核对的独立 TextMesh 标题、提示文字，共 877 条译文/数字模板、81 个 TextMesh 路径，覆盖宝库分类说明、图鉴、成就、邪神选择及开局全屏提示。

## 构建与安装

需要 PowerShell 7、.NET SDK 和安装了 BepInEx 5 的 Crawl。引用游戏自己的 Mono 3.5 / Unity 程序集，无需 NuGet。已有字体 bundle 时，插件构建无需 Unity Editor。

```powershell
pwsh -NoProfile -File .\build.ps1
# 使用其他游戏位置：
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录'
# 如需自动安装，先关闭游戏；脚本会备份原插件目录：
pwsh -NoProfile -File .\build.ps1 -Install
```

输出 `artifacts/CrawlLocalization-0.5.1.zip`。关闭游戏，将 ZIP 内容解压至 `BepInEx/plugins/CrawlLocalization/`，然后重新启动。更新时覆盖 DLL、Languages 和 Fonts。新版不再分发繁体包，旧 `Languages/zh-Hant` 目录可删除；保留也不会被旧配置继续选中。不自动启动游戏。

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

`Languages/zh-Hans/translations.tsv` 存放通用菜单译文（211 条），`vault-translations.tsv` 存放宝库译文（595 条），`deity-translations.tsv` 存放邪神选择与试炼译文（71 条）。三份表均为 UTF-8 TSV，有三列：

```text
原文<TAB>中文<TAB>位置注释
START GAME<TAB>开始游戏<TAB>主菜单／入口按钮
Note: Many keyboards do not\nsupport multiple players<TAB>注意：许多键盘不支持\n多名玩家同时操作<TAB>键盘设置／TextInfo
```

`<TAB>` 代表实际制表符，不是文件中的字面字符。第三列仅用于阅读，不参与匹配；兼容原来的两列文件。独立 TextMesh 的注释包含资源文件、PathID 和对象路径；PathID 仅是调查线索，不是运行时绑定条件。

原文匹配忽略大小写，并统一 CRLF、LF、CR 换行；不吞掉其他空格。支持 `\n`、`\r`、`\t`、`\\` 转义。`{0}` 至 `{9}` 只匹配连续非负整数；同一行可包含多个数字，译文必须保留全部编号，可以调整顺序。编号中的前导零保留，不匹配玩家名。重复键保留第一条并报警，格式错误给出文件和行号。缺译文保留原文。

## 菜单 TextMesh 如何处理

原来的菜单按钮仍在 `MenuTextMenuItem` 初始化和游戏 `GUIText.SetText` 显示入口本地化，保留内部原文、消息标识及回调。

像键盘设置 `TextInfo` 这样的文字直接保存在 TextMesh 上，不经过按钮入口。新增 `textmesh-paths.txt` 列出核对过的对象路径；插件在场景载入、菜单状态切换和 GUIText 启用时查找这些文字，包含未激活子对象，但不修改资源中的 prefab。

路径使用层级后缀匹配，忽略实例名 `(Clone)`；`MenuJoinPlayer1` 到 `MenuJoinPlayer4` 统一记成 `MenuJoinPlayer*`，其余路径不是任意通配符。绑定后复用字体替换和回退服务。直接写 TextMesh.text 的动态指令由已绑定标签检查变化，变化后才翻译；不每帧扫描全场景。经过 GUIText 的文字保留最终自动换行/图标展开结果，避免重复翻译。

增加独立文字时，在译文表添加原文和译文，再将核对过的对象路径添加到路径清单。不会自动翻译所有 MonoBehaviour 或所有 TextMesh；玩家名字、账号名、制作人员姓名、网址、编辑器占位文字和图标字符不作为通用替换目标。

已增加：键盘提示、街机模式说明、菜单标题、操作重绑的静态和动态提示、玩家加入的电脑说明、加载/保存提示、宝库标题及操作提示、暂停菜单游戏提示、制作人员页面的职能标题。宝库内容覆盖见下节；游戏内商店/拾取提示、暂停中途加入与胜利画面的神祇相关文字、所有弹窗正文、CrawlOnline F1 IMGUI 和 ex2D 仍未全面覆盖。

## 宝库汉化（0.4.0）

- 分类悬停：发现数量、挑战通过数/总成绩、解锁更多条目的提示，以及成就完成数量。
- 怪物：61 个条目共用的“上次挑战 / 最佳成绩”和“尚未尝试此挑战”。游戏使用怪物头像，不把内部 prefab 名称当作玩家可见名称。
- 图鉴：40 种武器、61 种道具、66 类物件的名称、带编号列表和完整介绍，包含尚未解锁内容的译文。
- 成就：45 项名称与达成条件。“QUESTS”分类显示为“成就”，怪物试炼按钮仍显示“挑战”。

译文从当前游戏的 SystemLibrary、武器/道具组件、AchievementData 及 MenuLibrary 模板提取；不会更改发现状态、成就进度或挑战成绩。表格注释包含资源位置与用途。编号列表按游戏先截断英文名称、再添加两位编号的规则匹配。

原游戏的 TextWrapper 只按英文空格换行。现在仅对宝库中已绑定的中文标签按实际字形 advance 自动换行，保留段落，尽量让中文标点、英文单词和数字保持完整；不改变面板宽度、字体大小或对象坐标。当前宝库正文不含富文本字号/内嵌图标；以后加入这些内容时需要另行验证其宽度。

已从实际资源生成并核对 616 个显示案例。独立 Unity 5.4 检查覆盖 220 条正文/统计模板，使用包内 CrawlTextBodyThin 和原菜单宽度 80/82/100/105，所有行均未超宽，最长 5 行。该检查不加载 Crawl，面板高度、切换和鼠标交互仍需游戏内验证。

## 邪神选择与挑衅试炼（0.5.0）

覆盖四名玩家准备后的邪神界面：11 位邪神的名称、能力与初始装备说明、确认后的背景介绍，随机、不信奉、尚未发现提示，以及“不信奉”下 5 项挑衅试炼的名称、限制和确认介绍。另外包含 11 组玩家名彩蛋触发后的神名与介绍，触发用的玩家名保持原样。

例如 Teok 显示为“特奥克”，Murkan 显示为“穆尔坎”；戈洛斯、施里姆、戈尔、卡尔和格拉布沿用宝库成就中的译名。名称译法可在 `deity-translations.tsv` 中调整。

`Deity.GetName()` 和 `m_name` 仍返回英文，供解锁、成就和存档使用。插件只翻译选择页的 `DeityName` TextMesh，以及 `GUIText.SetText` 接收到的效果和背景介绍。三个路径按 MenuJoinPlayer1..4 绑定，不替换玩家账号名或玩家输入。

介绍沿用原字号160、字符缩放0.8、行距0.45及35单位栏宽，增加中文换行。名称保留原 CrawlMedium 字体映射和0.8缩放，正文仍使用 CrawlTextBodyThin。原字体包无需重做。

离线覆盖检查读取真实资源的160个显示案例，包括四份玩家面板和隐藏内容；可通过下方命令重复导出与验证。游戏内重点检查四位玩家切换选择、确认/返回、随机、不信奉试炼，以及 F8 切换原文后的恢复。

## 开局全屏提示（0.5.1）

启用游戏提示后显示的开局说明，共4页、5段文字，均已汉化：逃离死去的同伴、消灭野兽、收集生命精华升级、升至10级激活传送门、击败首领逃离深渊。

原文来自 `level3` 的 `MenuIntroAnimatic` 场景，属于普通 TextMesh。沿用已有场景加载与菜单状态扫描，将五个精确对象路径加入清单即可，包含尚未显示的分页。译文在通用 `translations.tsv` 中，保留原文颜色标签、行数、字号、字体映射、坐标及锚点；没有增加新的游戏逻辑补丁。

原版显示条件保持不变：启用提示，且真人玩家多于1名或尚未完成教程。开关开启不代表已完成教程的单人游戏每次都显示。运行 `export_menu_texts.py` 现在也会导出该场景。

## 重玩教程开关

CrawlOnline 的 `Patch_SystemTutorial_GetShouldStartTutorial` 在 `Unlocks.Enabled` 时强制返回 false。原菜单用这个“当前是否允许启动教程”的结果显示开关，导致已保存的开关开启后仍显示关。

新版只修正菜单显示来源：读取实际保存的教程开关。关闭显示“重玩教程：关”；开启且允许启动显示“重玩教程：开”；开启但被当前模式限制时显示“重玩教程：开（当前模式禁用）”。保留 CrawlOnline 和原游戏的教程限制，不把显示为开当成联机模式能运行教程。

## 字体及排版

`Fonts/crawl-fonts.bundle` 由 Unity 5.4.2f2 / Windows x86 构建，包含十个合并中文字体。内嵌 TTF 与根目录源文件逐字节相同，保留原游戏 Font 的 ascent、descent、line spacing；0.5.1 继续使用 0.2.2 修正后的字体包。

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
# 从实际资源导出宝库，并用运行时语言包检查显示案例：
# 额外需要 Python 包 UnityPy、TypeTreeGeneratorAPI
python .\tools\export_vault_texts.py --game-path '你的 Crawl 目录' --output artifacts/vault-catalog.json --cases-output artifacts/vault-cases.json
pwsh -NoProfile -File .\tests\test.ps1 -VaultCases artifacts/vault-cases.json
# 使用 Unity 5.4 实际 TextMesh 测量中文长段落，不启动 Crawl：
pwsh -NoProfile -File .\tests\check-vault-layout.ps1
# 导出邪神选择与试炼（包括四份玩家面板及彩蛋），验证译文和实际布局：
python .\tools\export_deity_texts.py --game-path '你的 Crawl 目录' --output artifacts/deity-catalog.json --cases-output artifacts/deity-cases.json
pwsh -NoProfile -File .\tests\test.ps1 -DeityCases artifacts/deity-cases.json
pwsh -NoProfile -File .\tests\check-deity-layout.ps1 -GamePath '你的 Crawl 目录'
# 导出主菜单、选项、暂停菜单及开局全屏提示的 TextMesh 线索；不启动游戏：
python .\tools\export_menu_texts.py --game-path '你的 Crawl 目录' --output artifacts/menu-textmeshes.json
```

离线检查覆盖三列/两列译文、换行兼容、数字模板、教程保存状态与模式限制、对象路径范围、字体映射和回退；字体检查确认新增译文的字形及原游戏布局。它们不能代替游戏内视觉与交互验证。

用户验证建议：悬停宝库五个分类；查看未尝试/有记录的怪物挑战；浏览武器、道具、物件的长介绍与未发现条目；检查成就名称、条件和进度；连续切换重玩教程；打开键盘/街机设置；走完全部按键重绑提示；打开暂停菜单提示及制作人员页；F8 切换中文/原文；检查字体位置和鼠标选项对应关系。

日志在 `BepInEx/LogOutput.log`。`Loaded embedded font` 是包内字体；`Created font by family (file identity unverified)` 是按家族创建；缺字会触发回退。关闭游戏后移走插件目录可卸载。
