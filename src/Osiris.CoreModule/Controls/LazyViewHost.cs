using Avalonia;
using Avalonia.Controls;

namespace Osiris.CoreModule.Controls;

/// <summary>
/// 延迟视图宿主：模块贡献的 dock 面板视图经此延迟创建。
/// 壳模板（MainWindow.axaml）把模块的 PanelContentFactory（纯数据，含视图工厂委托）渲染为本控件；
/// 首次挂载视觉树时调用 ViewFactory 生成**新的**控件实例——
/// Dock 浮动/停靠重建内容时生成新实例，避免同一控件实例跨窗口双父级崩溃。
/// 视图内共享的模块状态（如 ToolState 单例）保证新实例数据一致。
/// </summary>
public sealed class LazyViewHost : ContentControl
{
    private bool _created;

    /// <summary>视图工厂（模块提供：创建其窗口 UserControl）。</summary>
    public static readonly StyledProperty<Func<object>?> ViewFactoryProperty =
        AvaloniaProperty.Register<LazyViewHost, Func<object>?>(nameof(ViewFactory));

    public LazyViewHost()
    {
        // 面板内容不应阻挡 Dock 拖拽头部的命中测试
        Focusable = false;
    }

    /// <summary>视图工厂（模块提供：创建其窗口 UserControl）。</summary>
    public Func<object>? ViewFactory
    {
        get => GetValue(ViewFactoryProperty);
        set => SetValue(ViewFactoryProperty, value);
    }

    /// <inheritdoc />
    /// 首次挂载时创建视图内容（之后不再重建；Dock 浮动移动本控件实例时内容保持）。
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_created) return;
        _created = true;
        Content = ViewFactory?.Invoke();
    }
}
