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
    }

    public interface IPluginRegistry
    {
        IReadOnlyList<IPlugin> Loaded { get; }
        T Find<T>(string id) where T : class, IPlugin;
    }

    /// <summary>进度上报。</summary>
    public interface IProgress
    {
        void Report(double percent, string message);
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

        public T Get<T>(string key, T fallback = default)
        {
            return _values.TryGetValue(key, out var v) && v is T t ? t : fallback;
        }
    }
}
