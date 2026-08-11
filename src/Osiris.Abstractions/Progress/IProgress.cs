namespace Osiris.Abstractions.Progress;

/// <summary>
/// 进度上报：插件与批处理共用（0~100 百分比 + 中文消息）。
/// App 侧适配为 System.IProgress 回调到 UI 线程。
/// </summary>
public interface IProgress
{
    /// <summary>上报进度。</summary>
    /// <param name="percent">0~100 的完成百分比。</param>
    /// <param name="message">进度消息（如"正在换底色…"）。</param>
    void Report(double percent, string message);
}
