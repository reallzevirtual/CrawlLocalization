# 通用多语言字体

`../crawl-multilingual.bundle` 为 Windows / Unity 5.4.2f2 字体包，供制作包和其他语言共用。中文字体及其 bundle 独立保留。本项目未修改游戏资源、文字坐标、点击区域或玩法代码。

## 可复现输入与构建

- `Original/`：从原游戏 sharedassets0.assets 的 1076、1077、1078、1079、1080、1081、1082、1083、1085 以及 sharedassets2.assets 的 18 导出的十个 TTF。
- `original-layout.json`：资源位置及原始 Unity Font 标量参数。
- `Donor/`：Pixelify Sans 的静态字重；许可见 `PixelifySans-OFL.txt`。
- `Generated/`：合并结果，构建时重算；`merge-report.json` 记录缩放与源文件哈希。
- `coverage.txt`：用于独立 Unity 实测的基本字符集。

从项目根运行 `pwsh -NoProfile -File build-multilingual-fonts.ps1`，需要已激活的 Unity 5.4.2f2 和 Python 包 fontTools、UnityPy。项目生成在 `artifacts/multilingual-font-project`，不触碰原中文 Unity 工程。构建、恢复原始 Font 行距、在新进程验证成功后才更新正式 bundle。

如需重新导出输入，显式执行：

```powershell
python tools/merge_multilingual_fonts.py --game-data 'D:/SteamLibrary/steamapps/common/Crawl/Crawl_Data' --pixelify 'E:/MyDoc/资源/字体/Pixelify_Sans'
```

## 匹配规则

| 原字体 | Pixelify 字重 |
| --- | --- |
| CrawlLarge、CrawlInvLarge、CrawlMedium、CrawlSmall、CrawlTiny | Bold |
| CrawlTextBodyTeensy | SemiBold |
| CrawlTextBodyThin、CrawlTextBodyWide、04B_04-Black、04B_03-White | Medium |

字重根据实际像素笔画而非原 TTF 的 weight 数字选择；小字体不等于细字体。每款字体以原 `H` 的高宽、`x` 的高度分别适配大写、小写，无重音上伸小写字母（如 б）使用大写高度比例，避免小字号中超过原大写高度；归零 Pixelify 的基线下溢，沿用原字体间距。拉丁重音字母使用原字母轮廓、原 advance 加重音；反白字体为新增正字形生成背景和反向轮廓。已有 cmap 字形、advance、hint、字距表、unitsPerEm 和垂直度量不替换。

Unity 导入会重算 ascent/descent/line spacing，所以最终 bundle 仍使用原有 `preserve_layout.py` 还原原游戏值；字体映射 scale 固定 1。没有通过移动文本补偿基线。

## 覆盖与局限

验证俄语 33 个大小写字母（含 Ё/ё）、西班牙语重音字母及基本字符。此版本 Pixelify 缺少俄语 О、П 和乌克兰语 І 的 cmap，使用同字体已有的 O、希腊 Pi、I 同形轮廓补齐。其余字符依捐赠字体的实际覆盖，不承诺完整希腊文、所有西里尔扩展或全部 Unicode。原字体已有特殊符号槽位不覆盖，其中某些非 ASCII 槽位可能是游戏自定义图形；特殊标点仍需实机确认。

小字号栅格化遵循游戏原设定；复杂字母在极小标签中仍需实机检查。不为俄语、西班牙语启用中文断行、标点或中文高采样规则。字母更长的译文可能需要精简措辞或显式换行。

`python tests/check_multilingual_fonts.py` 验证 bundle 字节、全量原字形、advance、TTF 度量和 Unity 标量属性。Unity 构建验证在字号 16/64/160 比较原英文实际 glyph bounds/advance，检查俄西字符实际生成非空字形。示例图位于 `artifacts/universal-fonts/unity-samples.png`，这是独立编辑器渲染，并非游戏截图。最终菜单交互和组合模组由用户实机验证。

发行时保留 bundle 同目录的 `crawl-multilingual.OFL.txt`、`crawl-multilingual.NOTICE.txt`。新语言生成器默认写入十条映射；缺字/缺文件继续采用原字体、该语言的系统备用字体、原英文显示的既有回退流程。
