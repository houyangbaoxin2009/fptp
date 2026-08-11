using Osiris.Abstractions.Modules;

namespace Osiris.Abstractions.Ui;

/// <summary>
/// 工具插件：模块向宿主暴露一组交互工具（IEditorTool）。
/// 宿主（工作台画布）把工具列表呈现给用户选择，激活的工具接收画布鼠标事件并绘制覆盖层；
/// 与 IModule 分级一致：Standard 静态贡献、Extension 经 ALC 动态加载贡献。
/// </summary>
public interface IToolPlugin : IModule
{
    /// <summary>本模块提供的交互工具列表（套索/画笔/选取等）。</summary>
    IReadOnlyList<IEditorTool> Tools { get; }
}
