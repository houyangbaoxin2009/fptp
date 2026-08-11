using System.Globalization;
using System.Text;
using Osiris.Abstractions;
using Osiris.Abstractions.Cli;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;
using Osiris.Core.Batch;
using Osiris.Core.Document;
using Osiris.CoreModule.Commands;
using Osiris.CoreModule.Services;
using Osiris.CoreModule.ViewModels;

namespace Osiris.CoreModule;

/// <summary>
/// 核心标准模块（Kind=Standard，随产品分发、静态加载）：
/// - 文档服务：注册 DocumentService（打开/撤销/重做/历史栈），并经画布呈现。
/// - 画布：SetCanvas(CanvasControl) 贡献主画布控件（架构第 7 节渲染协议）。
/// - 基础命令：文件/打开、保存、导出；编辑/撤销、重做；视图/缩放适应、实际大小。
/// - 设置：ISettingProvider 贡献 "osiris.core" 设置组（自动保存/撤销上限/画布底色/语言）。
/// - CLI：ICliCommandProvider 贡献 "batch" 批处理子命令（通用批处理管线，复用 Core BatchProcessor）。
/// 本模块不提供业务滤镜（证件照类滤镜归 Fptp.Plugins.Builtin 扩展模块）。
/// </summary>
public sealed class CoreModule : IModule, ISettingProvider, ICliCommandProvider
{
    /// <summary>宿主上下文：批处理 Handler 执行时读取注入的服务/委托。</summary>
    private IHostContext? _host;

    /// <summary>模块唯一 Id。</summary>
    public string Id => "osiris.core";

    /// <summary>模块显示名。</summary>
    public string Name => "核心模块";

    /// <summary>模块版本（与产品 2.1.0.0 对齐）。</summary>
    public string Version => "2.1.0.0";

    /// <summary>要求的最低宿主版本。</summary>
    public string MinHostVersion => "2.1.0.0";

    /// <summary>模块分级：标准模块（内置，用户不可卸载）。</summary>
    public ModuleKind Kind => ModuleKind.Standard;

    /// <summary>依赖模块：核心模块不依赖其他模块。</summary>
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>
    /// 设置组（ISettingProvider）："osiris.core" 核心设置示范五种设置类型
    /// （Bool / Number / Color / Choice / Text）。设置面板按组聚合渲染，编辑即 JSON 即时落盘。
    /// </summary>
    public IReadOnlyList<SettingGroup> Groups { get; } =
    [
        new SettingGroup
        {
            Id = "osiris.core",
            DisplayName = "核心",
            Items =
            [
                new BoolSettingItem(true)
                {
                    GroupId = "osiris.core",
                    Key = "autoSave",
                    Label = "自动保存",
                    Description = "编辑后自动保存文档",
                },
                new NumberSettingItem(50, 5, 200, 1)
                {
                    GroupId = "osiris.core",
                    Key = "maxUndo",
                    Label = "撤销步数上限",
                    Description = "历史栈深度上限（5~200，默认 50）",
                },
                new ColorSettingItem(0xFFFFFFFF)
                {
                    GroupId = "osiris.core",
                    Key = "canvasColor",
                    Label = "画布背景色",
                    Description = "画布空白区背景色（uint PackBgra，默认白色）",
                },
                new ChoiceSettingItem(["中文", "English"], "中文")
                {
                    GroupId = "osiris.core",
                    Key = "language",
                    Label = "界面语言",
                    Description = "界面显示语言",
                },
            ],
        },
    ];

    /// <summary>CLI 子命令列表（ICliCommandProvider）：Initialize 时构建（Handler 需持有宿主）。</summary>
    public IReadOnlyList<CliCommandDescriptor> Commands { get; private set; } = [];

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;

        // ---- 服务注册（插件互调：其他模块经 host.Services.Get<T>() 获取） ----
        host.Services.Register(new AppPaths());               // 应用路径（静态工具类，注册仅占位/发现用）
        var documents = new DocumentService();                 // 文档服务（Core 实现）
        host.Services.Register(documents);                     // 具体类型（本模块命令用）
        host.Services.Register<IDocumentService>(documents);   // 契约接口（扩展模块 fptm 等经接口编辑文档）

        // ---- UI 注册（无 UI 宿主（CLI/测试）时 Ui 为 null，跳过） ----
        if (host.Ui is { } ui)
        {
            var fileDialog = new AvaloniaFileDialogService();
            host.Services.Register<IFileDialogService>(fileDialog);

            // 画布视图模型（架构第 7 节渲染协议）：画布状态唯一数据源，贡献给 Dock 模板绑定。
            // 注意：不再直接贡献 CanvasControl 控件实例——Dock 浮动/移动画布时模板每次生成
            // 新 CanvasControl 绑定同一 VM，避免同一控件被挂到两个父级（Avalonia 拒绝双父级崩溃）。
            var canvasVm = new CanvasDocumentViewModel(documents);
            ui.SetCanvas(canvasVm);

            // 基础命令装配（共享上下文：画布 VM + 文档服务 + 文件对话框 + 当前路径）
            var ctx = new CommandContext
            {
                CanvasVm = canvasVm,
                Documents = documents,
                FileDialog = fileDialog,
            };

            // 注册命令 → 挂菜单（order 越小越靠前）
            ui.RegisterCommand(new OpenCommand(ctx));
            ui.RegisterCommand(new SaveCommand(ctx));
            ui.RegisterCommand(new ExportCommand(ctx));
            ui.RegisterCommand(new UndoCommand(ctx));
            ui.RegisterCommand(new RedoCommand(ctx));
            ui.RegisterCommand(new ZoomFitCommand(ctx));
            ui.RegisterCommand(new ZoomActualCommand(ctx));

            ui.AddMenu("文件/打开", KnownCommands.Open, 10);
            ui.AddMenu("文件/保存", KnownCommands.Save, 20);
            ui.AddMenu("文件/导出", KnownCommands.Export, 30);
            ui.AddMenu("编辑/撤销", KnownCommands.Undo, 10);
            ui.AddMenu("编辑/重做", KnownCommands.Redo, 20);
            ui.AddMenu("视图/缩放适应", KnownCommands.ZoomFit, 10);
            ui.AddMenu("视图/实际大小", KnownCommands.ZoomActual, 20);

            // 图层面板占位：宿主按内容类型渲染（骨架阶段内容为空）。
            ui.AddPanel("图层", null!);
        }

        // ---- CLI 子命令（GUI/CLI 宿主均贡献；batch 复用 Core BatchProcessor） ----
        Commands =
        [
            new CliCommandDescriptor(
                "batch",
                "批量处理图片：--input 输入文件/目录 --out 输出目录 [--filter 滤镜步骤] [--overwrite]",
                BuildBatchOptions(),
                BatchHandler),
        ];
    }

    /// <summary>构造 batch 子命令的选项描述（宿主据此生成 System.CommandLine Option）。</summary>
    private static IReadOnlyList<CliOptionDescriptor> BuildBatchOptions() =>
    [
        new CliOptionDescriptor("--input", "-i", "输入：图片文件路径或目录（多个以分号分隔）", Required: true),
        new CliOptionDescriptor("--out", "-o", "输出目录（不存在则创建）", Required: true),
        new CliOptionDescriptor("--filter", "-f", "滤镜步骤，格式 \"滤镜Id[:键=值[;键=值]]\"，可重复（多次以分号拼接）"),
        new CliOptionDescriptor("--overwrite", null, "覆盖已存在的输出文件", DefaultValue: "true"),
    ];

    /// <summary>
    /// batch 子命令处理器（CliInvocation 由宿主解析填充）。
    /// 滤镜解析与编解码委托**由宿主注入**：
    /// - 滤镜解析器：host.Services.Get&lt;Func&lt;string, IFilterProcessor?&gt;&gt;()
    ///   （App 壳 / CLI 宿主从 IModuleRegistry + 各 IFilterPlugin.Filters 构建并注册）；
    /// - 编解码委托：Get&lt;Func&lt;string, PixelSurface?&gt;&gt;() 与 Get&lt;Func&lt;string, PixelSurface, bool&gt;&gt;()
    ///   （宿主包装 SkiaCodec 注册）。
    /// 设计原因：扩展滤镜模块在 CoreModule 之后才加载，标准模块自身拿不到扩展模块的滤镜，
    /// 因此解析交给宿主编排（职责分离）。委托缺失时返回错误码 2。
    /// </summary>
    private int BatchHandler(CliInvocation invocation)
    {
        IHostContext? host = _host;
        if (host is null)
        {
            Console.Error.WriteLine("batch: 模块未初始化。");
            return 2;
        }

        // ---- 输入解析：--input 支持文件列表（分号分隔）或目录 ----
        string input = invocation.Get<string>("input", "");
        string output = invocation.Get<string>("out", "");
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("batch: --input 与 --out 为必填选项。");
            return 2;
        }

        var files = new List<string>();
        foreach (string item in input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Directory.Exists(item))
                files.AddRange(Directory.EnumerateFiles(item).Where(IsSupportedImage));
            else if (File.Exists(item))
                files.Add(item);
        }
        if (files.Count == 0)
        {
            Console.Error.WriteLine("batch: 未找到任何输入图片。");
            return 2;
        }

        // ---- 滤镜步骤解析：--filter 可重复（宿主以分号拼接为单值传入） ----
        bool overwrite = invocation.Get<bool>("overwrite", true);
        var steps = new List<BatchStep>();

        Func<string, IFilterProcessor?>? resolveFilter = host.Services.Get<Func<string, IFilterProcessor?>>();
        string filterArg = invocation.Get<string>("filter", "");
        if (!string.IsNullOrWhiteSpace(filterArg))
        {
            // 无滤镜解析器 → 无法执行滤镜步骤，报错退出
            if (resolveFilter is null)
            {
                Console.Error.WriteLine("batch: 宿主未注册滤镜解析器（Func<string, IFilterProcessor?>），无法解析 --filter。");
                return 2;
            }

            foreach (string spec in SplitFilterSpecs(filterArg))
            {
                (string filterId, IReadOnlyDictionary<string, string> rawParams) = SplitFilterSpec(spec);

                IFilterProcessor? filter = resolveFilter(filterId);
                if (filter is null)
                {
                    Console.Error.WriteLine($"batch: 未找到滤镜 '{filterId}'。");
                    return 2;
                }

                // 按滤镜参数声明把 CLI 字符串转成类型化值（Int/Double/Bool/Color），未声明键保持字符串
                var parameters = new FilterParameters();
                foreach ((string key, string value) in rawParams)
                    parameters[key] = ConvertCliValue(filter, key, value);

                steps.Add(new BatchStep { FilterId = filterId, Parameters = parameters });
            }
        }

        // ---- 编解码委托：宿主注入（未注册报错，错误码 2） ----
        Func<string, PixelSurface?>? decode = host.Services.Get<Func<string, PixelSurface?>>();
        Func<string, PixelSurface, bool>? encode = host.Services.Get<Func<string, PixelSurface, bool>>();
        if (decode is null || encode is null)
        {
            Console.Error.WriteLine("batch: 宿主未注册编解码委托（SkiaCodec 包装），无法执行批处理。");
            return 2;
        }

        // ---- 执行批处理管线（Core.BatchProcessor，逐张失败收集不中断） ----
        if (!overwrite)
        {
            // 非覆盖模式：过滤掉输出已存在的文件（输出文件与输入同名）。
            files = files.Where(f => !File.Exists(Path.Combine(output, Path.GetFileName(f)))).ToList();
        }

        BatchResult result = BatchProcessor.Run(
            files,
            output,
            steps,
            decode,
            encode,
            resolveFilter ?? (_ => null), // 无滤镜步骤时解析器不会真正被调用
            host.Report,
            CancellationToken.None);

        foreach (string error in result.Errors)
            Console.Error.WriteLine(error);
        Console.WriteLine($"batch: 完成 {result.Succeeded} 成功，{result.Failed} 失败。");
        return result.Failed == 0 ? 0 : 1;
    }

    /// <summary>是否为本批处理支持的图片扩展名（Skia 可解码格式）。</summary>
    private static bool IsSupportedImage(string path)
        => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";

    /// <summary>
    /// 拆分多条滤镜规格：宿主把多次 --filter 以 ';' 拼接；单个滤镜内键值对也用 ';' 分隔。
    /// 增量解析规则：含 ':'（滤镜头）或不含 '='（独立滤镜 Id）的段开启新滤镜；
    /// 含 '=' 且不含 ':' 的段视为参数，追加到当前滤镜（前缀 ';'）。
    /// 例："grayscale;replaceBackground:color=0,0,255" → ["grayscale", "replaceBackground:color=0,0,255"]。
    /// </summary>
    internal static IEnumerable<string> SplitFilterSpecs(string joined)
    {
        var specs = new List<string>();
        var builder = new StringBuilder();

        foreach (string segment in joined.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            bool isParam = segment.Contains('=') && !segment.Contains(':');
            if (isParam)
            {
                // 参数段：追加到当前滤镜规格末尾
                if (builder.Length > 0)
                    builder.Append(';');
                builder.Append(segment);
            }
            else
            {
                // 新滤镜：先结算上一条规格
                if (builder.Length > 0)
                {
                    specs.Add(builder.ToString());
                    builder.Clear();
                }
                builder.Append(segment);
            }
        }

        if (builder.Length > 0)
            specs.Add(builder.ToString());
        return specs;
    }

    /// <summary>
    /// 拆分单条滤镜规格 "滤镜Id[:键=值[;键=值]]"：
    /// 冒号前为滤镜 Id，冒号后按 ';' 拆键值对（键不区分大小写）。
    /// </summary>
    internal static (string Id, IReadOnlyDictionary<string, string> Parameters) SplitFilterSpec(string spec)
    {
        string id = spec;
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int colon = spec.IndexOf(':');
        if (colon > 0)
        {
            id = spec[..colon];
            foreach (string pair in spec[(colon + 1)..].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0)
                    parameters[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
            }
        }

        return (id, parameters);
    }

    /// <summary>
    /// 把 CLI 文本参数按滤镜参数声明转为类型化值：
    /// Bool→bool、Int→int、Double→double、Color→uint(PackBgra)，其余/未声明键保持字符串。
    /// </summary>
    internal static object ConvertCliValue(IFilterProcessor filter, string key, string text)
    {
        FilterParameterDescriptor? descriptor = filter.Parameters
            .FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (descriptor is null)
            return text; // 未声明的参数按原始字符串传递

        return descriptor.Kind switch
        {
            FilterParameterKind.Bool =>
                text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1",
            FilterParameterKind.Int =>
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : text,
            FilterParameterKind.Double =>
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : text,
            FilterParameterKind.Color => ParseColor(text),
            _ => text, // Choice 等类型保持字符串（滤镜侧按需解析）
        };
    }

    /// <summary>解析颜色文本 "r,g,b[,a]" → PackBgra uint（A&lt;&lt;24 | R&lt;&lt;16 | G&lt;&lt;8 | B）；解析失败原样返回字符串。</summary>
    private static object ParseColor(string text)
    {
        string[] parts = text.Split(',');
        if (parts.Length >= 3
            && byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r)
            && byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g)
            && byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b))
        {
            byte a = parts.Length >= 4
                && byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte alpha)
                ? alpha
                : (byte)255;
            return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        }
        return text;
    }
}
