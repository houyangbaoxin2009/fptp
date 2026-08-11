namespace Osiris.Abstractions.Ui;

/// <summary>
/// 面板内容工厂（纯数据，无 UI 依赖）：模块贡献 dock 面板视图时包装的视图工厂。
/// 壳模板据此创建 LazyViewHost 延迟渲染——每次 Dock 浮动/停靠重建时生成新控件实例，
/// 规避控件实例跨窗口双父级崩溃。工厂内捕获的模块状态（单例）保证新视图数据一致。
/// </summary>
public sealed class PanelContentFactory
{
    /// <summary>视图工厂：创建面板视图（UserControl 等）的新实例。</summary>
    public Func<object> ViewFactory { get; }

    public PanelContentFactory(Func<object> viewFactory) => ViewFactory = viewFactory;
}
