namespace Osiris.Abstractions.Localization;

/// <summary>
/// 可用语言描述：语言 id（BCP-47 小写形式，如 zh-cn / en-us）+ 该语言的显示名（用于设置面板下拉）。
/// </summary>
public sealed record LanguageInfo(string Id, string DisplayName);
