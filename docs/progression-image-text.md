# 暂停、升级与结算：图片文字清单（0.9.0）

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

## 动画索引

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

## 重现与后续处理

```powershell
python .\tools\export_progression_texts.py --game-path 'D:\SteamLibrary\steamapps\common\Crawl' --output artifacts/progression/catalog.json
```

导出49个相关源组件、91个TextMesh布局和14个相关动画clip。布局包含排除在汉化之外的玩家名、数字和调试占位文本，用于检查覆盖边界；动画记录图集、帧索引、时长、元素名、UV与裁切矩形。

以后制作中文图片时，应保留原始画布尺寸、裁切偏移、帧时长、动画状态以及共享图集内其他元素。帧名中带Ready的其他技能特效不属于本清单。0.8.0发现的升级、治疗、怒气等图片文字另见 [本局图片提示](gameplay-image-text.md)。
