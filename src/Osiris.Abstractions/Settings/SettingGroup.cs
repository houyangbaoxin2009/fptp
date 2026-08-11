namespace Osiris.Abstractions.Settings;

/// <summary>
/// 设置组：一组逻辑相关的设置项（设置面板左侧导航中的单个分组）。
/// 对应 SettingItem.GroupId，宿主按组聚合显示。
/// </summary>
public sealed class SettingGroup
{
    /// <summary>组唯一 Id（对应 SettingItem.GroupId）。</summary>
    public string Id { get; init; } = "";

    /// <summary>组显示名（UI 导航标签）。</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>组内设置项集合。</summary>
    public IReadOnlyList<SettingItem> Items { get; init; } = [];
}
