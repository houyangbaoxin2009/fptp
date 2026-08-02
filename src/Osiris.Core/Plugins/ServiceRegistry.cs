using System;
using System.Collections.Generic;

namespace Osiris.Core.Plugins
{
    /// <summary>
    /// 服务注册表：模组互调机制。
    /// 模组 A 在 Initialize 注册服务实例（按接口），模组 B 经 host.Services.Get&lt;T&gt;() 获取调用。
    /// 解耦：调用方只依赖接口类型（Core 契约），不依赖具体模组程序集。
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>注册服务（同类型重复注册则覆盖）。</summary>
        void Register<T>(T service) where T : class;
        /// <summary>按接口类型获取服务；未注册返回 null。</summary>
        T Get<T>() where T : class;
    }

    /// <summary>服务注册表实现（线程安全）。</summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            lock (_services) { _services[typeof(T)] = service; }
        }

        public T Get<T>() where T : class
        {
            lock (_services)
                return _services.TryGetValue(typeof(T), out var v) ? (T)v : null;
        }
    }
}
