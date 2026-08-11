using System.Globalization;

namespace Osiris.Abstractions.Cli;

/// <summary>
/// CLI 命令调用上下文：解析后的选项值访问（纯数据，宿主填充后传给模块 Handler）。
/// 选项键归一化：以长选项名（去 "--" 前缀后的小写）为键存储，
/// 读取时同样归一化，因此传 "--Filter" / "--filter" / "-f" 均可命中。
/// </summary>
public sealed class CliInvocation
{
    // 归一化后的选项值表：键 = 长选项名去前缀小写，值 = 原始文本。
    private readonly IReadOnlyDictionary<string, string> _optionValues;

    /// <summary>创建调用上下文（构造时归一化全部选项键）。</summary>
    public CliInvocation(string commandName, IReadOnlyDictionary<string, string> optionValues)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(optionValues);

        CommandName = commandName;
        var normalized = new Dictionary<string, string>(optionValues.Count, StringComparer.Ordinal);
        foreach (var (key, value) in optionValues)
            normalized[Normalize(key)] = value;
        _optionValues = normalized;
    }

    /// <summary>被调用的子命令名。</summary>
    public string CommandName { get; }

    /// <summary>
    /// 读取选项值（按长选项名）；不存在/值为空/解析失败时回退 fallback（默认 default(T)）。
    /// 支持 bool（"true"/"false"/"1"/"0"，不区分大小写）、int、double（不变区域解析）、string；
    /// 其他类型走 Convert.ChangeType 通用转换兜底。
    /// </summary>
    public T Get<T>(string optionName, T fallback = default!)
    {
        if (!_optionValues.TryGetValue(Normalize(optionName), out string? value) || value is null)
            return fallback;

        // 目标类型分发：内置类型直解析，其余走通用转换兜底
        if (typeof(T) == typeof(string))
            return value.Length > 0 ? (T)(object)value : fallback;
        if (typeof(T) == typeof(bool))
            return TryParseBool(value, out bool b) ? (T)(object)b : fallback;
        if (typeof(T) == typeof(int))
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? (T)(object)i : fallback;
        if (typeof(T) == typeof(double))
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? (T)(object)d : fallback;

        try
        {
            // 通用兜底：枚举/其他可转换类型
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>解析布尔文本（"true"/"false"/"1"/"0"，不区分大小写）。</summary>
    private static bool TryParseBool(string text, out bool result)
    {
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1")
        {
            result = true;
            return true;
        }
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0")
        {
            result = false;
            return true;
        }
        result = false;
        return false;
    }

    /// <summary>归一化选项名：去掉 "--"/"-" 前缀后转小写（如 "--Filter" → "filter"）。</summary>
    private static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ReadOnlySpan<char> s = name.AsSpan();
        if (s.StartsWith("--", StringComparison.Ordinal))
            s = s[2..];
        else if (s.Length > 0 && s[0] == '-')
            s = s[1..];
        return s.ToString().ToLowerInvariant();
    }
}
