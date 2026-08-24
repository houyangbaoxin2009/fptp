using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Plugins;

namespace Fpter.Views;

/// <summary>
/// 滤镜窗口（可停靠，合并自 Fptp.Filters）：左侧滤镜列表（聚合全部 IFilterPlugin 模块的滤镜），
/// 右侧按 FilterParameterDescriptor 声明自动生成参数控件，底部"应用"把滤镜应用到当前文档首层（可撤销）。
/// 滤镜经宿主注册的解析服务获取（Func&lt;string, IFilterProcessor?&gt; / Func&lt;IReadOnlyList&lt;IFilterProcessor&gt;&gt;）。
/// </summary>
public sealed class FilterWindowView : UserControl
{
    private readonly ListBox _filterList = new();
    private readonly StackPanel _paramHost = new() { Spacing = 8 };
    private readonly Dictionary<string, object> _paramValues = new();
    private IFilterProcessor? _selected;

    public FilterWindowView()
    {
        var root = new DockPanel();

        // 左：滤镜列表
        _filterList.Margin = new Thickness(8);
        _filterList.MinWidth = 160;
        _filterList.SelectionChanged += (_, _) => ShowSelectedFilter();
        root.Children.Add(_filterList);
        DockPanel.SetDock(_filterList, Dock.Left);

        // 右：参数 + 应用按钮
        var right = new DockPanel { Margin = new Thickness(8) };
        var apply = new Button { Content = L10n.T("应用"), MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        apply.Click += (_, _) => ApplyFilter();
        DockPanel.SetDock(apply, Dock.Bottom);
        right.Children.Add(apply);
        var scroll = new ScrollViewer { Content = _paramHost, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        right.Children.Add(scroll);
        root.Children.Add(right);

        Content = root;

        // 加载全部内置滤镜（宿主收集的 IFilterPlugin）
        var allFilters = FpterModule.HostContext?.Services.Get<Func<IReadOnlyList<IFilterProcessor>>>();
        if (allFilters is not null)
        {
            foreach (var filter in allFilters())
            {
                _filterList.Items.Add(new FilterListItem(filter.Id, filter.DisplayName));
                _paramValues[filter.Id] = new FilterParameters();
            }
        }
    }

    /// <summary>滤镜列表项（显示名 + Id）。</summary>
    private sealed record FilterListItem(string Id, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    /// <summary>选中滤镜：按声明生成参数控件。</summary>
    private void ShowSelectedFilter()
    {
        _paramHost.Children.Clear();
        _paramValues.Clear();

        if (_filterList.SelectedItem is not FilterListItem item) return;
        var resolve = FpterModule.HostContext?.Services.Get<Func<string, IFilterProcessor?>>();
        _selected = resolve?.Invoke(item.Id);
        if (_selected is null) return;

        var parameters = new FilterParameters();
        _paramValues[item.Id] = parameters;

        foreach (var desc in _selected.Parameters)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock { Text = desc.Label ?? desc.Key, Width = 110, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            switch (desc.Kind)
            {
                case FilterParameterKind.Bool:
                {
                    var cb = new CheckBox { IsChecked = desc.DefaultValue is bool b && b };
                    cb.IsCheckedChanged += (_, _) => parameters[desc.Key] = cb.IsChecked == true;
                    row.Children.Add(cb);
                    break;
                }
                case FilterParameterKind.Int:
                {
                    int min = (int)Math.Round(desc.Min ?? 0), max = (int)Math.Round(desc.Max ?? 100);
                    var nud = new NumericUpDown
                    {
                        Minimum = min,
                        Maximum = Math.Max(min, max),
                        Value = desc.DefaultValue is IConvertible c ? Convert.ToDecimal(c, CultureInfo.InvariantCulture) : min,
                        Width = 140,
                    };
                    nud.ValueChanged += (_, e) => parameters[desc.Key] = (int)(e.NewValue ?? min);
                    row.Children.Add(nud);
                    break;
                }
                case FilterParameterKind.Choice:
                {
                    var combo = new ComboBox { Width = 140 };
                    if (desc.Choices is not null)
                        foreach (var choice in desc.Choices)
                            combo.Items.Add(choice);
                    object? current = desc.DefaultValue;
                    int idx = desc.Choices is null ? -1 : desc.Choices.ToList().IndexOf(current?.ToString() ?? "");
                    combo.SelectedIndex = idx >= 0 ? idx : 0;
                    combo.SelectionChanged += (_, _) =>
                    {
                        if (combo.SelectedIndex >= 0 && desc.ChoiceValues is { } values && combo.SelectedIndex < values.Count)
                            parameters[desc.Key] = values[combo.SelectedIndex];
                    };
                    row.Children.Add(combo);
                    break;
                }
                case FilterParameterKind.Color:
                {
                    string hex = desc.DefaultValue is IFormattable f
                        ? f.ToString("X8", CultureInfo.InvariantCulture)
                        : "FF000000";
                    var input = new TextBox { Text = hex, Width = 120 };
                    input.LostFocus += (_, _) =>
                    {
                        if (uint.TryParse(input.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                            parameters[desc.Key] = v;
                    };
                    row.Children.Add(input);
                    break;
                }
                default: // Double
                {
                    double min = desc.Min ?? 0, max = desc.Max ?? 100;
                    var nud = new NumericUpDown
                    {
                        Minimum = (decimal)min,
                        Maximum = (decimal)Math.Max(min, max),
                        Value = desc.DefaultValue is IConvertible c ? Convert.ToDecimal(c, CultureInfo.InvariantCulture) : (decimal)min,
                        Width = 140,
                    };
                    nud.ValueChanged += (_, e) => parameters[desc.Key] = (double)(e.NewValue ?? (decimal)min);
                    row.Children.Add(nud);
                    break;
                }
            }
            _paramHost.Children.Add(row);
        }

        // 无参数时提示
        if (_selected.Parameters.Count == 0)
            _paramHost.Children.Add(new TextBlock { Text = L10n.T("（该滤镜无参数）"), Opacity = 0.6 });
    }

    /// <summary>应用选中滤镜到当前文档首层（经 IDocumentService.ApplyLayerChange，可撤销）。</summary>
    private void ApplyFilter()
    {
        if (_selected is null) return;
        var host = FpterModule.HostContext;
        var docs = host?.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (docs is null || doc is null || doc.Layers.Count == 0) return;

        var parameters = _paramValues.TryGetValue(_selected.Id, out object? p) && p is FilterParameters fp
            ? fp
            : new FilterParameters();
        try
        {
            Layer layer = doc.Layers[0];
            PixelSurface result = _selected.Apply(layer.Pixels, parameters, null, CancellationToken.None);
            Layer newLayer = layer.WithPixels(result);
            docs.ApplyLayerChange(layer.Id, layer, newLayer);
        }
        catch (OperationCanceledException) { /* 用户取消 */ }
        catch (Exception ex)
        {
            // 滤镜异常不崩溃：提示到控制台
            System.Diagnostics.Debug.WriteLine($"滤镜失败 {_selected.Id}: {ex.Message}");
        }
    }
}