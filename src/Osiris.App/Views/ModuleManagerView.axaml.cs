using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Modules;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>
/// 模块管理工具窗口：列表 + 启用/禁用/卸载（权限由 Core 注册表强制）。
/// 作为 Dock 工具窗口停靠，可拖拽/浮动/标签化；DataContext 由壳注入 ModuleManagerViewModel。
/// </summary>
public partial class ModuleManagerView : UserControl
{
    public ModuleManagerView() => InitializeComponent();

    /// <summary>卸载前确认（扩展模块），确认后执行卸载。</summary>
    private async void OnUninstallClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ModuleManagerViewModel vm || vm.SelectedModule is not { } m) return;
        if (m.Kind != ModuleKind.Extension) return;
        if (await ShowConfirmAsync($"确定要卸载模块「{m.Name}」吗？卸载后需重新安装才能使用。"))
            vm.UninstallConfirmedCommand.Execute(null);
    }

    /// <summary>关闭工具窗口：从停靠布局移除（经 DockFactory）。</summary>
    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime cdt &&
            cdt.MainWindow?.DataContext is MainWindowViewModel vm)
            vm.CloseToolWindow("moduleManager");
    }

    /// <summary>
    /// 轻量确认对话框（UserControl 无 ShowDialog，经顶层窗口宿主）。
    /// ShowDialog 为异步任务，须 await 等待关闭后再返回结果。
    /// </summary>
    private async Task<bool> ShowConfirmAsync(string message)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dlg = new Window
        {
            Title = "确认卸载",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowDecorations = WindowDecorations.BorderOnly,
            CanResize = false,
        };
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 14 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        var btns = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var ok = new Button { Content = "确定卸载", Width = 90 };
        var cancel = new Button { Content = "取消", Width = 80 };
        var tcs = new TaskCompletionSource<bool>();
        ok.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        panel.Children.Add(btns);
        dlg.Content = panel;
        if (owner is not null) await dlg.ShowDialog(owner);
        else dlg.Show();
        return await tcs.Task;
    }
}
