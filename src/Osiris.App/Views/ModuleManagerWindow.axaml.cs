using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Modules;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>模块管理窗口：列表 + 启用/禁用/卸载/配置（权限由 Core 注册表强制）。</summary>
public partial class ModuleManagerWindow : Window
{
    public ModuleManagerWindow(ModuleManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>卸载前确认（扩展模块），确认后执行卸载。</summary>
    private async void OnUninstallClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ModuleManagerViewModel vm || vm.SelectedModule is not { } m) return;
        if (m.Kind != ModuleKind.Extension) return;
        if (await ShowConfirmAsync($"确定要卸载模块「{m.Name}」吗？卸载后需重新安装才能使用。"))
            vm.UninstallConfirmedCommand.Execute(null);
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    /// <summary>
    /// 轻量确认对话框（Avalonia 无内置 MessageBox，代码构造）。
    /// ShowDialog 为异步任务，须 await 等待关闭后再返回结果。
    /// </summary>
    private async Task<bool> ShowConfirmAsync(string message)
    {
        var result = false;
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
        ok.Click += (_, _) => { result = true; dlg.Close(); };
        var cancel = new Button { Content = "取消", Width = 80 };
        cancel.Click += (_, _) => dlg.Close();
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        panel.Children.Add(btns);
        dlg.Content = panel;
        await dlg.ShowDialog(this); // 等待对话框关闭
        return result;
    }
}
