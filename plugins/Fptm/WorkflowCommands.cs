using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Fptm;

/// <summary>
/// 证件照工作流命令（fptm）：换底色 / 智能裁切 / 排版输出。
/// 换底色、智能裁切经 IDocumentService.ApplyLayerChange（可撤销）；
/// 排版生成新文档（相纸尺寸即画布，原图保留可撤销回退）。
/// 参数来自 WorkflowSettings 设置组（ISettingProvider 即时持久化的值）。
/// </summary>
internal static class WorkflowCommands
{
    /// <summary>注册全部工作流命令到菜单。</summary>
    public static void Register(IHostContext host)
    {
        var ui = host.Ui;
        if (ui is null)
            return;

        ui.RegisterCommand(new ReplaceBackgroundCommand(host));
        ui.AddMenu("图像/换底色", FptmModule.ModuleId + ".replaceBackground", 11);

        ui.RegisterCommand(new RedEyeCommand(host));
        ui.AddMenu("图像/红眼去除", FptmModule.ModuleId + ".redEye", 15);

        ui.RegisterCommand(new SmartCropCommand(host));
        ui.AddMenu("图像/智能裁切", FptmModule.ModuleId + ".smartCrop", 13);

        ui.RegisterCommand(new LayoutCommand(host));
        ui.AddMenu("文件/排版输出", FptmModule.ModuleId + ".layout", 20);
    }

    private static bool TryDoc(IHostContext host, out IDocumentService docs, out Layer layer)
    {
        docs = host.Services.Get<IDocumentService>() ?? throw new InvalidOperationException("无文档服务。");
        var doc = docs.Document;
        if (doc is null || doc.Layers.Count == 0)
        {
            docs = null!;
            layer = null!;
            return false;
        }
        layer = doc.Layers[0];
        return true;
    }

    /// <summary>换底色命令：读设置组装参数（含背景图片解码），作用当前文档首层。</summary>
    private sealed class ReplaceBackgroundCommand(Osiris.Abstractions.IHostContext host) : ICommand
    {
        public string Id => FptmModule.ModuleId + ".replaceBackground";
        public string DisplayName => L10n.T("换底色");

        public void Execute(object? parameter)
        {
            if (!TryDoc(host, out var docs, out var layer))
                return;

            var parameters = new FilterParameters
            {
                [Workflow.BackgroundReplace.ParamColor] = Settings.ReplaceBgColor.Value,
                [Workflow.BackgroundReplace.ParamTolerance] = (int)Math.Round(Settings.ReplaceBgTolerance.Value),
                [Workflow.BackgroundReplace.ParamFeather] = (int)Math.Round(Settings.ReplaceBgFeather.Value),
            };

            // 背景图片：设置路径非空且能解码 → 注入 PixelSurface
            if (!string.IsNullOrWhiteSpace(Settings.ReplaceBgImage.Value))
            {
                Func<string, PixelSurface?>? decode = host.Services.Get<Func<string, PixelSurface?>>();
                if (decode is not null)
                {
                    try { parameters[Workflow.BackgroundReplace.ParamBackground] = decode(Settings.ReplaceBgImage.Value); }
                    catch { /* 解码失败回退纯色 */ }
                }
            }

            var filter = new Workflow.BackgroundReplace();
            host.Report.Report(0, L10n.T("正在执行：换底色…"));
            PixelSurface result = filter.Apply(layer.Pixels, parameters, host.Report, CancellationToken.None);
            docs.ApplyLayerChange(layer.Id, layer, layer.WithPixels(result));
            host.Report.Report(100, L10n.T("换底色完成"));
        }
    }

    /// <summary>红眼去除命令：读设置参数，作用当前文档首层（可撤销）。</summary>
    private sealed class RedEyeCommand(Osiris.Abstractions.IHostContext host) : ICommand
    {
        public string Id => FptmModule.ModuleId + ".redEye";
        public string DisplayName => L10n.T("红眼去除");

        public void Execute(object? parameter)
        {
            if (!TryDoc(host, out var docs, out var layer))
                return;

            var parameters = new FilterParameters
            {
                [Workflow.RedEyeRemove.ParamTolerance] = (int)Math.Round(Settings.RedEyeTolerance.Value),
                [Workflow.RedEyeRemove.ParamStrength] = (int)Math.Round(Settings.RedEyeStrength.Value),
            };

            var filter = new Workflow.RedEyeRemove();
            host.Report.Report(0, L10n.T("正在执行：红眼去除…"));
            PixelSurface result = filter.Apply(layer.Pixels, parameters, host.Report, CancellationToken.None);
            docs.ApplyLayerChange(layer.Id, layer, layer.WithPixels(result));
            host.Report.Report(100, L10n.T("红眼去除完成"));
        }
    }

    /// <summary>智能裁切命令：读尺寸预设，作用当前文档首层（输出尺寸变化 → 新图层由宿主按像素面处理）。</summary>
    private sealed class SmartCropCommand(Osiris.Abstractions.IHostContext host) : ICommand
    {
        public string Id => FptmModule.ModuleId + ".smartCrop";
        public string DisplayName => L10n.T("智能裁切");

        public void Execute(object? parameter)
        {
            if (!TryDoc(host, out var docs, out var layer))
                return;

            var parameters = new FilterParameters
            {
                [Workflow.SmartCrop.ParamPreset] = SmartCropPresetIndex(Settings.SmartCropPreset.Value),
            };

            var filter = new Workflow.SmartCrop();
            host.Report.Report(0, L10n.T("正在执行：智能裁切…"));
            PixelSurface result = filter.Apply(layer.Pixels, parameters, host.Report, CancellationToken.None);
            docs.ApplyLayerChange(layer.Id, layer, layer.WithPixels(result));
            host.Report.Report(100, L10n.T("智能裁切完成"));
        }

        private static int SmartCropPresetIndex(string name)
        {
            for (int i = 0; i < Workflow.SmartCrop.SizePresets.Length; i++)
                if (Workflow.SmartCrop.SizePresets[i].Name == name)
                    return i;
            return 0;
        }
    }

    /// <summary>排版输出命令：当前文档首张照片按相纸网格排版 → 生成相纸尺寸新文档（可撤销回原图）。</summary>
    private sealed class LayoutCommand(Osiris.Abstractions.IHostContext host) : ICommand
    {
        public string Id => FptmModule.ModuleId + ".layout";
        public string DisplayName => L10n.T("排版输出");

        public void Execute(object? parameter)
        {
            var docs = host.Services.Get<IDocumentService>();
            var doc = docs?.Document;
            if (docs is null || doc is null || doc.Layers.Count == 0)
                return;

            PixelSurface? paper = Workflow.LayoutComposer.Compose(
                doc.Layers[0].Pixels,
                Settings.LayoutPaper.Value,
                out int columns,
                out int rows,
                (int)Math.Round(Settings.LayoutWidth.Value),
                (int)Math.Round(Settings.LayoutHeight.Value),
                Settings.LayoutGuides.Value);

            if (paper is null)
            {
                host.Report.Report(0, L10n.T("未知相纸预设或尺寸非法：{0}", Settings.LayoutPaper.Value));
                return;
            }

            docs.OpenDocument(paper);
            host.Report.Report(100, L10n.T("排版完成：{0}，{1}×{2} 张", Settings.LayoutPaper.Value, columns, rows));
        }
    }
}