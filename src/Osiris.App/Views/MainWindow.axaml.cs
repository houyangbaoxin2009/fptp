using Avalonia.Controls;
using Avalonia.Input;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>
/// 主窗口：空工作台框架，内容全部来自模块贡献。
/// 快捷键路由：KeyDown 解析修饰键组合 → 查注册表快捷键组（fptm.hotkeys 等，
/// 键=命令 Id、值=快捷键文本，User 级设置）→ 匹配后执行命令表对应命令。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>无参构造：Avalonia 运行时加载器要求（XAML 资源可达性）。</summary>
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    /// <inheritdoc />
    /// 快捷键路由：带修饰键或数字键的组合 → 查注册表快捷键配置 → 执行命令。
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is MainWindowViewModel vm && TryRouteShortcut(vm, e))
            e.Handled = true;
    }

    /// <summary>把按键事件匹配到注册表快捷键配置（fptm.hotkeys 组：键=命令 Id，值=快捷键文本）。</summary>
    private static bool TryRouteShortcut(MainWindowViewModel vm, KeyEventArgs e)
    {
        string? shortcut = FormatShortcut(e);
        if (shortcut is null) return false;

        // 遍历全部模块的快捷键组（组 Id 以 "hotkeys" 结尾），值经注册表配置取（回退设置项默认）
        foreach (var group in vm.Registry.GetSettingProviders().SelectMany(p => p.Groups))
        {
            if (!group.Id.EndsWith("hotkeys", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var item in group.Items)
            {
                if (item is not TextSettingItem text || !item.Key.StartsWith("fptm.")) continue;
                string configured = vm.Registry.GetConfig<string>(item.GroupId, item.Key, text.Value) ?? "";
                if (configured.Equals(shortcut, StringComparison.OrdinalIgnoreCase)
                    && vm.Commands.TryGetValue(item.Key, out ICommand? cmd))
                {
                    cmd.Execute(null);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>格式化为 "Ctrl+Shift+A+1" 形式；无修饰键且非数字键返回 null（不拦截普通输入）。</summary>
    private static string? FormatShortcut(KeyEventArgs e)
    {
        bool hasModifier = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)) != 0;
        string? key = KeyName(e.Key);
        if (key is null || (!hasModifier && !char.IsDigit(key[0]))) return null;

        var parts = new List<string>(4);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        parts.Add(key);
        return string.Join("+", parts);
    }

    /// <summary>按键 → 单字符/名称（数字/字母/功能键）；其他键返回 null。</summary>
    private static string? KeyName(Key key) => key switch
    {
        >= Key.D1 and <= Key.D9 => ((char)('1' + (key - Key.D1))).ToString(),
        >= Key.NumPad1 and <= Key.NumPad9 => ((char)('1' + (key - Key.NumPad1))).ToString(),
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.F1 and <= Key.F12 => key.ToString(),
        _ => null,
    };
}
