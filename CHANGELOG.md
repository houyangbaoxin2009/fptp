# 更新日志

本项目的所有重要更改都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)（4 段式 X.Y.Z.W，见 docs/2.0-architecture.md）。

## [2.0.3.0] - 2026-08-02

### 新增
- 证件照排版输出（2.0 替代 1.x GenSettings 排版能力，命名即职责）：
  - `LayoutProcessor` 排版处理器：照片网格居中排到相纸（5寸/6寸/A4/A5/自定义），可带虚线裁剪辅助线，纯 PixelSurface 合成
  - `AddLayerCommand` 添加图层命令：排版结果作为新图层置顶入栈，可撤销/重做
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
