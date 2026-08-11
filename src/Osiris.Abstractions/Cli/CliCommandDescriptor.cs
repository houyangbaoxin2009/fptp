namespace Osiris.Abstractions.Cli;

/// <summary>
/// CLI 子命令声明式描述（tie 友好，纯数据）：模块无需引用 System.CommandLine，
/// 宿主将描述挂载为根命令的子命令，并把解析结果封装为 CliInvocation 回调 Handler。
/// </summary>
/// <param name="Name">子命令名（如 "batch"）。</param>
/// <param name="Description">帮助文本。</param>
/// <param name="Options">选项描述列表。</param>
/// <param name="Handler">执行回调：接收解析后的调用上下文，返回进程退出码（0=成功，非 0=失败）。</param>
public sealed record CliCommandDescriptor(
    string Name,
    string Description,
    IReadOnlyList<CliOptionDescriptor> Options,
    Func<CliInvocation, int> Handler);

/// <summary>
/// CLI 选项描述（声明式，宿主据此生成 System.CommandLine Option 定义）。
/// </summary>
/// <param name="Name">长选项名（如 "--filter"）。</param>
/// <param name="ShortName">短选项（如 "-f"），可空。</param>
/// <param name="Description">帮助文本。</param>
/// <param name="Required">是否必填（默认 false）。</param>
/// <param name="DefaultValue">默认值文本（可空，宿主解析用）。</param>
public sealed record CliOptionDescriptor(
    string Name,
    string? ShortName,
    string Description,
    bool Required = false,
    string? DefaultValue = null);
