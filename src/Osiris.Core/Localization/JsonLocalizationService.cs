using System.Text.Json;
using Osiris.Abstractions.Localization;

namespace Osiris.Core.Localization;

/// <summary>
/// JSON 语言包服务实现：
/// - 语言包文件：<c>langs/{id}.json</c>，扁平键值对（key 即中文原文，value 为目标语言文本）。
/// - 搜索目录：①程序集旁 langs/（随产品分发的内置语言包）；②%APPDATA%/Fptp/langs/（用户自定义，优先覆盖）。
/// - 包内元数据键 <c>$name</c> 为该语言显示名（设置面板下拉用）；缺省回退为语言 id。
/// - 未命中 key 返回原文；语言包缺失回退空表（即全部返回原文，UI 保持中文），绝不抛异常。
/// </summary>
public sealed class JsonLocalizationService : ILocalizationService
{
    private readonly string[] _langDirectories;
    private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);
    private IReadOnlyList<LanguageInfo> _available = [];
    private string _current = "zh-cn";

    /// <summary>构造：指定语言包搜索目录（默认程序集旁 langs/ + 用户数据目录 langs/）。</summary>
    public JsonLocalizationService(params string[]? langDirectories)
    {
        _langDirectories = langDirectories is { Length: > 0 }
            ? langDirectories
            : DefaultDirectories();
        ScanAvailableLanguages();
    }

    /// <inheritdoc />
    public string CurrentLanguage => _current;

    /// <inheritdoc />
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _available;

    /// <inheritdoc />
    public event EventHandler? LanguageChanged;

    /// <inheritdoc />
    public bool LoadLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            return false;

        // 规范化：小写（BCP-47 id 约定小写，如 zh-cn）
        string id = languageId.ToLowerInvariant();
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        // 合并加载：内置目录在前（低优先），用户目录在后（高优先覆盖同名 key）
        foreach (string dir in _langDirectories)
        {
            string path = Path.Combine(dir, $"{id}.json");
            if (!File.Exists(path))
                continue;

            using var stream = File.OpenRead(path);
            var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "$name")
                    continue; // 元数据，不入翻译表
                if (prop.Value.ValueKind == JsonValueKind.String)
                    entries[prop.Name] = prop.Value.GetString() ?? prop.Name;
            }
        }

        _entries.Clear();
        foreach ((string k, string v) in entries)
            _entries[k] = v;
        _current = id;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc />
    public string Translate(string key, params object?[] args)
    {
        string text = _entries.TryGetValue(key, out string? value) ? value : key;
        return args is { Length: > 0 } ? string.Format(text, args) : text;
    }

    /// <summary>扫描全部目录的 langs/*.json：文件名前缀即语言 id，元数据 $name 为显示名。</summary>
    private void ScanAvailableLanguages()
    {
        var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in _langDirectories)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (string file in Directory.EnumerateFiles(dir, "*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (byId.ContainsKey(id))
                    continue; // 已收录（内置优先，用户目录同名不覆盖显示名）
                byId[id] = ReadDisplayName(file, id);
            }
        }

        _available = byId
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new LanguageInfo(kv.Key, kv.Value))
            .ToArray();
    }

    /// <summary>读取语言包内 $name 元数据；缺省回退为语言 id。</summary>
    private static string ReadDisplayName(string file, string fallbackId)
    {
        try
        {
            using var stream = File.OpenRead(file);
            var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("$name", out JsonElement name)
                && name.ValueKind == JsonValueKind.String)
                return name.GetString() ?? fallbackId;
        }
        catch (JsonException)
        {
            // 语言包损坏：显示名回退 id，加载时同样容错
        }
        return fallbackId;
    }

    /// <summary>默认搜索目录：程序集旁 langs/ + 用户数据目录 Fptp/langs/（仅存在的目录）。</summary>
    private static string[] DefaultDirectories()
    {
        var dirs = new List<string>(2);

        // ① 程序集旁 langs/：随产品分发的内置语言包
        string builtin = Path.Combine(AppContext.BaseDirectory, "langs");
        if (Directory.Exists(builtin))
            dirs.Add(builtin);

        // ② 用户数据目录 Fptp/langs/：用户自定义/覆盖语言包（优先加载顺序靠后）
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp", "langs");
        if (Directory.Exists(appData))
            dirs.Add(appData);

        return [.. dirs];
    }
}
