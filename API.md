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
| `LAYOUT_5INCH_W/H`、`LAYOUT_6INCH_W/H`、`LAYOUT_A4_W/H`、`LAYOUT_A5_W/H`、`LAYOUT_GAP` | `const int` | 相纸排版尺寸（5寸/6寸/A4/A5）与照片间距 |
| `CheckImage(Bitmap, Form)` | `static bool` | 检查图片是否已加载，否则弹警告 |
| `GetAppTitle()` | `static string` | 返回 `"FPTP v1.2.0.0"` 格式标题 |
| `OpenImageFile(Form)` | `static string` | 弹出文件选择对话框，返回路径或 null |

## `GenSettings` — 生成设置（`GenSettings.cs`）

| 成员 | 类型 | 说明 |
|------|------|------|
| `SaveFormat` | `string` | 默认保存格式（jpg/png/bmp/tiff/gif） |
| `SaveQuality` | `int` | JPEG 输出质量（70-100） |
| `GuideLineStyle` | `int` | 排版辅助线样式：0=虚线 1=实线 2=无 |
| `DefaultSize` | `int` | 默认裁剪尺寸（1=一寸 2=二寸 3=小二寸） |
| `BackgroundColor` | `string` | 默认底色（蓝色/红色/白色） |
| `Tolerance` | `int` | 换底容差（0-150） |
| `LayoutPreset` | `int` | 排版预设（0=5寸 1=6寸 2=A4 3=A5 4=自定义） |
| `CustomLayoutW/H` | `int` | 自定义排版宽高 |

## `Prepalg` — 预处理算法（`Prepalg.cs`）

| 方法 | 说明 |
|------|------|
| `SmartCrop(Bitmap, int, int)` | 居中裁剪 + 高质量双三次缩放至目标尺寸 |
| `ToGrayscale(Bitmap)` | ColorMatrix 灰度转换，性能优于逐像素操作 |
| `ReplaceBackground(Bitmap, Color, int, Form?)` | 色键换底：以左上角色为基准，容差内像素替换为目标色 |

## `Assalg` — 辅助算法（`Assalg.cs`）

| 方法 | 说明 |
|------|------|
| `SaveImage(Bitmap, string, int quality = 100)` | 按扩展名编码保存，支持 JPEG/PNG/BMP/TIFF/GIF；JPEG 应用质量参数（1-100） |
| `GetColorDifference(Color, Color)` | 计算曼哈顿颜色距离 |
| `CheckResolution(Bitmap, int, int)` | 检查图片分辨率是否达到最低要求 |

## `Program` — 入口点（`Program.cs`）

- 无参数：启动 GUI 窗体
- 有参数：附加父控制台 → 执行命令模式 → 释放控制台 → 退出
- `-v` / `--version`：打印版本号到控制台
- `--lang zh-CN|en-US`：指定启动语言（任意位置，优先级高于设置中的界面语言）

## `Lang` — 多语言（`Lang.cs`）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Current` | `static string` | 当前语言代码（如 `zh-CN` / `en-US`） |
| `Load(string)` | `static void` | 加载指定语言的 JSON 语言包（嵌入资源 `Resources/lang.*.json`） |
| `Get(string, params object[])` | `static string` | 按 key 取翻译文本，支持 `{0}` 占位符；缺失时回退英文再回退 key |

## `Updater` — 自动更新（`Updater.cs`）

| 方法 | 说明 |
|------|------|
| `CheckSilent(Form)` | 启动时后台线程静默检查 GitCode Releases，有新版本才弹窗 |
| `CheckManual(Form)` | 前台手动检查，无新版本/失败时给出提示 |

- 更新源：`https://api.gitcode.com/api/v5/repos/jiro2025/fptp/releases`（数组首个为最新）
- 下载安装包到 `%TEMP%\FPTP-Setup.exe` 后启动安装并关闭主窗体
