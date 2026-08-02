using System.Collections.Generic;
using Osiris.Core.Filters;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Fptp.Plugins.Builtin
{
    /// <summary>
    /// 内置模组包：官方功能（滤镜 + UI 贡献），与第三方模组完全同级。
    /// 演示工作台模式：模组在 Initialize 中贡献菜单/工具栏/命令。
    /// </summary>
    public sealed class BuiltinPlugin : IFilterPlugin
    {
        private readonly GrayscaleFilter _grayscale = new GrayscaleFilter();

        public string Id => "fptp.builtin";
        public string Name => "内置模组包";
        public string Version => "0.1.0";
        public string MinHostVersion => "2.0.0";

        public IReadOnlyList<IFilterProcessor> Filters => new IFilterProcessor[] { _grayscale };

        public void Initialize(IHostContext host)
        {
            // UI 服务由壳提供；CLI 等无 UI 宿主下为 null，模组跳过 UI 注册
            if (host.Ui == null) return;

            // 贡献"文件"菜单 → 打开命令（命令由壳实现，Id 共享）
            host.Ui.AddMenu(new MenuContribution("文件/打开", KnownCommands.OpenDocument, "Ctrl+O", 0));

            // 贡献"图像"菜单 → 灰度命令（壳自动创建中间节点）
            host.Ui.RegisterCommand(new GrayscaleCommand(host));
            host.Ui.AddMenu(new MenuContribution("图像/灰度", "builtin.grayscale", null, 10));
            host.Ui.AddToolbar(new ToolbarContribution("builtin.grayscale", null, 10));
        }
    }

    /// <summary>灰度命令：把当前文档首图层灰度化。</summary>
    internal sealed class GrayscaleCommand : ICommand
    {
        private readonly IHostContext _host;
        private readonly GrayscaleFilter _filter;

        public GrayscaleCommand(IHostContext host)
        {
            _host = host;
            _filter = new GrayscaleFilter();
        }

        public string Id => "builtin.grayscale";
        public string DisplayName => "灰度";

        public bool CanExecute(object parameter)
            => _host.ActiveDocument != null && _host.ActiveDocument.Layers.Count > 0;

        public void Execute(object parameter)
        {
            var doc = _host.ActiveDocument;
            if (doc == null || doc.Layers.Count == 0) return;
            var layer = doc.Layers[0];
            var result = _filter.Apply(layer.Pixels, _filter.Defaults,
                                       _host.Progress, _host.Cancellation);
            result.Pixels.CopyTo(layer.Pixels.Pixels);
        }
    }

    /// <summary>灰度滤镜：BT.601 加权平均（纯 PixelSurface 实现，不依赖渲染后端）。</summary>
    public sealed class GrayscaleFilter : IFilterProcessor
    {
        public string Id => "fptp.builtin.grayscale";
        public string DisplayName => "灰度";
        public FilterParameters Defaults => new FilterParameters();

        public PixelSurface Apply(PixelSurface input, FilterParameters p, IProgress progress, System.Threading.CancellationToken ct)
        {
            var output = new PixelSurface(input.Width, input.Height);
            var src = input.Pixels;
            var dst = output.Pixels;
            for (int i = 0; i + 3 < src.Length; i += 4)
            {
                ct.ThrowIfCancellationRequested();
                var b = src[i];
                var g = src[i + 1];
                var r = src[i + 2];
                var a = src[i + 3];
                // BT.601 亮度，按 alpha 预乘处理
                var gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                dst[i] = gray;
                dst[i + 1] = gray;
                dst[i + 2] = gray;
                dst[i + 3] = a;
            }
            return output;
        }
    }
}
