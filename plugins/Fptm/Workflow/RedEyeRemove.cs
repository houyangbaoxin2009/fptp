using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;
using Osiris.Algorithms;

namespace Fptm.Workflow;

/// <summary>
/// 红眼去除工作流（fptm）：检测瞳孔高红像素——直通色空间红色显著高于绿/蓝
/// （r - max(g,b) ≥ tolerance 且 r ≥ 100）——把红通道替换为另两通道均值（消除红光、
/// 瞳孔自然变灰暗），再按强度与原始颜色混合。透明像素原样保持。
/// 本类实现 IFilterProcessor 仅复用 Apply 签名；fptm 不注册为 IFilterPlugin，
/// 仅由 fptm 工作流命令调用。
/// </summary>
public sealed class RedEyeRemove : IFilterProcessor
{
    /// <summary>参数键：红光判定阈值（0~100，默认 60；越小判定越敏感）。</summary>
    public const string ParamTolerance = "tolerance";

    /// <summary>参数键：去红强度（0~100，默认 80；0=不变）。</summary>
    public const string ParamStrength = "strength";

    /// <inheritdoc />
    public string Id => "fpter.redEye";

    /// <inheritdoc />
    public string DisplayName => L10n.T("红眼去除");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamTolerance] = 60,
        [ParamStrength] = 80,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new() { Key = ParamTolerance, Label = L10n.T("红眼阈值"), Kind = FilterParameterKind.Int, Min = 0, Max = 100, DefaultValue = 60 },
        new() { Key = ParamStrength, Label = L10n.T("去红强度"), Kind = FilterParameterKind.Int, Min = 0, Max = 100, DefaultValue = 80 },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int tolerance = Math.Clamp(parameters.Get(ParamTolerance, 60), 0, 100);
        int strength = Math.Clamp(parameters.Get(ParamStrength, 80), 0, 100);
        if (strength == 0)
        {
            progress?.Report(100, L10n.T("红眼去除完成"));
            return input.CreateEditor().Commit();   // 强度 0：原样输出
        }

        int width = input.Width, height = input.Height;
        var editor = input.CreateEditor();

        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            Span<byte> row = editor.Row(y);
            for (int x = 0; x < width; x++)
            {
                int o = x * 4;
                byte a = row[o + 3];
                if (a == 0)
                    continue; // 透明像素原样保持

                uint premul = (uint)(row[o] | (row[o + 1] << 8) | (row[o + 2] << 16) | (row[o + 3] << 24));
                uint straight = (uint)ColorUtil.Unpremultiply((int)premul);
                int b = ColorUtil.B((int)straight);
                int g = ColorUtil.G((int)straight);
                int r = ColorUtil.R((int)straight);

                int redScore = r - Math.Max(g, b);
                if (redScore < tolerance || r < 100)
                    continue; // 非红眼像素（红光不足/整体偏暗），原样保持

                // 去红：红通道替换为绿蓝均值（瞳孔自然转灰暗），按强度与原始混合
                int cancel = (g + b) / 2;
                int newR = (r * (100 - strength) + cancel * strength) / 100;

                row[o] = (byte)b;
                row[o + 1] = (byte)g;
                row[o + 2] = (byte)newR;
            }
        }

        editor.MarkAllDirty();
        progress?.Report(100, L10n.T("红眼去除完成"));
        return editor.Commit();
    }
}