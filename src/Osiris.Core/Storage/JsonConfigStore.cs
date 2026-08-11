using System.Text.Json;

namespace Osiris.Core.Storage;

/// <summary>
/// IConfigStore 的 JSON 实现（System.Text.Json，零第三方依赖）。
/// 文件布局：扁平结构 { "组.键": 值 }；值仅支持 bool / double（整数存 double）/ string，
/// 其余对象类型在写入时降级为 ToString() 字符串。
/// 损坏/不存在文件 → Load 返回空字典（调用方安全重置）；Save 前确保目录存在。
/// </summary>
public sealed class JsonConfigStore : IConfigStore
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            return new Dictionary<string, object>(StringComparer.Ordinal);

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                object? value = ConvertValue(property.Value);
                if (value is not null)
                    result[property.Name] = value;
            }
            return result;
        }
        catch (JsonException)
        {
            // 文件损坏（非法 JSON）：按空配置处理，调用方自行重置
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
        catch (IOException)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
    }

    /// <inheritdoc />
    public void Save(string filePath, IReadOnlyDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(data);

        // 先确保目录存在，避免 File.Create 抛 DirectoryNotFoundException
        string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        foreach ((string key, object value) in data)
        {
            switch (value)
            {
                case bool b:            writer.WriteBoolean(key, b); break;
                case int i:             writer.WriteNumber(key, i); break;   // 整数按原样写
                case long l:            writer.WriteNumber(key, l); break;
                case double d:          writer.WriteNumber(key, d); break;
                case string s:          writer.WriteString(key, s); break;
                default:
                    // 非标量对象降级为字符串（tie:data 只承诺标量，防御性处理）
                    writer.WriteString(key, value.ToString());
                    break;
            }
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// JsonElement → 契约标量：数字统一转 double、bool 转 bool、string 转 string；
    /// 对象/数组/null 不支持，返回 null（上层忽略该键）。
    /// </summary>
    private static object? ConvertValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
        _ => null,
    };
}
