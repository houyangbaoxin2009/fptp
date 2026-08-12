using System.Text.Json;
using Osiris.Abstractions.Localization;

namespace Osiris.Core.Localization;

/// <summary>
/// JSON 语言包服务实现：
/// - 语言包文件：<c>langs/{id}.json</c>，扁平键值对（key 即中文原文，value 为目标语言文本）。
/// - 三类目录按优先级合并（后者覆盖前者同名 key）：
///   ①内置目录 langs/（随产品分发）；②模块语言包（模块目录 langs/，经 RegisterLanguagePack 注册）；
///   ③用户目录 %APPDATA%/Fptp/langs/（用户自定义/覆盖，最高优先）。
/// - 包内元数据键 <c>$name</c> 为该语言显示名（设置面板下拉用）；缺省回退为语言 id。
/// - 未命中 key 返回原文；语言包缺失回退空表（即全部返回原文，UI 保持中文），绝不抛异常。
/// </summary>
public sealed class JsonLocalizationService : ILocalizationService
{
    private readonly List<string> _builtinDirectories;
    private readonly List<string> _moduleDirectories = [];
    private readonly List<string> _userDirectories;
    private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);
    private IReadOnlyList<LanguageInfo> _available = [];
    private string _current = "zh-cn";

    /// <summary>
    /// 构造：指定语言包搜索目录。
    /// <paramref name="langDirectories"/>：内置目录（显式传入后不再自动加程序集旁 langs/；缺省自动发现）。
    /// <paramref name="userDirectories"/>：用户目录（最高优先；缺省自动发现 %APPDATA%/Fptp/langs/）。
    /// 合并优先级恒为：内置 → 模块（RegisterLanguagePack）→ 用户。
    /// </summary>
    public JsonLocalizationService(string[]? langDirectories = null, string[]? userDirectories = null)
    {
        if (langDirectories is { Length: > 0 })
        {
            _builtinDirectories = [.. langDirectories];
        }
        else
        {
            _builtinDirectories = [];
            string builtin = Path.Combine(AppContext.BaseDirectory, "langs");
            if (Directory.Exists(builtin))
                _builtinDirectories.Add(builtin);
        }

        if (userDirectories is { Length: > 0 })
        {
            _userDirectories = [.. userDirectories];
        }
        else
        {
            _userDirectories = [];
            string user = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp", "langs");
            if (Directory.Exists(user))
                _userDirectories.Add(user);
        }
        ScanAvailableLanguages();
    }

    /// <inheritdoc />
    public string CurrentLanguage => _current;

    /// <inheritdoc />
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _available;

    /// <inheritdoc />
    public event EventHandler? LanguageChanged;

    /// <inheritdoc />
    public void RegisterLanguagePack(string langDirectory)
    {
        if (string.IsNullOrWhiteSpace(langDirectory) || !Directory.Exists(langDirectory))
            return;
        // 幂等：同一目录只注册一次（模块语言包目录即模块目录/langs，不重复）
        if (_moduleDirectories.Contains(langDirectory, StringComparer.OrdinalIgnoreCase))
            return;
        _moduleDirectories.Add(langDirectory);
        // 新语言包目录：刷新可用语言列表，并重新合并当前语言（模块加载晚于初始 LoadLanguage）
        ScanAvailableLanguages();
        ReloadCurrent();
    }

    /// <inheritdoc />
    public bool LoadLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            return false;

        // 规范化：小写（BCP-47 id 约定小写，如 zh-cn）
        string id = languageId.ToLowerInvariant();

        // 语言包文件不存在（旧版配置值如 "中文"/"English"、拼写错误）→ 回退 zh-cn，
        // 避免 _current 指向无效 id（设置下拉无匹配项显示空白）。
        if (!AllDirectories().Any(dir => File.Exists(Path.Combine(dir, $"{id}.json"))))
        {
            // 回退默认中文：若连 zh-cn 包都不存在（异常部署），置空表（全部返回原文=中文）。
            if (id == "zh-cn")
            {
                _entries.Clear();
                _current = "zh-cn";
                return false;
            }
            _current = "zh-cn";
            LoadLanguage("zh-cn");
            return false;
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        // 按优先级合并：内置 → 模块 → 用户（后者覆盖前者）
        foreach (string dir in AllDirectories())
        {
            string path = Path.Combine(dir, $"{id}.json");
            if (!File.Exists(path))
                continue;

            try
            {
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
            catch (JsonException)
            {
                // 语言包损坏（非法 JSON）：跳过该文件，其余目录/条目不受影响（未命中返回原文）
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
        if (args is not { Length: > 0 })
            return text;
        try
        {
            return string.Format(text, args);
        }
        catch (FormatException)
        {
            // 翻译文本含非法格式占位符（如孤立 '{'）→ 回退未格式化文本，绝不崩溃
            return text;
        }
    }

    /// <summary>全部语言包目录（内置 → 模块 → 用户，LoadLanguage 合并顺序即此）。</summary>
    private IEnumerable<string> AllDirectories()
        => _builtinDirectories.Concat(_moduleDirectories).Concat(_userDirectories);

    /// <summary>扫描全部目录的 langs/*.json：文件名前缀即语言 id，元数据 $name 为显示名。</summary>
    private void ScanAvailableLanguages()
    {
        var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in AllDirectories())
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (string file in Directory.EnumerateFiles(dir, "*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (byId.ContainsKey(id))
                    continue; // 已收录（内置优先，用户/模块同名不覆盖显示名）
                byId[id] = ReadDisplayName(file, id);
            }
        }

        _available = byId
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new LanguageInfo(kv.Key, kv.Value))
            .ToArray();
    }

    /// <summary>重新合并当前语言（注册模块语言包后调用，把新条目并入已加载包）。</summary>
    private void ReloadCurrent()
    {
        string id = _current;
        _current = "zh-cn"; // 防重入：LoadLanguage 触发 LanguageChanged 时 CurrentLanguage 已就绪
        LoadLanguage(id);
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
}
