# FPTP (Foto Photo) — 免费开源的证件照处理工具

> 一张普通照片，3 秒变成标准证件照。免费、开源、离线、无需注册。

FPTP 是一款完全免费开源的 Windows 桌面证件照工具：导入生活照，即可智能裁剪成一寸 / 二寸 / 小二寸证件照，支持换底色、黑白转换、相纸排版、一键打印。**所有处理都在本地完成，照片不会上传到任何服务器**。支持命令行批处理，可**完全自动化**——批量证件照、脚本集成、无人值守一次跑完。

## 为什么选择 FPTP

| | FPTP | 在线证件照网站 | 照相馆 |
|---|---|---|---|
| **价格** | 完全免费开源 | 免费但水印 / 收费出图 | 20-50 元 / 次 |
| **隐私** | 照片本地处理，不上传 | 照片上传服务器 | 隐私取决于商家 |
| **老电脑** | 支持 Windows 7 SP1+，极低配置可流畅运行 | 需联网 + 浏览器 | 无此顾虑 |
| **性能** | 纯本地算法，秒级出图 | 受网络与服务器限制 | 等待排队 |
| **功能** | 裁剪 / 换底 / 黑白 / 排版 / 打印 / 批处理 | 常需付费解锁 | 固定套餐 |
| **自动化** | 命令行批量处理，脚本化无人值守 | 手动逐张操作 | 需到店 |
| **离线可用** | 完全离线可用 | 断网即失效 | 无 |

## 快速开始

**下载安装包**（Windows 7 SP1 及以上；Win10/11 自带运行环境，Win7/8 首次运行会提示安装 .NET Framework 4.8）：

- 🚀 国内下载（GitCode）：https://gitcode.com/jiro2025/fptp/releases
- 🌍 国际下载（GitHub）：https://github.com/houyangbaoxin2009/fptp/releases

下载 `FPTP-vX.X.X.X-Setup.exe`，双击安装即可使用。

**或运行免安装绿色版**：下载 `FPTP-vX.X.X.X.zip`，解压后直接运行 `fptp.exe`。

> 应用支持中英双语界面，启动后自动检查更新（中国用户走 GitCode、国外用户走 GitHub，源失败自动回退）。

## 功能

- **智能裁剪** — 按一寸（295×413）/ 二寸（413×626）/ 小二寸（390×567）标准比例居中裁剪缩放，照片自动对正
- **换底色** — 基于色键 + 容差的像素级背景替换算法，支持白 / 蓝 / 红底，容差可调，一键去除杂乱背景
- **变黑白** — ColorMatrix 颜色矩阵算法，证件照标准灰度
- **排版输出** — 预设 5 寸（1500×1050）/ 6 寸（1800×1200）/ A4 / A5 相纸布局，支持自定义宽高，自动排列，带裁剪辅助线，拿去打印店直接出片
- **一键打印** — 直接打印当前图片，自动缩放居中，可选手打印机
- **配置自由修改** — 所有设置（尺寸 / 底色 / 容差 / 输出质量 / 排版布局 / 语言 / 更新策略）统一存为 `setting.json` 纯文本，可直接编辑文件改配置，改完即生效；语言包支持独立导入导出
- **命令行批处理 / 完全自动化** — 支持 `-i -o -s` 参数批量转换，可脚本化循环处理成百上千张照片，无人值守一次跑完，适合证件照批量制作与工作流集成
- **高质量保存** — JPEG / PNG / BMP / TIFF / GIF 五种格式，JPEG 质量可调（70-100）
- **中英双语** — 界面语言即时切换，命令行可用 `--lang` 指定
- **自动更新** — 默认开启，静默检查更新并安装，不打断使用；可在设置中关闭或手动检查
- **开源可审计** — Apache 2.0 许可，代码完全公开，放心使用

## 使用

### GUI 模式

直接运行 `fptp.exe`，按以下流程操作：

1. **导入** → 点击"本地图片"加载照片
2. **预处理** → 智能裁剪 / 变黑白 / 修改底色（可选，可组合使用）
3. **排版** → 选择布局（5 寸 / 6 寸 / A4 / A5 / 自定义）后点击"排版"
4. **导出** → 保存为 JPG 或 PNG

**底色替换**：选择白/蓝/红底，拖动滑块调节容差（值越大替换越激进），点击"修改底色"。

**排版布局**：在"排版"按钮旁的下拉框选择 5 寸 / 6 寸 / A4 / A5 或自定义尺寸（设置 → 排版布局中配置自定义宽高）。

**切换语言**：设置 → 界面语言选择简体中文或 English，立即生效。

**输出选项**：设置中可调整 JPEG 质量（70-100）与排版辅助线样式（虚线/实线/无）。保存对话框支持 JPEG / PNG / BMP / TIFF / GIF，点击"打印"可直接打印当前图片。

### 命令行模式

在控制台（cmd / PowerShell）中执行：

```shell
fptp.exe -i "输入.jpg" -o "输出.jpg" -s 1
```

| 参数 | 说明 |
|------|------|
| `-i, --input` | 输入图片路径（必填） |
| `-o, --output` | 输出图片路径（必填） |
| `-s, --size` | `1` 一寸 295×413（默认） / `2` 二寸 413×626 |
| `--lang` | 启动语言：`zh-CN`（默认） / `en-US` |
| `-v, --version` | 显示版本号 |

**示例**：

```shell
fptp.exe -v
fptp.exe -i photo.jpg -o output.jpg -s 1
fptp.exe -i "C:\我的照片\证件照.jpg" -o "D:\out\一寸.jpg" -s 1 --lang en-US
```

**批量自动化**（cmd / PowerShell 循环处理整个文件夹）：

```shell
for %f in (C:\photos\*.jpg) do fptp.exe -i "%f" -o "C:\out\%~nf.jpg" -s 1
```

```powershell
Get-ChildItem C:\photos\*.jpg | ForEach-Object {
    fptp.exe -i $_.FullName -o "C:\out\$($_.BaseName).jpg" -s 1
}
```

处理完成退出码：成功 0，失败 1，便于脚本判断与日志记录。

## 支持平台

- **Windows 7 SP1 / 8 / 8.1 / 10 / 11**（Win10/11 自带 .NET Framework 4.8；Win7/8 需安装 .NET Framework 4.8，系统会引导下载）
- 32 位 / 64 位均可

## 项目结构

```
fptp/
├── Program.cs              入口点 —— 双模式启动（CLI + GUI）
├── mainBox.cs              主窗体交互逻辑和事件处理
├── mainBox.Designer.cs     主窗体设计器代码（控件布局）
├── AboutBox.cs             关于对话框逻辑
├── AboutBox.Designer.cs    关于对话框设计器代码
├── GenSettingsBox.cs       设置对话框逻辑（语言/自动更新/排版布局）
├── GenSettingsBox.Designer.cs  设置对话框设计器代码
├── CustomSizeBox.cs        自定义排版尺寸对话框
├── Basic.cs                应用配置常量（版本、尺寸）和通用工具方法
├── Lang.cs                 多语言支持（语言包加载与查询）
├── Updater.cs              自动更新（GitCode / GitHub 按地区选源）
├── RegionDetector.cs       用户地区检测（IP 定位）
├── Prepalg.cs              预处理算法（智能裁剪 / 灰度转换 / 换底）
├── Assalg.cs               辅助算法（图片保存 / 颜色计算 / 分辨率检查）
├── Resources/lang.zh-CN.json   简体中文语言包
├── Resources/lang.en-US.json   English 语言包
├── fptp.csproj             .NET Framework 4.8 SDK 风格项目文件
├── README.md               说明文档（本文档）
├── CONTRIBUTING.md         贡献指南
├── CHANGELOG.md            更新日志
├── API.md                  API 参考
├── LICENSE                 许可证（Apache 2.0）
```

## API 参考

详见 [API.md](API.md)。

## 技术细节

- **目标框架**: .NET Framework 4.8
- **语言**: C#（LangVersion latest）
- **项目格式**: SDK 风格（Microsoft.NET.Sdk），`OutputType=WinExe`
- **最低系统**: Windows 7 SP1（需 .NET Framework 4.8；Win10/11 已内置）
- **UI**: Windows Forms
- **图像处理**: `System.Drawing`（Bitmap / Graphics / ColorMatrix / ImageAttributes）
- **多语言**: 内嵌 JSON 语言包（`Resources/lang.*.json`），`Lang.Get(key)` 统一取词
- **自动更新**: `HttpWebRequest` 请求 GitCode / GitHub Releases API，按用户地区选源（`RegionDetector` IP 定位）
- **DPI 基准**: 所有尺寸常量基于 300 DPI
- **命名约定**: `Assalg` = 助理 + 算法；`Prepalg` = 预处理 + 算法

## 构建

```shell
# 安装 .NET Framework 4.8 SDK 后
dotnet build
dotnet run
```

## 参与贡献

欢迎提交 Issue、PR 或建议！请先阅读[贡献指南](CONTRIBUTING.md)。

## 相关文档

- [API 参考](API.md)
- [贡献指南](CONTRIBUTING.md)
- [更新日志](CHANGELOG.md)
- [许可证](LICENSE)

## 许可

Copyright © 2026 Jiro. Licensed under the Apache License, Version 2.0.
See [LICENSE](LICENSE) for details.
