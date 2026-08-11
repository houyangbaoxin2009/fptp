using Osiris.Abstractions.Modules;

namespace Fptm.Editing;

/// <summary>
/// 模块内共享工具状态（单例）：各画笔工具独立颜色/大小 + 颜料盘 9 槽 + 当前工具。
/// 经注册表持久化（模块 Id "fptm"，键带工具名/槽位前缀，User 级，JSON 即时落盘）。
/// 工具、操作窗口、画笔窗口、颜料盘均经此共享状态。
/// </summary>
public sealed class ToolState
{
    /// <summary>全局单例。</summary>
    public static ToolState Instance { get; } = new();

    private readonly Dictionary<string, uint> _colors = new();
    private readonly Dictionary<string, double> _sizes = new();

    /// <summary>当前选中工具 Id（操作/画笔窗口设置，画布路由用）。</summary>
    public string CurrentToolId { get; set; } = "brush";

    /// <summary>状态变化事件（颜色/大小/当前工具变化时触发，UI 刷新用）。</summary>
    public event Action? Changed;

    // 工具默认颜色（PackBgra 布局：A<<24|R<<16|G<<8|B）：铅笔/钢笔=黑、毛笔/刷子=蓝、颜料桶=红。
    private static readonly Dictionary<string, uint> DefaultColors = new()
    {
        ["pencil"] = 0xFF000000u,
        ["pen"] = 0xFF000000u,
        ["inkBrush"] = 0xFF0000FFu,
        ["brush"] = 0xFF0000FFu,
        ["bucket"] = 0xFFFF0000u,
    };

    // 工具默认大小（铅笔恒 1px、毛笔恒 8px 不在列表）。
    private static readonly Dictionary<string, double> DefaultSizes = new()
    {
        ["pen"] = 3,
        ["inkBrush"] = 8,
        ["brush"] = 16,
    };

    /// <summary>读取工具颜色（未设置回退默认）。</summary>
    public uint GetColor(string toolId)
        => _colors.TryGetValue(toolId, out uint c) ? c : DefaultColors.GetValueOrDefault(toolId, 0xFF000000u);

    /// <summary>设置工具颜色并触发变化事件。</summary>
    public void SetColor(string toolId, uint color)
    {
        _colors[toolId] = color;
        Changed?.Invoke();
    }

    /// <summary>读取工具大小（未设置回退默认）。</summary>
    public double GetSize(string toolId)
        => _sizes.TryGetValue(toolId, out double s) ? s : DefaultSizes.GetValueOrDefault(toolId, 8);

    /// <summary>设置工具大小并触发变化事件。</summary>
    public void SetSize(string toolId, double size)
    {
        _sizes[toolId] = size;
        Changed?.Invoke();
    }

    /// <summary>是否为绘制类工具（铅笔/钢笔/毛笔/刷子；滴管取色时以此决定目标工具）。</summary>
    public bool IsStrokeTool(string toolId) => toolId is "pencil" or "pen" or "inkBrush" or "brush";

    // ---- 颜料盘：9 槽 ----

    /// <summary>颜料盘 9 个颜色槽位（PackBgra）。</summary>
    public uint[] Slots { get; } = new uint[9];

    /// <summary>颜料盘变化事件（UI 刷新用）。</summary>
    public event Action? PaletteChanged;

    /// <summary>读取槽位颜色（未初始化回退默认彩虹渐变）。</summary>
    public uint GetSlot(int index) => Slots[index];

    /// <summary>设置槽位颜色并触发事件。</summary>
    public void SetSlot(int index, uint color)
    {
        Slots[index] = color;
        PaletteChanged?.Invoke();
    }

    /// <summary>默认颜料盘颜色（彩虹渐变，开箱即用）。</summary>
    private static readonly uint[] DefaultPalette =
    [
        0xFF000000u, 0xFFFFFFFFu, 0xFFFF0000u, 0xFF00FF00u, 0xFF0000FFu,
        0xFFFFFF00u, 0xFFFF00FFu, 0xFF00FFFFu, 0xFFFF8000u,
    ];

    // ---- 持久化（注册表，User 级）----

    /// <summary>从注册表加载工具状态与颜料盘（模块初始化时调用）。值经注册表归一化为 double 存取。</summary>
    public void Load(IModuleRegistry registry)
    {
        if (registry is null) return;
        foreach (string toolId in DefaultColors.Keys)
        {
            double? c = registry.GetConfig<double>("fptm", $"{toolId}Color", double.NaN);
            if (c is double cv && double.IsFinite(cv)) _colors[toolId] = (uint)cv;
            double? s = registry.GetConfig<double>("fptm", $"{toolId}Size", double.NaN);
            if (s is double sv && double.IsFinite(sv)) _sizes[toolId] = sv;
        }
        for (int i = 0; i < Slots.Length; i++)
        {
            double? c = registry.GetConfig<double>("fptm", $"slot{i + 1}", double.NaN);
            Slots[i] = c is double cv && double.IsFinite(cv) ? (uint)cv : DefaultPalette[i];
        }
    }

    /// <summary>保存工具状态与颜料盘到注册表（设置面板编辑/颜料盘操作时调用，即时落盘）。</summary>
    public void Save(IModuleRegistry registry)
    {
        if (registry is null) return;
        foreach ((string key, uint color) in _colors)
            registry.SetConfig("fptm", $"{key}Color", (double)color);
        foreach ((string key, double size) in _sizes)
            registry.SetConfig("fptm", $"{key}Size", size);
        for (int i = 0; i < Slots.Length; i++)
            registry.SetConfig("fptm", $"slot{i + 1}", (double)Slots[i]);
    }
}
