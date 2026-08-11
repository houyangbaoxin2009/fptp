namespace Osiris.Cli;

/// <summary>
/// 终端输出着色：检测 Windows Terminal（环境变量 WT_SESSION 存在）时输出 ANSI 转义序列，
/// 其他终端（cmd/旧控制台/管道重定向）自动回退纯文本——保证脚本与重定向输出无颜色污染。
/// Win11 默认终端为 Windows Terminal，彩色输出即开即用。
/// </summary>
internal static class TerminalColor
{
    /// <summary>当前是否运行在 Windows Terminal（或支持 ANSI 的现代终端）。</summary>
    public static readonly bool IsColorSupported =
        Environment.GetEnvironmentVariable("WT_SESSION") is not null
        || Environment.GetEnvironmentVariable("TERM") is { Length: > 0 };

    /// <summary>错误（红）。</summary>
    public static string Error(string text) => IsColorSupported ? $"\u001b[31m{text}\u001b[0m" : text;

    /// <summary>成功（绿）。</summary>
    public static string Success(string text) => IsColorSupported ? $"\u001b[32m{text}\u001b[0m" : text;

    /// <summary>信息/进度（青）。</summary>
    public static string Info(string text) => IsColorSupported ? $"\u001b[36m{text}\u001b[0m" : text;

    /// <summary>警告（黄）。</summary>
    public static string Warn(string text) => IsColorSupported ? $"\u001b[33m{text}\u001b[0m" : text;

    /// <summary>加粗标题。</summary>
    public static string Bold(string text) => IsColorSupported ? $"\u001b[1m{text}\u001b[0m" : text;
}
