using Osiris.Abstractions.Plugins;

namespace Osiris.Core.Plugins;

/// <summary>
/// 服务注册表实现（插件互调机制）：
/// Dictionary&lt;Type, object&gt; + lock 保证线程安全；同类型重复注册覆盖旧实例。
/// 插件 A 注册服务，插件 B 经 host.Services.Get&lt;T&gt;() 获取，只依赖接口契约。
/// </summary>
public sealed class ServiceRegistry : IServiceRegistry
{
    // 服务实例表：按服务接口类型登记（值可为任意 class 实现）
    private readonly Dictionary<Type, object> _services = [];

    // 读写互斥：注册/获取并发安全
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        lock (_lock)
            _services[typeof(T)] = service;   // 同类型覆盖旧实例
    }

    /// <inheritdoc />
    public T? Get<T>() where T : class
    {
        lock (_lock)
            return _services.TryGetValue(typeof(T), out object? service) ? (T)service : null;
    }
}
