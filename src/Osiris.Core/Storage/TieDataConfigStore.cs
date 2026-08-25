using DoNetTD;

namespace Osiris.Core.Storage;

/// <summary>
/// IConfigStore 的 tie:data 实现（DoNetTD，与 tiec 配置文件同一格式）：
/// 文件布局：顶层表 [ "组.键": 值, ... ]，值仅支持 bool / double（整数存 double）/ string；
/// 其余对象类型写入时 ToString() 降级（与 JsonConfigStore 语义一致）。
/// 损坏/不存在文件 → Load 返回空字典（调用方安全重置）；Save 前确保目录存在。
/// <para>兼容迁移（tie:data 全面替换 JSON）：</para>
/// <list type="bullet">
///   <item>目标 *.data.tie 文件不存在 → 尝试读同名旧 JSON（*.json），数据立即可用（下次 Save 落盘 tie:data 完成迁移）；</item>
///   <item>tie:data 解析失败（文件实为旧 JSON 或损坏）→ 回退 JsonConfigStore 读取。</item>
/// </list>
/// </summary>
public sealed class TieDataConfigStore : IConfigStore
{
    /// <summary>tie:data 文件后缀。</summary>
    public const string DataTieExtension = ".data.tie";

    /// <summary>JSON 文件后缀（旧格式兼容迁移目标）。</summary>
    public const string JsonExtension = ".json";

    /// <summary>解析选项：根必须为容器（表/数组），不接受标量根。</summary>
    private static readonly TieParseOptions ParseOptions = new() { AllowScalarRoot = false };

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            return LoadLegacyJson(filePath);   // 新 tie:data 文件不存在 → 尝试同名旧 JSON 迁移

        string text;
        try
        {
            text = File.ReadAllText(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        if (TieDocument.TryParse(text, out TieDocument? doc, out _, ParseOptions)
            && doc!.Root is TieTable table)
            return ReadTable(table);

        // tie:data 解析失败（文件实为旧 JSON、标量根或损坏）→ 回退 JSON 读取（兼容迁移）
        return new JsonConfigStore().Load(filePath);
    }

    /// <inheritdoc />
    public void Save(string filePath, IReadOnlyDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(data);

        // 先确保目录存在，避免 WriteToFile 抛 DirectoryNotFoundException
        string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var table = new TieTable();
        foreach ((string key, object value) in data)
        {
            switch (value)
            {
                case bool b:            table.SetItem(key, new TieBool(b)); break;
                case int i:             table.SetItem(key, new TieInteger(i)); break;
                case long l:            table.SetItem(key, new TieInteger(l)); break;
                case uint u:            table.SetItem(key, new TieInteger(u)); break;
                case float f:           table.SetItem(key, new TieFloat(f)); break;
                case double d:          table.SetItem(key, new TieFloat(d)); break;
                case string s:          table.SetItem(key, new TieString(s)); break;
                default:
                    // 非标量对象降级为字符串（IConfigStore 只承诺标量，防御性处理）
                    table.SetItem(key, new TieString(value.ToString() ?? ""));
                    break;
            }
        }
        // 官方风格：4 空格缩进 + 尾逗号 + type tie<data> 头部声明（.data.tie 文件角色）
        TieDocument.FromValue(table).WriteToFile(filePath, new TieWriteOptions { EmitHeader = true });
    }

    /// <summary>
    /// 兼容迁移：目标 tie:data 路径不存在 → 读同名旧 JSON。
    /// 把 "*/settings.data.tie" 换成 "*/settings.json" 求旧路径；非 .data.tie 后缀原样返回。
    /// </summary>
    private static IReadOnlyDictionary<string, object> LoadLegacyJson(string filePath)
    {
        string legacy = ToLegacyJsonPath(filePath);
        if (!string.Equals(legacy, filePath, StringComparison.OrdinalIgnoreCase) && File.Exists(legacy))
            return new JsonConfigStore().Load(legacy);
        return new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <summary>"*.data.tie" → "*.json"（保留中间文件名段）；其余路径原样返回。</summary>
    public static string ToLegacyJsonPath(string path)
        => path.EndsWith(DataTieExtension, StringComparison.OrdinalIgnoreCase)
            ? path[..^DataTieExtension.Length] + JsonExtension
            : path;

    /// <summary>顶层 tie:data 表 → 扁平键值字典（仅标量；表/数组/null 等非标量忽略）。</summary>
    private static Dictionary<string, object> ReadTable(TieTable table)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TieValue> pair in table.Items)
        {
            object? value = ToScalar(pair.Value);
            if (value is not null)
                result[pair.Key] = value;
        }
        return result;
    }

    /// <summary>tie:data 标量 → 契约标量（数字统一 double，与 JsonConfigStore 一致）；非标量返回 null。</summary>
    private static object? ToScalar(TieValue value) => value switch
    {
        TieString s => s.Value,
        TieBool b => b.Value,
        TieInteger i => i.AsDouble(),
        TieFloat f => f.Value,
        _ => null,   // 表/数组/null/trit/char 不映射（合约只承诺标量）
    };
}