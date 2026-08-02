using System;
using System.Collections.Generic;
using System.IO;
using Osiris.App.PluginHost;
using Osiris.Core.Document;
using Osiris.Core.Filters;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "plugins")
                return PluginCommands(args);

            Console.WriteLine("Osiris.Cli (2.0)");
            Console.WriteLine("用法: Osiris.Cli plugins list | plugins gray <输入> <输出>");
            return 0;
        }

        /// <summary>插件相关命令：list 枚举 / gray 执行灰度滤镜。</summary>
        private static int PluginCommands(string[] args)
        {
            var registry = new PluginRegistry();
            var context = new HostContext(registry);

            var dir = Path.Combine(AppContext.BaseDirectory, "plugins");
            if (!Directory.Exists(dir))
                dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\plugins\bin"));
            if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine("找不到插件目录: " + dir);
                return 1;
            }

            var count = PluginLoader.LoadFromDirectory(dir, registry, context,
                (name, ex) => Console.Error.WriteLine("插件加载失败 {0}: {1}", name, ex.Message));
            Console.WriteLine("已加载插件: " + count);

            if (args.Length >= 2 && args[1] == "list")
            {
                foreach (var plugin in registry.Loaded)
                    Console.WriteLine(" - {0} v{1} ({2})", plugin.Name, plugin.Version, plugin.Id);
                return 0;
            }

            if (args.Length >= 4 && args[1] == "gray")
                return RunGray(registry, args[2], args[3]);

            Console.WriteLine("用法: plugins list | plugins gray <输入> <输出>");
            return 0;
        }

        /// <summary>执行灰度滤镜：读图 → 找滤镜插件 → 应用 → 写图。</summary>
        private static int RunGray(PluginRegistry registry, string input, string output)
        {
            if (!File.Exists(input))
            {
                Console.Error.WriteLine("输入文件不存在: " + input);
                return 1;
            }

            var filters = new List<IFilterProcessor>();
            foreach (var plugin in registry.Loaded)
                if (plugin is IFilterPlugin fp)
                    filters.AddRange(fp.Filters);

            var gray = filters.Find(f => f.Id.EndsWith("grayscale", StringComparison.OrdinalIgnoreCase));
            if (gray == null)
            {
                Console.Error.WriteLine("未找到灰度滤镜");
                return 1;
            }

            using (var stream = File.OpenRead(input))
            {
                var surface = new Osiris.Engine.Skia.ImageCodecSkia().Read(stream, Path.GetExtension(input));
                var result = gray.Apply(surface, gray.Defaults, null, System.Threading.CancellationToken.None);
                using (var outStream = File.Create(output))
                    new Osiris.Engine.Skia.ImageCodecSkia().Write(result, outStream, Path.GetExtension(output));
            }
            Console.WriteLine("灰度处理完成: " + output);
            return 0;
        }
    }

    /// <summary>CLI 宿主上下文（插件 Initialize 需要）。</summary>
    internal sealed class HostContext : IHostContext
    {
        public HostContext(IPluginRegistry registry)
        {
            Plugins = registry;
            Services = new ServiceRegistry();
        }

        public OsirisDocument ActiveDocument { get; set; }
        public IPluginRegistry Plugins { get; }
        public IProgress Progress => null;
        public System.Threading.CancellationToken Cancellation => System.Threading.CancellationToken.None;
        public Osiris.Core.Ui.IUiService Ui => null;
        public IServiceRegistry Services { get; }
    }
}
