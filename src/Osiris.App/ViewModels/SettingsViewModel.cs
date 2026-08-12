using CommunityToolkit.Mvvm.ComponentModel;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Settings;
using Osiris.Core.Plugins;

namespace Osiris.App.ViewModels;

/// <summary>设置组视图模型：一组设置项（设置窗口左导航一项）。</summary>
public sealed record SettingGroupViewModel(SettingGroup Group, IReadOnlyList<SettingItem> Items);

/// <summary>
/// 设置视图模型：聚合全部模块贡献的设置组（ISettingProvider.Groups）。
/// Security 级别项不展示（仅更新模块可改）；User/Core 项编辑即经注册表即时保存。
/// 语言设置（osiris.core/language）保存时额外触发语言包切换（LoadLanguage → 壳 Rebuild 即时刷新 UI）。
/// 打开时按**当前语言**生成设置项翻译副本（Label/Description 经 L10n.T 翻译；
/// language 项选项动态取 L10n.AvailableLanguages）——语言切换后重开设置窗口即新语言。
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ModuleRegistry _registry;
    private readonly ILocalizationService _localization;

    /// <summary>全部可见设置组（过滤 Security）。</summary>
    public IReadOnlyList<SettingGroupViewModel> Groups { get; }

    /// <summary>颜色设置行的"选择颜色 (AARRGGBB)..."按钮文字（DataTemplate 内 DataContext 为 ColorSettingItem，
    /// 无法绑 VM 实例属性，故用静态属性经 x:Static 引用；key 即中文原文，未命中返回原文）。</summary>
    public static string BtnChooseColor => L10n.T("选择颜色 (AARRGGBB)...");

    /// <summary>当前选中的设置组（设置窗口左导航选中项）。</summary>
    [ObservableProperty] private SettingGroupViewModel? _selectedGroup;

    public SettingsViewModel(ModuleRegistry registry, ILocalizationService? localization = null)
    {
        _registry = registry;
        _localization = localization ?? new Osiris.Core.Localization.JsonLocalizationService();
        Groups = registry.GetSettingProviders()
            .SelectMany(p => p.Groups)
            .Select(g => new SettingGroupViewModel(g,
                g.Items.Where(i => i.Scope != SettingScope.Security)
                    .Select(LocalizeItem)
                    .ToList()))
            .Where(gv => gv.Items.Count > 0)
            .ToList();
        SelectedGroup = Groups.FirstOrDefault();
    }

    /// <summary>
    /// 生成设置项的语言翻译副本：Label/Description 按当前语言翻译（key 即中文原文）；
    /// Value 回填注册表实际保存值（修复"设置窗口显示构造默认而非已保存值"的既有问题）；
    /// language 项选项动态取 L10n.AvailableLanguages（含模块语言包注册后的全部语言）。
    /// 副本保留 GroupId/Key/Scope——保存仍走原逻辑，模块原对象不被污染。
    /// </summary>
    private SettingItem LocalizeItem(SettingItem item)
    {
        string label = L10n.T(item.Label);
        string? description = item.Description is { } d ? L10n.T(d) : null;
        var common = new { item.GroupId, item.Key, item.Scope };

        return item switch
        {
            BoolSettingItem b => new BoolSettingItem(_registry.GetConfig<bool>(common.GroupId, common.Key, b.Value))
                { GroupId = common.GroupId, Key = common.Key, Label = label, Description = description, Scope = common.Scope },
            NumberSettingItem n => new NumberSettingItem(_registry.GetConfig<double>(common.GroupId, common.Key, n.Value), n.Min, n.Max, n.Step)
                { GroupId = common.GroupId, Key = common.Key, Label = label, Description = description, Scope = common.Scope },
            TextSettingItem t => new TextSettingItem(_registry.GetConfig<string>(common.GroupId, common.Key, t.Value) ?? t.Value)
                { GroupId = common.GroupId, Key = common.Key, Label = label, Description = description, Scope = common.Scope },
            ChoiceSettingItem c => NewChoice(c, label, description, common.GroupId, common.Key, common.Scope),
            ColorSettingItem co => new ColorSettingItem(_registry.GetConfig<uint>(common.GroupId, common.Key, co.Value))
                { GroupId = common.GroupId, Key = common.Key, Label = label, Description = description, Scope = common.Scope },
            FilePathSettingItem f => new FilePathSettingItem(_registry.GetConfig<string>(common.GroupId, common.Key, f.Value) ?? f.Value, f.IsFolder)
                { GroupId = common.GroupId, Key = common.Key, Label = label, Description = description, Scope = common.Scope },
            _ => item,
        };
    }

    /// <summary>
    /// Choice 项副本：Value 回填注册表实际保存值；
    /// language 项（osiris.core/language）选项动态取当前可用语言 id 列表，
    /// Value 不在选项内（旧版配置值）时兜底为当前生效语言，避免下拉空白。
    /// </summary>
    private ChoiceSettingItem NewChoice(ChoiceSettingItem c, string label, string? description, string groupId, string key, SettingScope scope)
    {
        string value = _registry.GetConfig<string>(groupId, key, c.Value) ?? c.Value;
        IReadOnlyList<string> options = groupId == "osiris.core" && key == "language"
            ? L10n.AvailableLanguages.Select(l => l.Id).ToArray()
            : c.Options;
        if (options.Count > 0 && !options.Contains(value, StringComparer.OrdinalIgnoreCase))
            value = L10n.CurrentLanguage; // 旧配置值（如 "中文"）不在可用语言列表 → 兜底当前语言
        return new ChoiceSettingItem(options, value)
        {
            GroupId = groupId,
            Key = key,
            Label = label,
            Description = description,
            Scope = scope,
        };
    }

    /// <summary>保存设置项（即时 JSON 落盘；语言项额外切换语言包）。</summary>
    public void Save(SettingItem item, object? value)
    {
        if (item is null || value is null) return;
        _registry.SetConfig(item.GroupId, item.Key, value);

        // 语言设置：立即加载新语言包（LanguageChanged → 壳 Rebuild 重装配菜单/工具栏/面板标题）
        if (item.Key == "language" && item.GroupId == "osiris.core" && value is string languageId)
            _localization.LoadLanguage(languageId);
    }
}
