using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Settings;
using Osiris.Core.Storage;

namespace Osiris.Core.Plugins;

/// <summary>
/// 模块注册表实现（IModuleRegistry）：记录全部模块（含 Removed）信息与模块级配置。
/// 三文件持久化（经 IConfigStore 抽象，格式中立）：
/// - modules.json：扁平 {模块Id: "enabled"|"disabled"|"removed"}，模块状态记录；
/// - settings.json：扁平 {模块Id.键: 值}，User/Core 级别配置（用户可写）；
/// - secure.json：扁平 {模块Id.键: 值}，Security 级别配置（仅更新模块可写，独立文件隔离）。
/// 损坏文件加载时清空重置；SetEnabled/MarkRemoved/SetConfig/ResetConfig 均即时落盘。
/// </summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    // 全部模块记录（含已卸载 Removed；状态变更以 with 派生新记录保持不可变性）
    private readonly List<ModuleRecord> _records = [];

    // modules.json 持久化状态映射：模块Id → 持久化 Status（未记录 = 从未登记/默认启用）
    private readonly Dictionary<string, ModuleStatus> _statusMap = [];

    // settings.json 配置：模块Id → {键: 值}（User/Core 级别）
    private readonly Dictionary<string, Dictionary<string, object>> _configs = [];

    // secure.json 配置：模块Id → {键: 值}（Security 级别，与用户配置隔离）
    private readonly Dictionary<string, Dictionary<string, object>> _secure = [];

    // 设置提供者：GetConfig 回退描述符默认值 + SetConfig 级别校验用（ModuleLoader 初始化后注册）
    private readonly List<ISettingProvider> _settingProviders = [];

    private readonly IConfigStore _store;
    private readonly string _modulesPath;
    private readonly string _configPath;
    private readonly string _securePath;

    // 全部操作互斥：保证读取/持久化原子性
    private readonly object _lock = new();

    /// <summary>构造并立即加载三个配置文件（损坏文件重置为空）。</summary>
    public ModuleRegistry(string modulesPath, string configPath, string securePath, IConfigStore store)
    {
        _modulesPath = modulesPath ?? throw new ArgumentNullException(nameof(modulesPath));
        _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
        _securePath = securePath ?? throw new ArgumentNullException(nameof(securePath));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Load();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModuleRecord> Modules
    {
        get { lock (_lock) return _records.ToArray(); }
    }

    /// <inheritdoc />
    public void Register(ModuleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_lock)
        {
            // 持久化状态优先：modules.json 已有该 Id → 以持久化状态覆盖（record.Status 仅作新模块默认）
            ModuleStatus status = _statusMap.TryGetValue(record.Id, out ModuleStatus persisted) ? persisted : record.Status;
            if (!_statusMap.ContainsKey(record.Id))
            {
                _statusMap[record.Id] = status;
                SaveModules();
            }

            ModuleRecord final = record with { Status = status };
            int index = _records.FindIndex(r => r.Id == record.Id);
            if (index >= 0)
                _records[index] = final;        // 重复 Id 覆盖旧记录
            else
                _records.Add(final);
        }
    }

    /// <inheritdoc />
    public ModuleRecord? Get(string id)
    {
        lock (_lock)
            return _records.Find(r => r.Id == id);
    }

    /// <inheritdoc />
    public bool IsEnabled(string id)
    {
        lock (_lock)
        {
            // 未记录 Id 的模块视为默认启用；Disabled / Removed 均视为不启用
            return !_statusMap.TryGetValue(id, out ModuleStatus status) || status == ModuleStatus.Enabled;
        }
    }

    /// <inheritdoc />
    public void SetEnabled(string id, bool enabled)
    {
        lock (_lock)
        {
            int index = _records.FindIndex(r => r.Id == id);
            ModuleRecord record = index >= 0
                ? _records[index]
                : throw new InvalidOperationException($"模块未登记，无法设置状态: {id}");

            // 权限：内置模块（Standard）不可禁用/卸载。
            // 2.1 契约 ModuleKind 冻结为 Standard/Extension（无 Update 分级），
            // 未来 Update 分级加入后此处同样拦截（更新模块不可被用户禁用）。
            if (record.Kind == ModuleKind.Standard)
                throw new InvalidOperationException($"内置模块不可禁用或卸载: {id}");

            ModuleStatus status = enabled ? ModuleStatus.Enabled : ModuleStatus.Disabled;
            _statusMap[id] = status;
            _records[index] = record with { Status = status };
            SaveModules();
        }
    }

    /// <inheritdoc />
    public void MarkRemoved(string id)
    {
        lock (_lock)
        {
            int index = _records.FindIndex(r => r.Id == id);
            ModuleRecord record = index >= 0
                ? _records[index]
                : throw new InvalidOperationException($"模块未登记，无法卸载: {id}");

            // 权限：内置模块不可卸载（仅扩展模块合法）
            if (record.Kind == ModuleKind.Standard)
                throw new InvalidOperationException($"内置模块不可卸载: {id}");

            _statusMap[id] = ModuleStatus.Removed;
            _records[index] = record with { Status = ModuleStatus.Removed };
            SaveModules();
        }
    }

    /// <inheritdoc />
    public T? GetConfig<T>(string moduleId, string key, T? fallback = default)
    {
        lock (_lock)
        {
            // 1) 用户配置（settings.json，User/Core 级别）
            if (_configs.TryGetValue(moduleId, out Dictionary<string, object>? userCfg)
                && userCfg.TryGetValue(key, out object? userValue) && userValue is T typedUser)
                return typedUser;

            // 2) 安全配置（secure.json，Security 级别——可读不可写）
            if (_secure.TryGetValue(moduleId, out Dictionary<string, object>? secCfg)
                && secCfg.TryGetValue(key, out object? secValue) && secValue is T typedSecure)
                return typedSecure;

            // 3) 回退 ISettingProvider 描述符当前值（即默认值）
            object? descriptor = FindDescriptorDefault(moduleId, key);
            if (descriptor is T typedDescriptor)
                return typedDescriptor;

            // 4) 最终 fallback
            return fallback;
        }
    }

    /// <inheritdoc />
    public void SetConfig(string moduleId, string key, object? value)
    {
        lock (_lock)
        {
            // 设置分级校验：Security 级别仅更新模块可写（写入走 IModuleUpdater.SetSecurityConfig），
            // 普通写入一律拒绝，保证 secure.json 不被用户面污染。
            if (FindScope(moduleId, key) == SettingScope.Security)
                throw new InvalidOperationException($"安全设置仅更新模块可写: {moduleId}.{key}（请经 IModuleUpdater.SetSecurityConfig 写入）");

            if (value is null)
            {
                // null 等价删除该键
                if (_configs.TryGetValue(moduleId, out Dictionary<string, object>? dict))
                {
                    dict.Remove(key);
                    if (dict.Count == 0)
                        _configs.Remove(moduleId);
                }
            }
            else
            {
                if (!_configs.TryGetValue(moduleId, out Dictionary<string, object>? dict))
                    _configs[moduleId] = dict = new Dictionary<string, object>(StringComparer.Ordinal);
                dict[key] = NormalizeValue(value);
            }
            SaveConfigs();
        }
    }

    /// <inheritdoc />
    public void ResetConfig(string moduleId)
    {
        lock (_lock)
        {
            // 仅清空 User/Core 段；安全设置（secure.json）不受影响
            if (_configs.Remove(moduleId))
                SaveConfigs();
        }
    }

    /// <summary>
    /// 登记设置提供者（非接口成员）：GetConfig 回退默认值与 SetConfig 级别校验的数据源。
    /// 由 ModuleLoader 在模块 Initialize 后调用；重复 Id 覆盖旧提供者。
    /// </summary>
    public void RegisterSettingProvider(ISettingProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_lock)
        {
            _settingProviders.RemoveAll(p => p.Id == provider.Id);
            _settingProviders.Add(provider);
        }
    }

    /// <summary>写入安全设置（仅供 IModuleUpdater 调用；即时落盘 secure.json）。</summary>
    internal void SetSecureValue(string moduleId, string key, object? value)
    {
        lock (_lock)
        {
            if (value is null)
            {
                if (_secure.TryGetValue(moduleId, out Dictionary<string, object>? dict))
                {
                    dict.Remove(key);
                    if (dict.Count == 0)
                        _secure.Remove(moduleId);
                }
            }
            else
            {
                if (!_secure.TryGetValue(moduleId, out Dictionary<string, object>? dict))
                    _secure[moduleId] = dict = new Dictionary<string, object>(StringComparer.Ordinal);
                dict[key] = NormalizeValue(value);
            }
            SaveSecure();
        }
    }

    /// <summary>读取安全设置（可读不可写；键不存在返回 null）。</summary>
    internal object? GetSecureValue(string moduleId, string key)
    {
        lock (_lock)
            return _secure.TryGetValue(moduleId, out Dictionary<string, object>? dict) && dict.TryGetValue(key, out object? value) ? value : null;
    }

    // ---- 内部：加载与持久化 ----

    /// <summary>加载三个配置文件；损坏文件经 IConfigStore 返回空字典 → 等价重置为空。</summary>
    private void Load()
    {
        // modules.json：{模块Id: "enabled"|"disabled"|"removed"} → _statusMap
        foreach ((string id, object value) in _store.Load(_modulesPath))
        {
            if (value is string s && Enum.TryParse<ModuleStatus>(s, ignoreCase: true, out ModuleStatus status))
                _statusMap[id] = status;
        }

        // settings.json / secure.json：扁平 "模块Id.键" → 按模块分组
        _configs.Clear();
        foreach ((string moduleId, Dictionary<string, object> dict) in GroupByModule(_store.Load(_configPath)))
            _configs[moduleId] = dict;
        _secure.Clear();
        foreach ((string moduleId, Dictionary<string, object> dict) in GroupByModule(_store.Load(_securePath)))
            _secure[moduleId] = dict;
    }

    /// <summary>扁平字典 → 按"模块Id"分组（键约定 "模块Id.键"，首个 '.' 切分）。</summary>
    private static Dictionary<string, Dictionary<string, object>> GroupByModule(IReadOnlyDictionary<string, object> flat)
    {
        var result = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
        foreach ((string key, object value) in flat)
        {
            int dot = key.IndexOf('.');
            if (dot <= 0 || dot == key.Length - 1)
                continue;   // 无分组前缀的孤立键忽略
            string moduleId = key[..dot];
            string settingKey = key[(dot + 1)..];
            if (!result.TryGetValue(moduleId, out Dictionary<string, object>? dict))
                result[moduleId] = dict = new Dictionary<string, object>(StringComparer.Ordinal);
            dict[settingKey] = value;
        }
        return result;
    }

    /// <summary>分组配置 → 扁平字典（"模块Id.键" 前缀拼接）。</summary>
    private static Dictionary<string, object> Flatten(Dictionary<string, Dictionary<string, object>> grouped)
    {
        var flat = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach ((string moduleId, Dictionary<string, object> dict) in grouped)
            foreach ((string key, object value) in dict)
                flat[$"{moduleId}.{key}"] = value;
        return flat;
    }

    /// <summary>持久化 modules.json（状态即时落盘）。</summary>
    private void SaveModules()
    {
        var flat = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach ((string id, ModuleStatus status) in _statusMap)
            flat[id] = status switch
            {
                ModuleStatus.Disabled => "disabled",
                ModuleStatus.Removed => "removed",
                _ => "enabled",
            };
        _store.Save(_modulesPath, flat);
    }

    /// <summary>持久化 settings.json（User/Core 配置即时落盘）。</summary>
    private void SaveConfigs() => _store.Save(_configPath, Flatten(_configs));

    /// <summary>持久化 secure.json（Security 配置即时落盘）。</summary>
    private void SaveSecure() => _store.Save(_securePath, Flatten(_secure));

    // ---- 内部：设置提供者描述符查询 ----

    /// <summary>在登记的设置提供者中查找匹配的设置项（GroupId==moduleId 且 Key==key）。</summary>
    private SettingItem? FindSettingItem(string moduleId, string key)
    {
        foreach (ISettingProvider provider in _settingProviders)
            foreach (SettingGroup group in provider.Groups)
                foreach (SettingItem item in group.Items)
                    if (item.GroupId == moduleId && item.Key == key)
                        return item;
        return null;
    }

    /// <summary>设置项声明级别；未声明默认 User。</summary>
    private SettingScope FindScope(string moduleId, string key)
        => FindSettingItem(moduleId, key)?.Scope ?? SettingScope.User;

    /// <summary>设置项当前值（作默认值回退用）。</summary>
    private object? FindDescriptorDefault(string moduleId, string key)
        => FindSettingItem(moduleId, key)?.GetValue();

    /// <summary>配置值规范化：数值统一存 double（契约值模型 bool/double/string）。</summary>
    private static object NormalizeValue(object value) => value switch
    {
        int i => (double)i,
        uint u => (double)u,
        float f => (double)f,
        _ => value,
    };

    /// <summary>已注册的设置提供者（设置面板数据源；GetConfig 回退默认值用）。</summary>
    public IReadOnlyList<ISettingProvider> GetSettingProviders() => _settingProviders;
}
