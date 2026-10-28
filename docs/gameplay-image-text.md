# 本局图片提示调查（0.8.0）

这些提示是 ex2D 精灵图集动画，**0.8.0 保留英文图片**。它们没有可直接替换的对应 TextMesh；向 TSV 增加词条或替换字体不会改变图片里的字。

| 显示内容 | 对象 / GameObject PathID | 动画 clip PathID | 图集帧名称 |
| --- | --- | --- | --- |
| LEVEL X / 升级 | LevelUpFeedback 1444；LevelUpL10 1445；LevelUpL11andUp 1446；LevelUpL2–L9 1447–1454 | 40735–40745 | level_up_* |
| HEALTH UP | StatUpHealth 1455 | 40746 | health_stat_up_feedback_* |
| 速度 / 力量提升 | StatUpSpeed 1456；StatUpStrength 1457 | 40747、40748 | 见下方完整索引 |
| HEAL | HealFeedback 2413 | 42380、42381 | heal_feedback_*、heal_feedback_flipped_* |
| Altered … 墓碑效果 | FX_Collect_AlteredBeastAxe/Flame/Skill/Speed/Strength/Sword 2494–2499 | 共用 42421 | alter_beast_* |
| +WRATH | PlusWrathSmall 9640 | 40843 | plusWrath_* |
| 教程版本升级 / 怒气 | LevelUpL2Pause/Tut/TutEnd 3171–3173；PlusWrathSmallTut/TutP2/TutP3 9641–9643 | 43861–43866 | 见下方完整索引 |

以上位置均在 `Crawl_Data/sharedassets0.assets`，使用图集 `exAtlas` PathID **39923**。PathID 用于调查，不是新增运行时替换条件。动画会改写精灵帧，所以不能只替换 exSprite 的初始帧；例如 HealFeedback 的初始帧是 orb_particle_02，实际治疗动画才使用 heal_feedback_*。同理，Altered各变体的初始精灵并不代表动画中文字。

## 完整对象索引

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

## 后续图片汉化方式

1. 按动画 clip 的每一帧定位图集元素，导出对应图片；确认文字、数字和闪光帧分别在哪些元素中。
2. 制作中文像素字动画，保留画布尺寸、原始尺寸、裁切偏移、帧时长与锚点，避免文字漂移。
3. 在本地插件内替换相关精灵/动画的显示资源，保留原动画状态和触发逻辑；不要把共享图集整体改写后影响其他对象。
4. 分别验证普通版、教程版、翻转治疗动画和不同等级；没有替换资源时回退原图。

此任务需要单独制作图片资源，本次没有修改图集、动画或这些反馈的游戏逻辑。

## 重现调查

```powershell
python .\tools\export_gameplay_texts.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl' --output artifacts/gameplay/catalog.json
```

导出结果包含133个非菜单TextMesh、204条交互组件记录（118条非空、39个不同用语）和28个图片反馈对象。每个图片对象记录组件、clip、逐帧图集引用/索引/时长，以及图集元素名、裁切矩形和UV坐标，便于后续继续处理。该清单只覆盖本次调查范围，不代表全游戏所有图片文字。
