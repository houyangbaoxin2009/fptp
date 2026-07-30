# FPTP (Foto Photo) — 证件照处理工具

基于 .NET Framework 4.8 的 Windows Forms 桌面应用，快速将普通照片转为标准证件照，支持智能裁剪、换底色、排版输出。兼容 Windows 7 SP1 及以上系统，无需额外安装运行时。

## 功能

- **智能裁剪** — 按一寸（295×413）/ 二寸（413×626）/ 小二寸（390×567）比例居中裁剪缩放
- **变黑白** — 使用 ColorMatrix 颜色矩阵算法将彩色照片转为灰度照
- **换底色** — 基于色键 + 容差的像素级背景替换算法，支持白/蓝/红底，容差可调
- **排版输出** — 在 5 寸（1500×1050）或 6 寸（1800×1200）相纸上自动排列裁剪后的照片，张数根据照片尺寸动态计算，带虚线裁剪辅助线
- **高质量保存** — JPEG 最高质量（Quality=100）或 PNG 格式保存

## 使用

### GUI 模式

直接运行 `fptp.exe`，按以下流程操作：

1. **导入** → 点击"本地图片"加载照片
2. **预处理** → 智能裁剪 / 变黑白 / 修改底色（可选，可组合使用）
3. **排版** → 5 寸排版（8 张）或 6 寸排版（10 张）
4. **导出** → 保存为 JPG 或 PNG

**底色替换**：选择白/蓝/红底，拖动滑块调节容差（值越大替换越激进），点击"修改底色"。

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
| `-v, --version` | 显示版本号 |

**示例**：

```shell
fptp.exe -v
fptp.exe -i photo.jpg -o output.jpg -s 1
fptp.exe -i "C:\我的照片\证件照.jpg" -o "D:\out\一寸.jpg" -s 1
```

## 项目结构

```
fptp/
├── Program.cs              入口点 —— 双模式启动（CLI + GUI）
├── mainBox.cs              主窗体交互逻辑和事件处理
├── mainBox.Designer.cs     主窗体设计器代码（控件布局）
├── AboutBox.cs             关于对话框逻辑
├── AboutBox.Designer.cs    关于对话框设计器代码
├── Basic.cs                应用配置常量（版本、尺寸）和通用工具方法
├── Prepalg.cs              预处理算法（智能裁剪 / 灰度转换 / 换底）
├── Assalg.cs               辅助算法（图片保存 / 颜色计算 / 分辨率检查）
├── fptp.csproj             .NET Framework 4.8 SDK 风格项目文件
├── README.md               说明文档（本文档）
├── CONTRIBUTING.md         贡献指南
```

## API 参考

### `Basic` — 配置与工具（`Basic.cs`）

| 成员 | 类型 | 说明 |
|------|------|------|
| `AppName` | `const string` | 应用名称 `"FPTP"` |
| `AppVersion` | `const string` | 版本号 `"1.1.1.0"` |
| `AppCopyright` | `const string` | 版权声明 |
| `AppCompany` | `const string` | 公司名 |
| `ONE_INCH_W / ONE_INCH_H` | `const int` | 一寸尺寸 295×413 @300DPI |
| `TWO_INCH_W / TWO_INCH_H` | `const int` | 二寸尺寸 413×626 @300DPI |
| `PASSPORT_W / PASSPORT_H` | `const int` | 小二寸 390×567 @300DPI |
| `CheckImage(Bitmap, Form)` | `static bool` | 检查图片是否已加载，否则弹警告 |
| `GetAppTitle()` | `static string` | 返回 `"FPTP v1.1.1.0"` 格式标题 |
| `OpenImageFile(Form)` | `static string` | 弹出文件选择对话框，返回路径或 null |

### `Prepalg` — 预处理算法（`Prepalg.cs`）

| 方法 | 说明 |
|------|------|
| `SmartCrop(Bitmap, int, int)` | 居中裁剪 + 高质量双三次缩放至目标尺寸 |
| `ToGrayscale(Bitmap)` | ColorMatrix 灰度转换，性能优于逐像素操作 |
| `ReplaceBackground(Bitmap, Color, int, Form?)` | 色键换底：以左上角色为基准，容差内像素替换为目标色 |

### `Assalg` — 辅助算法（`Assalg.cs`）

| 方法 | 说明 |
|------|------|
| `SaveImage(Bitmap, string)` | 按扩展名编码保存，JPEG 自动设为 Quality=100 |
| `GetColorDifference(Color, Color)` | 计算曼哈顿颜色距离 |
| `CheckResolution(Bitmap, int, int)` | 检查图片分辨率是否达到最低要求 |

### `Program` — 入口点（`Program.cs`）

- 无参数：启动 GUI 窗体
- 有参数：附加父控制台 → 执行命令模式 → 释放控制台 → 退出
- `-v` / `--version`：打印版本号到控制台

## 技术细节

- **目标框架**: .NET Framework 4.8
- **语言**: C#（LangVersion latest）
- **项目格式**: SDK 风格（Microsoft.NET.Sdk），`OutputType=WinExe`
- **最低系统**: Windows 7 SP1（.NET Framework 4.8 需单独安装）
- **UI**: Windows Forms
- **图像处理**: `System.Drawing`（Bitmap / Graphics / ColorMatrix / ImageAttributes）
- **DPI 基准**: 所有尺寸常量基于 300 DPI
- **命名约定**: `Assalg` = 助理 + 算法；`Prepalg` = 预处理 + 算法

## 构建

```shell
# 安装 .NET Framework 4.8 SDK 后
dotnet build
dotnet run
```

## 许可

Copyright © 2026 Jiro. All rights reserved.
