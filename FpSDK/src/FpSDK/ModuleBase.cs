using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;

namespace FpSDK;

/// <summary>
/// 模块基类：实现 <see cref="IModule"/> 的便捷起点。
/// 子类只需给出 Id / Name，实现 <see cref="OnInitialize"/> 即可；
/// 宿主自动注入 <see cref="Host"/> 与 <see cref="Context"/>。
/// 注意：宿主扫描 <see cref="PluginExportAttribute"/>（Inherited=false），
/// 子类必须自行标记 <c>[PluginExport]</c> 才会被加载。
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual string Version => "1.0.0.0";

    /// <inheritdoc />
    public virtual string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public virtual ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <summary>宿主上下文（Initialize 后可用；无 UI 宿主时 Context.HasUi 为 false）。</summary>
    public IHostContext? Host { get; private set; }

    /// <summary>宿主便捷访问（Services / ActiveDocument / UI 判空）。</summary>
    public FpContext Context => FpContext.From(Host!);

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        OnInitialize(host);
    }

    /// <summary>子类初始化钩子：注册服务、贡献命令/菜单/面板/设置组。</summary>
    protected abstract void OnInitialize(IHostContext host);
}