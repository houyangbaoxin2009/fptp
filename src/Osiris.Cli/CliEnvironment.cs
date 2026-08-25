namespace Osiris.Cli;

/// <summary>
/// CLI 路径常量（AppPaths 逻辑内联，见架构 11 节"路径：Environment.SpecialFolder.ApplicationData + Fptp"）：
/// 与 GUI 共用同一用户数据目录 %APPDATA%/Fptp（Linux 为 ~/.config/Fptp），因此
/// modules.data.tie / settings.data.tie / secure.data.tie 与扩展模块安装目录全部与 GUI 相同——
/// CLI 与 GUI 共享同一模块注册表、同一用户配置、同一安全设置（"CLI 无特权"，与 GUI 同级）。
/// </summary>
internal static class CliEnvironment
{
    /// <summary>用户数据根目录（与 GUI 完全同一目录，跨进程共享状态）。</summary>
    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fptp");

    /// <summary>模块状态注册表：{模块Id: "enabled"|"disabled"|"removed"}，tie:data 格式，与 GUI 同文件。</summary>
    public static string ModulesPath => Path.Combine(AppDataDir, "modules.data.tie");

    /// <summary>模块配置（User/Core 段）：{模块Id.键: 值}，tie:data 格式，与 GUI 同文件。</summary>
    public static string SettingsPath => Path.Combine(AppDataDir, "settings.data.tie");

    /// <summary>安全配置（Security 段，仅更新模块可写、独立文件隔离），tie:data 格式，与 GUI 同文件。</summary>
    public static string SecurePath => Path.Combine(AppDataDir, "secure.data.tie");

    /// <summary>用户安装的扩展模块目录（与 GUI 扫描的同一目录）。</summary>
    public static string UserModuleDirectory => Path.Combine(AppDataDir, "modules");

    /// <summary>程序集旁扩展模块目录（部署/开发期随产物输出，如 plugins/）。</summary>
    public static string ExtensionDirectory => Path.Combine(AppContext.BaseDirectory, "plugins");
}
