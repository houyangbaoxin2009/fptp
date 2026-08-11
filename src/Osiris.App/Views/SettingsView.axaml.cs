using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Osiris.Abstractions.Settings;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>
/// 设置工具窗口：左侧导航（各模组设置组）+ 右侧该模组设置（DataType 模板零转换器）。
/// 编辑事件直接写注册表（即时 JSON 落盘）；Security 级项已在 ViewModel 过滤不展示。
/// 作为 Dock 工具窗口停靠，可拖拽/浮动；DataContext 由壳注入 SettingsViewModel。
/// </summary>
public partial class SettingsView : UserControl
{
    private bool _loading = true; // 初始化绑定触发的事件忽略

    public SettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => _loading = false;
    }

    private SettingsViewModel? Vm => DataContext as SettingsViewModel;

    private void OnBoolChanged(object? sender, RoutedEventArgs e)
    {
        if (_loading || sender is not CheckBox { DataContext: SettingItem item } cb) return;
        Vm?.Save(item, cb.IsChecked);
    }

    private void OnNumberChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_loading || sender is not NumericUpDown { DataContext: SettingItem item } nud) return;
        Vm?.Save(item, (double)(nud.Value ?? 0m));
    }

    private void OnTextLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: SettingItem item } tb) return;
        Vm?.Save(item, tb.Text ?? "");
    }

    private void OnChoiceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_loading || sender is not ComboBox { DataContext: SettingItem item } combo) return;
        Vm?.Save(item, combo.SelectedItem?.ToString() ?? "");
    }

    /// <summary>
    /// 颜色设置：Hex 输入框（AARRGGBB），关闭后解析保存为 PackBgra 的 uint。
    /// Avalonia 12 主包无内置 ColorPicker 控件，用 Hex 输入（简单数字输入方案）。
    /// </summary>
    private async void OnColorClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ColorSettingItem item }) return;
        var input = new TextBox { Text = item.Value.ToString("X8"), Width = 140 };
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dlg = new Window
        {
            Title = "输入颜色（AARRGGBB，如 FF0000FF=蓝）",
            Width = 300,
            Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(input);
        var ok = new Button { Content = "确定", Width = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        ok.Click += (_, _) => dlg.Close();
        panel.Children.Add(ok);
        dlg.Content = panel;
        if (owner is not null) await dlg.ShowDialog(owner); // 等待对话框关闭后再解析保存
        else dlg.Show();
        if (uint.TryParse(input.Text, NumberStyles.HexNumber, null, out var argb))
            Vm?.Save(item, argb);
    }
}
