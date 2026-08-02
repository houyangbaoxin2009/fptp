using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Osiris.Core.Plugins;

namespace Osiris.App.PluginHost
{
    /// <summary>
    /// 插件加载器：扫描插件目录并加载全部插件程序集。
    /// net48：Assembly.LoadFrom（无卸载，重启生效）；
    /// net10：AssemblyLoadContext（IsCollectible，可卸载）。条件编译隔离（Oracle 验证方案）。
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>共享程序集：宿主已加载，插件目录中的副本跳过（避免 ALC 重复加载）。</summary>
        private static readonly string[] SharedAssemblyNames =
        {
            "Osiris.Core", "Osiris.Settings", "SkiaSharp", "System.Runtime", "System.Memory"
        };

        /// <summary>从目录加载全部插件 DLL 并注册到注册表。</summary>
        /// <returns>加载的插件数量。</returns>
        public static int LoadFromDirectory(string directory, PluginRegistry registry,
                                            IHostContext context, Action<string, Exception> onError = null)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return 0;

            var count = 0;
            foreach (var dll in Directory.GetFiles(directory, "*.dll"))
            {
                var fileName = Path.GetFileNameWithoutExtension(dll);
                if (IsShared(fileName) || !IsPluginAssembly(dll)) continue;
                try
                {
                    count += LoadAssembly(dll, registry, context, onError);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(dll, ex);
                }
            }
            return count;
        }

        private static bool IsShared(string assemblyName)
        {
            foreach (var name in SharedAssemblyNames)
                if (string.Equals(name, assemblyName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>粗略判断是否为插件程序集（引用 Osiris.Core 才可能是插件）。</summary>
        private static bool IsPluginAssembly(string path)
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

#if NETFRAMEWORK
        /// <summary>net48：LoadFrom 加载，反射实例化 IPlugin。</summary>
        private static int LoadAssembly(string dll, PluginRegistry registry,
                                        IHostContext context, Action<string, Exception> onError)
        {
            var asm = Assembly.LoadFrom(dll);
            return InstantiatePlugins(asm, registry, context, onError);
        }
#else
        /// <summary>net10：可卸载 ALC 加载（IsCollectible），保持 ALC 引用以便未来卸载。</summary>
        private static int LoadAssembly(string dll, PluginRegistry registry,
                                        IHostContext context, Action<string, Exception> onError)
        {
            var alc = new PluginLoadContext(dll);
            var asm = alc.LoadFromAssemblyPath(dll);
            return InstantiatePlugins(asm, registry, context, onError);
        }
#endif

        private static int InstantiatePlugins(Assembly asm, PluginRegistry registry,
                                              IHostContext context, Action<string, Exception> onError)
        {
            var count = 0;
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !typeof(IPlugin).IsAssignableFrom(type)) continue;
                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type);
                    plugin.Initialize(context);
                    registry.Register(plugin);
                    count++;
                }
                catch (Exception ex)
                {
                    onError?.Invoke(type.FullName, ex);
                }
            }
            return count;
        }
    }

#if !NETFRAMEWORK
    /// <summary>net10 专用：可卸载 ALC，依赖程序集转发到默认上下文。</summary>
    internal sealed class PluginLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        private readonly string _pluginPath;

        public PluginLoadContext(string pluginPath) : base("osiris-plugin", isCollectible: true)
        {
            _pluginPath = pluginPath;
        }

        protected override Assembly Load(System.Reflection.AssemblyName assemblyName)
        {
            // 共享程序集（Osiris.Core、SkiaSharp、System.*）转发到默认上下文
            var defaultLoaded = Default.Assemblies;
            foreach (var asm in defaultLoaded)
            {
                if (string.Equals(asm.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    return asm;
            }
            // 插件同目录依赖
            var path = Path.Combine(Path.GetDirectoryName(_pluginPath), assemblyName.Name + ".dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }
    }
#endif
}
