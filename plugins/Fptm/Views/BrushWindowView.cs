using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Ui;
using Fptm.Editing;

namespace Fptm.Views;

/// <summary>
/// 画笔窗口（可停靠）：铅笔/钢笔/毛笔/刷子/颜料桶 工具切换 +
/// 每种画笔工具独立颜色（点击色块 Hex 编辑）+ 大小调节（钢笔/刷子）+
/// 颜料盘（9 槽：单击使用到当前工具、双击编辑槽位色；保存/加载经注册表）。
/// 代码构造 UI；Dock 浮动重建时由视图工厂生成新实例，共享 ToolState 单例数据。
/// </summary>
public sealed class BrushWindowView : UserControl
{
    private readonly Dictionary<string, Border> _colorSwatches = new();
    private readonly Dictionary<string, TextBlock> _sizeLabels = new();

    public BrushWindowView()
    {
        var root = new ScrollViewer
        {
            Content = BuildPanel(),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Content = root;

        // 订阅工具状态/颜料盘变化 → 刷新色块（窗口浮动重建后自动恢复当前状态）
        Editing.ToolState.Instance.Changed += RefreshColors;
        Editing.ToolState.Instance.PaletteChanged += RefreshPalette;
        Unloaded += (_, _) =>
        {
            Editing.ToolState.Instance.Changed -= RefreshColors;
            Editing.ToolState.Instance.PaletteChanged -= RefreshPalette;
        };
    }

    private StackPanel BuildPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(10), Spacing = 8 };

        // ---- 画笔工具区 ----
        panel.Children.Add(SectionLabel("画笔工具"));
        var toolRow = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        toolRow.Children.Add(ToolButton("铅笔", "pencil"));
        toolRow.Children.Add(ToolButton("钢笔", "pen"));
        toolRow.Children.Add(ToolButton("毛笔", "inkBrush"));
        toolRow.Children.Add(ToolButton("刷子", "brush"));
        toolRow.Children.Add(ToolButton("颜料桶", "bucket"));
        panel.Children.Add(toolRow);

        // ---- 颜色区：每种画笔工具独立颜色 ----
        panel.Children.Add(SectionLabel("颜色（每工具独立）"));
        panel.Children.Add(ColorRow("铅笔", "pencil"));
        panel.Children.Add(ColorRow("钢笔", "pen"));
        panel.Children.Add(ColorRow("毛笔", "inkBrush"));
        panel.Children.Add(ColorRow("刷子", "brush"));
        panel.Children.Add(ColorRow("颜料桶", "bucket"));

        // ---- 大小区：钢笔/刷子可调 ----
        panel.Children.Add(SectionLabel("大小"));
        panel.Children.Add(SizeRow("钢笔", "pen", 1, 10));
        panel.Children.Add(SizeRow("刷子", "brush", 1, 50));

        // ---- 颜料盘：9 槽 ----
        panel.Children.Add(SectionLabel("颜料盘（单击使用 / 双击编辑，Ctrl+A+1~9 快捷键）"));
        var palette = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        _paletteSwatches = new Border[9];
        for (int i = 0; i < 9; i++)
        {
            int index = i;
            var swatch = new Border
            {
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 6),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Background = ToBrush(Editing.ToolState.Instance.GetSlot(index)),
                Tag = index,
            };
            // 单击：应用到当前画笔工具；双击：编辑槽位色
            swatch.PointerPressed += (_, e) =>
            {
                if (e.ClickCount >= 2) EditSlotColor(index);
                else ApplySlotToCurrent(index);
            };
            palette.Children.Add(swatch);
            _paletteSwatches[index] = swatch;
        }
        panel.Children.Add(palette);

        // 保存 / 加载颜料盘配置（注册表即时落盘）
        var saveRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var saveBtn = new Button { Content = "保存颜料盘", MinWidth = 90 };
        saveBtn.Click += (_, _) => SavePalette();
        var loadBtn = new Button { Content = "加载颜料盘", MinWidth = 90 };
        loadBtn.Click += (_, _) => LoadPalette();
        saveRow.Children.Add(saveBtn);
        saveRow.Children.Add(loadBtn);
        panel.Children.Add(saveRow);

        RefreshColors();
        RefreshPalette();
        return panel;
    }

    private Border[] _paletteSwatches = [];

    private static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.Bold,
        Foreground = Brushes.Gray,
        Margin = new Thickness(0, 4, 0, 0),
    };

    /// <summary>工具切换按钮：设置当前工具并激活。</summary>
    private static Button ToolButton(string label, string toolId)
    {
        var btn = new Button { Content = label, Margin = new Thickness(0, 0, 6, 6), MinWidth = 64 };
        btn.Click += (_, _) =>
        {
            Editing.ToolState.Instance.CurrentToolId = toolId;
            FptmModule.HostContext?.Services.Get<IToolHostService>()?.ActivateTool(toolId);
        };
        return btn;
    }

    /// <summary>工具颜色行：标签 + 色块（点击弹 Hex 编辑）。</summary>
    private StackPanel ColorRow(string label, string toolId)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = label, Width = 56, VerticalAlignment = VerticalAlignment.Center });
        var swatch = new Border
        {
            Width = 24,
            Height = 24,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Background = ToBrush(Editing.ToolState.Instance.GetColor(toolId)),
        };
        swatch.PointerPressed += (_, _) => EditToolColor(toolId);
        row.Children.Add(swatch);
        _colorSwatches[toolId] = swatch;
        return row;
    }

    /// <summary>工具大小行：标签 + Slider + 当前值。</summary>
    private StackPanel SizeRow(string label, string toolId, int min, int max)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = label, Width = 56, VerticalAlignment = VerticalAlignment.Center });
        var slider = new Slider { Minimum = min, Maximum = max, Width = 110, Value = Editing.ToolState.Instance.GetSize(toolId) };
        var valueLabel = new TextBlock { Width = 30, VerticalAlignment = VerticalAlignment.Center, Text = $"{(int)slider.Value}" };
        slider.ValueChanged += (_, e) =>
        {
            Editing.ToolState.Instance.SetSize(toolId, e.NewValue);
            valueLabel.Text = $"{(int)e.NewValue}";
        };
        row.Children.Add(slider);
        row.Children.Add(valueLabel);
        _sizeLabels[toolId] = valueLabel;
        return row;
    }

    /// <summary>编辑工具颜色（Hex 输入 AARRGGBB）。</summary>
    private static void EditToolColor(string toolId)
    {
        uint current = Editing.ToolState.Instance.GetColor(toolId);
        if (PromptHex($"设置「{ToolName(toolId)}」颜色（AARRGGBB）", current) is { } color)
        {
            Editing.ToolState.Instance.SetColor(toolId, color);
            IModuleRegistry? registry = FptmModule.HostContext?.Services.Get<IModuleRegistry>();
            if (registry is not null)
                Editing.ToolState.Instance.Save(registry);
        }
    }

    /// <summary>编辑颜料盘槽位颜色。</summary>
    private void EditSlotColor(int index)
    {
        uint current = Editing.ToolState.Instance.GetSlot(index);
        if (PromptHex($"设置颜料槽 {index + 1}（AARRGGBB）", current) is { } color)
        {
            Editing.ToolState.Instance.SetSlot(index, color);
            SavePalette();
        }
    }

    /// <summary>把颜料盘槽位应用到当前画笔工具（单击行为）。</summary>
    private void ApplySlotToCurrent(int index)
    {
        string toolId = Editing.ToolState.Instance.IsStrokeTool(Editing.ToolState.Instance.CurrentToolId)
            ? Editing.ToolState.Instance.CurrentToolId
            : "brush";
        Editing.ToolState.Instance.SetColor(toolId, Editing.ToolState.Instance.GetSlot(index));
    }

    /// <summary>保存颜料盘与工具状态到注册表（即时落盘）。</summary>
    private void SavePalette()
    {
        IModuleRegistry? registry = FptmModule.HostContext?.Services.Get<IModuleRegistry>();
        if (registry is not null)
            Editing.ToolState.Instance.Save(registry);
    }

    /// <summary>从注册表加载颜料盘与工具状态。</summary>
    private void LoadPalette()
    {
        if (FptmModule.HostContext?.Services.Get<IModuleRegistry>() is { } reg)
        {
            Editing.ToolState.Instance.Load(reg);
            RefreshColors();
            RefreshPalette();
        }
    }

    /// <summary>刷新工具颜色色块。</summary>
    private void RefreshColors()
    {
        foreach ((string toolId, Border swatch) in _colorSwatches)
            swatch.Background = ToBrush(Editing.ToolState.Instance.GetColor(toolId));
    }

    /// <summary>刷新颜料盘色块。</summary>
    private void RefreshPalette()
    {
        for (int i = 0; i < _paletteSwatches.Length; i++)
            _paletteSwatches[i].Background = ToBrush(Editing.ToolState.Instance.GetSlot(i));
    }

    /// <summary>Hex 输入对话框（AARRGGBB），返回解析的颜色；取消返回 null。</summary>
    private static uint? PromptHex(string title, uint current)
    {
        uint? result = null;
        var input = new TextBox { Text = current.ToString("X8"), Width = 150, Margin = new Thickness(4) };
        var dlg = new Window
        {
            Title = title,
            Width = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var panel = new StackPanel { Margin = new Thickness(14), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "颜色值（AARRGGBB，如 FF0000FF=蓝）", FontSize = 11, Opacity = 0.7 });
        panel.Children.Add(input);
        var ok = new Button { Content = "确定", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) =>
        {
            if (uint.TryParse(input.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                result = v;
            dlg.Close();
        };
        var cancel = new Button { Content = "取消", Width = 80, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(88, -34, 0, 0) };
        cancel.Click += (_, _) => dlg.Close();
        panel.Children.Add(ok);
        panel.Children.Add(cancel);
        dlg.Content = panel;
        dlg.Show(); // 独立非模态对话框（模块内自建，无宿主窗口依赖）
        return result;
    }

    /// <summary>工具 Id → 中文名（提示用）。</summary>
    private static string ToolName(string toolId) => toolId switch
    {
        "pencil" => "铅笔",
        "pen" => "钢笔",
        "inkBrush" => "毛笔",
        "brush" => "刷子",
        "bucket" => "颜料桶",
        _ => toolId,
    };

    /// <summary>PackBgra uint → Avalonia 画刷。</summary>
    private static IBrush ToBrush(uint bgra)
        => new SolidColorBrush(Color.FromArgb((byte)(bgra >> 24), (byte)(bgra >> 16), (byte)(bgra >> 8), (byte)bgra));
}
