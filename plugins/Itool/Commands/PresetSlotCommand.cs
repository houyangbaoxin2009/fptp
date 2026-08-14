using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Itool.Commands;

/// <summary>
/// 预设槽位命令（itool.preset1..9）：应用指定预设（整套画笔颜色）到全部画笔工具。
/// 壳快捷键路由（默认 Ctrl+B+1..9）执行本命令；画笔窗口预设栏也可经命令表触发。
/// </summary>
public sealed class PresetSlotCommand : ICommand
{
    private readonly int _index; // 0-based 预设索引

    public PresetSlotCommand(int index) => _index = index;

    /// <inheritdoc />
    public string Id => $"itool.preset{_index + 1}";

    /// <inheritdoc />
    public string DisplayName => L10n.T("预设 {0}", _index + 1);

    /// <inheritdoc />
    public void Execute(object? parameter) => Editing.ToolState.Instance.ApplyPreset(_index);
}
