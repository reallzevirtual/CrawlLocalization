# 检查与资源导出

面向源码维护者；在项目根目录执行，按改动范围选择检查。日常翻译流程见 [翻译技能](../translator-kit/crawl-language-pack/SKILL.md)。

离线 C# 检查需要 PowerShell 7；资源导出按脚本需要安装 Python、UnityPy、TypeTreeGeneratorAPI；字体检查还需要 fontTools。实际 TextMesh 布局检查需要已激活的 Unity 5.4.2f2。默认游戏和编辑器路径需按本机情况调整。

部分检查含固定中文译文断言；调整措辞后应核对期望值与行为，不应为了通过检查恢复旧译法或跳过真实错误。

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


## 通用字体与最小语言包

`pwsh -NoProfile -File build-multilingual-fonts.ps1` 重建通用字体并在独立 Unity 5.4.2 进程验证实际 glyph；`python tests/check_multilingual_fonts.py` 检查已生成 bundle 的原始字形、字宽、原游戏标量排版参数和俄西字符覆盖。

先运行 `build.ps1`，再运行 `pwsh -NoProfile -File tools/build-menu-examples.ps1`。示例生成器直接解压 Translator ZIP，使用包内脚本创建 ru/es；在 docs/my/backups 保存不可覆盖的翻译前基线，填入六个菜单译文并比对。最终用发行 DLL 本身测试查表、待译项回退和非中文规则，输出两份带字体和说明的 MenuDemo ZIP。测试不启动游戏。
