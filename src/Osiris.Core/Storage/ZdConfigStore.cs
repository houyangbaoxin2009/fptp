using DoNetZD;

namespace Osiris.Core.Storage;

/// <summary>
/// IConfigStore 的 tie:zd 实现（DoNetZD，tie:data 的压缩二进制变体，见 tieDB）：
/// 数据面与 JsonConfigStore/TieDataConfigStore 同构（扁平键值，bool/double/string 标量），
/// 面向**数据传输/临时文件/备份**场景——紧凑二进制（免解析文本、体积小、速度高），
/// 与 tie 生态 tieDB 载体（zd.codec）互通。
/// 文件后缀约定 *.zd；存在同名 *.data.tie 时可用 TieDataConfigStore 读回等价数据（两格式同数据面）。
/// 损坏/不存在文件 → Load 返回空字典（调用方安全重置）；Save 前确保目录存在。
/// </summary>
public sealed class ZdConfigStore : IConfigStore
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            return new Dictionary<string, object>(StringComparer.Ordinal);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        try
        {
            if (ZdCodec.Decode(bytes) is not ZdValue.Map map)
                return new Dictionary<string, object>(StringComparer.Ordinal);   // 根非 map → 无数据

            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach ((string key, ZdValue value) in map.Entries)
            {
                object? scalar = ToScalar(value);
                if (scalar is not null)
                    result[key] = scalar;
            }
            return result;
        }
        catch (Exception ex) when (ex is ZdFormatException or IOException or UnauthorizedAccessException)
        {
            // 损坏 zd 文件：按空配置处理（调用方安全重置）
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
    }

    /// <inheritdoc />
    public void Save(string filePath, IReadOnlyDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(data);

        // 先确保目录存在，避免 File.WriteAllBytes 抛 DirectoryNotFoundException
        string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var entries = new Dictionary<string, ZdValue>(data.Count, StringComparer.Ordinal);
        foreach ((string key, object value) in data)
        {
            ZdValue? node = value switch
            {
                bool b => new ZdValue.Bool(b),
                int i => new ZdValue.Integer(i),
                long l => new ZdValue.Integer(l),
                uint u => new ZdValue.Integer(u),
                float f => new ZdValue.Float(f),
                double d => new ZdValue.Float(d),
                string s => new ZdValue.String(s),
                // 非标量对象降级为字符串（IConfigStore 只承诺标量，防御性处理）
                _ => new ZdValue.String(value.ToString() ?? ""),
            };
            entries[key] = node;
        }
        File.WriteAllBytes(filePath, ZdCodec.Encode(new ZdValue.Map(entries)));
    }

    /// <summary>zd 标量 → 契约标量（数字统一 double，与 Json/TieData 存储一致）；非标量返回 null。</summary>
    private static object? ToScalar(ZdValue value) => value switch
    {
        ZdValue.String s => s.Value,
        ZdValue.Bool b => b.Value,
        ZdValue.Integer i => (double)i.Value,
        ZdValue.Float f => f.Value,
        _ => null,   // 数组/map/null/trit/char 不映射（合约只承诺标量）
    };
}