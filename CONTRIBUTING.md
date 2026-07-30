# 贡献指南

## 开发环境

- **Visual Studio 2022**（推荐）或任何 C# IDE
- **.NET Framework 4.8 SDK**（Windows 7 SP1+）
- **PowerShell 7+**（用于运行构建脚本）

## 构建与运行

```shell
git clone https://github.com/houyangbaoxin2009/fptp.git
cd fptp
dotnet build
dotnet run
```

首次构建自动还原 NuGet 包。构建产物位于 `bin/Debug/net48/`。

## 项目架构

### 命名空间

所有代码位于 `fptp` 命名空间下，使用文件范围的命名空间声明。

### 项目结构

| 文件 | 职责 | 依赖 |
|------|------|------|
| `Program.cs` | 双模式入口：CLI 命令解析 + GUI 启动 | `Basic`、`Prepalg`、`Assalg` |
| `mainBox.cs` | 主窗体 WinForms 交互，按钮事件调度 | `Basic`、`Prepalg`、`Assalg`、`AboutBox` |
| `Basic.cs` | 纯常量 + 静态工具方法，无副作用 | `System.Windows.Forms` |
| `Prepalg.cs` | 纯像素处理算法，与 UI 解耦 | `System.Drawing` |
| `Assalg.cs` | 文件 I/O 和辅助计算，与 UI 解耦 | `System.Drawing`、`System.IO` |
| `AboutBox.cs` | 关于对话框，读取程序集信息 | `Basic` |

### 依赖方向

```
Program.cs → mainBox.cs → AboutBox.cs
          ↘              ↗
           Basic.cs  Prepalg.cs  Assalg.cs
```

`Basic` → `Prepalg` → `Assalg` 为单向依赖，不存在循环引用。

### 窗口设计器

`mainBox.Designer.cs` 和 `AboutBox.Designer.cs` 由 Visual Studio 设计器自动生成，**不应手动编辑**。添加控件或修改属性应通过设计器界面操作。

### 命名约定

- `Assalg` — "助理" + "算法"（Assistant Algorithm）
- `Prepalg` — "预处理" + "算法"（Preprocessing Algorithm）

## 编码规范

### 通用
- 花括号使用 K&R 风格（左大括号不换行）
- 缩进使用 Tab，宽度 4
- 空事件处理方法体写 `{ }` 而非换行
- 所有 `public` 成员写 XML 文档注释

### 资源管理
- `Bitmap`、`Graphics`、`ImageAttributes`、`EncoderParameters` 等非托管资源必须用 `using` 包裹或显式 `Dispose()`
- `Clone()` 前先将源赋值给临时变量，避免 `Clone()` 抛出时源也被污染

### 错误处理
- GUI 模式不抛未处理异常，用 `MessageBox` 向用户展示友好错误信息
- CLI 模式错误写入 `Console.Error` 或 `Console.Out`，以非零退出码返回
- 关键操作前后设置 `Cursor.WaitCursor` / `Cursor.Default`

### 线程安全
- 主线程为 STA 线程，所有 UI 操作在事件处理中完成
- 耗时代理用 `Application.DoEvents()` 刷新界面，**不使用** `BackgroundWorker` 或 `Task.Run`

## 添加新功能的步骤

1. 在 `Basic.cs` 中定义尺寸常量（如有）
2. 在 `Prepalg.cs` 或新建文件中实现算法逻辑
3. 在 `mainBox.cs` 中添加按钮事件处理
4. 如有新的 Designer 事件订阅，务必保留对应空事件桩代码（否则编译错误）
5. 新增文件需添加到 `.csproj`（SDK 风格项目默认自动包含）
6. 运行 `dotnet build` 确认 0 错误
7. 更新 `README.md` 功能列表和 API 参考

## 测试

当前项目无独立测试框架。提交前请至少手动验证以下流程：

1. 加载图片 → 智能裁剪 → 保存（确认尺寸正确）
2. 加载图片 → 变黑白 → 保存（确认灰度输出）
3. 加载图片 → 修改底色（白/蓝/红） → 保存（确认背景替换正确）
4. 加载图片 → 5 寸排版 → 6 寸排版（确认 8 张 / 10 张布局）
5. CLI: `fptp.exe -v`（确认版本号输出）
6. CLI: `fptp.exe -i input.jpg -o output.jpg -s 1`（确认静默处理成功）
