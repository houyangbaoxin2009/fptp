using System.Collections.Generic;
using Osiris.Core.Document;
using Osiris.Core.Filters;
using Osiris.Core.History;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Fptp.Plugins.Builtin
{
    /// <summary>
    /// 内置模组包：官方功能（滤镜 + UI 贡献），与第三方模组完全同级。
    /// 演示工作台模式：模组在 Initialize 中贡献菜单/工具栏/命令/面板。
    /// </summary>
    public sealed class BuiltinPlugin : IFilterPlugin
    {
        private readonly GrayscaleFilter _grayscale = new GrayscaleFilter();
        private readonly ReplaceBackgroundFilter _replaceBackground = new ReplaceBackgroundFilter();
        private readonly SmartCropFilter _smartCrop = new SmartCropFilter();
        private readonly LassoTool _lasso = new LassoTool();

        public string Id => "fptp.builtin";
        public string Name => "内置模组包";
        public string Version => "2.0.4.0";
        public string MinHostVersion => "2.0.4.0";

        public IReadOnlyList<IFilterProcessor> Filters => new IFilterProcessor[]
        {
            _grayscale, _replaceBackground, _smartCrop
        };

        public void Initialize(IHostContext host)
        {
            // 工具模组自身初始化（接收宿主；无 UI 宿主下仍可被脚本/CLI 激活）
            _lasso.Initialize(host);

            // UI 服务由壳提供；CLI 等无 UI 宿主下为 null，模组跳过 UI 注册
            if (host.Ui == null) return;

            // 贡献"文件"菜单 → 打开命令（命令由壳实现，Id 共享）
            host.Ui.AddMenu(new MenuContribution("文件/打开", KnownCommands.OpenDocument, "Ctrl+O", 0));
            host.Ui.AddMenu(new MenuContribution("文件/保存", KnownCommands.Save, "Ctrl+S", 1));
            host.Ui.AddMenu(new MenuContribution("文件/另存为", KnownCommands.SaveAs, null, 2));
            host.Ui.AddMenu(new MenuContribution("文件/打印", KnownCommands.Print, "Ctrl+P", 3));

            // 编辑菜单 → 撤销/重做（壳命令）
            host.Ui.AddMenu(new MenuContribution("编辑/撤销", KnownCommands.Undo, "Ctrl+Z", 1));
            host.Ui.AddMenu(new MenuContribution("编辑/重做", KnownCommands.Redo, "Ctrl+Y", 2));

            // 贡献"图像"菜单 → 滤镜命令（壳自动创建中间节点）
            host.Ui.RegisterCommand(new FptpFilterCommand(host, "builtin.grayscale", "灰度", _grayscale, null));
            host.Ui.AddMenu(new MenuContribution("图像/灰度", "builtin.grayscale", null, 10));
            host.Ui.AddToolbar(new ToolbarContribution("builtin.grayscale", null, 10));

            host.Ui.RegisterCommand(new FptpFilterCommand(host, "builtin.replaceBackground", "换底色",
                _replaceBackground, new Osiris.Core.Plugins.FilterParameters()));
            host.Ui.AddMenu(new MenuContribution("图像/换底色", "builtin.replaceBackground", null, 11));

            // 智能裁切改变尺寸 → 生成新文档（画布=结果尺寸，不走 PixelEditCommand 避免越界）
            host.Ui.RegisterCommand(new GenerateDocumentCommand(host, "builtin.smartCrop", "智能裁切",
                _smartCrop, new Osiris.Core.Plugins.FilterParameters()));
            host.Ui.AddMenu(new MenuContribution("图像/智能裁切", "builtin.smartCrop", null, 12));

            // 排版输出：把当前照片网格居中排到相纸（5寸），生成新图层
            host.Ui.RegisterCommand(new LayoutCommand(host, "builtin.layout5", "5寸排版", "5寸"));
            host.Ui.AddMenu(new MenuContribution("图像/排版输出/5寸排版", "builtin.layout5", null, 13));
            host.Ui.RegisterCommand(new LayoutCommand(host, "builtin.layout6", "6寸排版", "6寸"));
            host.Ui.AddMenu(new MenuContribution("图像/排版输出/6寸排版", "builtin.layout6", null, 14));
            host.Ui.RegisterCommand(new LayoutCommand(host, "builtin.layoutA4", "A4排版", "A4"));
            host.Ui.AddMenu(new MenuContribution("图像/排版输出/A4排版", "builtin.layoutA4", null, 15));

            // "选择"菜单 → 套索选框工具（切换激活/取消）
            host.Ui.RegisterCommand(new LassoToolCommand(host, _lasso));
            host.Ui.AddMenu(new MenuContribution("选择/套索选框", "builtin.lasso", "L", 20));
            host.Ui.AddToolbar(new ToolbarContribution("builtin.lasso", null, 20));

            // 历史面板：展示当前文档撤销栈，点击跳转
            host.Ui.AddPanel(new PanelContribution("builtin.history", "历史",
                PanelSide.Left, () => CreateHistoryPanel(host), 0));
        }

        /// <summary>历史面板数据契约：内容随 History.Changed 刷新，点击跳转到对应命令。</summary>
        private static ListPanelContent CreateHistoryPanel(IHostContext host)
        {
            var panel = new ListPanelContent();
            var doc = host.ActiveDocument;
            if (doc == null) return panel;

            // 刷新：列出撤销栈命令名（0..游标），选中当前游标
            System.Action refresh = null;
            refresh = () =>
            {
                var names = new List<string>();
                for (int i = 0; i <= doc.History.Cursor; i++)
                    names.Add(doc.History.Commands[i].Name);
                panel.SelectedIndex = doc.History.Cursor;
                panel.Items = () => names;
                panel.NotifyChanged();
            };
            doc.History.Changed += (s, e) => refresh();
            refresh();

            panel.SelectedIndexChanged = idx =>
            {
                if (idx >= 0 && idx <= doc.History.Cursor)
                    doc.History.JumpTo(idx, doc);
            };
            return panel;
        }
    }

    /// <summary>套索工具切换命令：激活/取消激活当前工具（壳只路由，状态由工具自持）。</summary>
    internal sealed class LassoToolCommand : ICommand
    {
        private readonly IHostContext _host;
        private readonly LassoTool _tool;

        public LassoToolCommand(IHostContext host, LassoTool tool)
        {
            _host = host;
            _tool = tool;
        }

        public string Id => "builtin.lasso";
        public string DisplayName => "套索选框";

        public bool CanExecute(object parameter)
            => _host.ActiveDocument != null && _host.ActiveDocument.Layers.Count > 0;

        public void Execute(object parameter)
        {
            // 激活中 → 取消；否则激活。模组经 Ui 服务告知壳，壳只做路由。
            _host.Ui?.ActivateTool(_tool.Active ? null : _tool);
        }
    }

    /// <summary>
    /// 通用滤镜命令：把滤镜应用到当前文档首图层（经历史栈入栈，可撤销）。
    /// 参数经 FilterParameters 传入（默认用滤镜 Defaults，UI 设置面板可覆盖）。
    /// </summary>
    internal sealed class FptpFilterCommand : ICommand
    {
        private readonly IHostContext _host;
        private readonly string _id;
        private readonly string _displayName;
        private readonly IFilterProcessor _filter;
        private readonly Osiris.Core.Plugins.FilterParameters _overrides;

        public FptpFilterCommand(IHostContext host, string id, string displayName,
                                 IFilterProcessor filter, Osiris.Core.Plugins.FilterParameters overrides)
        {
            _host = host;
            _id = id;
            _displayName = displayName;
            _filter = filter;
            _overrides = overrides;
        }

        public string Id => _id;
        public string DisplayName => _displayName;

        public bool CanExecute(object parameter)
            => _host.ActiveDocument != null && _host.ActiveDocument.Layers.Count > 0;

        public void Execute(object parameter)
        {
            var doc = _host.ActiveDocument;
            if (doc == null || doc.Layers.Count == 0) return;
            var layer = doc.Layers[0];

            // 合并参数：命令覆盖值优先，缺省用滤镜 Defaults
            var p = MergeParameters(_filter.Defaults, _overrides);
            var result = _filter.Apply(layer.Pixels, p, _host.Progress, _host.Cancellation);
            var cmd = new PixelEditCommand(_displayName, layer,
                0, 0, layer.Pixels.Width, layer.Pixels.Height, result.Data);
            doc.History.Push(cmd, doc);
        }

        /// <summary>合并滤镜默认参数与命令覆盖值（覆盖优先）。</summary>
        internal static Osiris.Core.Plugins.FilterParameters MergeParameters(
            Osiris.Core.Plugins.FilterParameters defaults,
            Osiris.Core.Plugins.FilterParameters overrides)
        {
            var merged = new Osiris.Core.Plugins.FilterParameters();
            if (defaults != null)
                foreach (var k in defaults.Keys)
                    merged[k] = defaults[k];
            if (overrides != null)
                foreach (var k in overrides.Keys)
                    merged[k] = overrides[k];
            return merged;
        }
    }

    /// <summary>排版命令：把当前文档首图层照片网格居中排到相纸，结果作为新文档加载（相纸尺寸即画布）。</summary>
    internal sealed class LayoutCommand : ICommand
    {
        private readonly IHostContext _host;
        private readonly string _id;
        private readonly string _displayName;
        private readonly string _paperName;

        public LayoutCommand(IHostContext host, string id, string displayName, string paperName)
        {
            _host = host;
            _id = id;
            _displayName = displayName;
            _paperName = paperName;
        }

        public string Id => _id;
        public string DisplayName => _displayName;

        public bool CanExecute(object parameter)
            => _host.ActiveDocument != null && _host.ActiveDocument.Layers.Count > 0;

        public void Execute(object parameter)
        {
            var doc = _host.ActiveDocument;
            if (doc == null || doc.Layers.Count == 0) return;
            var layer = doc.Layers[0];

            var result = Osiris.Core.Imaging.LayoutProcessor.LayoutPreset(
                layer.Pixels, _paperName, Osiris.Core.Imaging.LayoutProcessor.GuideLineStyle.Dash);

            // 相纸是新文档（尺寸=相纸），消除渲染裁剪；原文档由历史保留可撤销回退
            var paperDoc = new OsirisDocument(result.Paper.Width, result.Paper.Height);
            var paperLayer = new Layer(_displayName, result.Paper.Width, result.Paper.Height);
            System.Buffer.BlockCopy(result.Paper.Data, 0, paperLayer.Pixels.Data, 0, result.Paper.Data.Length);
            paperDoc.Layers.Add(paperLayer);
            _host.Ui?.LoadDocument(paperDoc, _displayName);
        }
    }

    /// <summary>
    /// 生成新文档命令：滤镜输出尺寸不同于原图层时使用（如智能裁切/排版）。
    /// 结果作为新文档加载（画布=结果尺寸），原文档留在历史中。
    /// </summary>
    internal sealed class GenerateDocumentCommand : ICommand
    {
        private readonly IHostContext _host;
        private readonly string _id;
        private readonly string _displayName;
        private readonly IFilterProcessor _filter;
        private readonly Osiris.Core.Plugins.FilterParameters _overrides;

        public GenerateDocumentCommand(IHostContext host, string id, string displayName,
                                       IFilterProcessor filter, Osiris.Core.Plugins.FilterParameters overrides)
        {
            _host = host;
            _id = id;
            _displayName = displayName;
            _filter = filter;
            _overrides = overrides;
        }

        public string Id => _id;
        public string DisplayName => _displayName;

        public bool CanExecute(object parameter)
            => _host.ActiveDocument != null && _host.ActiveDocument.Layers.Count > 0;

        public void Execute(object parameter)
        {
            var doc = _host.ActiveDocument;
            if (doc == null || doc.Layers.Count == 0) return;
            var layer = doc.Layers[0];

            var p = FptpFilterCommand.MergeParameters(_filter.Defaults, _overrides);
            var result = _filter.Apply(layer.Pixels, p, _host.Progress, _host.Cancellation);

            var resultDoc = new OsirisDocument(result.Width, result.Height);
            var resultLayer = new Layer(_displayName, result.Width, result.Height);
            System.Buffer.BlockCopy(result.Data, 0, resultLayer.Pixels.Data, 0, result.Data.Length);
            resultDoc.Layers.Add(resultLayer);
            _host.Ui?.LoadDocument(resultDoc, _displayName);
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
