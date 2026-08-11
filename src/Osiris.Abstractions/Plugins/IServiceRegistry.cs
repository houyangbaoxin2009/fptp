namespace Osiris.Abstractions.Plugins;

/// <summary>
/// 服务注册表：插件互调机制。插件 A 注册服务实例（按接口），
/// 插件 B 经 host.Services.Get&lt;T&gt;() 获取调用，只依赖接口契约、不依赖具体实现程序集。
/// </summary>
public interface IServiceRegistry
{
    /// <summary>注册服务（同类型重复注册则覆盖）。</summary>
    void Register<T>(T service) where T : class;

    /// <summary>按接口类型获取服务；未注册返回 null。</summary>
    T? Get<T>() where T : class;
}
