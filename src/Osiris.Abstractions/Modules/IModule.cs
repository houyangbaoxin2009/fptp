namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块契约：模块化图像编辑器的加载单位（插件 IPlugin 的模块化扩展）。
/// Standard 模块随产品分发、静态加载；Extension 模块经独立 ALC 动态加载可卸载。
/// 宿主读取 module.json 清单驱动加载；本接口供宿主在反射扫描后确认模块元数据。
/// </summary>
public interface IModule : IPlugin
{
    /// <summary>模块分级（Standard / Extension），与 module.json 的 kind 一致。</summary>
    ModuleKind Kind { get; }

    /// <summary>依赖模块 Id 列表（加载前先解析依赖；无依赖返回空列表）。</summary>
    IReadOnlyList<string> Dependencies { get; }
}
