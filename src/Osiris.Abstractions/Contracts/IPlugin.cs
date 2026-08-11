namespace Osiris.Abstractions;

/// <summary>
/// 插件统一入口契约：滤镜/工具/格式/设置提供者共同基接口。
/// 宿主在 ALC 中加载插件程序集后，对实现 [PluginExport] 的类型实例化并调用 Initialize。
/// ABI 红线：本接口及其派生接口不得引入 SkiaSharp/Avalonia 等宿主渲染类型。
/// </summary>
public interface IPlugin
{
    /// <summary>插件唯一 Id（如 "fptp.builtin"）。</summary>
    string Id { get; }

    /// <summary>插件显示名。</summary>
    string Name { get; }

    /// <summary>插件版本（SemVer）。</summary>
    string Version { get; }

    /// <summary>要求的最低宿主版本（宿主校验，不满足则拒绝加载）。</summary>
    string MinHostVersion { get; }

    /// <summary>初始化：接收宿主上下文，注册命令/滤镜/设置组等服务。</summary>
    void Initialize(IHostContext host);
}
