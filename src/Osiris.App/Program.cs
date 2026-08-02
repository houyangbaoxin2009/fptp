using System;
using System.IO;
using System.Windows.Forms;
using Osiris.App.PluginHost;
using Osiris.App.Workbench;
using Osiris.Core.IO;
using Osiris.Core.Plugins;
using Osiris.Engine.Skia;

namespace Osiris.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
#if NETFRAMEWORK
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#else
            ApplicationConfiguration.Initialize();
#endif

            // 注册内置编解码器（同一实例兼导入/导出）
            var codec = new ImageCodecSkia();
            CodecRegistry.Register((IDocumentImporter)codec);
            CodecRegistry.Register((IDocumentExporter)codec);

            // 工作台壳：先建壳，模组经 Ui 服务贡献 UI 资源
            var form = new WorkbenchForm(null, 0);
            var registry = new PluginRegistry();
            var context = new HostContext(registry, form.Ui);

            // 加载插件：优先程序集旁 plugins/，回退仓库根 plugins/bin
            var pluginCount = 0;
            foreach (var dir in GetPluginDirectories())
            {
                pluginCount += PluginLoader.LoadFromDirectory(dir, registry, context,
                    (name, ex) => Console.Error.WriteLine("插件加载失败 {0}: {1}", name, ex.Message));
            }

            // 模组全部加载后装配 UI
            form.RebuildUi();
            form.SetStatus("已加载模组: " + pluginCount);

            Application.Run(form);
        }

        /// <summary>候选插件目录：程序集旁 plugins/ → 仓库根 plugins/bin。</summary>
        private static string[] GetPluginDirectories()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\plugins\bin"));
            return new[] { local, repoRoot };
        }
    }
}
