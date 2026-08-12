using System.Security.Principal;

namespace Osiris.Core.Security;

/// <summary>
/// 权限检测工具：判断当前进程是否以管理员（Windows）/ root（Linux/macOS）身份运行。
/// 用途：启动时检测提权运行——插件是 ALC 加载的任意代码，管理员权限下可写系统目录/装驱动，
/// 恶意插件可借提权破坏系统。宿主（App/Cli）检测到后应提示用户降权运行。
/// 注意：这是纵深防御的一环，真正防线是插件来源可信（签名校验/外部模块警告）。
/// </summary>
public static class ElevationGuard
{
    /// <summary>当前进程是否以管理员（Windows）/ root（Linux/macOS）身份运行。</summary>
    public static bool IsElevated()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return IsWindowsAdmin();
            // Linux/macOS：root 用户即最高权限（环境变量校验，无需调用外部命令）
            return string.Equals(
                Environment.GetEnvironmentVariable("USER") ?? Environment.UserName,
                "root",
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // 权限查询失败（异常环境）→ 保守按非提权处理，不阻断启动
            return false;
        }
    }

    /// <summary>
    /// Windows 管理员检测：WindowsPrincipal.IsInRole 对 UAC 提权后的进程返回 true。
    /// 注意：这是"进程令牌是否含管理员 SID"，非"用户是否属于管理员组"。
    /// </summary>
    private static bool IsWindowsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
