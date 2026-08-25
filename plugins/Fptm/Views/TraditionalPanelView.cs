using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Algorithms;

namespace Fptm.Views;

/// <summary>
/// 传统面板（fptm 老版 FPTP 功能，可停靠）：
/// - 批量处理：输入路径 + 输出目录 + 滤镜链（"id:键=值;键=值" 分号分隔）→ 逐张解码/滤镜/编码；
/// - 一键证件照：尺寸预设 + 底色 + 相纸拼版向导（智能裁切 → 换底色 → 可选排版，单步生成）；
/// - 排版生成：相纸（5寸/6寸/A4 @300dpi）网格排版当前文档首层 → 新文档（可撤销回原图）。
/// 编解码/滤镜解析经宿主注入的服务委托（与 CLI batch 同一套），打印简化为导出图片。
/// </summary>
public sealed class TraditionalPanelView : UserControl
{
    private readonly TextBox _inputBox = new() { Width = 220 };
    private readonly TextBox _outputBox = new() { Width = 220 };
    private readonly TextBox _filterBox = new() { Width = 220, Text = "grayscale" };
    private readonly TextBlock _batchStatus = new() { Text = "", Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox _paperBox = new() { Width = 120 };
    private readonly NumericUpDown _colsBox = new() { Minimum = 1, Maximum = 10, Value = 2, Width = 70 };
    private readonly NumericUpDown _rowsBox = new() { Minimum = 1, Maximum = 10, Value = 3, Width = 70 };
    private readonly TextBlock _layoutStatus = new() { Text = "", Opacity = 0.7, TextWrapping = TextWrapping.Wrap };

    // ---- 一键证件照向导控件 ----
    private readonly ComboBox _wizardPresetBox = new() { Width = 150 };
    private readonly TextBox _wizardColorBox = new() { Width = 110, Text = "FF0000FF" };
    private readonly NumericUpDown _wizardToleranceBox = new() { Minimum = 0, Maximum = 200, Value = 60, Width = 70 };
    private readonly NumericUpDown _wizardFeatherBox = new() { Minimum = 0, Maximum = 20, Value = 3, Width = 70 };
    private readonly ComboBox _wizardPaperBox = new() { Width = 120 };
    private readonly NumericUpDown _wizardWidthBox = new() { Minimum = 100, Maximum = 6000, Value = 1500, Width = 80 };
    private readonly NumericUpDown _wizardHeightBox = new() { Minimum = 100, Maximum = 6000, Value = 1050, Width = 80 };
    private readonly CheckBox _wizardGuidesBox = new() { IsChecked = true };
    private readonly TextBlock _wizardStatus = new() { Text = "", Opacity = 0.7, TextWrapping = TextWrapping.Wrap };

    public TraditionalPanelView()
    {
        // 向导默认值沿用设置（颜色 AARRGGBB 文本；未解析成功时正视 hex）
        _wizardColorBox.Text = $"{(uint)Settings.ReplaceBgColor.Value:X8}";
        _wizardToleranceBox.Value = (decimal)Settings.ReplaceBgTolerance.Value;
        _wizardFeatherBox.Value = (decimal)Settings.ReplaceBgFeather.Value;
        _wizardWidthBox.Value = (decimal)Settings.LayoutWidth.Value;
        _wizardHeightBox.Value = (decimal)Settings.LayoutHeight.Value;
        _wizardGuidesBox.IsChecked = Settings.LayoutGuides.Value;

        var root = new ScrollViewer { Content = BuildPanel(), HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Content = root;
    }

    private StackPanel BuildPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 8 };

        // ---- 批量处理区 ----
        panel.Children.Add(SectionLabel("批量处理"));
        panel.Children.Add(FieldRow("输入(文件/目录,;分隔)", _inputBox));
        panel.Children.Add(FieldRow("输出目录", _outputBox));
        panel.Children.Add(FieldRow("滤镜链", _filterBox));
        var run = new Button { Content = L10n.T("开始批量"), MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        run.Click += (_, _) => RunBatch();
        panel.Children.Add(run);
        panel.Children.Add(_batchStatus);

        // ---- 一键证件照向导区 ----
        panel.Children.Add(SectionLabel("一键证件照"));
        // 尺寸预设（下标即 SmartCrop.SizePresets 索引）
        _wizardPresetBox.Items.Clear();
        foreach ((string name, _, _) in Workflow.SmartCrop.SizePresets)
            _wizardPresetBox.Items.Add(name);
        _wizardPresetBox.SelectedIndex = Math.Max(0, Array.IndexOf(
            Workflow.SmartCrop.SizePresets.Select(p => p.Name).ToArray(), Settings.SmartCropPreset.Value));
        panel.Children.Add(FieldRow("证件照尺寸", _wizardPresetBox));
        panel.Children.Add(FieldRow("目标底色(AARRGGBB)", _wizardColorBox));
        panel.Children.Add(FieldRow("换底容差", _wizardToleranceBox));
        panel.Children.Add(FieldRow("边缘羽化", _wizardFeatherBox));
        // 相纸/模板（项存稳定 key：不排版 / 相纸预设 / 拼版模板 / 自定义）
        _wizardPaperBox.ItemTemplate = new FuncDataTemplate<string>((key, _) => new TextBlock { Text = L10n.T(key) });
        _wizardPaperBox.Items.Clear();
        _wizardPaperBox.Items.Add(Workflow.IdPhotoWizard.NoLayoutPaper);
        foreach ((string key, _) in Workflow.LayoutComposer.PaperPresets)
            _wizardPaperBox.Items.Add(key);
        foreach (Workflow.LayoutComposer.LayoutTemplate tpl in Workflow.LayoutComposer.LayoutTemplates)
            _wizardPaperBox.Items.Add(tpl.Name);
        _wizardPaperBox.Items.Add(Workflow.LayoutComposer.CustomPaper);
        _wizardPaperBox.SelectedIndex = 0;
        panel.Children.Add(FieldRow("相纸/拼版", _wizardPaperBox));
        panel.Children.Add(FieldRow("自定义宽", _wizardWidthBox));
        panel.Children.Add(FieldRow("自定义高", _wizardHeightBox));
        _wizardGuidesBox.Content = L10n.T("画裁剪引导线");
        panel.Children.Add(FieldRow("", _wizardGuidesBox));
        var wizardRun = new Button { Content = L10n.T("一键生成"), MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        wizardRun.Click += (_, _) => RunWizard();
        panel.Children.Add(wizardRun);
        panel.Children.Add(_wizardStatus);

        // ---- 排版区 ----
        panel.Children.Add(SectionLabel("证件照排版"));
        // 下拉项存**稳定 key**（"5寸"/"6寸"/"A4"），ItemTemplate 按当前语言翻译显示；
        // 匹配走 key（GenerateLayout 按 key 取尺寸）——语言切换后即使面板内容不重建，
        // 逻辑也始终正确（不会因翻译文本变化而静默回退默认 5 寸）。
        _paperBox.ItemTemplate = new FuncDataTemplate<string>((key, _) => new TextBlock { Text = L10n.T(key) });
        _paperBox.Items.Add("5寸");
        _paperBox.Items.Add("6寸");
        _paperBox.Items.Add("A4");
        _paperBox.SelectedIndex = 0;
        var layoutRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        layoutRow.Children.Add(new TextBlock { Text = L10n.T("相纸"), VerticalAlignment = VerticalAlignment.Center });
        layoutRow.Children.Add(_paperBox);
        layoutRow.Children.Add(new TextBlock { Text = L10n.T("列"), VerticalAlignment = VerticalAlignment.Center });
        layoutRow.Children.Add(_colsBox);
        layoutRow.Children.Add(new TextBlock { Text = L10n.T("行"), VerticalAlignment = VerticalAlignment.Center });
        layoutRow.Children.Add(_rowsBox);
        panel.Children.Add(layoutRow);
        var generate = new Button { Content = L10n.T("生成排版"), MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        generate.Click += (_, _) => GenerateLayout();
        panel.Children.Add(generate);
        panel.Children.Add(_layoutStatus);

        return panel;
    }

    private static TextBlock SectionLabel(string text) => new()
    {
        Text = L10n.T(text),
        FontWeight = FontWeight.Bold,
        Foreground = Brushes.Gray,
        Margin = new Thickness(0, 4, 0, 0),
    };

    private static StackPanel FieldRow(string label, Control control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = L10n.T(label), Width = 150, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(control);
        return row;
    }

    /// <summary>批量执行：逐输入文件 decode → 滤镜链逐滤镜 Apply → encode 到输出目录。</summary>
    private void RunBatch()
    {
        _batchStatus.Text = "";
        var host = FptmModule.HostContext;
        if (host is null) return;
        Func<string, PixelSurface?>? decode = host.Services.Get<Func<string, PixelSurface?>>();
        Func<string, PixelSurface, bool>? encode = host.Services.Get<Func<string, PixelSurface, bool>>();
        Func<string, IFilterProcessor?>? resolve = host.Services.Get<Func<string, IFilterProcessor?>>();
        if (decode is null || encode is null)
        {
            _batchStatus.Text = L10n.T("宿主未提供编解码服务（需打开图片模块）。");
            return;
        }

        // 收集输入文件（分号分隔；目录则枚举图片）
        var files = new List<string>();
        foreach (string item in (_inputBox.Text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Directory.Exists(item))
                files.AddRange(Directory.EnumerateFiles(item)
                    .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp"));
            else if (File.Exists(item))
                files.Add(item);
        }
        if (files.Count == 0) { _batchStatus.Text = L10n.T("未找到输入图片。"); return; }

        // 解析滤镜链（"id[:键=值[;键=值]]" 分号分隔）
        var steps = new List<(IFilterProcessor Filter, FilterParameters Parameters)>();
        foreach (string spec in (_filterBox.Text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string id = spec;
            var parameters = new FilterParameters();
            int colon = spec.IndexOf(':');
            if (colon > 0)
            {
                id = spec[..colon];
                foreach (string pair in spec[(colon + 1)..].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    int eq = pair.IndexOf('=');
                    if (eq > 0) parameters[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
                }
            }
            IFilterProcessor? filter = resolve?.Invoke(id);
            if (filter is null) { _batchStatus.Text = L10n.T("未找到滤镜 '{0}'。", id); return; }
            steps.Add((filter, parameters));
        }

        string outputDir = string.IsNullOrWhiteSpace(_outputBox.Text) ? Path.GetDirectoryName(files[0]) ?? "." : _outputBox.Text;
        Directory.CreateDirectory(outputDir);
        int ok = 0, fail = 0;
        foreach (string file in files)
        {
            try
            {
                PixelSurface? image = decode(file);
                if (image is null) { fail++; continue; }
                foreach ((IFilterProcessor filter, FilterParameters parameters) in steps)
                    image = filter.Apply(image, parameters, null, CancellationToken.None);
                string target = Path.Combine(outputDir, Path.GetFileName(file));
                if (encode(target, image)) ok++;
                else fail++;
            }
            catch { fail++; }
        }
        _batchStatus.Text = L10n.T("批量完成：成功 {0}，失败 {1}。输出目录：{2}", ok, fail, outputDir);
    }

    /// <summary>一键证件照：智能裁切 → 换底色 → 可选排版，单步生成（参数即时写回设置）。</summary>
    private void RunWizard()
    {
        _wizardStatus.Text = "";
        var host = FptmModule.HostContext;
        var docs = host?.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (host is null || docs is null || doc is null || doc.Layers.Count == 0)
        {
            _wizardStatus.Text = L10n.T("请先打开一张图片。");
            return;
        }

        Layer layer = doc.Layers[0];
        if (layer.Pixels.Width == 0 || layer.Pixels.Height == 0)
            return;

        int presetIndex = Math.Max(0, _wizardPresetBox.SelectedIndex);
        uint color = ParseArgb(_wizardColorBox.Text, Settings.ReplaceBgColor.Value);
        int tolerance = (int)Math.Clamp(_wizardToleranceBox.Value ?? 60, 0, 200);
        int feather = (int)Math.Clamp(_wizardFeatherBox.Value ?? 3, 0, 20);
        string? paper = _wizardPaperBox.SelectedItem?.ToString() ?? Workflow.IdPhotoWizard.NoLayoutPaper;
        int customW = (int)Math.Max(0, _wizardWidthBox.Value ?? 1500);
        int customH = (int)Math.Max(0, _wizardHeightBox.Value ?? 1050);
        bool guides = _wizardGuidesBox.IsChecked == true;

        // 即时保存本次参数（下次打开面板/菜单命令沿用）
        Settings.SmartCropPreset.Value = Workflow.SmartCrop.SizePresets[presetIndex].Name;
        Settings.ReplaceBgColor.Value = color;
        Settings.ReplaceBgTolerance.Value = tolerance;
        Settings.ReplaceBgFeather.Value = feather;
        Settings.LayoutGuides.Value = guides;

        var options = new Workflow.IdPhotoWizardOptions(presetIndex, color, tolerance, feather, paper, customW, customH, guides);
        host.Report.Report(0, L10n.T("正在执行：一键证件照…"));
        PixelSurface result;
        try
        {
            result = Workflow.IdPhotoWizard.Run(layer.Pixels, options, host.Report, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (paper == Workflow.IdPhotoWizard.NoLayoutPaper)
            docs.ApplyLayerChange(layer.Id, layer, layer.WithPixels(result));
        else
            docs.OpenDocument(result);   // 相纸尺寸新文档（原图保留在历史栈可撤销）

        host.Report.Report(100, L10n.T("一键证件照完成"));
        _wizardStatus.Text = paper == Workflow.IdPhotoWizard.NoLayoutPaper
            ? L10n.T("已生成证件照（{0}）。", Workflow.SmartCrop.SizePresets[presetIndex].Name)
            : L10n.T("已生成 {0} 相纸排版。", paper);
    }

    /// <summary>解析 AARRGGBB 十六进制为 PackBgra 颜色值；失败返回 fallback。</summary>
    private static uint ParseArgb(string? text, uint fallback)
    {
        if (!string.IsNullOrWhiteSpace(text)
            && uint.TryParse(text.Trim(), NumberStyles.HexNumber, null, out uint value))
            return value;
        return fallback;
    }

    /// <summary>排版生成：当前文档首层按相纸网格排版 → 新文档（历史保留原图可撤销）。</summary>
    private void GenerateLayout()
    {
        _layoutStatus.Text = "";
        var host = FptmModule.HostContext;
        var docs = host?.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (host is null || docs is null || doc is null || doc.Layers.Count == 0)
        {
            _layoutStatus.Text = L10n.T("请先打开一张图片。");
            return;
        }

        // 相纸尺寸（@300dpi）：下拉项为稳定 key（"5寸"/"6寸"/"A4"），按 key 匹配（未命中回退 5 寸）。
        // 注意：不可按 L10n.T 翻译文本匹配——语言切换后已打开面板的下拉项仍是旧语言文本，会静默回退。
        (int paperW, int paperH) = _paperBox.SelectedItem?.ToString() switch
        {
            "6寸" => (1800, 1200),
            "A4" => (2480, 3508),
            _ => (1500, 1050), // 5寸
        };
        int cols = Math.Max(1, (int)(_colsBox.Value ?? 2));
        int rows = Math.Max(1, (int)(_rowsBox.Value ?? 3));

        PixelSurface photo = doc.Layers[0].Pixels;
        int cellW = (paperW - (cols + 1) * 24) / cols;
        int cellH = (paperH - (rows + 1) * 24) / rows;
        double scale = Math.Min(cellW / (double)photo.Width, cellH / (double)photo.Height);
        int fitW = Math.Max(1, (int)(photo.Width * scale));
        int fitH = Math.Max(1, (int)(photo.Height * scale));
        PixelSurface scaled = Scaling.ScaleBilinear(photo, fitW, fitH);

        // 白底相纸 + 网格居中拷贝
        var editor = PixelSurface.Create(paperW, paperH).CreateEditor();
        for (int y = 0; y < paperH; y++)
            for (int x = 0; x < paperW; x++)
            {
                int i = x * 4;
                editor.Row(y)[i] = 255; editor.Row(y)[i + 1] = 255; editor.Row(y)[i + 2] = 255; editor.Row(y)[i + 3] = 255;
            }
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int destX = 24 + c * (cellW + 24) + (cellW - fitW) / 2;
                int destY = 24 + r * (cellH + 24) + (cellH - fitH) / 2;
                for (int py = 0; py < fitH && destY + py < paperH; py++)
                    scaled.Row(py).CopyTo(editor.Row(destY + py).Slice(destX * 4, fitW * 4));
            }
        editor.MarkAllDirty();

        // 生成新文档（原图保留在历史栈，可撤销回退）
        docs.OpenDocument(editor.Commit());
        _layoutStatus.Text = L10n.T("已生成 {0}x{1} 相纸排版（{2}x{3}）。", paperW, paperH, cols, rows);
    }
}
