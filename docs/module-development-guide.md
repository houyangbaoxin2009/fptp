# 模块开发指南

本文档面向 Osiris 模块开发者：如何编写、打包、本地化一个扩展模块。
完整架构见 `docs/2.1-architecture.md`；本文是实操指南。

## 1. 模块是什么

模块 = 一个独立程序集（DLL），实现 `IModule` 并用 `[PluginExport]` 标记，
由宿主（Osiris.App / Osiris.Cli）动态加载。模块可贡献：

- **滤镜**（`IFilterPlugin`）—— 图像处理算法
- **交互工具**（`IEditorTool`）—— 画布上的编辑工具
- **UI**（`IUiService`）—— 菜单 / 工具栏 / 面板 / 画布 / 状态栏
- **设置组**（`ISettingProvider`）—— 设置窗口的分组与设置项
- **CLI 子命令**（`ICliCommandProvider`）—— 命令行命令

## 2. 目录结构与最小清单

```
plugins/MyModule/
├── module.json          # 模块清单（必需）
├── MyModule.csproj
├── MyModule.cs          # [PluginExport] + IModule
├── langs/               # 语言包（可选，见第 6 节）
│   └── en-us.json
└── Views/               # Avalonia 视图（可选）
```

`module.json` 最小示例：

```json
{
  "id": "mymodule",
  "name": "我的模块",
  "version": "1.0.0.0",
  "kind": "extension",
  "type": "native",
  "language": "dotnet",
  "entryPoint": "MyModule.dll",
  "minHostVersion": "1.0.0"
}
```

| 字段 | 说明 |
|---|---|
| `id` | 全局唯一（点分命名，如 `fptp.idphoto`），配置键前缀 |
| `kind` | `extension`（可装卸）/ `standard`（内置受保护）/ `update`（内置特殊权限） |
| `type` | `native`（.NET 程序集）/ `script`（tie 脚本，2.1 未支持） |
| `language` | **模块实现语言**（dotnet/tie），与 UI 语言无关 |
| `entryPoint` | 入口 DLL 文件名 |
| `minHostVersion` | 最低宿主版本（`1.0.0`） |

## 3. csproj 约定

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- 输出到 plugins/bin/<模块名>/ 子目录（防 module.json 冲突） -->
    <OutputPath>$(MSBuildThisFileDirectory)..\bin\MyModule\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <!-- ABI 红线：只允许引用 Abstractions（+ Algorithms 算法库） -->
    <ProjectReference Include="..\..\src\Osiris.Abstractions\Osiris.Abstractions.csproj" />
    <!-- 托管 UI 视图可引用 Avalonia（经 ALC 转发到默认上下文） -->
    <PackageReference Include="Avalonia" Version="12.1.1" />
  </ItemGroup>
  <ItemGroup>
    <None Include="module.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="langs\**\*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

### ABI 红线（不可违反）

- **允许**：`Osiris.Abstractions`、`Osiris.Algorithms`、`Avalonia`（UI 层）
- **禁止**：`Osiris.Core`、`Osiris.Engine.Skia`、`SkiaSharp` —— 宿主提供能力经
  `IHostContext` / 服务注册表注入

## 4. 模块主体

```csharp
using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;

namespace MyModule;

/// <summary>模块主类：Initialize 时注册服务与贡献 UI。</summary>
[PluginExport]
public sealed class MyModule : IModule
{
    /// <summary>宿主上下文（批处理/命令执行时读取注入服务）。</summary>
    private IHostContext? _host;

    public string Id => "mymodule";
    public string Name => "我的模块";
    public string Version => "1.0.0.0";
    public string MinHostVersion => "1.0.0";
    public ModuleKind Kind => ModuleKind.Extension;
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>初始化：注册服务 → 贡献菜单/命令/面板。</summary>
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;

        // 注册模块服务（其他模块可经 host.Services.Get<T>() 互调）
        host.Services.Register(new MyService());

        // UI 贡献（无 UI 宿主（CLI/测试）时 Ui 为 null，跳过）
        if (host.Ui is { } ui)
        {
            // 1) 命令
            ui.RegisterCommand(new MyCommand(host));

            // 2) 菜单：路径用"段/段"形式（工作台自动按当前语言翻译段）
            ui.AddMenu("我的菜单/执行", "mymodule.run", 100);

            // 3) 面板：视图工厂版（Dock 浮动重建时生成新实例，防双父级崩溃）
            ui.AddPanel("我的面板", () => new MyPanelView(host), DockSide.Right);
        }
    }
}
```

### UI 文本本地化（必做）

所有 UI 文本用 `L10n.T("中文原文")` 包一层——语言包 key 即中文原文，
未命中返回原文，**增量翻译零破坏**：

```csharp
public string DisplayName => L10n.T("执行");   // 命令显示名（惰性：切换语言后 Rebuild 即时刷新）

ui.AddMenu("我的菜单/执行", "mymodule.run", 100);   // 菜单路径保持原文，Rebuild 自动分段翻译
ui.AddPanel("我的面板", ...);                        // 面板标题保持原文
```

参数化文本：`L10n.T("版本：{0}", version)`。

## 5. 贡献设置组

实现 `ISettingProvider`（只需 `Groups` 属性），设置窗口自动渲染并即时 JSON 落盘：

```csharp
public sealed class MyModule : IModule, ISettingProvider
{
    // 设置项类型：Bool / Number / Text / Choice / Color / FilePath
    public IReadOnlyList<SettingGroup> Groups { get; } =
    [
        new SettingGroup
        {
            Id = "mymodule",                    // 组 Id（= 模块 Id，配置存储前缀）
            DisplayName = L10n.T("我的模块"),    // 导航显示名（翻译）
            Items =
            [
                new NumberSettingItem(50, 5, 200, 1)
                {
                    GroupId = "mymodule",
                    Key = "threshold",
                    Label = L10n.T("阈值"),
                    Description = L10n.T("处理阈值（5~200）"),
                    // Scope = SettingScope.User   // User（默认）/ Core / Security（隐藏）
                },
                new ChoiceSettingItem(["a", "b"], "a")
                {
                    GroupId = "mymodule",
                    Key = "mode",
                    Label = L10n.T("模式"),
                },
            ],
        },
    ];
}
```

读取持久化值**必须经注册表**（不要直接读 `SettingItem.Value`——那是构造默认值）：

```csharp
IModuleRegistry? registry = host.Services.Get<IModuleRegistry>();
double threshold = registry.GetConfig("mymodule", "threshold", 50.0) ?? 50.0;
```

## 6. 语言包（i18n）

模块在 `langs/{语言id}.json` 放自己的翻译条目，**随模块分发**：

```json
{
  "$name": "English",
  "我的模块": "My Module",
  "阈值": "Threshold",
  "执行": "Run",
  "我的菜单/执行": "My Menu/Run"
}
```

- **语言 id**：BCP-47 小写形式（`zh-cn` / `en-us` / `ja-jp`）
- **key**：中文原文；未翻译的 key 自动显示中文
- **自动注册**：宿主加载模块后自动合并 `langs/` 进全局语言表，**无需任何代码**
- **优先级**：内置语言包 < 模块语言包 < 用户自定义（`%APPDATA%/Fptp/langs/`）
- **生命周期**：卸载模块 = 翻译条目一并消失
- **切换**：设置 → 核心 → 界面语言（模块贡献的新语言自动出现在下拉）

用户可自定义语言：向 `%APPDATA%\Fptp\langs\` 放入 `{id}.json` 即可新增/覆盖。

## 7. 常见陷阱

| 陷阱 | 正确做法 |
|---|---|
| 面板贡献控件实例 → Dock 浮动双父级崩溃 | 用 `AddPanel(title, () => new View(), side)` 工厂重载 |
| `SettingItem.Value` 读不到持久化值 | 用 `registry.GetConfig<T>(moduleId, key, fallback)` |
| 翻译文本含 `{` 格式符 | 参数化用 `L10n.T("模板 {0}", arg)`；不要硬拼 |
| 下拉项存翻译文本 → 语言切换后匹配失败 | 存稳定 key，ItemTemplate 翻译显示（见传统面板相纸） |
| 模块引用 Core/Skia → ALC 加载失败 | 只引 Abstractions/Algorithms/Avalonia |

## 8. 测试与验证

```bash
# 构建（0 错误 0 警告）
dotnet build FPTP.Osiris.slnx

# 全量测试
dotnet test FPTP.Osiris.slnx

# GUI 验证（构建前先停 Osiris.App 进程防 dll 锁定）
dotnet run --project src/Osiris.App

# CLI 验证（模块子命令动态挂载）
dotnet run --project src/Osiris.Cli -- --help
```

模块加载失败信息打印到控制台（`模块加载失败 {name}: {message}`）——验证 `module.json`
的 `id`/`entryPoint`/`minHostVersion` 是否与宿主版本（`1.0.0`）匹配。

## 9. 版本号（osiris 插件必须遵守）

- 4 段式 `X.Y.Z.W`，`csproj <Version>` 与 `module.json version` 同步
- 每次编译一次 → `W+1`（不推送）；每完成一个任务 → `W` 清零、`Z+1`，提交推送并发布
- `X/Y` 由用户指定，开发者不得修改
- 发布一律打预发布标签（prerelease），直到另行告知转正式
