# 更新日志

## [1.3.3.1] — 2026-07-31

### 新增
- 新增应用图标：白底蓝红双色相框 + 人像剪影，扁平简洁风，小尺寸下清晰
- 图标接入 exe（ApplicationIcon）、安装包（SetupIconFile）、主窗体与任务栏

## [1.3.3.0] — 2026-07-31

### 修复
- 修复国内用户检测不到更新的问题
  - 原因：GitCode Releases API 数组为升序（旧版在前），`Updater.cs` 误取数组首个作为最新版本
  - 方案：遍历全部 Releases 取 tag 版本号最大者，兼容 GitCode（升序）与 GitHub（降序）两种顺序

## [1.3.2.0] — 2026-07-31

### 修复
- 修复安装到 Program Files 后非管理员启动报"保存设置失败：拒绝访问"
  - 原因：设置文件硬编码写到 exe 目录，普通用户对 Program Files 无写权限
  - 方案：设置文件路径优先 exe 目录，不可写时自动回退 `%APPDATA%\FPTP\setting.json`（启动时探测并缓存）
- 修复安装向导在 Inno Setup 6.7.3 下崩溃 "Runtime error: Could not call proc"
  - 原因：6.7.3 中访问 `TNewRadioButton.Checked` 属性必崩（即使用户正停留在该页）
  - 方案：改用 `TInputOptionWizardPage.Values[]` 索引读取选项，并在用户离开页面时缓存

### 优化
- 安装选项合并改为仅当值变化时才写盘，避免每次启动无谓写入
- 启动自动路径（设置迁移 / 安装选项合并）写盘失败静默，不弹错误框；用户主动操作仍提示

## [1.3.1.0] — 2026-07-31

### 新增
- 安装向导新增"文档安装"选项页：可选择安装 Markdown / PDF 文档或不安装（安装到 Program Files 后仍可用）
- 设置文件新增 high 隐藏段（docsFormat / installLang）：安装程序写入、应用读取，不入设置面板与导入导出
- 关于窗口文档按钮按安装的文档格式打开（.md / .pdf），选择"不安装"时禁用

### 变更
- 安装向导支持中英双语（[Languages] 段，跟随系统语言）
- 安装选项写入 `install-options.json`，应用启动时合并到设置隐藏段（high：docsFormat / installLang）
- 自动更新改为静默安装：/VERYSILENT /NORESTART /SUPPRESSMSGBOXES，不再弹安装向导

## [1.3.0.0] — 2026-07-31

### 新增
- 关于窗口重构分组：文档 / 支持与联系 / 操作
- 关于窗口新增"报告问题"按钮：按地区跳转 GitCode / GitHub Issues
- 关于窗口新增"联系作者"按钮：邮箱联系（3187909557@qq.com）
- 关于窗口显示当前更新源（按地区自动判断 GitCode / GitHub）

## [1.2.1.0] — 2026-07-31

### 新增
- 自动更新按用户地区选择源：中国用户从 GitCode 获取更新，国外用户从 GitHub 获取（IP 定位自动判断，无需配置）
- 新增 `RegionDetector.cs`：级联 IP 定位服务判断用户地区，进程内缓存只检测一次
- 所选更新源请求失败时自动回退另一平台

### 变更
- 更新源从单一 GitCode 扩展为 GitCode + GitHub 双平台

## [1.2.0.0] — 2026-07-31

### 新增
- 中英双语界面切换（设置 → 界面语言，支持 zh-CN / en-US，即时生效）
- 语言包独立导入/导出（设置 → 界面语言）：文件名 `lang.{id}.{name}.json`，内容为翻译表，导入后立即切换
- 命令行新增 `--lang` 参数指定启动语言
- 排版支持预设布局：5 寸、6 寸、A4、A5，以及自定义宽高（设置 → 排版布局）
- 自动更新（默认开启，可在设置中关闭）：启动时静默检查，关于窗口可手动检查，从 GitCode Releases 下载安装包
- 新增 `Updater.cs`（GitCode API 检查更新、下载、启动安装）、`CustomSizeBox.cs`（自定义尺寸输入）、`Lang.cs`（翻译资源加载）
- 输出格式扩展：保存支持 JPEG / PNG / BMP / TIFF / GIF 五种格式
- JPEG 质量可调（70-100，设置 → JPEG 质量，默认 100）
- 排版辅助线样式可选：虚线 / 实线 / 无（设置 → 辅助线样式）
- 新增"打印"按钮：PrintDocument 打印当前图片，自动缩放居中，可选手打印机

### 变更
- 排版按钮统一为一个"排版"按钮 + 布局下拉框（原 5 寸 / 6 寸两个按钮）
- 关于窗口与主界面全部文本迁移至语言包（lang.zh-CN.json / lang.en-US.json）
- 设置文件统一为 `setting.json`：`{app}{gen}{lang}` 三部分，旧格式（setting.json + gen_setting.json）自动迁移
- 语言加载优先级：设置文件语言包 → 内置嵌入资源 → 回退中文
- 仓库链接更新为 GitCode（https://gitcode.com/jiro2025/fptp）
- 完成区按钮重排：导出本地 / 打印 / 卸载图片 一行三列

## [1.1.1.1] — 2026-07-30

### 修复
- 修复捐赠弹窗在低分辨率屏幕上超出显示区域无法关闭的问题
  - 原因：收款码图片（1080×1383）直接作为窗口大小，部分屏幕容纳不下
  - 方案：缩放至屏幕工作区 80%，同时支持 Escape 键关闭弹窗

## [1.1.1.0] — 2026-07-30

### 新增
- 关于窗口增加功能按钮：查看更新日志、查看 README、查看贡献指南、查看 API 文档、GitHub、给作者买一杯咖啡、访问官网
- 添加 CHANGELOG.md 更新日志文件
- 完善 README.md（API 参考表格、详细使用说明）
- 添加 CONTRIBUTING.md 贡献指南（项目架构、编码规范、开发流程）

### 修复
- 修复 Designer 文件中 5 处事件处理程序缺失导致的编译错误（补回空实现桩）
- 修复 `-v` / `--version` 命令行参数无输出的问题
  - 原因：`WinExe` 子系统不关联父控制台
  - 方案：添加 `AttachConsole` / `FreeConsole` P/Invoke，CLI 模式主动附加父控制台

### 优化
- `Program.cs` CLI 模式增加退出码：成功返回 0，失败返回 1

## [1.1.0.0] — 2026-07-29

### 新增
- 命令行批处理模式：`-i input -o output -s size`
- `-v` / `--version` 参数显示版本号

### 变更
- 项目目标框架从 .NET 8 迁移到 .NET Framework 4.8
  - 最低支持 Windows 7 SP1（需安装 .NET Framework 4.8）
  - 项目格式改为 SDK 风格 `Microsoft.NET.Sdk`

### 修复
- 排版布局不再覆盖 `currentImage`，保存时读取 `pictureBox1.Image`
- 重复点击排版按钮时正确释放上一张排版图
- `Assalg.cs` 中 `GetImageDecoders()` 更正为 `GetImageEncoders()`
- `sourceImage.Clone()` 异常时不再污染源引用
- 多个 `Graphics`、`ImageAttributes`、`EncoderParameters` 添加 `using` 释放
- `BtnSave_Click` 添加 `finally` 块确保恢复光标
- 低分辨率图片无限制加载的问题（添加 300px 最低检查）

## [1.0.0.0] — 2026-07-28

### 初始版本
- 基本的 Windows Forms 证件照处理工具
- 支持智能裁剪（一寸 / 二寸 / 小二寸）
- 支持彩色转黑白（ColorMatrix 灰度算法）
- 支持背景颜色替换（色键 + 容差算法）
- 支持 5 寸（4×2）和 6 寸（5×2）排版输出
- 支持高质量 JPEG / PNG 导出
- 关于对话框
