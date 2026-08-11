using Osiris.Abstractions.Cli;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Osiris.Cli;

/// <summary>
/// CLI 子命令装配器：把模块声明的 CliCommandDescriptor（纯数据、tie 友好，不依赖 System.CommandLine）
/// 翻译为 System.CommandLine Command（选项 + 同步处理器），挂到根命令后即参与解析。
/// 选项统一声明为 Option&lt;string&gt;（描述符不携带类型信息），值在 Handler 内经
/// CliInvocation.Get&lt;T&gt; 类型化转换；裸开关约定：选项出现但无值（如 "--crop"）视为 "true"，
/// 便于布尔开关；未出现的选项不进入调用上下文（模块 Handler 走 fallback）。
/// </summary>
internal static class CliCommandMounter
{
    /// <summary>按描述符装配一个子命令（System.CommandLine 层）。</summary>
    public static Command Mount(CliCommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var command = new Command(descriptor.Name, descriptor.Description);
        var optionMap = new Dictionary<string, Option<string>>(StringComparer.Ordinal);

        // 逐选项生成 Option<string>：短选项走别名；必填 → 恰好一个值，可选 → 允许裸开关
        foreach (CliOptionDescriptor option in descriptor.Options)
        {
            Option<string> cliOption = CreateOption(option);
            optionMap[option.Name] = cliOption;
            command.Add(cliOption);
        }

        // 同步处理器：解析结果 → CliInvocation → 模块 Handler → 进程退出码
        command.SetAction(parseResult => Handle(descriptor, optionMap, parseResult));
        return command;
    }

    /// <summary>生成 Option&lt;string&gt;（描述符无类型信息，统一按字符串解析，取值在 CliInvocation 侧转换）。</summary>
    private static Option<string> CreateOption(CliOptionDescriptor option)
    {
        // 短选项作为别名（"-f"）；无短选项时仅长选项名（"--filter"）
        Option<string> cliOption = option.ShortName is null
            ? new Option<string>(option.Name)
            : new Option<string>(option.Name, [option.ShortName]);

        cliOption.Description = option.Description;
        cliOption.Required = option.Required;
        // 必填选项必须带值（--out dir）；可选选项允许裸开关（--crop → 视为 "true"）
        cliOption.Arity = option.Required ? ArgumentArity.ExactlyOne : ArgumentArity.ZeroOrOne;
        return cliOption;
    }

    /// <summary>
    /// 处理器：把解析结果封装为 CliInvocation 后交给模块 Handler，返回其退出码。
    /// 键 = 长选项名（CliInvocation 构造时内部归一化：去 "--" 前缀转小写）；
    /// 值 = 选项出现但无值（裸开关）→ "true"，否则原始文本。
    /// </summary>
    private static int Handle(
        CliCommandDescriptor descriptor,
        IReadOnlyDictionary<string, Option<string>> optionMap,
        ParseResult parseResult)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (CliOptionDescriptor option in descriptor.Options)
        {
            OptionResult? result = parseResult.GetResult(optionMap[option.Name]);
            if (result is null)
                continue;   // 选项未出现：不进入字典，模块 Handler 经 CliInvocation.Get 走 fallback
            values[option.Name] = result.GetValueOrDefault<string>() ?? "true";   // 裸开关 → "true"
        }

        var invocation = new CliInvocation(descriptor.Name, values);
        return descriptor.Handler(invocation);
    }
}
