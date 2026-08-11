using CommunityToolkit.Mvvm.ComponentModel;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Settings;
using Osiris.Core.Plugins;

namespace Osiris.App.ViewModels;

/// <summary>设置组视图模型：一组设置项（设置窗口左导航一项）。</summary>
public sealed record SettingGroupViewModel(SettingGroup Group, IReadOnlyList<SettingItem> Items);

/// <summary>
/// 设置视图模型：聚合全部模块贡献的设置组（ISettingProvider.Groups）。
/// Security 级别项不展示（仅更新模块可改）；User/Core 项编辑即经注册表即时保存。
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ModuleRegistry _registry;

    /// <summary>全部可见设置组（过滤 Security）。</summary>
    public IReadOnlyList<SettingGroupViewModel> Groups { get; }

    /// <summary>当前选中的设置组（设置窗口左导航选中项）。</summary>
    [ObservableProperty] private SettingGroupViewModel? _selectedGroup;

    public SettingsViewModel(ModuleRegistry registry)
    {
        _registry = registry;
        Groups = registry.GetSettingProviders()
            .SelectMany(p => p.Groups)
            .Select(g => new SettingGroupViewModel(g,
                g.Items.Where(i => i.Scope != SettingScope.Security).ToList()))
            .Where(gv => gv.Items.Count > 0)
            .ToList();
        SelectedGroup = Groups.FirstOrDefault();
    }

    /// <summary>保存设置项（即时 JSON 落盘）。</summary>
    public void Save(SettingItem item, object? value)
    {
        if (item is null || value is null) return;
        _registry.SetConfig(item.GroupId, item.Key, value);
    }
}
