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

            if (args.Length >= 4 && args[1] == "filter")
                return RunFilter(registry, args[2], args[3], args[4]);

            if (args.Length >= 4 && args[1] == "gray")
                return RunFilter(registry, "grayscale", args[2], args[3]);

            if (args.Length >= 4 && args[1] == "layout")
                return RunLayout(registry, args);

            Console.WriteLine("用法: plugins list | plugins gray <输入> <输出> | plugins filter <滤镜Id> <输入> <输出> | plugins layout <输入> <输出> [相纸] [辅助线]");
            return 0;
        }

        /// <summary>执行滤镜：读图 → 按 Id 后缀找滤镜 → 应用 → 写图（CLI 也是无 UI 宿主验证通道）。</summary>
        private static int RunFilter(PluginRegistry registry, string filterId, string input, string output)
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

            var filter = filters.Find(f => f.Id.EndsWith(filterId, StringComparison.OrdinalIgnoreCase));
            if (filter == null)
            {
                Console.Error.WriteLine("未找到滤镜: " + filterId);
                return 1;
            }

            using (var stream = File.OpenRead(input))
            {
                var surface = new Osiris.Engine.Skia.ImageCodecSkia().Read(stream, Path.GetExtension(input));
                var result = filter.Apply(surface, filter.Defaults, null, System.Threading.CancellationToken.None);
                using (var outStream = File.Create(output))
                    new Osiris.Engine.Skia.ImageCodecSkia().Write(result, outStream, Path.GetExtension(output));
            }
            Console.WriteLine("{0}处理完成: {1}", filter.DisplayName, output);
            return 0;
        }

        /// <summary>执行排版：读图 → 网格居中排到相纸 → 写图。</summary>
        private static int RunLayout(PluginRegistry registry, string[] args)
        {
            var input = args[2];
            var output = args[3];
            var paperName = args.Length >= 5 ? args[4] : "5寸";
            var guideLine = args.Length >= 6 ? ParseGuideLine(args[5]) : Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle.Dash;

            if (!File.Exists(input))
            {
                Console.Error.WriteLine("输入文件不存在: " + input);
                return 1;
            }

            using (var stream = File.OpenRead(input))
            {
                var surface = new Osiris.Engine.Skia.ImageCodecSkia().Read(stream, Path.GetExtension(input));
                Osiris.Core.Imaging.LayoutProcessor.LayoutResult result;
                try
                {
                    result = Osiris.Core.Imaging.LayoutProcessor.LayoutPreset(surface, paperName, guideLine);
                }
                catch (ArgumentException ex)
                {
                    Console.Error.WriteLine("排版失败: " + ex.Message);
                    return 1;
                }
                using (var outStream = File.Create(output))
                    new Osiris.Engine.Skia.ImageCodecSkia().Write(result.Paper, outStream, Path.GetExtension(output));
                Console.WriteLine("排版完成: {0}（{1}列 x {2}行 = {3}张）", output, result.Columns, result.Rows, result.Count);
            }
            return 0;
        }

        private static Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle ParseGuideLine(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "none":
                case "无":
                    return Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle.None;
                case "solid":
                case "实线":
                    return Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle.Solid;
                default:
                    return Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle.Dash;
            }
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
