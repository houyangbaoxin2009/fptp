global using Xunit;
global using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Headless;
using Osiris.App;

// Headless 测试应用装配（Avalonia 12 + xunit.v3）：
// 会话按程序集启动一次，经 OnFrameworkInitializationCompleted 走真实组合路径（不影响本测试自身构造）。
[assembly: AvaloniaTestApplication(typeof(Osiris.App.Tests.TestAppBuilder))]

namespace Osiris.App.Tests;

/// <summary>Headless 应用构建器：以 Osiris.App 的 App 类启动无头平台。</summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
