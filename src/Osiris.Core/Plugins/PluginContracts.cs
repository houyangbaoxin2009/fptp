using System.Collections.Generic;
using System.Threading;
using Osiris.Core.Document;
using Osiris.Core.Imaging;

namespace Osiris.Core.Plugins
{
    /// <summary>插件契约：滤镜/工具/格式统一入口（Oracle 验证：不泄漏 SkiaSharp 类型）。</summary>
    public interface IPlugin
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string MinHostVersion { get; }
        void Initialize(IHostContext host);
    }

    /// <summary>宿主上下文：插件与 Leibniz 脚本共用。</summary>
    public interface IHostContext
    {
        OsirisDocument ActiveDocument { get; }
        IPluginRegistry Plugins { get; }
        IProgress Progress { get; }
        CancellationToken Cancellation { get; }
        /// <summary>UI 服务：模组贡献菜单/工具栏/面板（壳为 null 时模组跳过 UI 注册）。</summary>
        Ui.IUiService Ui { get; }
        /// <summary>服务注册表：模组间互相调用（注册服务/按接口获取）。</summary>
        IServiceRegistry Services { get; }
    }

    public interface IPluginRegistry
    {
        IReadOnlyList<IPlugin> Loaded { get; }
        T Find<T>(string id) where T : class, IPlugin;
    }

    /// <summary>滤镜插件：暴露一组滤镜处理器（插件与脚本共用契约）。</summary>
    public interface IFilterPlugin : IPlugin
    {
        IReadOnlyList<Filters.IFilterProcessor> Filters { get; }
    }

    /// <summary>进度上报。</summary>
    public interface IProgress
    {
        void Report(double percent, string message);
    }

    /// <summary>鼠标按钮（纯数据，不泄漏 WinForms）。</summary>
    public enum ToolMouseButton { Left, Middle, Right }

    /// <summary>修饰键（纯数据位标志）。</summary>
    public enum ToolModifiers { None = 0, Shift = 1, Control = 2, Alt = 4 }

    /// <summary>工具鼠标事件：画布像素坐标（文档坐标系，非屏幕坐标）。</summary>
    public struct ToolMouseEvent
    {
        public int X;
        public int Y;
        public ToolMouseButton Button;
        public ToolModifiers Modifiers;
    }

    /// <summary>工具覆盖层绘制：壳提供画布绘制能力（蚂蚁线等），工具经此自绘。</summary>
    public interface IToolOverlay
    {
        /// <summary>绘制折线（闭合时首尾相连）。</summary>
        void DrawPolyline(System.Collections.Generic.IReadOnlyList<Document.Point2> points, bool closed);
    }

    /// <summary>
    /// 交互工具契约（套索/画笔/选框等）：壳只做事件路由，工具自绘覆盖层。
    /// 零 UI 依赖——事件为纯数据（画布坐标），CLI/脚本亦可激活。
    /// </summary>
    public interface IEditorTool : IPlugin
    {
        /// <summary>激活为当前工具（壳调用；DrawOverlay 随画布重绘触发）。</summary>
        void Activate();
        /// <summary>取消激活（壳调用）。</summary>
        void Deactivate();
        /// <summary>鼠标按下（画布坐标系）。</summary>
        void MouseDown(ToolMouseEvent e);
        /// <summary>鼠标移动。</summary>
        void MouseMove(ToolMouseEvent e);
        /// <summary>鼠标抬起。</summary>
        void MouseUp(ToolMouseEvent e);
        /// <summary>绘制覆盖层（壳在画布重绘时调用；工具画蚂蚁线等）。</summary>
        void DrawOverlay(IToolOverlay overlay);
    }

    /// <summary>滤镜参数（声明式自描述，Leibniz 脚本可构造）。</summary>
    public sealed class FilterParameters
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public object this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }

        /// <summary>已设置的全部参数键。</summary>
        public IEnumerable<string> Keys => _values.Keys;

        public T Get<T>(string key, T fallback = default)
        {
            return _values.TryGetValue(key, out var v) && v is T t ? t : fallback;
        }
    }
}
