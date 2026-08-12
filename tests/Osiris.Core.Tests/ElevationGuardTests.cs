using Osiris.Core.Security;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// 权限检测工具测试：ElevationGuard.IsElevated 不抛异常且返回布尔（环境相关，只验证健壮性）。
/// </summary>
public class ElevationGuardTests
{
    [Fact]
    public void IsElevated_不抛异常且结果稳定()
    {
        // 正常环境：返回 bool 且连续调用一致；任何异常环境返回 false 不崩溃
        bool first = ElevationGuard.IsElevated();
        bool second = ElevationGuard.IsElevated();
        Assert.Equal(first, second);
    }
}
