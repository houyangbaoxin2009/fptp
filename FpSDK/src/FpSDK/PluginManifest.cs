using System.Text.Json;
using System.Text.Json.Serialization;

namespace FpSDK;

/// <summary>
/// 插件清单（module.json）的强类型模型：宿主按此字段加载模块。
/// 字段与 fptp docs/module-development-guide.md 的 module.json 约定一致（camelCase 键）。
/// 脚手架生成骨架时写入，插件可读回校验；也可用于自检 minHostVersion 等。
/// </summary>
public sealed class PluginManifest
{
    /// <summary>模块唯一 Id（点分命名，如 "fpter"）。</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>模块显示名。</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>模块版本（4 段式，SemVer）。</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>分级：extension（可装卸）/ standard（内置受保护）/ update（内置特殊权限）。</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "extension";

    /// <summary>载体：native（.NET 程序集）/ script（tie 脚本，预留）。</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "native";

    /// <summary>实现语言：dotnet / tie（预留）。</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "dotnet";

    /// <summary>入口 DLL 文件名（Native）。</summary>
    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = "";

    /// <summary>最低宿主版本。</summary>
    [JsonPropertyName("minHostVersion")]
    public string MinHostVersion { get; set; } = "1.0.0";

    /// <summary>依赖模块 Id 列表。</summary>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>从 module.json 文本解析清单。</summary>
    public static PluginManifest Load(string json) =>
        JsonSerializer.Deserialize<PluginManifest>(json, Options)
        ?? throw new FormatException("module.json 解析结果为空");

    /// <summary>序列化为 module.json 文本（2 空格缩进，camelCase 键）。</summary>
    public string ToJson() =>
        JsonSerializer.Serialize(this, Options);
}