# 序章教程图片文字（0.12.0）

下列内容位于 `Crawl_Data/sharedassets0.assets`，由 ex2D 精灵动画显示，不是 TextMesh。此次保留原图；翻译表和字体替换不会改变它们。

| 出现位置 | 动画 clip PathID / 名称 | 处理 |
| --- | --- | --- |
| 开头 `- PROLOGUE -` | 43859 / FX_TitleCardPrologue | 保留英文图片 |
| 开启传送阵所需等级插图 | 43838 / UI_DialogBoxImage_Level10 | 保留图片；上下说明已翻译，并保留四个换行的插图留白 |
| 教程后段“记住……”之后的规则动画 | 43845–43850 / FX_GameFormat_1FollowQMarks、2HarvestXP、3GainLev10、4OpenPortal、5DefeatBeast、6Escape | 保留原动画；本次流程实际播放索引 0、1、3、4、5，索引2也列出供后续调查 |
| 教程中英雄升级 / 怒气小字 | 43862–43866 / LevelUpL2Tut、LevelUpL2TutEnd、PlusWrathSmallTut/P2/P3 | 保留原图，另见 gameplay-image-text.md |

攻击、特殊招式、门、史莱姆、怒气、经验的侧边插图为 43835–43841。它们继续由原教程显示，未替换图像；其中有无烘焙文字应在后续图片汉化时逐帧检查。

正文中的 `[A]` / `[B]` 是游戏控制键图片标签，完整保留，由游戏根据键盘/手柄选择图片。它们不应直接翻译成键盘字母 A/B。

`GuiMessageBox/Text` 和 `GuiMessageBoxTop/Text` 的资源初始文字是编辑器占位文字，实际显示前由 `SetContent` 改写，故没有把测试文案作为教程词条收录。

此清单来自资源引用和教程代码调查。没有启动游戏；动画帧的视觉效果、真实输入设备图标和完整游玩流程仍由用户验证。
