using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
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

    /// <summary>刷新选中模块记录（操作后 ModuleRecord 以 with 派生新实例）。</summary>
    private void RefreshSelected()
    {
        OnPropertyChanged(nameof(Modules));
        var id = SelectedModule?.Id;
        SelectedModule = id is null ? null : _registry.Get(id);
    }

    // ---- 翻译只读属性（XAML 静态中文文本改绑；key 即中文原文，未命中返回原文）----

    /// <summary>底部操作栏"启用/禁用"按钮文字。</summary>
    public string BtnEnableDisable => L10n.T("启用/禁用");

    /// <summary>底部操作栏"卸载"按钮文字。</summary>
    public string BtnUninstall => L10n.T("卸载");

    /// <summary>底部操作栏"关闭"按钮文字。</summary>
    public string BtnClose => L10n.T("关闭");

    /// <summary>版本详情行（"版本：{0}"，参数即模块版本号）。</summary>
    public string VersionText => SelectedModule is { } m ? L10n.T("版本：{0}", m.Version) : "";

    /// <summary>类型详情行（"类型：{0}"，参数经 KindLabel 翻译）。</summary>
    public string KindText => SelectedModule is { } m ? L10n.T("类型：{0}", KindLabel(m)) : "";

    /// <summary>状态详情行（"状态：{0}"，参数经 StatusLabel 翻译）。</summary>
    public string StatusText => SelectedModule is { } m ? L10n.T("状态：{0}", StatusLabel(m)) : "";

    /// <summary>语言详情行（"语言：{0}"，参数即模块语言）。</summary>
    public string LanguageText => SelectedModule is { } m ? L10n.T("语言：{0}", m.Language) : "";

    /// <summary>底部权限提示文字。</summary>
    public string HintText => L10n.T("提示：标准/更新模块受保护，不可禁用或卸载；扩展模块可自由管理。");

    /// <summary>选中模块变化时刷新依赖其的详情行（仅当选中项实际存在才发通知）。</summary>
    partial void OnSelectedModuleChanged(ModuleRecord? value)
    {
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(KindText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(LanguageText));
    }

    /// <summary>模块 Kind 徽标文本（翻译）。</summary>
    public static string KindLabel(ModuleRecord r) => r.Kind switch
    {
        ModuleKind.Standard => L10n.T("标准"),
        ModuleKind.Extension => L10n.T("扩展"),
        ModuleKind.Update => L10n.T("更新"),
        _ => r.Kind.ToString(),
    };

    /// <summary>模块状态文本（翻译）。</summary>
    public static string StatusLabel(ModuleRecord r) => r.Status switch
    {
        ModuleStatus.Enabled => L10n.T("已启用"),
        ModuleStatus.Disabled => L10n.T("已禁用"),
        ModuleStatus.Removed => L10n.T("已卸载"),
        _ => r.Status.ToString(),
    };
}
