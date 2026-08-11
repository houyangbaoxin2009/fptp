namespace Osiris.Plugins.Tests;

/// <summary>
/// 插件输出目录定位器：从测试输出目录向上回溯查找仓库 plugins/bin
/// （宿主运行时扫描目录，含 Fptp.Plugins.Builtin.dll）。
/// </summary>
internal static class PluginsBinLocator
{
    /// <summary>仓库 plugins/bin 绝对路径。</summary>
    public static string Path
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = System.IO.Path.Combine(dir.FullName, "plugins", "bin");
                if (File.Exists(System.IO.Path.Combine(candidate, "Fptp.Plugins.Builtin.dll")))
                    return candidate;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("未找到仓库 plugins/bin 目录（请先构建 Fptp.Plugins.Builtin 项目）。");
        }
    }
}
