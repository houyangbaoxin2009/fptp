namespace Osiris.Abstractions.Plugins;

/// <summary>
/// 插件导出标记：宿主在独立 ALC 中扫描实现 [PluginExport] 且实现 IPlugin 的类型，
/// 实例化并调用 Initialize。一个程序集可标记多个导出类型；
/// 契约层仅声明，扫描/实例化由宿主 PluginLoader 实现。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginExportAttribute : Attribute
{
}
