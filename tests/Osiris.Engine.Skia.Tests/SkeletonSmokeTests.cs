using Osiris.Engine.Skia;
using Xunit;

namespace Osiris.Engine.Skia.Tests;

/// <summary>骨架冒烟测试：验证渲染引擎程序集可加载且核心类型存在（EngineMarker 占位已删除）。</summary>
public class SkeletonSmokeTests
{
    [Fact]
    public void Engine_Core_Types_Exist()
    {
        // 渲染引擎正式实现类型应存在（占位 EngineMarker 已由 ZeroCopyImage/DocumentRenderer/SkiaCodec 取代）
        Assert.NotNull(typeof(ZeroCopyImage));
        Assert.NotNull(typeof(DocumentRenderer));
        Assert.NotNull(typeof(SkiaCodec));
    }

    [Fact]
    public void Engine_Assembly_Version_Is_2100()
    {
        // 渲染引擎程序集版本应与产品版本 2.1.0.0 一致（Directory.Build.props 注入）
        Assert.Equal("2.1.0.0", typeof(SkiaCodec).Assembly.GetName().Version?.ToString());
    }
}
