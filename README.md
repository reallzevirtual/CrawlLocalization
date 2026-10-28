# Crawl Localization

BepInEx 5 中文本地化插件，版本 **0.13.0**，适配 Windows Crawl / Unity 5.4.2。项目当前只维护简体中文译文；支持独立安装其他语言包和恢复游戏原文。包含菜单按钮及经过核对的独立 TextMesh 标题、提示文字，共 1492 条译文/模板、140 个 TextMesh 路径，覆盖宝库分类说明、图鉴、成就、邪神选择、开局全屏提示、章节标题、商店、常驻玩家装备面板、本局交互提示、暂停弹窗、怪物升级、首领结算、序章教程流程，以及可选 CrawlOnline 0.4.0 的联机界面。

## 构建与安装

需要 PowerShell 7、.NET SDK 和安装了 BepInEx 5 的 Crawl。引用游戏自己的 Mono 3.5 / Unity 程序集，无需 NuGet。已有字体 bundle 时，插件构建无需 Unity Editor。

```powershell
pwsh -NoProfile -File .\build.ps1
# 使用其他游戏位置：
pwsh -NoProfile -File .\build.ps1 -GamePath '你的 Crawl 目录'
# 如需自动安装，先关闭游戏；脚本会备份原插件目录：
pwsh -NoProfile -File .\build.ps1 -Install
```

输出 `artifacts/CrawlLocalization-0.13.0.zip`。关闭游戏，将 ZIP 内容解压至 `BepInEx/plugins/CrawlLocalization/`，然后重新启动。更新时覆盖 DLL、Languages 和 Fonts。新版不再分发繁体包，已有 `Languages/zh-Hant` 可作为独立繁体包使用。不自动启动游戏。

普通构建会先运行离线检查，再编译 DLL，最后自动生成 ZIP；不会启动游戏。字体包已存在时无需启动 Unity，只有重新制作字体才运行 `build-fonts.ps1`。

给玩家的 ZIP 仅含 `CrawlLocalization.dll`、`Languages/zh-Hans/` 的运行数据，以及引用的 `Fonts/crawl-fonts.bundle`。不包含项目 README、CHANGELOG、docs、源码、测试文件或字体源 TTF。构建前清空版本对应的暂存目录内容，并按明确清单复制语言文件，避免旧文件残留或误带开发文件。

语言目录的 `textmesh-paths.txt` 是独立文字对象的绑定清单，删除会漏译；`language.ini` 配置备用字体，缺失会产生读取警告并失去这层字体回退；`fonts.tsv` 是游戏字体到替换字体的映射。这些是程序数据，玩家无需编辑。

## 配置与重载

配置文件：`BepInEx/config/crawl.localization.cfg`。

```ini
[General]
Language = zh-Hans
ReloadKey = F8
```

`zh-Hans` 或 `zh-CN` 使用简体中文；`zh-Hant` 或 `zh-TW` 使用独立繁体包，未安装则保留英文。`es`、`ru` 等加载 `Languages/` 下的同名目录，各语言不互相补译。`en` 恢复游戏提供的原文与原字体，不撤销此前手动改过的游戏资源或 DLL。

F8 重载配置、译文及路径清单，刷新已创建的文字。`ReloadKey = None` 关闭快捷键。修改同路径字体二进制或恢复缺失字体后必须重启，F8 不清空字体缓存。

## 译文表及位置注释

`Languages/zh-Hans/translations.tsv` 存放通用菜单译文（194 条），`vault-translations.tsv` 存放宝库译文（595 条），`deity-translations.tsv` 存放邪神选择与试炼译文（71 条），`chapter-translations.tsv` 存放章节编号和随机楼层名（132 条），`shop-translations.tsv` 存放商店新增组件和提示（170 条）。`gameplay-translations.tsv` 存放本局交互、金币单位、传送阵和连杀提示（50 条）。`progression-translations.tsv` 存放暂停弹窗、升级和首领结算新增文案（49 条）。`shop-il-translations.tsv` 存放原始字符串操作中的连接词、格式模板和属性比较片段（35 条）。`crawlonline-translations.tsv` 单独维护 CrawlOnline 的135条界面词条（含从通用表迁出的17条）。`tutorial-translations.tsv` 单独维护序章流程的61条新增词条，复用确认、暂停退出、传送阵和怪物升级已有译文。十份表均为 UTF-8 TSV，有三列：

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

已增加：键盘提示、街机模式说明、菜单标题、操作重绑的静态和动态提示、玩家加入的电脑说明、加载/保存提示、宝库标题及操作提示、暂停菜单游戏提示、制作人员页面的职能标题。宝库内容覆盖见下节；未列入本次范围的拾取/剧情提示、其他弹窗正文、CrawlOnline F1 IMGUI 和 ex2D 图片文字仍未全面覆盖。

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

## 商店汉化（0.10.0 IL 重构）

覆盖商店武器、技能、遗物、药水的名称与说明，武器随机前缀、魔法句式、持有/命中状态、攻速与伤害说明，以及换购、替换、金币不足、禁止购买、已拥有等提示。基础词条复用宝库；组件词条在 `shop-translations.tsv`，原始拼接片段在 `shop-il-translations.tsv`，第三列保留来源注释。

使用 Harmony Transpiler 在内存中替换五个方法里的字符串操作调用：`WeaponData.GetDisplayName`、`WeaponData.GetDescription`、`PropPurchasable.ShowInfoBox`、`GUIStore.Show`、`GUIStoreStatsDisplay.Show`。不修改磁盘上的游戏 DLL。原方法继续决定前缀、随机魔法句式、攻速/伤害分支、换购条件、比较符号及数值；本地化不再另外读取武器状态、重算比较或重复调用属性获取方法。

字符串包装函数执行原来的 Append、AppendFormat、Concat 等操作，原返回值及名称、前缀、描述缓存仍为英文；同时记录此次实际使用的文本片段、参数和顺序。显示入口按这份记录逐段查表，生成中文副本交给 TextMesh/GUIText。记录按字符串对象身份关联，不解析最终英文句子；F8重载只重新渲染已有记录，不重新选择前缀或生成描述。

缺少任一片段译文、出现未记录的拼接修改或无法安全记录的自定义对象时，整个标签回退到原文，保留其他 mod 的内容。新增内容若沿用已覆盖的字符串操作，可以补词条；另行生成字符串、替换显示入口或重写同一段 IL 的 mod 仍可能需要适配。缺字体继续走原有回退机制。

`@shop.prefix:`、`@shop.status:` 等为组件键；`@shop.il:` 为原始连接词、颜色比较字符串和格式模板。格式参数 `{0}`、`{1}` 必须保留。仅 `@shop.il:` 允许空译文，用于去掉游戏单独追加的英文句点（既有中文基础描述已带标点）。中文连接词和标点由表格定义，名称保留原拼接空格，例如 `Swift Shortsword` →“迅捷 短剑”。`@shop.traits:` 等旧组合模板保留作词表兼容，新实现不再用它们重写属性分支。

商店正文继续使用资源中的115单位宽度，字体包及布局元数据不变。检查覆盖456项离线断言、五个真实游戏方法的70处IL替换、隔离方法的Harmony执行、190组源资源、3,893个Unity文字排版案例及17项Unity显示/语言重载检查。游戏内完整面板、实际交互及其他mod组合仍由用户验证；没有安装或启动Crawl。

建议验证：连续切换武器/药水/技能/遗物；查看带前缀、魔法和状态的武器；对照属性符号、数值和价格；测试金币不足、禁止购买、已拥有及换购；切换 `en` 后按F8确认原文恢复，再切回中文。


## 常驻玩家装备面板（0.7.2）

汉化 `GUIHeroStats` 面板上的武器名称、独立前缀、技能及遗物名称，直接复用宝库与商店词条，无需新增翻译。适配面板本身的刷新方法，不依赖进入商店；购买、换装及面板重新显示时会随原游戏更新。

武器/技能在原 `UpdateText` 写入各个名称后翻译。相同输入复用已渲染文本，避免每帧重复查表和检查字体；F8刷新后缓存会使用新结果。遗物只在原 `UpdateArtifactIcons` 实际改写名称时处理，保持原列表次序，并提前翻译暂时隐藏的滚动项。HUD简称没有专用词条时复用物品全名译文，恢复英文时仍显示原简称。

保留原前缀分行、属性数字、图标、布局、滚动和透明度。独立Unity检查覆盖12项换装/列表复用/缓存/重载行为，以及328个来自实际资源的名称显示案例；完整游戏里的位置和滚动效果仍需用户确认。

## 本局交互与反馈（0.8.0）

新增 `GAME TIPS`（游戏提示）、`DEFILE`（亵渎）、`WISH`（许愿）、`MAKE OFFERING`（献上祭品）等39种资源交互用语；补齐书籍、墓碑、献祭装置的静态提示。`GUIActionIndicator.SetText` 写入原文后仅翻译显示，未知mod交互保留原文，并更新重载来源，避免反复使用同一提示框时残留旧文本。

金币奖励中的 `gold` 单独译为“金”，数字与“+”标签不动；`REWARD` 为“奖励”。此单位采用专用键 `@gameplay.reward-unit:gold`，其他位置的“金币”译文不受影响。首领传送阵显示“需要 10 级”与“激活”；`{0}x Streak` 显示为“{0}x 连杀”，保留数字、前导零和 x，另补“本局最佳”及战斗奖励提示。

动态道具创建后在各自 Start 结束时绑定已核对路径，包含隐藏文字；连杀在游戏写入数字后即时刷新。字体、颜色、显示开关、交互状态、等级条件及回调由原游戏维护。没有全场景逐帧搜索。

**尚未汉化的图片文字：** `LEVEL X`、`HEALTH UP`（以及速度/力量提升）、`+WRATH`、`HEAL`、墓碑效果 `Altered …` 属于 ex2D 精灵/图集动画，不能靠本次 TextMesh 译文替换。资源位置与后续方案见 [图片提示清单](docs/gameplay-image-text.md)。本次不修改这些图片。

验证使用独立Unity5.4项目，覆盖118条非空交互资源、21个静态标签，以及提示框复用、原文恢复、金币单位、隐藏传送阵文字和连杀数字。实际游戏的位置、淡入淡出和操作反馈仍由用户确认；未安装或启动Crawl。

## 暂停、升级与首领结算（0.9.0）

暂停弹窗覆盖退出本局、退出教程，以及中途加入/退出通知。玩家编号、名字和等级从原游戏数据读取并保存快照；只有与原游戏组合结果完全一致的通知才套用中文模板，不通过宽泛正则拆解玩家名。邪神名称与能力复用已有词条，未知mod内容保留原文。弹窗先翻译再按原宽132换行和计算背景高度，退出、取消、电脑接管等回调不变。

四名玩家的下层界面覆盖怒气标签、升级怪物、强化灵质、查看英雄装备、游戏提示、返回地牢、返回、怒气不足、未解锁和“已达顶峰”悬停说明。怒气、等级和升级费用数字、购买条件与闪烁逻辑保持原游戏行为。

首领登场的KOUROK、GHIDRAAK、Tezekcal分别显示为库洛克、吉德拉克、特泽卡尔，末次机会提示为“最后的挣扎”。只绑定两个登场文字对象，不修改Boss.GetDisplayName，避免干扰成就比较。胜负结算补齐逃出生天、永远迷失、被吞噬、21种称号、经验单位与排名后缀；玩家名和所有数值保持原样。称号查表处理原列表残留的回车，英文模式恢复游戏原始文本。

动态玩家通知使用 `@progression.*` 模板，参数由显示代码提供，不参与全局数字匹配。维护译文时保留所有占位符；缺词或无效格式回退原文。F8使用通知创建时的数据快照，不会因玩家随后改名而改变已打开的通知。

**图片暂未汉化：** SALVATION、HUMANITY STOLEN、READY按钮/横幅及中央PEAK REACHED动画。详情见 [升级与结算图片清单](docs/progression-image-text.md)。其中“已达顶峰”的文字悬停说明已经翻译。

验证：544项既有离线检查、234项新组合检查和245项独立Unity5.4检查通过；后者包含40个资源文字布局、72个源组件显示案例、四玩家弹窗、玩家名保护、原文恢复、胜负初始化顺序及首领文字绑定。十字体字形覆盖通过。未安装或启动Crawl；实际按钮操作、视觉位置与动画仍由用户验证。

## 章节编号与楼层名（0.6.0）

进局和换层时显示的 `- Chapter {0} -` 翻译为 `- 第 {0} 章 -`，并覆盖地牢、墓穴、矿区环境的全部131个不同随机楼层名。源资源是 sharedassets0.assets 中44824、44832、44876、44921、45936的Environment，以及55903的LevelTitle。重复环境共用译文，表格注释保留全部来源。

标题由游戏动态生成，直接写入TextMesh。新增一个 `LevelTitle.Start` 后置补丁，在游戏选好名称后直接绑定 `LevelTitle` 的 `m_textLevel` 和 `m_textFloor` 引用，复用原字体替换和恢复机制。0.7.1 修正了旧版按路径扫描漏绑的问题：这两个对象还挂着 `MenuTextMenuItem`，会被通用扫描跳过，却没有走普通菜单初始化。直接绑定后立即读取当前原文并刷新，支持已有标签、中英文重载和缺词回退。只改屏幕文字，环境名称池、随机选名及已使用名称记录仍保留原文。

编号使用CrawlTextBodyThin，楼层名使用CrawlLarge，均保留字号160、字符缩放1和居中锚点。220个资源显示案例与Unity5.4实际尺寸检查通过；中文标题维持单行，最宽楼层名94.5单位，小于原文集合最大宽度176。该检查不启动Crawl；标题生成、淡入淡出和换层仍由用户在游戏中验证。

## CrawlOnline 界面（0.11.0）

`crawlonline-translations.tsv` 使用 `@online:` 键，独立维护模组文案。原来混在通用表中的17条联机按钮译文已迁出；游戏本体也使用的返回、取消、游戏提示开关等仍保留通用词条。模组表的 Public、Back 等词不会作为普通全局词条去匹配玩家名字。

覆盖主菜单联机入口、PLAY ONLINE/SERVER LIST标题、寻找或创建房间说明、创建大厅与可见性、搜索状态、服务器数量/分页、好友标记、满员/版本不符提示、关闭确认、动态大厅名单和角落面板。还适配F1面板的创建/加入/直连、地址/端口标签、邀请/移出/断开、连接状态、属性开关和常见错误。

这是同一个本地化插件中的可选适配，不需要再安装第二个汉化插件。通过软依赖与反射检测CrawlOnline，无模组时不安装这些补丁，编译也不引用其DLL。当前核对版本为0.4.0；未来模组改动显示入口可能需要更新适配。

只在OnlineMenu和Overlay的17个UI方法中标记固定字符串、记录原始拼接并替换显示调用。原始字符串值、按钮回调、输入框内容和模组状态保持原样；实际译文交给现有GUIText/TextMesh或F1的IMGUI显示。玩家名、房间名、地址、版本号、延迟与人数作为原样数据处理。已经组合好的客户端名单仅识别末尾带数字的手柄数量结构；错误只识别已知提示和前缀，底层异常详情、未识别诊断保留原文。

F1采用屏幕像素字号，不套用TextMesh的字形采样缩放；优先使用可显示译文的原字体，再使用language.ini的FallbackFonts。可在fonts.tsv中增加原字体键 `CrawlOnlineOverlay` 指定专用字体。显示时复制GUIStyle，原样式和输入框字体不改；缺字回退原文。主游戏菜单继续使用现有字体bundle和中文换行。

F8可刷新已打开的普通弹窗、名单和菜单；F1在下一次绘制读取当前语言。切换en恢复原文。请在游戏内检查：主菜单联机→服务器列表→创建大厅→可见性→关闭确认，以及F1直连面板、名单刷新和英文恢复。离线检查不会连接Steam、创建房间或启动Crawl。

## 重玩教程开关

CrawlOnline 的 `Patch_SystemTutorial_GetShouldStartTutorial` 在 `Unlocks.Enabled` 时强制返回 false。原菜单用这个“当前是否允许启动教程”的结果显示开关，导致已保存的开关开启后仍显示关。

新版只修正菜单显示来源：读取实际保存的教程开关。关闭显示“重玩教程：关”；开启且允许启动显示“重玩教程：开”；开启但被当前模式限制时显示“重玩教程：开（当前模式禁用）”。保留 CrawlOnline 和原游戏的教程限制，不把显示为开当成联机模式能运行教程。

## 字体及排版

`Fonts/crawl-fonts.bundle` 由 Unity 5.4.2f2 / Windows x86 构建，包含十个合并中文字体。内嵌 TTF 与根目录源文件逐字节相同，保留原游戏 Font 的 ascent、descent、line spacing；0.13.0 继续使用 0.2.2 修正后的字体包。

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
# 只读核对已安装CrawlOnline的UI IL，隔离Unity检查显示（不启动游戏）：
pwsh -NoProfile -File .\tests\check-online-il.ps1
pwsh -NoProfile -File .\tests\check-online-display.ps1
# 序章教程：只读核对原DLL词条，以及独立Unity显示/换行/回退检查：
pwsh -NoProfile -File .\tests\check-tutorial-il.ps1
pwsh -NoProfile -File .\tests\check-tutorial.ps1
python .\tests\check_fonts.py
python .\tests\check_bundle.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl'
# 导出并验证暂停弹窗、升级及首领结算（不启动游戏）：
pwsh -NoProfile -File .\tests\check-progression.ps1
# 导出并验证本局交互、传送阵、奖励和连杀（不启动游戏）：
pwsh -NoProfile -File .\tests\check-gameplay.ps1
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
# 导出章节编号、环境楼层名及其实际布局（只需Python + UnityPy）：
python .\tools\export_chapter_texts.py --game-path '你的 Crawl 目录' --output artifacts/chapter-catalog.json --cases-output artifacts/chapter-cases.json
pwsh -NoProfile -File .\tests\test.ps1 -ChapterCases artifacts/chapter-cases.json
pwsh -NoProfile -File .\tests\check-chapter-layout.ps1 -GamePath '你的 Crawl 目录'
# 复现章节MenuTextMenuItem漏绑，检查实际绑定组件、字体及语言恢复：
pwsh -NoProfile -File .\tests\check-chapter-binding.ps1 -GamePath '你的 Crawl 目录'
# 从实际资源核对商店组件，并验证字符串操作IL补丁和Unity文字排版：
python .\tools\export_shop_texts.py --game-path '你的 Crawl 目录' --output artifacts/shop-catalog.json
pwsh -NoProfile -File .\tests\check-shop-catalog.ps1
pwsh -NoProfile -File .\tests\check-shop-il.ps1 -GamePath '你的 Crawl 目录'
pwsh -NoProfile -File .\tests\check-shop-layout.ps1 -GamePath '你的 Crawl 目录'
pwsh -NoProfile -File .\tests\check-shop-display.ps1 -GamePath '你的 Crawl 目录'
# 常驻装备面板：名称复用、每帧缓存、滚动遗物、语言恢复及实际字体测量：
pwsh -NoProfile -File .\tests\check-hero-stats.ps1 -GamePath '你的 Crawl 目录'
# 导出主菜单、选项、暂停菜单及开局全屏提示的 TextMesh 线索；不启动游戏：
python .\tools\export_menu_texts.py --game-path '你的 Crawl 目录' --output artifacts/menu-textmeshes.json
```

离线检查覆盖三列/两列译文、换行兼容、数字模板、教程保存状态与模式限制、对象路径范围、字体映射和回退；字体检查确认新增译文的字形及原游戏布局。它们不能代替游戏内视觉与交互验证。

用户验证建议：悬停宝库五个分类；查看未尝试/有记录的怪物挑战；浏览武器、道具、物件的长介绍与未发现条目；检查成就名称、条件和进度；连续切换重玩教程；打开键盘/街机设置；走完全部按键重绑提示；打开暂停菜单提示及制作人员页；F8 切换中文/原文；检查字体位置和鼠标选项对应关系。

日志在 `BepInEx/LogOutput.log`。`Loaded embedded font` 是包内字体；`Created font by family (file identity unverified)` 是按家族创建；缺字会触发回退。关闭游戏后移走插件目录可卸载。

## 序章教程（0.12.0）

覆盖问号引路、攻击与冷却、门与水晶、特殊技、死亡与幽灵、附身陷阱、灵质与黏液怪、五芒星法阵、弓箭、伤害与金币、人性与复活、经验与怒气、传送阵、多名玩家、下层怪物升级和教程结束选项。

在 `GuiTutorialMessageBox.SetContent` / `ShowButton` 执行前绑定其文字组件，再由现有 `GUIText.SetText` 显示入口查表和换字体。原方法继续完成显示、图标解析、背景尺寸和位置计算；不修改教程协程、等待条件、伤害、输入、关卡或存档，也不改变 CrawlOnline 的教程禁用规则。

`[A]` / `[B]` 保留给游戏按当前输入设备生成图标。仅教程中文提示启用富文本换行，计入局部字号和内嵌图片占宽，保留高亮、原有插图留白和显式换行。F8 可刷新当前提示，英文或缺失词条回退原文。

开头 `PROLOGUE`、后段规则动画及升级/怒气小字中的图片文字尚未替换，索引见 `docs/tutorial-image-text.md`。正文不会把图片当作漏译词条。

## 创建其他语言（0.13.0）

给译者使用 `artifacts/CrawlLocalization-0.13.0-Translator.zip`：它包含普通插件运行文件，以及 `crawl-language-pack/SKILL.md` 和两个 PowerShell 脚本。给普通玩家的 ZIP 仍只有运行必需文件。可以让另一个 AI 读取该 SKILL.md，按指定语言创建和维护译文。

解压翻译制作包，在其目录运行（系统 PowerShell 或 pwsh 均可）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\new-language.ps1 -PluginRoot . -Language es -DisplayName "Español"
powershell -NoProfile -ExecutionPolicy Bypass -File .\crawl-language-pack\scripts\check-language.ps1 -PluginRoot . -Language es
```

脚本生成 `Languages/es/`，只需把各译文表第二列的 `@TODO` 换成西班牙语，再在游戏的 `BepInEx/config/crawl.localization.cfg` 设置 `Language = es`，按 F8 或重新启动。无需修改 DLL 或安装 Unity。`@TODO`、缺译和字体失败均回退游戏／模组英文原文，不查中文或其他语言表；有意省略商店连接片段时仍可使用空的 `@shop.il:` 译文。

新建包默认尝试原游戏字体和所选系统备用字体，未自动继承中文字体。需要时在 `fonts.tsv` 配置适合目标语言的字体。中文按字换行、中文标点规则和小字号采样增强只在当前中文语言包中触发；其他语言不触发这些规则。特殊文字系统的排版支持需要单独验证，不能仅凭有字体就承诺完全支持。

也可手动复制语言目录，仅修改第二列与字体配置；未译项目设为 `@TODO`，不要把中文留作待译占位。首次生成的第三列包含中文参考，仅供译者阅读，不参与显示或回退。创建后，目标语言的运行不依赖中文语言包。新增语言由其维护者负责，项目没有附带完整西班牙语或俄语译文。

维护者从源码选择发行语言可运行 `build.ps1 -Languages zh-Hans,es`（在 PowerShell 中），按指定目录打包；无需改构建脚本。默认仍只发布已有简体中文。完整规范见 [翻译技能](translator-kit/crawl-language-pack/SKILL.md)。
