using Osiris.Abstractions.Modules;

namespace Osiris.Abstractions.Filters;

/// <summary>
/// 滤镜模块：实现此接口的模块向宿主暴露一组滤镜处理器。
/// 宿主（GUI 菜单/CLI 批处理）经模块注册表收集全部 IFilterPlugin.Filters 供解析调用；
/// 与 IModule 分级一致：Standard 静态贡献、Extension 经 ALC 动态加载贡献。
/// </summary>
public interface IFilterPlugin : IModule
{
    /// <summary>本模块提供的滤镜处理器列表。</summary>
    IReadOnlyList<IFilterProcessor> Filters { get; }
}
