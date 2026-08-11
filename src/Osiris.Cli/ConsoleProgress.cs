using Osiris.Abstractions.Progress;

namespace Osiris.Cli;

/// <summary>
/// 控制台进度上报（IHostContext.Report 的 CLI 适配）：
/// percent/message 打印到 stderr（与 stdout 的正式输出分离，便于管道/重定向）；
/// 仅当百分比（四舍五入取整）变化时才打印，避免高频上报刷屏。
/// </summary>
internal sealed class ConsoleProgress : IProgress
{
    // 上次已打印的百分比（-1 表示尚未打印过任何进度）
    private int _lastPercent = -1;

    // 打印互斥：滤镜/批处理可能后台线程上报，保证输出不交错
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Report(double percent, string message)
    {
        // 防御：非法百分比按 0 处理，绝不打印 NaN/越界值
        int rounded = double.IsFinite(percent) ? (int)Math.Round(percent) : 0;
        rounded = Math.Clamp(rounded, 0, 100);

        lock (_lock)
        {
            if (rounded == _lastPercent)
                return;     // 百分比未变化且消息未更新，跳过打印（避免刷屏）
            _lastPercent = rounded;
            Console.Error.WriteLine($"{TerminalColor.Info($"[{rounded,3}%]")} {message}");
        }
    }
}
