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
| `id` | 全局唯一（点分命名，如 `fpter`），配置键前缀 |
| `kind` | `extension`（可装卸）/ `standard`（内置受保护）/ `update`（内置特殊权限） |
| `type` | `native`（.NET 程序集）/ `script`（tie 脚本，见第 11 节） |
| `language` | **模块实现语言**（dotnet/tie），与 UI 语言无关 |
| `entryPoint` | 入口：Native 为 DLL 文件名；Script（tie）为 `.tie` 文件（如 `main.tie`） |
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

## 8. 安全机制（防恶意模块）

模块是 ALC 加载的**任意代码**，宿主内置三层防护：

| 层 | 机制 | 说明 |
|---|---|---|
| 1 | 管理员权限警告 | 管理员/root 下启动弹警告（降权重启/仍要继续）；CLI 打印警告继续 |
| 2 | 外部模块加载确认 | `%APPDATA%/Fptp/modules/`（用户手动安装）的模块加载前弹确认框（列出清单）；确认后哈希写入用户信任名单，后续启动自动通过；CLI 打印警告 |
| 3 | 哈希白名单校验 | **已实现**：构建后自动生成内置信任名单 `trusted-modules.json`（模块主 DLL 的 SHA-256，见 `scripts/generate-trusted-modules.ps1`）；加载时校验模块哈希 ∈（内置名单 ∪ 用户名单 `%APPDATA%/Fptp/trusted-modules.json`），不匹配 → 拒绝加载。无内置名单（开发模式）降级放行 |

**可信目录**：程序集旁 `plugins/`（随产品分发）直接加载，但**哈希仍校验**（防 DLL 被替换/篡改）。
**模块哈希校验数据源**：`module.json` 的 `entryPoint` 指定的主文件——Native 为主 DLL，Script 为入口 `.tie` 脚本（`scripts/generate-trusted-modules.ps1` 对两类一视同仁计算 SHA-256）；`signature` 字段为预留扩展。
**用户自定义语言包**：`%APPDATA%\Fptp\langs\`（最高优先，可覆盖一切翻译）。

## 9. 测试与验证

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

## 10. 版本号（osiris 插件必须遵守）

- 4 段式 `X.Y.Z.W`，`csproj <Version>` 与 `module.json version` 同步
- 每次编译一次 → `W+1`（不推送）；每完成一个任务 → `W` 清零、`Z+1`，提交推送并发布
- `X/Y` 由用户指定，开发者不得修改
- 发布一律打预发布标签（prerelease），直到另行告知转正式

## 11. tie 脚本插件（type=script / language=tie）

从 osiris 2026.1 起脚本模块**真正可加载**：宿主（Osiris.Core）经
`TieRunner`（进程调用随包 tiec.exe，v2 帧桥）+ `TieModuleAdapter` 把脚本插件包装为
`IModule + IFilterPlugin`，贡献一个"脚本滤镜"；**进程隔离**天然安全（无 ALC/反射）。

### 11.1 快速开始（脚手架）

```bash
fpsdk new MyFilter --lang tie --id my.filter --path plugins/MyFilter
```

生成（`FpSDK/templates/tie-module`）：

```
plugins/MyFilter/
├── main.tie            # type tie<logic>：实现 process + main 调 fptp.bridge(process)
├── fptp_sdk.tie        # 运行桥库（namespace fptp）：bridge/reply_ok/参数声明/数据/像素工具
├── std/tink.tie        # 帧协议层（tink 帧编解码 + CRC32，import 依赖）
├── rdu/crc.tie         # 增量 CRC32（std/tink.tie 的依赖）
├── module.json         # type=script / language=tie / entryPoint=main.tie
└── langs/              # 语言包（同上第 6 节）
```

**注意**：`std/tink.tie` + `rdu/crc.tie` 必须随插件目录分发——tiec 按工作目录（CWD）解析 import。

### 11.2 协议（fptp.tie-bridge.v2，tink 帧桥）

- 通道：stdin/stdout 文本流，`\n` 定界，每行一条 `base64(帧)`；
- 帧：`[len:u32 BE][payload:len 字节][crc:u32 BE]`，crc = CRC32-IEEE(payload)（校验向量 0xCBF43926）；
- 输入帧 payload = 协议文本（UTF-8）；输出帧 payload = `[tag:1][正文(UTF-8)]`，tag 0x00=OK / 0x01=ERR；
- 宿主写完输入后关闭 stdin（脚本 `read_line()` EOF 退出）；
- 无环境变量 32K 长度限制（脚本滤镜像素上限 4M，防失控）。

### 11.3 编写业务逻辑（示例：亮度滤镜）

```tie
import "fptp_sdk.tie" as fptp

func process(src: string) -> string {
    // 参数自描述探测：识别后返回 "params\n" + 参数声明行，宿主据此自动生成滤镜参数 UI
    if fptp.data_get(src, "action", "") == "params" {
        return "params\n" + fptp.param_int("delta", "亮度增量", -255, 255, 20)
    }
    // 滤镜逻辑：读声明参数真实值 → 处理像素 → 返回 ["pixels": "..."]（尺寸不变）
    var delta = fptp.data_get_int(src, "delta", 20)
    var pixels = fptp.data_get(src, "pixels", "")
    return fptp.data_make("pixels", fptp.pixel_add(pixels, delta))
}

func main() { fptp.bridge(process) }   // 运行桥（勿删）
```

- 输入协议文本含 `width` / `height` / `pixels`（BGRA 预乘字节逗号分隔）+ 脚本声明的参数真实值；
- 返回值必须为 `["pixels": "..."]`（新像素文本，字节数 = width×height×4）或 `["action": "identity"]`（原样）；
- 超限/失败/非法输出 → 脚本滤镜原样返回，不崩。

### 11.4 参数自描述（滤镜参数 UI）

process 识别 `["action": "params"]` 即返回声明文本：首行 `params`，其后每行
`key=..|label=..|kind=..|min=..|max=..|default=..`（`kind`: int / float）。宿主加载时探测一次，
**动态生成** `Parameters`/`Defaults`（滤镜窗口自动渲染参数控件）；未响应 `params` 的脚本视为无参数，
参数用脚本内默认值兜底（向后兼容）。

### 11.5 验证与部署

```bash
# 脚本模块无 csproj（0 .NET 编译），宿主加载时经 tiec 即时编译：
# 放入 plugins/bin/<Name>/（目录含 main.tie + fptp_sdk.tie + std/ + rdu/）自动扫描加载
```

脚本模块与 Native 模块同走模块加载流程（module.json / module.data.tie 清单 + 信任校验）；
开发期调试可仅用 `FpSDK.TieRunner.Run(entry, input)` 直接编译运行验证。

### 11.6 约束

- **零 tie-interp 依赖**：只用 tie 内联底座 + `std/tink.tie` 帧层（import 编译期内联）；
- 像素经协议文本传输（无 file 桥）；`data_*`/`pixel_add` 在字符串层解析 tie:data 顶层标量；
- 编译依赖匹配 LLVM：开发机需 `FPTP_TIE_HOME` 指向含 LLVM 的 tie 发行根，或 tiec.exe 同级放 `llvm/`；
