# FPTP (Foto Photo) — 证件照处理工具

基于 .NET Framework 4.8 的 Windows Forms 桌面应用，快速将普通照片转为标准证件照，支持智能裁剪、换底色、排版输出。兼容 Windows 7 SP1 及以上系统，无需额外安装运行时。支持中英双语界面与自动更新。

## 功能

- **智能裁剪** — 按一寸（295×413）/ 二寸（413×626）/ 小二寸（390×567）比例居中裁剪缩放
- **变黑白** — 使用 ColorMatrix 颜色矩阵算法将彩色照片转为灰度照
- **换底色** — 基于色键 + 容差的像素级背景替换算法，支持白/蓝/红底，容差可调
- **排版输出** — 预设 5 寸（1500×1050）、6 寸（1800×1200）、A4、A5 相纸布局，支持自定义宽高，自动排列裁剪后的照片，张数根据照片尺寸动态计算，带虚线裁剪辅助线
- **中英双语** — 设置中切换界面语言（简体中文 / English），即时生效；命令行可用 `--lang` 指定
- **自动更新** — 默认开启，启动时静默检查更新，发现新版本自动下载并静默安装（不打断当前使用）；按用户地区自动选择源（中国走 GitCode，国外走 GitHub，源失败自动回退）；可在设置中关闭或于"关于"窗口手动检查
- **安装向导** — 支持中英双语，可选择安装的文档格式（Markdown / PDF / 不安装）；安装到 Program Files 后设置自动保存到用户目录，普通用户无需管理员权限
- **高质量保存** — 支持 JPEG / PNG / BMP / TIFF / GIF 五种格式，JPEG 质量可调（70-100）
- **直接打印** — 一键打印当前图片，自动缩放居中，可选手打印机

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
- **最低系统**: Windows 7 SP1（.NET Framework 4.8 需单独安装）
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

## 相关文档

- [API 参考](API.md)
- [贡献指南](CONTRIBUTING.md)
- [更新日志](CHANGELOG.md)
- [许可证](LICENSE)

## 许可

Copyright © 2026 Jiro. Licensed under the Apache License, Version 2.0.
See [LICENSE](LICENSE) for details.
