using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Osiris.Core.Document;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Osiris.App.PluginHost
{
    /// <summary>宿主上下文：插件与宿主交互的桥。</summary>
    public sealed class HostContext : IHostContext
    {
        public OsirisDocument ActiveDocument { get; set; }
        public IPluginRegistry Plugins { get; }
        public IProgress Progress { get; }
        public CancellationToken Cancellation { get; }
        public IUiService Ui { get; }
        public IServiceRegistry Services { get; }

        public HostContext(IPluginRegistry registry, IUiService ui = null)
        {
            Plugins = registry;
            Ui = ui;
            Services = new ServiceRegistry();
            Progress = new NullProgress();
            Cancellation = CancellationToken.None;
        }

        private sealed class NullProgress : IProgress
        {
            public void Report(double percent, string message) { }
        }
    }
}
