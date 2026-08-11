namespace Osiris.Abstractions.Filters;

/// <summary>
/// 滤镜参数包：Dictionary&lt;string, object&gt; 的类型安全封装。
/// 键为 FilterParameterDescriptor.Key；插件按声明读取、宿主按用户输入写入。
/// </summary>
public sealed class FilterParameters
{
    // 参数字典：键不区分大小写；值的运行时类型与描述符 Kind 约定一致。
    private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>参数总数。</summary>
    public int Count => _values.Count;

    /// <summary>所有参数键。</summary>
    public IReadOnlyCollection<string> Keys => _values.Keys;

    /// <summary>索引器：按键读写（键不存在时 get 抛 KeyNotFoundException）。</summary>
    public object this[string key]
    {
        get => _values[key];
        set => _values[key] = value;
    }

    /// <summary>是否包含指定键。</summary>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <summary>
    /// 类型化取值：键存在且值类型为 T 时返回该值，否则返回 fallback
    /// （避免滤镜侧对每个参数做 try/cast 样板）。
    /// </summary>
    public T Get<T>(string key, T fallback = default!)
        => _values.TryGetValue(key, out var value) && value is T typed ? typed : fallback;

    /// <summary>
    /// 就地合并：overrides 中的非空值覆盖自身同名键，返回 this（可链式调用）。
    /// 宿主用默认参数打底后与用户调整值合并；合并不改写原 overrides。
    /// </summary>
    public FilterParameters Merge(FilterParameters overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        foreach (var (key, value) in overrides._values)
        {
            if (value is not null)
                _values[key] = value;
        }
        return this;
    }
}
