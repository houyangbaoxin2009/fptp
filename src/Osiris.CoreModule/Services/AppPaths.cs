using System.Runtime.InteropServices;

namespace Osiris.CoreModule.Services;

/// <summary>
/// 应用路径服务：集中提供本软件在用户主目录下的数据目录与配置文件路径。
/// 与 CLI 共享同一注册表路径（%APPDATA%/Fptp），保证 GUI 与 CLI 的状态一致
/// （模块注册表 modules.json、模块配置 settings.json、安全配置 secure.json）。
/// 全部成员为静态：无需实例化即可在任何宿主（GUI/CLI/测试）下使用。
/// </summary>
public sealed class AppPaths
{
    // 成员全部静态，实例无状态；构造公开仅用于宿主把本类型注册进服务（依赖注入占位）。
    public AppPaths() { }

    /// <summary>
    /// 应用数据根目录：Windows/macOS 取 %APPDATA%/Fptp，
    /// Linux 按 XDG 约定取 ~/.config/Fptp（XDG_CONFIG_HOME 优先）。
    /// </summary>
    public static string AppDataDir
    {
        get
        {
            if (!OperatingSystem.IsLinux())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp");

            // Linux：遵循 XDG Base Directory 规范，目录不存在时创建。
            string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string baseDir = string.IsNullOrEmpty(xdg)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
                : xdg;
            return Path.Combine(baseDir, "Fptp");
        }
    }

    /// <summary>用户设置文件（settings.json，User/Core 段配置）。</summary>
    public static string SettingsPath => Path.Combine(AppDataDir, "settings.json");

    /// <summary>模块注册表文件（modules.json，模块状态与配置）。</summary>
    public static string ModulesPath => Path.Combine(AppDataDir, "modules.json");

    /// <summary>安全设置文件（secure.json，Security 段配置，与普通设置隔离）。</summary>
    public static string SecurePath => Path.Combine(AppDataDir, "secure.json");

    /// <summary>
    /// 插件扫描目录列表：①程序集旁的 plugins/（随产品分发的扩展模块）；
    /// ②%APPDATA%/Fptp/modules/（用户手动安装的扩展模块）。只返回已存在的目录。
    /// </summary>
    public static string[] GetPluginDirectories()
    {
        var dirs = new List<string>(2);

        // ① 程序集旁 plugins/：随产品分发，与主程序同目录。
        string beside = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (Directory.Exists(beside))
            dirs.Add(beside);

        // ② 用户数据目录 modules/：用户手动安装的模块。
        string userModules = Path.Combine(AppDataDir, "modules");
        if (Directory.Exists(userModules))
            dirs.Add(userModules);

        return [.. dirs];
    }
}
