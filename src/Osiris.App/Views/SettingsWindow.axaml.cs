using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Settings;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>
/// 设置窗口（独立窗口，不可停靠工作区）：
/// 左侧导航（各模组设置组）+ 右侧该模组设置（DataType 模板零转换器）。
/// 编辑事件直接写注册表（即时 JSON 落盘）；Security 级项已在 ViewModel 过滤不展示。
/// 独立窗口设计：规避 Dock 浮动设置的卡死问题，且设置不允许停靠到工作区。
/// </summary>
public partial class SettingsWindow : Window
{
    private bool _loading = true; // 初始化绑定触发的事件忽略

    /// <summary>无参构造：Avalonia 运行时加载器要求。</summary>
    public SettingsWindow() => InitializeComponent();

    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
        // 窗口标题（语言 key 即中文原文，未命中返回原文）
        Title = L10n.T("设置");
        Opened += (_, _) => _loading = false;
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
        var dlg = new Window
        {
            Title = L10n.T("输入颜色（AARRGGBB，如 FF0000FF=蓝）"),
            Width = 300,
            Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(input);
        var ok = new Button { Content = L10n.T("确定"), Width = 80, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        ok.Click += (_, _) => dlg.Close();
        panel.Children.Add(ok);
        dlg.Content = panel;
        await dlg.ShowDialog(this); // 等待对话框关闭后再解析保存
        if (uint.TryParse(input.Text, NumberStyles.HexNumber, null, out var argb))
            Vm?.Save(item, argb);
    }
}
