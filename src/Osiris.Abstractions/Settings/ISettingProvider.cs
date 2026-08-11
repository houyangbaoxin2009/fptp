namespace Osiris.Abstractions.Settings;

/// <summary>
/// 设置提供者契约：插件贡献一组可持久化设置。
/// 宿主聚合各提供者的 Groups 到设置面板，并经 SettingRegistry 落盘 JSON 即时保存。
/// </summary>
public interface ISettingProvider : IPlugin
{
    /// <summary>本插件贡献的设置组集合。</summary>
    IReadOnlyList<SettingGroup> Groups { get; }
}
