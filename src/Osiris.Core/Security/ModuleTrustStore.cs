using System.Text.Json;

namespace Osiris.Core.Security;

/// <summary>
/// 模块信任名单存储（防篡改白名单）：
/// - 内置名单（trusted-modules.json，随产品分发，构建后自动生成）：官方模块的 DLL SHA-256。
/// - 用户名单（%APPDATA%/Fptp/trusted-modules.json）：用户确认加载的外部模块哈希（即时落盘）。
/// - 校验规则（ModuleLoader 经 IModuleSignatureValidator 调用）：模块哈希 ∈（内置 ∪ 用户）才可信。
/// - 无内置名单文件（开发模式）→ 不启用哈希校验（防锁死开发），仍走外部模块确认流程。
/// JSON 格式：{ "modules": { "模块Id": ["sha256", ...] } }（支持同一模块多版本哈希）。
/// </summary>
public sealed class ModuleTrustStore
{
    private const string ModulesKey = "modules";
    private readonly string _builtinPath;
    private readonly string _userPath;
    private readonly object _lock = new();
    private Dictionary<string, List<string>> _user = new(StringComparer.Ordinal);

    /// <summary>内置名单文件是否存在（不存在 = 开发模式，哈希校验降级放行）。</summary>
    public bool HasBuiltinTrustFile => File.Exists(_builtinPath);

    /// <summary>构造：指定内置/用户名单路径（默认程序集旁 trusted-modules.json + %APPDATA%/Fptp/trusted-modules.json）。</summary>
    public ModuleTrustStore(string? builtinPath = null, string? userPath = null)
    {
        _builtinPath = builtinPath ?? Path.Combine(AppContext.BaseDirectory, "trusted-modules.json");
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp");
        _userPath = userPath ?? Path.Combine(appData, "trusted-modules.json");
        LoadUser();
    }

    /// <summary>模块哈希是否可信（内置名单 ∪ 用户名单）。</summary>
    public bool IsTrusted(string moduleId, string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
            return false;
        lock (_lock)
        {
            if (TryGetHashes(_user, moduleId, out List<string>? userHashes)
                && userHashes.Contains(sha256, StringComparer.OrdinalIgnoreCase))
                return true;
        }
        // 内置名单：文件读取失败/不存在按未命中（调用方据 HasBuiltinTrustFile 决定是否降级）
        return ReadBuiltinHashes(moduleId)?.Contains(sha256, StringComparer.OrdinalIgnoreCase) == true;
    }

    /// <summary>把模块哈希加入用户信任名单（外部模块确认加载后调用；即时落盘）。</summary>
    public void Trust(string moduleId, string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
            return;
        lock (_lock)
        {
            if (!_user.TryGetValue(moduleId, out List<string>? hashes))
                _user[moduleId] = hashes = [];
            if (!hashes.Contains(sha256, StringComparer.OrdinalIgnoreCase))
                hashes.Add(sha256);
            SaveUser();
        }
    }

    /// <summary>加载用户名单（损坏/缺失 → 空名单，不崩溃）。</summary>
    private void LoadUser()
    {
        try
        {
            if (!File.Exists(_userPath))
                return;
            using var stream = File.OpenRead(_userPath);
            var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty(ModulesKey, out JsonElement modules))
                _user = ReadModules(modules);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _user = new Dictionary<string, List<string>>(StringComparer.Ordinal); // 损坏名单 → 空（重新确认）
        }
    }

    /// <summary>即时保存用户名单（JSON 缩进写入；写失败不抛——下次 Trust 重试）。</summary>
    private void SaveUser()
    {
        try
        {
            string dir = Path.GetDirectoryName(_userPath) ?? ".";
            Directory.CreateDirectory(dir);
            var doc = JsonSerializer.SerializeToDocument(
                new Dictionary<string, object> { [ModulesKey] = _user },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_userPath, doc.RootElement.GetRawText());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"信任名单写入失败：{ex.Message}");
        }
    }

    /// <summary>读取内置名单指定模块的哈希列表（缺失/损坏返回 null）。</summary>
    private List<string>? ReadBuiltinHashes(string moduleId)
    {
        try
        {
            if (!File.Exists(_builtinPath))
                return null;
            using var stream = File.OpenRead(_builtinPath);
            var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty(ModulesKey, out JsonElement modules)
                && modules.TryGetProperty(moduleId, out JsonElement hashes)
                && hashes.ValueKind == JsonValueKind.Array)
                return hashes.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? "").Where(h => h.Length > 0).ToList();
            return null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>解析 {"模块Id": ["hash",...]} 结构。</summary>
    private static Dictionary<string, List<string>> ReadModules(JsonElement modules)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (JsonProperty prop in modules.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
                continue;
            result[prop.Name] = prop.Value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? "").Where(h => h.Length > 0).ToList();
        }
        return result;
    }

    /// <summary>按模块 Id 取用户名单哈希（锁内调用）。</summary>
    private static bool TryGetHashes(Dictionary<string, List<string>> map, string moduleId, out List<string>? hashes)
        => map.TryGetValue(moduleId, out hashes) && hashes is { Count: > 0 };
}
