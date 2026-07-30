# API 参考

## `Basic` — 配置与工具（`Basic.cs`）

| 成员 | 类型 | 说明 |
|------|------|------|
| `AppName` | `const string` | 应用名称 |
| `AppVersion` | `const string` | 版本号 |
| `AppCopyright` | `const string` | 版权声明 |
| `AppCompany` | `const string` | 公司名 |
| `ONE_INCH_W / ONE_INCH_H` | `const int` | 一寸尺寸 295×413 @300DPI |
| `TWO_INCH_W / TWO_INCH_H` | `const int` | 二寸尺寸 413×626 @300DPI |
| `PASSPORT_W / PASSPORT_H` | `const int` | 小二寸 390×567 @300DPI |
| `CheckImage(Bitmap, Form)` | `static bool` | 检查图片是否已加载，否则弹警告 |
| `GetAppTitle()` | `static string` | 返回 `"FPTP v1.1.1.0"` 格式标题 |
| `OpenImageFile(Form)` | `static string` | 弹出文件选择对话框，返回路径或 null |

## `Prepalg` — 预处理算法（`Prepalg.cs`）

| 方法 | 说明 |
|------|------|
| `SmartCrop(Bitmap, int, int)` | 居中裁剪 + 高质量双三次缩放至目标尺寸 |
| `ToGrayscale(Bitmap)` | ColorMatrix 灰度转换，性能优于逐像素操作 |
| `ReplaceBackground(Bitmap, Color, int, Form?)` | 色键换底：以左上角色为基准，容差内像素替换为目标色 |

## `Assalg` — 辅助算法（`Assalg.cs`）

| 方法 | 说明 |
|------|------|
| `SaveImage(Bitmap, string)` | 按扩展名编码保存，JPEG 自动设为 Quality=100 |
| `GetColorDifference(Color, Color)` | 计算曼哈顿颜色距离 |
| `CheckResolution(Bitmap, int, int)` | 检查图片分辨率是否达到最低要求 |

## `Program` — 入口点（`Program.cs`）

- 无参数：启动 GUI 窗体
- 有参数：附加父控制台 → 执行命令模式 → 释放控制台 → 退出
- `-v` / `--version`：打印版本号到控制台
