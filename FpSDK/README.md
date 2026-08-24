# FpSDK

fptp（Osiris）**插件开发 SDK**：聚合包 + 脚手架 CLI + NuGet 打包的三合一入口。
**不是插件本身**，不参与宿主加载——它面向插件开发者。

## 它做什么

写一个 Osiris 插件（模块），你需要：合约（`Osiris.Abstractions`）、算法（`Osiris.Algorithms`）、模块骨架（`IModule` + `[PluginExport]` + `module.json` + `langs/`）。FpSDK 把这些收拢为一件事：

| 能力 | 说明 |
|---|---|
| **聚合包（库）** | `FpSDK` 程序集聚合 `Osiris.Abstractions` + `Osiris.Algorithms`，插件只引 FpSDK 一个项目/包即可写插件；另附开发辅助：`ModuleBase` 基类、`FpContext` 宿主便捷访问、`PluginManifest` 清单模型、`ProjectGenerator` 模板引擎 |
| **脚手架 CLI** | `fpsdk new <name>` 一键生成标准插件骨架（csproj + module.json + IModule + langs），严格遵循 `docs/module-development-guide.md` 约定 |
| **NuGet 打包** | `dotnet pack` 产出 `FpSDK.nupkg`（契约 dll 私有打入，无外部依赖）与 `FpSdkCli` dotnet tool（安装后命令 `fpsdk`） |

## 快速开始

### 方式一：脚手架 CLI（推荐写新插件）

```bash
# 仓库内模式（默认）：生成到 fptp 仓库，csproj 引本地 FpSDK 项目
fpsdk new MyWidget --path plugins/MyWidget --id my.widget

# 外部模式：引 NuGet 包 FpSDK
fpsdk new MyWidget --sdk --path ~/dev/MyWidget

# 开发期也可直接跑源码
dotnet run --project FpSDK/cli/FpSdkCli -- new MyWidget --path plugins/MyWidget
```

生成后 `dotnet build` 即得插件，放到 `plugins/bin/<Name>/` 宿主自动扫描加载。

### 方式二：直接引用 FpSDK（库）

```xml
<!-- 仓库内：ProjectReference -->
<ProjectReference Include="..\..\FpSDK\src\FpSDK\FpSDK.csproj" />
```

```xml
<!-- 外部：NuGet 包（需先 pack/发布） -->
<PackageReference Include="FpSDK" Version="1.0.0" />
```

### 写一个模块

```csharp
using FpSDK;
using Osiris.Abstractions;
using Osiris.Abstractions.Plugins;

namespace MyWidget;

/// <summary>我的组件模块。</summary>
[PluginExport]                       // 宿主扫描 [PluginExport] + IModule（子类必须自行标记）
public sealed class MyWidgetModule : ModuleBase
{
    public override string Id => "my.widget";
    public override string Name => "我的组件";

    protected override void OnInitialize(IHostContext host)
    {
        host.Services.Register(new MyService());
        if (host.Ui is { } ui)
            ui.RegisterCommand(new MyCommand(host));
        // Context / Host 已由基类注入
    }
}
```

## 目录结构

```
FpSDK/
├── FpSDK.slnx
├── src/FpSDK/                   # 聚合库（net10.0）
│   ├── ModuleBase.cs            # IModule 基类（Id/Name 子类给，其余默认）
│   ├── FpContext.cs             # IHostContext 便捷访问（Services/ActiveDocument/HasUi/Report）
│   ├── PluginManifest.cs        # module.json 强类型模型（camelCase 键）
│   └── ProjectGenerator.cs      # 插件骨架生成引擎（CLI/任意宿主可调）
├── cli/FpSdkCli/                # fpsdk 脚手架（dotnet tool，命令 fpsdk）
├── templates/dotnet-module/     # 插件项目模板（token 化）
└── tests/FpSDK.Tests/           # 聚合库 + 模板生成回环测试
```

## 打包发布

```bash
# 聚合包（lib/net10.0 内含 FpSDK+Abstractions+Algorithms，零外部依赖）
dotnet pack FpSDK/src/FpSDK -c Release -o dist

# 脚手架（dotnet tool，安装后命令 fpsdk）
dotnet pack FpSDK/cli/FpSdkCli -c Release -o dist

# 安装脚手架
dotnet tool install --global FpSdkCli --add-source ./dist
```

## ABI 红线（继承宿主约定）

插件只允许引用 `FpSDK / Osiris.Abstractions / Osiris.Algorithms / Avalonia(UI)`；
禁止引用 `Osiris.Core / Osiris.Engine.Skia / SkiaSharp`。宿主能力经
`IHostContext` / 服务注册表注入。

## 测试

```bash
dotnet test FpSDK.slnx   # 7 项：ModuleBase/FpContext 冒烟 + 生成回环（repo/sdk 模式）
```