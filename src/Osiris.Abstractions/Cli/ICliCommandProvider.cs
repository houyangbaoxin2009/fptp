namespace Osiris.Abstractions.Cli;

/// <summary>
/// CLI 命令提供者：模块贡献命令行子命令（CLI 宿主扫描全部模块收集挂载）。
/// 与 GUI 同安全设计：模块经同一注册表加载校验（禁用/卸载/MinHostVersion 校验跳过）。
/// 模块在 CLI 宿主下 IHostContext.Ui == null → 跳过 UI 注册、仅贡献 CLI 命令。
/// </summary>
public interface ICliCommandProvider : IPlugin
{
    /// <summary>本模块贡献的 CLI 子命令列表。</summary>
    IReadOnlyList<CliCommandDescriptor> Commands { get; }
}
