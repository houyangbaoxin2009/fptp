using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Itool.Commands;

/// <summary>
/// 颜料盘槽位命令（itool.palette1..9）：把对应槽位颜色应用到当前画笔工具。
/// 壳快捷键路由（默认 Ctrl+A+1..9）执行本命令；画笔窗口也可经命令表触发。
/// 目标工具经壳当前激活工具判断（绘制类则设给该工具，否则默认刷子）。
/// </summary>
public sealed class PaletteSlotCommand : ICommand
{
    private readonly int _index; // 0-based 槽位索引

    public PaletteSlotCommand(int index) => _index = index;

    /// <inheritdoc />
    public string Id => $"itool.palette{_index + 1}";

    /// <inheritdoc />
    public string DisplayName => L10n.T("颜料槽 {0}", _index + 1);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (_index < 0 || _index >= Editing.ToolState.Instance.Slots.Length) return;
        string? current = ItoolModule.HostContext?.Services.Get<IToolHostService>()?.CurrentToolId;
        string toolId = current is { } c && Editing.ToolState.Instance.IsStrokeTool(c) ? c : "brush";
        Editing.ToolState.Instance.SetColor(toolId, Editing.ToolState.Instance.GetSlot(_index));
    }
}
