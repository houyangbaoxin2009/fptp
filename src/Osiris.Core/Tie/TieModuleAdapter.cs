using System.Text;
using DoNetTD;
using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Plugins;

namespace Osiris.Core.Tie;

/// <summary>
/// tie 脚本模块适配器：把 type=script/language=tie 的模块（entryPoint 指向 .tie 入口脚本）包装为
/// 宿主可见的 IModule + IFilterPlugin——脚本贡献一个"脚本滤镜"（TieScriptFilter）。
/// 滤镜执行 = 进程隔离运行 tie 脚本（TieRunner，v2 行帧桥）：宿主把像素面编码为协议文本
/// （RGBA 数字逗号分隔），经 tink 帧 stdin 送入脚本，脚本处理后帧回新像素文本，宿主重建像素面。
/// 脚本依赖 tie 内联底座 + std/tink.tie 帧层 + fptp_sdk.tie 库，进程隔离天然安全（无 ALC/反射）。
/// </summary>
public sealed class TieModuleAdapter : IModule, IFilterPlugin
{
    /// <summary>脚本滤镜实例（命令与 Filters 列表共享）。</summary>
    private readonly TieScriptFilter _filter;

    /// <summary>模块 Id（module.data.tie 的 id）。</summary>
    public string Id { get; }

    /// <summary>模块显示名（module.data.tie 的 name）。</summary>
    public string Name { get; }

    /// <summary>模块版本（module.data.tie 的 version）。</summary>
    public string Version { get; }

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <inheritdoc />
    public IReadOnlyList<IFilterProcessor> Filters => [_filter];

    /// <summary>构造：脚本路径（.tie 入口）+ 清单元数据。</summary>
    public TieModuleAdapter(string scriptPath, string id, string name, string version)
    {
        ArgumentNullException.ThrowIfNull(scriptPath);
        _filter = new TieScriptFilter(id, scriptPath);
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Version = string.IsNullOrWhiteSpace(version) ? "0.0.0.0" : version;
    }

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        // 脚本模块无 .NET 服务注册；滤镜经 Filters 被宿主收集（IFilterPlugin），命令/设置暂由脚本协议承载
    }
}

/// <summary>
/// tie 脚本滤镜：把像素面经协议文本送 tie 脚本处理（进程隔离，v2 行帧桥），重建结果像素面。
/// <para>协议（fptp_sdk.tie 滤镜桥 v2，脚本 main 用 fptp.bridge(process)）：</para>
/// <list type="bullet">
///   <item>参数探测（模块加载/首次使用时一次）：输入 <c>["action": "params"]</c>，脚本返回参数声明文本
///       （首行 <c>params</c>，其后每行 <c>key=..|label=..|kind=..|min=..|max=..|default=..</c>），
///       宿主据此动态构造 Parameters/Defaults（自动生成滤镜参数 UI）；非 <c>params</c> 开头 → 无参数。</item>
///   <item>输入（stdin 一帧）：[ "width": W, "height": H, "pixels": "b,g,r,a,...", "参数键": 值... ]</item>
///   <item>输出（stdout 一帧）：[ "pixels": "b,g,r,a,..." ]（尺寸不变）或 [ "action": "identity" ]（原样）</item>
/// </list>
/// pixels 为原始 BGRA 预乘字节的逗号分隔数字（脚本按字节索引处理，无需理解预乘）。
/// v2 走 stdin/stdout 流（无环境变量长度限制），仅保留大图防失控上限 <see cref="MaxPixels"/>。
/// </summary>
internal sealed class TieScriptFilter : IFilterProcessor
{
    /// <summary>最大像素数（协议文本为逗号分隔数字，防超大图失控；v2 流无 32K 限制）。</summary>
    public const int MaxPixels = 4_000_000;

    /// <summary>参数探测帧：脚本 process 识别后返回参数声明文本（fptp_sdk.tie param_int/param_float）。</summary>
    private const string ProbeInput = "[\"action\": \"params\"]";

    private readonly string _id;
    private readonly string _scriptPath;

    // 参数探测结果（懒加载，只跑一次；失败/无声明 → 空）。参数由脚本自描述，宿主据此动态生成参数 UI。
    private FilterParameterDescriptor[]? _declaredParams;
    private FilterParameters? _declaredDefaults;

    public TieScriptFilter(string moduleId, string scriptPath)
    {
        _id = moduleId + ".script";
        _scriptPath = scriptPath;
    }

    /// <inheritdoc />
    public string Id => _id;

    /// <inheritdoc />
    public string DisplayName => L10n.T("tie 脚本滤镜");

    /// <inheritdoc />
    public FilterParameters Defaults
    {
        get { EnsureProbe(); return _declaredDefaults!; }
    }

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters
    {
        get { EnsureProbe(); return _declaredParams!; }
    }

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int width = input.Width, height = input.Height;
        if (width * height > MaxPixels)
        {
            // 超限：原样返回（脚本桥受环境变量长度限制，v1 演示级小图）
            progress?.Report(100, L10n.T("tie 脚本滤镜仅支持 {0} 像素内的小图（当前 {1}×{2}）", MaxPixels, width, height));
            return input.CreateEditor().Commit();
        }

        // 脚本声明的参数（宿主/用户调整合并后传入）序列化进输入协议文本，脚本用 data_get_* 读取
        string inputText = BuildInput(input, parameters);

        ct.ThrowIfCancellationRequested();
        TieResult result = TieRunner.Run(_scriptPath, inputText);
        if (!result.Ok)
        {
            progress?.Report(100, L10n.T("tie 脚本执行失败：{0}", result.Message));
            return input.CreateEditor().Commit();   // 脚本失败回退原样
        }

        string? pixelsText = ParsePixelsOutput(result.Output);
        if (pixelsText is null)
            return input.CreateEditor().Commit();   // identity：脚本返回原样

        byte[] data = ParsePixelBytes(pixelsText, width, height);
        progress?.Report(100, L10n.T("tie 脚本滤镜完成"));
        return PixelSurface.Create(width, height, data);
    }

    /// <summary>懒探测脚本参数声明（只跑一次）；失败/无声明 → 空参数（脚本用自身默认值兜底）。</summary>
    private void EnsureProbe()
    {
        if (_declaredParams is not null)
            return;
        try
        {
            (FilterParameterDescriptor[] descs, FilterParameters defaults) = ProbeParams();
            _declaredParams = descs;
            _declaredDefaults = defaults;
        }
        catch
        {
            // 探测失败（脚本无 "action" 分支/编译异常等）：视为无参数，不阻断滤镜与加载
            _declaredParams = [];
            _declaredDefaults = new FilterParameters();
        }
    }

    /// <summary>向脚本发探测帧并解析参数声明；脚本未响应 params 协议 → 空。</summary>
    private (FilterParameterDescriptor[] Descriptors, FilterParameters Defaults) ProbeParams()
    {
        TieResult result = TieRunner.Run(_scriptPath, ProbeInput);
        if (!result.Ok)
            return ([], new FilterParameters());
        return ParseParamsDeclaration(result.Output);
    }

    /// <summary>
    /// 解析参数声明文本：首行须为 "params"，其后每行一条
    /// <c>key=..|label=..|kind=..|min=..|max=..|default=..</c>（kind: int / float）。
    /// </summary>
    private static (FilterParameterDescriptor[] Descriptors, FilterParameters Defaults) ParseParamsDeclaration(string output)
    {
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0 || lines[0] != "params")
            return ([], new FilterParameters());

        var descriptors = new List<FilterParameterDescriptor>();
        var defaults = new FilterParameters();
        for (int i = 1; i < lines.Length; i++)
        {
            Dictionary<string, string> fields = ParseParamFields(lines[i]);
            if (!fields.TryGetValue("key", out string? key) || key.Length == 0)
                continue;
            bool isFloat = fields.GetValueOrDefault("kind", "int") == "float";
            double min = ParseFieldDouble(fields, "min", double.MinValue);
            double max = ParseFieldDouble(fields, "max", double.MaxValue);
            double def = ParseFieldDouble(fields, "default", 0);
            // 注意：isFloat ? def : (int)def 会统一成 double（boxing 后 Get<int> 失配），须显式分支定型
            object defaultBoxed = isFloat ? (object)def : (int)def;
            descriptors.Add(new FilterParameterDescriptor
            {
                Key = key,
                Label = L10n.T(fields.GetValueOrDefault("label", key)),   // label 中文原文经语言包翻译
                Kind = isFloat ? FilterParameterKind.Double : FilterParameterKind.Int,
                Min = min,
                Max = max,
                DefaultValue = defaultBoxed,
            });
            defaults[key] = defaultBoxed;
        }
        return (descriptors.ToArray(), defaults);
    }

    /// <summary>解析单条参数行：空白分隔各 <c>键=值</c> 字段。</summary>
    private static Dictionary<string, string> ParseParamFields(string line)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0)
                continue;
            fields[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
        }
        return fields;
    }

    /// <summary>参数字段数值解析：缺省/非法 → fallback。</summary>
    private static double ParseFieldDouble(Dictionary<string, string> fields, string name, double fallback)
        => fields.TryGetValue(name, out string? raw) && double.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double value) ? value : fallback;

    /// <summary>像素面 + 运行时参数 → 输入协议文本（width/height/pixels 固定，脚本声明参数并入）。</summary>
    private string BuildInput(PixelSurface surface, FilterParameters parameters)
    {
        EnsureProbe();
        var sb = new StringBuilder(64 + surface.Pixels.Length * 4);
        sb.Append("[\"width\": ").Append(surface.Width)
          .Append(", \"height\": ").Append(surface.Height);
        // 脚本声明参数的真实值（宿主合并后的 FilterParameters；脚本自身默认值兜底缺失键）
        foreach (FilterParameterDescriptor p in _declaredParams!)
        {
            sb.Append(", \"").Append(p.Key).Append("\": ");
            if (p.Kind == FilterParameterKind.Double)
            {
                sb.Append(parameters.Get(p.Key, p.DefaultValue is double d ? d : 0d)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(parameters.Get(p.Key, p.DefaultValue is int i ? i : 0));
            }
        }
        sb.Append(", \"pixels\": \"");
        ReadOnlySpan<byte> pixels = surface.Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(pixels[i]);
        }
        sb.Append("\"]");
        return sb.ToString();
    }

    /// <summary>解析脚本输出协议文本 → 像素文本；无 pixels / action=identity 返回 null。</summary>
    private static string? ParsePixelsOutput(string output)
    {
        if (!TieDocument.TryParse(output, out TieDocument? doc, out _))
            return null;
        if (doc!.Root is not TieTable table)
            return null;
        if (table["pixels"] is TieString pixels)
            return pixels.Value;
        return null;   // 无 pixels → 视为 identity（原样）
    }

    /// <summary>像素文本（逗号分隔字节）→ BGRA byte[]；长度不符抛 InvalidDataException。</summary>
    private static byte[] ParsePixelBytes(string text, int width, int height)
    {
        int expected = checked(width * height * 4);
        var data = new byte[expected];
        var values = new List<int>(expected);
        var current = new StringBuilder();
        foreach (char c in text)
        {
            if (c == ',')
            {
                if (current.Length > 0)
                {
                    if (int.TryParse(current.ToString(), out int v))
                        values.Add(Math.Clamp(v, 0, 255));
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0 && int.TryParse(current.ToString(), out int last))
            values.Add(Math.Clamp(last, 0, 255));

        if (values.Count != expected)
            throw new InvalidDataException($"tie 脚本返回像素数不符：期望 {expected}，实际 {values.Count}。");

        for (int i = 0; i < expected; i++)
            data[i] = (byte)values[i];
        return data;
    }
}