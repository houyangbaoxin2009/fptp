namespace Osiris.Abstractions.Localization;

/// <summary>
/// 语言包服务契约：按语言 id（BCP-47 小写，如 zh-cn / en-us）加载 JSON 语言包，
/// 提供"key（中文原文）→ 目标语言文本"的翻译查询。
/// 设计：语言包 key 即中文原文，未命中返回原文——增量翻译，未翻译条目保持中文，零破坏。
/// </summary>
public interface ILocalizationService
{
    /// <summary>当前语言 id（如 "zh-cn"）。</summary>
    string CurrentLanguage { get; }

    /// <summary>可用语言列表（扫描语言包目录 *.json 得到）。</summary>
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>切换语言（加载对应语言包；包缺失时回退并保持旧包）。返回是否成功。</summary>
    bool LoadLanguage(string languageId);

    /// <summary>
    /// 注册模块语言包目录（模块目录下 langs/）：模块随包分发自己的翻译条目，
    /// 合并进全局语言表（优先级：内置语言包 &lt; 模块语言包 &lt; 用户自定义语言包）。
    /// 由宿主在模块加载成功后自动调用，插件无需任何代码接入。
    /// </summary>
    void RegisterLanguagePack(string langDirectory);

    /// <summary>翻译 key（未命中返回 key 原文；含参数时先替换 {0}/{1}）。</summary>
    string Translate(string key, params object?[] args);

    /// <summary>语言切换完成事件（壳订阅后重装配 UI）。</summary>
    event EventHandler? LanguageChanged;
}
