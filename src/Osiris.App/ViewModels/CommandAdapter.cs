using System.Windows.Input;
using AbstractionsCommand = Osiris.Abstractions.Ui.ICommand;

namespace Osiris.App.ViewModels;

/// <summary>
/// 命令适配器：把 Osiris.Abstractions.Ui.ICommand（契约命令）包装为 Avalonia 绑定的 System.Windows.Input.ICommand。
/// 壳内模块贡献的菜单/工具栏按钮经此绑定。
/// </summary>
internal sealed class CommandAdapter : ICommand
{
    private readonly AbstractionsCommand _inner;

    public CommandAdapter(AbstractionsCommand inner) => _inner = inner;

    /// <summary>契约命令暂不支持 CanExecute 状态（恒可执行）。</summary>
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _inner.Execute(null);
}
