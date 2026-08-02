# 更新日志

本项目的所有重要更改都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)（4 段式 X.Y.Z.W，见 docs/2.0-architecture.md）。

## [2.0.9.0] - 2026-08-02

### 修复
- **画布挤在左上角一小块（严重布局 bug）**：工作区容器多包了一层 `_leftPanelArea`，`SplitterDistance=180` 误设在 `_root` 上，把整个画布+面板区压成 180px 宽，其余窗口空白。去掉多余层级，`_root.Panel1` 直接承载左面板（180px），画布占满剩余区域
- **Ctrl+滚轮缩放不生效**：`PictureBox.MouseWheel` 事件仅在控件有焦点时触发，画布无焦点时滚轮被 AutoScroll 面板消费。改为 `IMessageFilter` 全局拦截 WM_MOUSEWHEEL，光标在画布区域且按住 Ctrl 即缩放

### 新增
- **以光标为中心的缩放**：Ctrl+滚轮/工具栏按钮缩放时，光标下的图片像素保持不动，视口自动跟随（不再跳回左上角）
- **工具栏缩放按钮**：放大 / 缩小 / 适应窗口 / 实际大小（视图菜单的快捷入口）
- **状态栏缩放标签可点击**：点击在 适应窗口 ↔ 实际大小 间切换
- **画布双击**：在 适应窗口 ↔ 实际大小 间切换

### 变更
- 窗口默认 1100×720 → 1400×900，左右面板宽度 220 → 180，画布可视区更大
- 版本号提升至 2.0.9.0（全部项目与内置模组同步）

## [2.0.8.0] - 2026-08-02

### 新增
- **画布缩放**：证件照换底边缘、裁切边界需放大检查，原画布固定"适应窗口"无法放大。新增视图控制：
  - Ctrl+滚轮缩放（10%~3200%），画布超出可视区可滚动查看
  - 视图菜单：放大（Ctrl+=）/ 缩小（Ctrl+-）/ 适应窗口（Ctrl+0）/ 实际大小（Ctrl+1）
  - 状态栏显示当前缩放比例（适应窗口模式显示实际比例）
  - 坐标映射同步适配缩放（套索等交互工具在任意缩放下选区准确）
- **拖放打开图片**：把图片文件拖入窗口直接打开为新文档（复用打开命令，含保存路径保留）
- **真实快捷键绑定修复**：此前菜单快捷键仅显示文本（`ShortcutKeyDisplayString`）实际按键不生效。现在解析快捷键文本绑定真实 `ShortcutKeys`（Ctrl+O/S/P/Z/Y/Ctrl+=/Ctrl+-/Ctrl+0/Ctrl+1），Ctrl+=/Ctrl+- 经 `ProcessCmdKey` 处理（WinForms `ShortcutKeys` 不支持 OEM 键与裸键）

### 变更
- 版本号提升至 2.0.8.0（全部项目与内置模组同步）

## [2.0.7.0] - 2026-08-02

### 新增
- **批量处理**（1.x BatchBox 对应能力）：`Core.BatchProcessor` 抽取为 CLI/App 共享的批量处理引擎，遍历目录图片逐张执行 裁切/灰度/换底/排版 组合链，逐张失败不中断
  - `BatchOptions`/`BatchResult`/`Run`/`IsImage`：选项组合、结果汇总、IO 经委托注入（`readImage`/`writeImage`），Core 零渲染后端依赖，CLI 与 App 各自提供 Skia 实现
  - App 壳新增"文件/批量处理"命令：目录选择对话框（输入/输出目录 + 选项勾选 + 排版相纸下拉），后台线程执行不卡 UI，状态栏实时进度，完成弹窗汇总成功/失败数
  - CLI `plugins batch <输入目录> <输出目录> [--crop] [--gray] [--bg] [--layout <相纸>]` 改为复用 Core `BatchProcessor.Run`（删除重复遍历/收集/写盘逻辑）
- `WorkbenchForm.SetStatus` 线程安全：后台任务跨线程更新状态栏自动 `BeginInvoke` 切回 UI 线程
- `WorkbenchForm.PluginRegistry`：壳命令（批量处理）从插件注册表收集滤镜

### 变更
- 版本号提升至 2.0.7.0（全部项目与内置模组同步）

## [2.0.6.0] - 2026-08-02

### 修复
- **文档级撤销链断裂**：裁切/排版生成新文档经 `LoadDocument` 替换后，原文档被直接丢弃，Ctrl+Z 无法回到原图。新增文档导航栈（`_docBack`/`_docForward`），撤销优先回退当前文档历史，历史为空时回退到上一个文档（裁切/排版可撤回原图）
- **历史面板绑定过期文档**：历史面板在模组初始化时捕获 `ActiveDocument`（初始空文档），此后打开/裁切/排版切换文档，面板数据永不刷新。改为动态绑定当前 `ActiveDocument`，文档替换后自动重订阅历史事件并刷新
- **打开文件后丢失保存路径**：`OpenDocumentCommand` 先设 `CurrentPath` 再经 `LoadDocument` 被重置为 null，导致打开后按 Ctrl+S 走另存为。`LoadDocument` 增加 path 参数，打开即保留原保存路径

### 新增
- `ListPanelContent.ActiveDocumentChanged` 通知事件：壳在切换当前文档后触发，模组据此重绑定面板数据源（`WorkbenchForm.AddPanelInternal` 登记列表面板）
- `WorkbenchForm.UndoDocument/RedoDocument/CanUndoDocument/CanRedoDocument`：文档级撤销/重做（含标题与保存路径还原）

### 变更
- 版本号提升至 2.0.6.0（全部项目与内置模组同步）

## [2.0.5.0] - 2026-08-02

### 新增
- **滤镜参数对话框**：`FilterParameterDescriptor` 声明式参数描述（Int/Choice/Color 三种控件类型），壳按描述自动生成对话框，模组零 WinForms 依赖
  - 换底色：目标颜色下拉（蓝/红/白/透明，带色块预览）+ 容差数值框（0~150）
  - 智能裁切：证件照尺寸预设下拉（1寸 295×413 / 小2寸 390×567 / 2寸 413×579 / 3寸 649×1000）
  - 灰度无参数，直接执行
- `IFilterProcessor.Parameters`：滤镜暴露参数描述；`IUiService.PromptFilterParameters`：壳实现参数对话框（确认返回参数、取消返回 null 中止执行）
- `SmartCropFilter.SizePreset` 参数键：尺寸预设（int[]{宽,高}）优先于 Width/Height，CLI/脚本传参不受影响

### 变更
- 版本号提升至 2.0.5.0（全部项目与内置模组同步）

## [2.0.4.0] - 2026-08-02

### 修复
- **智能裁切越界崩溃**：裁切改变输出尺寸，原实现按原图层尺寸写回导致 `Buffer.BlockCopy` 越界。改为生成新文档（结果尺寸即画布），不再写回原图层
- **排版渲染裁剪**：排版相纸（如 5寸 1500x1050）原作为图层塞入照片文档，画布按文档尺寸渲染只显示左上角。改为生成新文档（相纸尺寸即画布），整张相纸完整显示
- `FptpFilterCommand.MergeParameters` 提升为 internal（供生成新文档命令复用）

### 新增
- 文件/保存（Ctrl+S）、文件/另存为 壳命令：合成当前文档写盘（PNG/JPEG/BMP/WebP），复用 `ImageCodecSkia`
- 文件/打印（Ctrl+P）壳命令：合成当前文档按页面可打印区域等比缩放居中打印（对齐 1.x）
- CLI 批量处理：`plugins batch <输入目录> <输出目录> [--crop] [--gray] [--bg] [--layout <相纸>]`，逐张失败不中断（对齐 1.x BatchBox）
- `ImageCodecSkia.SaveComposite` 静态方法：合成位图按扩展名编码保存
- `IUiService.LoadDocument`：模组生成结果（裁切/排版）以新文档呈现，壳负责替换文档并重绘
- `GenerateDocumentCommand` 通用命令：滤镜输出尺寸变化时生成新文档
- `LayoutCommand` 改为生成相纸新文档（替代原"添加图层"方式）

### 变更
- 版本号提升至 2.0.4.0（全部项目与内置模组同步）

## [2.0.3.0] - 2026-08-02

### 新增
- 证件照排版输出（2.0 替代 1.x GenSettings 排版能力，命名即职责）：
  - `LayoutProcessor` 排版处理器：照片网格居中排到相纸（5寸/6寸/A4/A5/自定义），可带虚线裁剪辅助线，纯 PixelSurface 合成
- 内置模组新增"图像/排版输出"子菜单：5寸排版、6寸排版、A4排版
- CLI 新增 `plugins layout <输入> <输出> [相纸] [辅助线]` 命令

### 变更
- 版本号提升至 2.0.3.0（全部项目与内置模组同步）

## [2.0.2.0] - 2026-08-02

### 新增
- 证件照核心滤镜（2.0 替代 1.x Prepalg，按模块归类命名）：
  - `ReplaceBackgroundFilter` 换底色：色键 + 边缘羽化（四角采样，容差可调，支持透明背景）
  - `SmartCropFilter` 智能裁切：中心裁切 + 双线性缩放（默认 1 寸 295x413）
  - `ColorUtil` 颜色工具（2.0 替代 1.x Assalg 颜色部分）
- CLI 泛化滤镜执行：`plugins filter <滤镜Id> <输入> <输出>`
- 通用滤镜命令 `FptpFilterCommand`（参数合并：命令覆盖值优先）

### 变更
- `FilterParameters` 新增 `Keys`（参数合并用）

## [2.0.2.0] - 2026-08-02

### 新增
- 工具框架：
  - `Selection` 选区模型（像素级蒙版 + 多边形栅格化，偶数-奇数规则）
  - `IEditorTool` 交互工具契约（纯数据鼠标事件，零 UI 依赖）
  - `IServiceRegistry` 服务注册表：模组间互相调用（注册服务/按接口获取）
  - 壳画布事件路由：激活工具、鼠标事件转发、Skia 蚂蚁线覆盖层
- 内置模组新增"选择/套索选框"工具（首个 IEditorTool 落地实现）：
  - 一笔画选区，闭合后栅格化写入选区
  - 选区修改入历史栈，可撤销/重做
- 新增 `SelectionEditCommand`（选区编辑命令，区域快照）

### 变更
- `IHostContext` 新增 `Services`（服务注册表）
- `IUiService` 新增 `ActivateTool`（激活/取消交互工具）
- 架构文档同步：契约对齐、模组互调机制、工具契约

## [2.0.1.0] - 2026-08-02

### 新增
- 撤销/重做框架落地：`HistoryStack` 重写（List + 游标、变更通知、深度上限 100）
- `PixelEditCommand` 区域快照命令
- 历史面板（左侧停靠，点击跳转历史）
- 编辑菜单（撤销 Ctrl+Z / 重做 Ctrl+Y）
- 灰度滤镜经历史栈入栈，可撤销

### 变更
- 全项目统一 4 段式版本命名规则（X.Y.Z.W）

## [2.0.0.0] - 2026-08-02

### 新增
- osiris 2.0 工作台模式（VS Code 式模组化架构）：
  - 壳零业务：Workbench 只渲染模组注册的 UI
  - 模组契约：菜单/工具栏/面板/命令/滤镜统一走模组管线
  - 内置模组包 `Fptp.Plugins.Builtin`（官方功能，与第三方同级）
- M0 能力：打开图片、灰度滤镜、插件加载器（net10 ALC 可卸载 / net48 探测）

[2.0.2.0]: https://github.com/houyangbaoxin2009/fptp/tree/osiris
[2.0.1.0]: https://github.com/houyangbaoxin2009/fptp/tree/osiris
[2.0.0.0]: https://github.com/houyangbaoxin2009/fptp/tree/osiris
