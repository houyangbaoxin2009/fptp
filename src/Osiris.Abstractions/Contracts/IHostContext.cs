namespace Osiris.Abstractions;

/// <summary>
/// 宿主上下文：插件与脚本共用的宿主能力面。
/// 壳在加载插件时构造并传入；无 UI 宿主（CLI/测试）下 Ui 为 null，插件应跳过 UI 注册。
/// </summary>
public interface IHostContext
{
    /// <summary>当前活动文档（无文档时为 null）。</summary>
    Document.OsirisDocument? ActiveDocument { get; }

    /// <summary>服务注册表：插件间互调（注册服务/按接口获取）。</summary>
    Plugins.IServiceRegistry Services { get; }

    /// <summary>UI 服务：贡献菜单/工具栏/命令/设置组（无 UI 宿主时为 null）。</summary>
    Ui.IUiService? Ui { get; }

    /// <summary>进度上报（0~100 + 消息）。</summary>
    Progress.IProgress Report { get; }
}
