# Crawl 字体 AssetBundle 构建

使用 Unity **5.4.2f2** 和 Python + UnityPy，为 Crawl 的 Windows 32 位版本生成 `crawl-fonts.bundle`。工程只包含字体与原游戏的排版参数快照，不修改游戏资源。

## 执行

在项目根目录运行：

```powershell
pwsh -NoProfile -File .\build-fonts.ps1
# 编辑器装在其它路径时：
pwsh -NoProfile -File .\build-fonts.ps1 -UnityPath '其它目录\Editor\Unity.exe'
```

编辑器需要有效的官方许可证。构建脚本对子进程设置 `OPENSSL_ia32cap=0:0`，使用旧 OpenSSL 的通用实现，避开现代 CPU 上 `sha1_block_data_order` 的启动崩溃；退出启动脚本后恢复原环境，不修改系统环境变量。需要手动打开本工程时，双击根目录 `open-unity54.cmd`，它使用相同的兼容参数。

如果加上兼容参数后仍报告 `Unity has not been activated with a valid License`，再检查旧编辑器的官方激活状态；不应只凭该错误就删除许可证或重装驱动。

脚本会将根目录 `Fonts/*.ttf` 复制到本工程，隐藏窗口启动 Unity，通过 `-executeMethod` 自动构建并用 `-quit` 退出，不会运行 Crawl。本机旧编辑器的 `-batchmode` 激活路径仍会报错，因此使用正常编辑器的命令行自动化路径，并保留 Unity 自身的许可证检查。导入使用 Dynamic 模式并包含字体数据，保留原游戏十个字体的默认字号和渲染模式；以 `StandaloneWindows` 目标打包。

合并字体的 TTF 中含有更大的上升高度和行距；Unity 默认重新计算这些字段会导致文字下移。手动替换游戏内 `m_FontData` 时通常保留原 Font 的这些字段。0.2.2 使用 `font-layout.tsv` 保存十个原字体的 `m_Ascent`、`m_Descent`、`m_LineSpacing`，由 `preserve_layout.py` 在生成的 bundle 中恢复这三项；内嵌 TTF、引用及其它 Font 字段逐项验证不变。

构建流程先在一个编辑器进程中完成脚本编译，然后生成原始包、恢复布局，最后在全新的 Unity 进程中重新打开最终文件。验证十个 Font 的排版参数、动态字体属性，以及简繁译文所需字形。只有这些步骤都成功，脚本才会：

1. 将生成的 bundle 复制到根目录 `Fonts/crawl-fonts.bundle`。
2. 将 `fonts.bundle.tsv` 复制为简繁两种语言的 `fonts.tsv`，切换为 bundle 加载。

如果构建失败，已有字体映射不变；旧文件不视为本次成功产物。

## 进一步验证与插件构建

```powershell
# 需要 Python、UnityPy、fontTools：
python .\tests\check_bundle.py
pwsh -NoProfile -File .\build.ps1
```

`check_bundle.py` 校验 Unity 版本、32 位 Windows 平台、十个字体的默认属性、布局快照、语言包中资源路径及字形覆盖，并逐字节比对 bundle 内嵌 TTF 与根目录的源文件。传入 `--game-path '你的 Crawl 目录'` 可进一步比对原游戏 Font 的全部标量排版/样式属性；`--bundle '其它包路径'` 可检查旧版或已安装的包，0.2.1 包会被布局回归检查拒绝。

日志在 `artifacts/font-bundle/prepare.log`、`build.log`、`validate.log`。最终验证日志包含十条 `FONT_VALIDATED` 与最后的 `CRAWL_FONT_BUNDLE_OK`。插件日志中加载包内字体会显示 `Loaded embedded font: bundle|...`。发布包使用直接文件加载，只分发最终 bundle，不使用排版修正前的 Unity 构建 manifest。

字体包本身不依赖玩家系统安装字体。之后替换字体，只需更新源 TTF 并重新运行上述命令；玩家只需更新插件包，无须安装 Unity。

编辑器生成的 `Library/`、`Temp/` 等缓存不应提交版本控制。`Assets/Fonts/*.meta` 应保留以稳定资源 GUID。Unity 内检查和二进制检查仍不能代替 Crawl 内的实际显示验证。
