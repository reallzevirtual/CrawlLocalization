# 尚未替换的图片文字

这些文字位于 ex2D 精灵或图集动画中，当前保留原图；修改 TSV 或字体不会改变图片里的字。资源位置来自各阶段调查，不代表全游戏完整清单。PathID 是调查线索，不是运行时绑定条件。

<a id="gameplay"></a>

## 本局交互与反馈

这些提示是 ex2D 精灵图集动画，**0.8.0 保留英文图片**。它们没有可直接替换的对应 TextMesh；向 TSV 增加词条或替换字体不会改变图片里的字。

| 显示内容 | 对象 / GameObject PathID | 动画 clip PathID | 图集帧名称 |
| --- | --- | --- | --- |
| LEVEL X / 升级 | LevelUpFeedback 1444；LevelUpL10 1445；LevelUpL11andUp 1446；LevelUpL2–L9 1447–1454 | 40735–40745 | level_up_* |
| HEALTH UP | StatUpHealth 1455 | 40746 | health_stat_up_feedback_* |
| 速度 / 力量提升 | StatUpSpeed 1456；StatUpStrength 1457 | 40747、40748 | 见下方完整索引 |
| 治疗粒子（非 HEAL 文字本体） | HealFeedback 2413 | 42380、42381 | heal_feedback_*、heal_feedback_flipped_* |
| Altered … 墓碑效果 | FX_Collect_AlteredBeastAxe/Flame/Skill/Speed/Strength/Sword 2494–2499 | 共用 42421 | alter_beast_* |
| +WRATH | PlusWrathSmall 9640 | 40843 | plusWrath_* |
| 教程版本升级 / 怒气 | LevelUpL2Pause/Tut/TutEnd 3171–3173；PlusWrathSmallTut/TutP2/TutP3 9641–9643 | 43861–43866 | 见下方完整索引 |

以上位置均在 `Crawl_Data/sharedassets0.assets`，使用图集 `exAtlas` PathID **39923**。PathID 用于调查，不是新增运行时替换条件。动画会改写精灵帧，所以不能只替换 exSprite 的初始帧；例如 HealFeedback 的初始帧是 orb_particle_02，实际治疗粒子动画才使用 heal_feedback_*；2026-10-29 逐帧确认这两个 clip 只有粒子，没有 HEAL 字形，HEAL 文字本体仍需另外追踪。同理，Altered各变体的初始精灵并不代表动画中文字。

### 完整对象索引

| 对象 | GameObject | exSpriteAnimation | clip（帧名称示例） |
| --- | --- | --- | --- |
| LevelUpFeedback | 1444 | 48739 | 40735 (level_up_feedback_01) |
| LevelUpL10 | 1445 | 48740 | 40736 (level_up_L10_01) |
| LevelUpL11andUp | 1446 | 48741 | 40737 (level_up_Lup_01) |
| LevelUpL2 | 1447 | 48742 | 40738 (level_up_L2_01) |
| LevelUpL3 | 1448 | 48743 | 40739 (level_up_L3_01) |
| LevelUpL4 | 1449 | 48744 | 40740 (level_up_L4_01) |
| LevelUpL5 | 1450 | 48745 | 40741 (level_up_L5_01) |
| LevelUpL6 | 1451 | 48746 | 40742 (level_up_L6_01) |
| LevelUpL7 | 1452 | 48747 | 40743 (level_up_L7_01) |
| LevelUpL8 | 1453 | 48748 | 40744 (level_up_L8_01) |
| LevelUpL9 | 1454 | 48749 | 40745 (level_up_L9_01) |
| StatUpHealth | 1455 | 48750 | 40746 (health_stat_up_feedback_01) |
| StatUpSpeed | 1456 | 48751 | 40747 (speed_stat_up_feedback_01) |
| StatUpStrength | 1457 | 48752 | 40748 (strength_stat_up_feedback_01) |
| HealFeedback | 2413 | 51451 | 42380 (heal_feedback_01)；42381 (heal_feedback_flipped_01) |
| FX_Collect_AlteredBeastAxe | 2494 | 51530 | 42421 (alter_beast_13) |
| FX_Collect_AlteredBeastFlame | 2495 | 51531 | 42421 (alter_beast_13) |
| FX_Collect_AlteredBeastSkill | 2496 | 51532 | 42421 (alter_beast_13) |
| FX_Collect_AlteredBeastSpeed | 2497 | 51533 | 42421 (alter_beast_13) |
| FX_Collect_AlteredBeastStrength | 2498 | 51534 | 42421 (alter_beast_13) |
| FX_Collect_AlteredBeastSword | 2499 | 51535 | 42421 (alter_beast_13) |
| LevelUpL2Pause | 3171 | 50318 | 43861 (level_up_L2_07) |
| LevelUpL2Tut | 3172 | 50319 | 43862 (level_up_L2_01) |
| LevelUpL2TutEnd | 3173 | 50320 | 43863 (level_up_L2_11) |
| PlusWrathSmall | 9640 | 64381 | 40843 (plusWrath_14) |
| PlusWrathSmallTut | 9641 | 64382 | 43864 (plusWrath_01) |
| PlusWrathSmallTutP2 | 9642 | 64383 | 43865 (plusWrath_08) |
| PlusWrathSmallTutP3 | 9643 | 64384 | 43866 (plusWrath_08) |

### 后续图片汉化方式

1. 按动画 clip 的每一帧定位图集元素，导出对应图片；确认文字、数字和闪光帧分别在哪些元素中。
2. 制作中文像素字动画，保留画布尺寸、原始尺寸、裁切偏移、帧时长与锚点，避免文字漂移。
3. 在本地插件内替换相关精灵/动画的显示资源，保留原动画状态和触发逻辑；不要把共享图集整体改写后影响其他对象。
4. 分别验证普通版、教程版、翻转治疗动画和不同等级；没有替换资源时回退原图。

此任务需要单独制作图片资源，本次没有修改图集、动画或这些反馈的游戏逻辑。

### 重现调查

```powershell
python .\tools\export_gameplay_texts.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl' --output artifacts/gameplay/catalog.json
```

导出结果包含133个非菜单TextMesh、204条交互组件记录（118条非空、39个不同用语）和28个图片反馈对象。每个图片对象记录组件、clip、逐帧图集引用/索引/时长，以及图集元素名、裁切矩形和UV坐标，便于后续继续处理。该清单只覆盖本次调查范围，不代表全游戏所有图片文字。

<a id="progression"></a>

## 升级与首领结算

本次汉化 TextMesh、菜单显示输入及动态弹窗。下列内容使用 ex2D 图集动画，按要求保留原图；增加字体或 TSV 词条不会改变其中的文字。

| 内容 | 动画资源 | 图集元素 | 状态 |
| --- | --- | --- | --- |
| SALVATION 胜利大字 | sharedassets0.assets:40765 / FXWinSalvation | salvation_03…16 | 保留原图 |
| HUMANITY STOLEN 失败效果 | sharedassets0.assets:40731 / HumanityStolen2Lines | humanity_stolen_2lines_* | 保留原图 |
| READY 准备按钮 | sharedassets5.assets:152–155 / ButtonReady_* | MonSlctBtns_05…08 | 保留原图 |
| READY 准备横幅 | sharedassets5.assets:206–210、216 / GUIBgrReady*、GUIBgrUnready | Bgr_Ready_* | 保留原图 |
| PEAK REACHED 中央动画 | sharedassets5.assets:220、221 / FX_PeakReached、FX_PeakReachedIdle | hands_peakReached_01…18 | 保留原图 |

升级按钮的悬停说明 `PEAK LEVEL\nREACHED` 是独立的文字，已经汉化为“已达／顶峰”；不要与中央图片动画混淆。`WRATH` 文字、`not enough`、升级怪物、强化灵质、查看英雄装备、返回地牢、返回等均为文字显示，已经处理。

结算画面的 `ESCAPED ALIVE!`、`LOST FOREVER`、`CONSUMED`、排名后缀、经验单位和首领击杀称号均为 TextMesh，已经汉化；玩家名字、排名数值和经验数值保留原样。三次失败后的结算与正常失败结算共用 MenuWin 的失败分支。

首领资源 BossHydra 56950、BossTentacleBlob 61646、BossBuriedStatue 65498 均指向 GameObject 4074（FXWinSalvation）和4070（HumanityStolenInFront）。后者实际使用的clip叫 HumanityStolen2Lines，不能只按对象名猜测动画资源。以上五个GameObject/组件位置都在sharedassets0.assets。

### 动画索引

| 文件与clip PathID | 动画名称 | 图集 |
| --- | --- | --- |
| sharedassets0.assets:40731 | HumanityStolen2Lines | sharedassets0.assets:39923 |
| sharedassets0.assets:40765 | FXWinSalvation | sharedassets0.assets:39923 |
| sharedassets5.assets:152 | ButtonReady_activateFromAbove | sharedassets0.assets:39934 |
| sharedassets5.assets:153 | ButtonReady_active | sharedassets0.assets:39934 |
| sharedassets5.assets:154 | ButtonReady_deactivate | sharedassets0.assets:39934 |
| sharedassets5.assets:155 | ButtonReady_inactive | sharedassets0.assets:39934 |
| sharedassets5.assets:206 | GUIBgrReadyIdle | sharedassets0.assets:39934 |
| sharedassets5.assets:207 | GUIBgrReadyP1Blue | sharedassets0.assets:39934 |
| sharedassets5.assets:208 | GUIBgrReadyP2Red | sharedassets0.assets:39934 |
| sharedassets5.assets:209 | GUIBgrReadyP3Green | sharedassets0.assets:39934 |
| sharedassets5.assets:210 | GUIBgrReadyP4Yellow | sharedassets0.assets:39934 |
| sharedassets5.assets:216 | GUIBgrUnready | sharedassets0.assets:39934 |
| sharedassets5.assets:220 | FX_PeakReached | sharedassets0.assets:39934 |
| sharedassets5.assets:221 | FX_PeakReachedIdle | sharedassets0.assets:39934 |

### 重现与后续处理

```powershell
python .\tools\export_progression_texts.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl' --output artifacts/progression/catalog.json
```

导出49个相关源组件、91个TextMesh布局和14个相关动画clip。布局包含排除在汉化之外的玩家名、数字和调试占位文本，用于检查覆盖边界；动画记录图集、帧索引、时长、元素名、UV与裁切矩形。

以后制作中文图片时，应保留原始画布尺寸、裁切偏移、帧时长、动画状态以及共享图集内其他元素。帧名中带Ready的其他技能特效不属于本清单。0.8.0发现的升级、治疗、怒气等图片文字另见 [本局图片提示](#gameplay)。

<a id="tutorial"></a>

## 序章教程

下列内容位于 `Crawl_Data/sharedassets0.assets`，由 ex2D 精灵动画显示，不是 TextMesh。此次保留原图；翻译表和字体替换不会改变它们。

| 出现位置 | 动画 clip PathID / 名称 | 处理 |
| --- | --- | --- |
| 开头 `- PROLOGUE -` | 43859 / FX_TitleCardPrologue | 保留英文图片 |
| 开启传送阵所需等级插图 | 43838 / UI_DialogBoxImage_Level10 | 保留图片；上下说明已翻译，并保留四个换行的插图留白 |
| 教程后段“记住……”之后的规则动画 | 43845–43850 / FX_GameFormat_1FollowQMarks、2HarvestXP、3GainLev10、4OpenPortal、5DefeatBeast、6Escape | 保留原动画；本次流程实际播放索引 0、1、3、4、5，索引2也列出供后续调查 |
| 教程中英雄升级 / 怒气小字 | 43862–43866 / LevelUpL2Tut、LevelUpL2TutEnd、PlusWrathSmallTut/P2/P3 | 保留原图，另见 [对应清单](#gameplay) |

攻击、特殊招式、门、史莱姆、怒气、经验的侧边插图为 43835–43841。它们继续由原教程显示，未替换图像；其中有无烘焙文字应在后续图片汉化时逐帧检查。

正文中的 `[A]` / `[B]` 是游戏控制键图片标签，完整保留，由游戏根据键盘/手柄选择图片。它们不应直接翻译成键盘字母 A/B。

`GuiMessageBox/Text` 和 `GuiMessageBoxTop/Text` 的资源初始文字是编辑器占位文字，实际显示前由 `SetContent` 改写，故没有把测试文案作为教程词条收录。

此清单来自资源引用和教程代码调查。没有启动游戏；动画帧的视觉效果、真实输入设备图标和完整游玩流程仍由用户验证。


## ex2D 帧图调查（2026-10-29）

可运行 `python tools/inspect_ex2d_frames.py --game-path <Crawl目录> --output artifacts/ex2d-audit`，导出六个代表动画的逐帧原画布 PNG、帧时长/事件及对照图。已确认序章标题为“空白帧＋标题图”，Follow ?为两帧问号闪烁，+WRATH为12帧、SALVATION为30个时间帧且含音效事件。字体生成图片可作为素材来源；保留原播放器及事件，仅替换显示资源的方案可行，但当前尚未实现图片本地化适配。
