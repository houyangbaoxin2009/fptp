using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Ui;

namespace Fptm.Views;

/// <summary>
/// 操作窗口（可停靠）：选取/套索/智能框选/滴管 工具切换 + 复制/粘贴/撤销/重做。
/// 工具切换经 IToolHostService 路由到画布激活工具；编辑操作经模块命令（IDocumentService）。
/// 代码构造 UI（Avalonia），避免 XAML 编译绑定；每次 Dock 浮动重建时由视图工厂生成新实例。
/// </summary>
public sealed class OperationWindowView : UserControl
{
    public OperationWindowView()
    {
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 8 };

        // ---- 选择工具区 ----
        panel.Children.Add(SectionLabel("选择工具"));
        var toolButtons = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        toolButtons.Children.Add(ToolButton("选取", "selectRect"));
        toolButtons.Children.Add(ToolButton("套索", "lasso"));
        toolButtons.Children.Add(ToolButton("智能框选", "magicWand"));
        panel.Children.Add(toolButtons);

        // ---- 编辑区 ----
        panel.Children.Add(SectionLabel("编辑"));
        var editButtons = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        editButtons.Children.Add(EditButton("复制"));
        editButtons.Children.Add(EditButton("粘贴"));
        editButtons.Children.Add(EditButton("撤销"));
        editButtons.Children.Add(EditButton("重做"));
        panel.Children.Add(editButtons);

        Content = panel;
    }

    /// <summary>小节标题。</summary>
    private static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.Bold,
        Foreground = Brushes.Gray,
        Margin = new Thickness(0, 4, 0, 0),
    };

    /// <summary>工具切换按钮：设置当前工具并激活。</summary>
    private static Button ToolButton(string label, string toolId)
    {
        var btn = new Button
        {
            Content = label,
            Margin = new Thickness(0, 0, 6, 6),
            MinWidth = 72,
        };
        btn.Click += (_, _) =>
        {
            Editing.ToolState.Instance.CurrentToolId = toolId;
            FptmModule.HostContext?.Services.Get<IToolHostService>()?.ActivateTool(toolId);
        };
        return btn;
    }

    /// <summary>编辑操作按钮（复制/粘贴/撤销/重做 → 模块命令）。</summary>
    private static Button EditButton(string label)
    {
        var btn = new Button
        {
            Content = label,
            Margin = new Thickness(0, 0, 6, 6),
            MinWidth = 72,
        };
        btn.Click += (_, _) =>
        {
            var host = FptmModule.HostContext;
            if (host is null) return;
            Osiris.Abstractions.Ui.ICommand cmd = label switch
            {
                "复制" => new Commands.CopyCommand(host),
                "粘贴" => new Commands.PasteCommand(host),
                "撤销" => new Commands.UndoCommand(host),
                _ => new Commands.RedoCommand(host),
            };
            cmd.Execute(null);
        };
        return btn;
    }
}
