using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.App.Views;
using Osiris.Core.Plugins;

namespace Osiris.App.ViewModels;

/// <summary>
/// 模块管理：列出全部模块（标准锁定/扩展可禁用卸载），提供启用/禁用/卸载/配置入口。
/// 权限由 Core 的 ModuleRegistry 强制（Standard 的 SetEnabled/MarkRemoved 抛 InvalidOperationException）。
/// </summary>
public sealed partial class ModuleManagerViewModel : ObservableObject
{
    private readonly ModuleRegistry _registry;
    private readonly IServiceRegistry _services;

    public ModuleManagerViewModel(ModuleRegistry registry, IServiceRegistry services)
    {
        _registry = registry;
        _services = services;
    }

    /// <summary>全部模块记录（含 Removed；操作后刷新）。</summary>
    public IReadOnlyList<ModuleRecord> Modules => _registry.Modules;

    /// <summary>当前选中的模块。</summary>
    [ObservableProperty] private ModuleRecord? selectedModule;

    /// <summary>启用/禁用当前模块（仅 Extension 生效；Standard 静默忽略——注册表层会抛异常，此处先判 Kind 避免弹错）。</summary>
    [RelayCommand]
    private void ToggleEnabled()
    {
        if (SelectedModule is not { } m || m.Kind != ModuleKind.Extension) return;
        try
        {
            _registry.SetEnabled(m.Id, m.Status != ModuleStatus.Enabled);
        }
        catch (InvalidOperationException)
        {
            // 权限拒绝：忽略（UI 已按 Kind 限制）
        }
        RefreshSelected();
    }

    /// <summary>卸载当前模块（确认由窗口层完成，确认后调用本方法）。</summary>
    [RelayCommand]
    private void UninstallConfirmed()
    {
        if (SelectedModule is not { } m || m.Kind != ModuleKind.Extension) return;
        try
        {
            _registry.MarkRemoved(m.Id);
        }
        catch (InvalidOperationException)
        {
        }
        RefreshSelected();
    }

    /// <summary>打开设置窗口（该模块或全部模块的设置组）。</summary>
    [RelayCommand]
    private void EditSettings()
    {
        var owner = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime cdt
            ? cdt.MainWindow : null;
        var dlg = new SettingsWindow(new SettingsViewModel(_registry));
        if (owner is not null) dlg.ShowDialog(owner);
        else dlg.Show();
    }

    /// <summary>刷新选中模块记录（操作后 ModuleRecord 以 with 派生新实例）。</summary>
    private void RefreshSelected()
    {
        OnPropertyChanged(nameof(Modules));
        var id = SelectedModule?.Id;
        SelectedModule = id is null ? null : _registry.Get(id);
    }

    /// <summary>模块 Kind 徽标文本。</summary>
    public static string KindLabel(ModuleRecord r) => r.Kind switch
    {
        ModuleKind.Standard => "标准",
        ModuleKind.Extension => "扩展",
        ModuleKind.Update => "更新",
        _ => r.Kind.ToString(),
    };

    /// <summary>模块状态文本。</summary>
    public static string StatusLabel(ModuleRecord r) => r.Status switch
    {
        ModuleStatus.Enabled => "已启用",
        ModuleStatus.Disabled => "已禁用",
        ModuleStatus.Removed => "已卸载",
        _ => r.Status.ToString(),
    };
}
