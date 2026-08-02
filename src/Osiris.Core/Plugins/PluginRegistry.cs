using System;
using System.Collections.Generic;

namespace Osiris.Core.Plugins
{
    /// <summary>插件注册表：运行时发现的插件登记于此。</summary>
    public sealed class PluginRegistry : IPluginRegistry
    {
        private readonly List<IPlugin> _plugins = new List<IPlugin>();

        public IReadOnlyList<IPlugin> Loaded => _plugins;

        /// <summary>注册插件实例。</summary>
        public void Register(IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            lock (_plugins) { if (!Contains(plugin.Id)) _plugins.Add(plugin); }
        }

        /// <summary>按 Id 查插件。</summary>
        public T Find<T>(string id) where T : class, IPlugin
        {
            lock (_plugins)
            {
                foreach (var p in _plugins)
                    if (string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase) && p is T t)
                        return t;
            }
            return null;
        }

        /// <summary>按类型查全部匹配插件。</summary>
        public IReadOnlyList<T> FindAll<T>() where T : class, IPlugin
        {
            var result = new List<T>();
            lock (_plugins)
            {
                foreach (var p in _plugins)
                    if (p is T t) result.Add(t);
            }
            return result;
        }

        private bool Contains(string id)
        {
            foreach (var p in _plugins)
                if (string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
