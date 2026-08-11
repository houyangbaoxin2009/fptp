using Avalonia;

namespace Osiris.App;

/// <summary>
/// Avalonia 应用入口：构建跨平台 AppBuilder。
/// DI 容器在 App.axaml.cs 的 OnFrameworkInitializationCompleted 中构建。
/// </summary>
internal static class Program
{
    // Avalonia 生成的初始化代码。不要使用任何平台特定 API。
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>构建 Avalonia 应用（测试经此启动 headless 实例）。</summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
