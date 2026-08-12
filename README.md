# Osiris 1.0.0 — 模块化跨平台图像工作台

> Osiris 是 FPTP 的第二代（2.x 产品线）；老版 FPTP 1.x（WinForms）已由本仓库 osiris 分支的 2.0 历史承接，Osiris 自身版本线为 1.x。

基于 **.NET 10 + Avalonia 12** 完全重写的跨平台（Win / macOS / Linux）模块化图像编辑器。
从 2.0（WinForms + net48）抛弃全部旧代码，按"PS 级模块化编辑器"架构重建。

## 特性

- **界面完全由模块控制**：壳只是模块运行时 + 空工作台框架，菜单/工具栏/面板/画布全部来自模块贡献
- **模块注册表系统**：记录全部模块信息与配置（JSON 即时持久化），支持启用/禁用/卸载
- **模块分级**：标准模块（内置，受保护，仅更新模块可改文件）/ 扩展模块（用户可安装卸载）/ 更新模块（内置特殊权限）
- **设置分级**：用户设置 / 核心设置（用户可改）+ 安全设置（仅更新模块可改，隔离存储）
- **证件照工作流**：灰度 / 智能裁切 / 换底色 / 动漫模式 / 网格排版（`fptp.idphoto` 扩展模块）
- **撤销重做**：COW 不可变图层，命令级历史栈（零拷贝指针回退）
- **批量处理**：GUI 与 CLI 复用同一批处理管线
- **可卸载插件系统**：ALC 加载扩展模块，ABI 红线契约测试保证卸载承诺
- **tie 语言预留**：模块开发未来迁移到自研 tie 语言；`tie:data` 将取代 JSON 配置（`IConfigStore` 格式中立）

## 解决方案结构

```
src/
├── Osiris.Abstractions/   纯契约（插件可见面：模块/滤镜/工具/设置/CLI 契约）
├── Osiris.Core/           核心实现（注册表/模块加载/历史/成像/批处理，零 UI 依赖）
├── Osiris.Engine.Skia/    Skia 渲染引擎（零拷贝视图/文档合成/编解码）
├── Osiris.CoreModule/     标准模块（文档服务/画布/基础命令/批处理命令）
├── Osiris.App/            壳（模块运行时 + 空工作台 + 模块管理面板）
├── Osiris.Cli/            模块化 CLI 宿主（与 GUI 共享注册表与权限）
plugins/
└── Fptp.Plugins.Builtin/  证件照扩展模块（module.json 清单 + ALC 加载）
tests/                     Core / Engine / Plugins / App 四套测试
```

## 快速开始

```bash
# 构建
dotnet build FPTP.Osiris.slnx

# GUI（模块化工作台）
dotnet run --project src/Osiris.App

# 测试
dotnet test FPTP.Osiris.slnx

# CLI（模块子命令动态挂载，与 GUI 共享模块注册表）
dotnet run --project src/Osiris.Cli -- --help
```

## 模块开发

模块 = 实现 `IModule` 的扩展程序集（`[PluginExport]` 标记），只允许引用 `Osiris.Abstractions`（ABI 红线）。
每个模块根目录放 `module.json` 清单（Id/Name/Version/kind/type/language/entryPoint/minHostVersion/dependencies）。

模块可贡献：滤镜（`IFilterPlugin`）、交互工具（`IEditorTool`）、UI（`IUiService` 菜单/工具栏/面板/画布）、设置组（`ISettingProvider`）、CLI 子命令（`ICliCommandProvider`）。

**语言包（i18n）**：UI 文本用 `L10n.T("中文原文")` 包一层（未命中返回原文，增量翻译零破坏）。
翻译条目放模块目录 `langs/{语言id}.json`（如 `langs/en-us.json`），随模块分发——宿主加载模块后自动注册，
卸载/移除模块时其翻译一并消失。合并优先级：内置语言包 < 模块语言包 < 用户自定义（`%APPDATA%/Fptp/langs/`）。
语言 id 用 BCP-47 小写形式（zh-cn / en-us）；语言选择在 设置 → 核心 → 界面语言，切换即时生效。

详见 `docs/2.1-architecture.md`。

## 技术栈

| 组件 | 版本 |
|---|---|
| .NET | net10.0（单目标） |
| Avalonia | 12.1.1 |
| SkiaSharp | 3.119.4（与 Avalonia.Skia 对齐） |
| DI | Microsoft.Extensions.DependencyInjection |
| MVVM | CommunityToolkit.Mvvm 8.4.2 |
| CLI | System.CommandLine 2.0.10 |
| 测试 | xUnit（App 用 v3）+ Avalonia.Headless.XUnit |


