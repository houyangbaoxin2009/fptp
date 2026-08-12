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
                g.Items.Where(i => i.Scope != SettingScope.Security).ToList()))
            .Where(gv => gv.Items.Count > 0)
            .ToList();
        SelectedGroup = Groups.FirstOrDefault();
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
