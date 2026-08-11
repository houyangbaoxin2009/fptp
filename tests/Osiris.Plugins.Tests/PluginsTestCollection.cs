using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 插件测试集合定义：全部插件测试共享 plugins/bin 的 ALC 加载路径，
/// 且 ModuleLoaderTests 的卸载断言依赖进程内无其它插件引用存活，
/// 故整个集合串行执行（DisableParallelization=true）防互扰。
/// </summary>
[CollectionDefinition("Plugins", DisableParallelization = true)]
public class PluginsTestCollection
{
}
