namespace Osiris.Abstractions.Settings;

/// <summary>
/// 设置项抽象基类：所有设置类型的公共描述面（GroupId/Key/Label/Description）。
/// 派生子类承载各自类型化的可编辑 Value（set）；
/// GetValue/SetValue 供序列化层统一读写（JSON 即时持久化，零转换器）。
/// </summary>
public abstract class SettingItem
{
    /// <summary>所属设置组 Id（对应 SettingGroup.Id）。</summary>
    public string GroupId { get; init; } = "";

    /// <summary>设置键（组内唯一）。</summary>
    public string Key { get; init; } = "";

    /// <summary>显示名（UI 标签）。</summary>
    public string Label { get; init; } = "";

    /// <summary>说明文字（可空）。</summary>
    public string? Description { get; init; }

    /// <summary>
    /// 设置级别（User/Core/Security），决定展示面与写权限：
    /// 面板只渲染 User/Core 项；Security 项隐藏且仅更新模块可写。
    /// 子类无需重复声明，经对象初始化器按项指定（默认 User）。
    /// </summary>
    public SettingScope Scope { get; init; } = SettingScope.User;

    /// <summary>取当前值（object 视图，序列化层用）。</summary>
    public abstract object GetValue();

    /// <summary>设值（object 视图，序列化层用；类型不符抛 InvalidCastException）。</summary>
    public abstract void SetValue(object value);
}

/// <summary>布尔设置项。</summary>
public sealed class BoolSettingItem(bool value) : SettingItem
{
    /// <summary>当前值（可编辑）。</summary>
    public bool Value { get; set; } = value;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (bool)value;
}

/// <summary>数值设置项（double，带 Min/Max/Step 约束）。</summary>
public sealed class NumberSettingItem(double value, double min, double max, double step) : SettingItem
{
    /// <summary>当前值（可编辑）。</summary>
    public double Value { get; set; } = value;

    /// <summary>最小值（含）。</summary>
    public double Min { get; set; } = min;

    /// <summary>最大值（含）。</summary>
    public double Max { get; set; } = max;

    /// <summary>调整步长（UI 微调/滑动条用）。</summary>
    public double Step { get; set; } = step;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (double)value;
}

/// <summary>文本设置项。</summary>
public sealed class TextSettingItem(string value) : SettingItem
{
    /// <summary>当前值（可编辑）。</summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (string)value;
}

/// <summary>选项设置项（下拉列表，选中项以文本存储）。</summary>
public sealed class ChoiceSettingItem(IReadOnlyList<string> options, string value) : SettingItem
{
    /// <summary>候选选项文本（UI 下拉列表，创建后固定）。</summary>
    public IReadOnlyList<string> Options { get; init; } = options;

    /// <summary>当前选中项（可编辑）。</summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (string)value;
}

/// <summary>颜色设置项（uint PackBgra 格式）。</summary>
public sealed class ColorSettingItem(uint value) : SettingItem
{
    /// <summary>当前颜色（可编辑，uint PackBgra）。</summary>
    public uint Value { get; set; } = value;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (uint)value;
}

/// <summary>文件/文件夹路径设置项。</summary>
public sealed class FilePathSettingItem(string value, bool isFolder) : SettingItem
{
    /// <summary>当前路径（可编辑）。</summary>
    public string Value { get; set; } = value;

    /// <summary>是否为文件夹路径（否则为文件路径，影响选择器类型）。</summary>
    public bool IsFolder { get; init; } = isFolder;

    /// <inheritdoc />
    public override object GetValue() => Value;

    /// <inheritdoc />
    public override void SetValue(object value) => Value = (string)value;
}
