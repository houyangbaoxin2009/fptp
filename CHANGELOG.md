# 更新日志

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
