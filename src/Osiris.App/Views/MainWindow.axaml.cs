using Avalonia.Controls;
using Osiris.App.ViewModels;

namespace Osiris.App.Views;

/// <summary>主窗口：空工作台框架，内容全部来自模块贡献。</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
