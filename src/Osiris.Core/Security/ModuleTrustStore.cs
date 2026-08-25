namespace Osiris.Core.Security;

/// <summary>
/// 模块信任名单存储（防篡改白名单）：
/// - 内置名单（trusted-modules.data.tie，tie:data 格式，随产品分发，构建后自动生成）：官方模块的 DLL SHA-256。
/// - 用户名单（%APPDATA%/Fptp/trusted-modules.data.tie）：用户确认加载的外部模块哈希（即时落盘）。
/// - 校验规则（ModuleLoader 经 IModuleSignatureValidator 调用）：模块哈希 ∈（内置 ∪ 用户）才可信。
/// - 无内置名单文件（开发模式）→ 不启用哈希校验（防锁死开发），仍走外部模块确认流程。
/// 格式（tie:data 顶层表）：[ "modules": [ "模块Id": ["sha256", ...], ... ] ]（支持同一模块多版本哈希）。
/// 兼容：旧 trusted-modules.json 自动回退读取；保存写 .data.tie（tie:data 全面替换 JSON）。
/// </summary>
public sealed class ModuleTrustStore
{
    private const string ModulesKey = "modules";
    private readonly string _builtinPath;
    private readonly string _userPath;
    private readonly object _lock = new();
    private Dictionary<string, List<string>> _user = new(StringComparer.Ordinal);

    /// <summary>内置名单文件是否存在（不存在 = 开发模式，哈希校验降级放行）。</summary>
    public bool HasBuiltinTrustFile => File.Exists(_builtinPath) || File.Exists(ToLegacyJsonPath(_builtinPath));

    /// <summary>构造：指定内置/用户名单路径（默认程序集旁 trusted-modules.data.tie + %APPDATA%/Fptp/trusted-modules.data.tie）。</summary>
    public ModuleTrustStore(string? builtinPath = null, string? userPath = null)
    {
        _builtinPath = builtinPath ?? Path.Combine(AppContext.BaseDirectory, "trusted-modules.data.tie");
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp");
        _userPath = userPath ?? Path.Combine(appData, "trusted-modules.data.tie");
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

    /// <summary>加载用户名单（缺失/损坏 → 空名单，不崩溃；旧 JSON 名单自动迁移读取）。</summary>
    private void LoadUser()
    {
        try
        {
            if (File.Exists(_userPath))
                _user = ReadTrustFile(_userPath);
            else if (File.Exists(ToLegacyJsonPath(_userPath)))
                _user = ReadTrustFile(ToLegacyJsonPath(_userPath));   // 旧 trusted-modules.json 迁移
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            _user = new Dictionary<string, List<string>>(StringComparer.Ordinal); // 损坏名单 → 空（重新确认）
        }
    }

    /// <summary>即时保存用户名单（tie:data 写 .data.tie；写失败不抛——下次 Trust 重试）。</summary>
    private void SaveUser()
    {
        try
        {
            string dir = Path.GetDirectoryName(_userPath) ?? ".";
            Directory.CreateDirectory(dir);
            File.WriteAllText(_userPath, ToTieDataText(_user));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"信任名单写入失败：{ex.Message}");
        }
    }

    /// <summary>读取内置名单指定模块的哈希列表（缺失/损坏返回 null；旧 JSON 名单自动回退读取）。</summary>
    private List<string>? ReadBuiltinHashes(string moduleId)
    {
        try
        {
            string path = File.Exists(_builtinPath) ? _builtinPath : ToLegacyJsonPath(_builtinPath);
            if (!File.Exists(path))
                return null;
            if (!ReadTrustFile(path).TryGetValue(moduleId, out List<string>? hashes))
                return null;
            return hashes;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>把名单解析为 {"模块Id": ["hash",...]}：先按 tie:data（DoNetTD）解析，失败回退 JSON。</summary>
    private static Dictionary<string, List<string>> ReadTrustFile(string path)
    {
        string text = File.ReadAllText(path);
        // tie:data 解析：顶层表 ["modules": [ "模块Id": ["hash",...] ] ]
        if (DoNetTD.TieDocument.TryParse(text, out DoNetTD.TieDocument? doc, out _)
            && doc!.Root is DoNetTD.TieTable table
            && table["modules"] is DoNetTD.TieTable modules)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, DoNetTD.TieValue> pair in modules.Items)
            {
                if (pair.Value is not DoNetTD.TieArray array)
                    continue;
                var hashes = new List<string>();
                foreach (DoNetTD.TieValue item in array)
                    if (item is DoNetTD.TieString s && s.Value.Length > 0)
                        hashes.Add(s.Value);
                result[pair.Key] = hashes;
            }
            return result;
        }
        // 旧 JSON 名单兼容读取
        try
        {
            using var stream = File.OpenRead(path);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(stream);
            if (jsonDoc.RootElement.TryGetProperty(ModulesKey, out System.Text.Json.JsonElement jsonModules))
                return ReadJsonModules(jsonModules);
        }
        catch (System.Text.Json.JsonException)
        {
            // 非法 JSON：按空名单处理
        }
        return new Dictionary<string, List<string>>(StringComparer.Ordinal);
    }

    /// <summary>名单 → tie:data 文本（顶层表，DoNetTD 官方风格缩进/尾逗号）。</summary>
    private static string ToTieDataText(Dictionary<string, List<string>> modules)
    {
        var root = new DoNetTD.TieTable();
        var modulesTable = new DoNetTD.TieTable();
        foreach ((string moduleId, List<string> hashes) in modules)
        {
            var array = new DoNetTD.TieArray();
            foreach (string hash in hashes)
                array.Add(new DoNetTD.TieString(hash));
            modulesTable.SetItem(moduleId, array);
        }
        root.SetItem(ModulesKey, modulesTable);
        return DoNetTD.TieDocument.FromValue(root).Write(new DoNetTD.TieWriteOptions { EmitHeader = true });
    }

    /// <summary>"*.data.tie" → "*.json"（旧路径，兼容迁移）。</summary>
    private static string ToLegacyJsonPath(string path)
        => path.EndsWith(".data.tie", StringComparison.OrdinalIgnoreCase)
            ? path[..^".data.tie".Length] + ".json"
            : path;

    /// <summary>解析 JSON 名单 {"模块Id": ["hash",...]} 结构。</summary>
    private static Dictionary<string, List<string>> ReadJsonModules(System.Text.Json.JsonElement modules)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (System.Text.Json.JsonProperty prop in modules.EnumerateObject())
        {
            if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Array)
                continue;
            result[prop.Name] = prop.Value.EnumerateArray()
                .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
                .Select(e => e.GetString() ?? "").Where(h => h.Length > 0).ToList();
        }
        return result;
    }

    /// <summary>按模块 Id 取用户名单哈希（锁内调用）。</summary>
    private static bool TryGetHashes(Dictionary<string, List<string>> map, string moduleId, out List<string>? hashes)
        => map.TryGetValue(moduleId, out hashes) && hashes is { Count: > 0 };
}
