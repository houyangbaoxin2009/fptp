using Avalonia.Controls;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>主窗口：空工作台框架，内容全部来自模块贡献。</summary>
public partial class MainWindow : Window
{
    /// <summary>无参构造：Avalonia 运行时加载器要求（XAML 资源可达性）。</summary>
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
