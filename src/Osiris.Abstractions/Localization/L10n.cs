namespace Osiris.Abstractions.Localization;

/// <summary>
/// 翻译静态门面：宿主启动时注入 <see cref="ILocalizationService"/> 实例，
/// 模块/视图/命令任意处经 <c>L10n.T("中文原文")</c> 获取翻译文本。
/// 未注入服务或未命中 key 时一律返回原文（中文），保证本地化改造零破坏、可增量推进。
/// </summary>
public static class L10n
{
    private static ILocalizationService? _service;

    /// <summary>当前语言 id（未注入时默认 "zh-cn"）。</summary>
    public static string CurrentLanguage => _service?.CurrentLanguage ?? "zh-cn";

    /// <summary>可用语言列表（未注入时仅内置中文）。</summary>
    public static IReadOnlyList<LanguageInfo> AvailableLanguages =>
        _service?.AvailableLanguages ?? [new LanguageInfo("zh-cn", "简体中文")];

    /// <summary>
    /// 注入语言服务（宿主启动时调用一次；测试可用假实现或置 null 恢复默认）。
    /// </summary>
    public static void SetService(ILocalizationService? service) => _service = service;

    /// <summary>翻译 key（未命中返回原文）。</summary>
    public static string T(string key) => _service?.Translate(key) ?? key;

    /// <summary>翻译带参数 key（如 T("版本：{0}", "1.0.0")）。</summary>
    public static string T(string key, params object?[] args) => _service?.Translate(key, args) ?? key;
}
