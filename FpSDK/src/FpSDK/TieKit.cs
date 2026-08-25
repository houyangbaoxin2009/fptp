using System.Diagnostics;
using System.Text;

namespace FpSDK;

/// <summary>tie 语言版本常量（绑定 tie-main Harbor 发行线）。</summary>
public static class TieVersion
{
    /// <summary>绑定的 tie 发行版：Harbor 2026.1 预发布4。</summary>
    public const string Harbor = "Harbor-2026.1-preview.4";

    /// <summary>tie 插件运行桥协议版本（宿主 ↔ tie 脚本）。</summary>
    public const string BridgeProtocol = "fptp.tie-bridge.v1";
}

/// <summary>tie 脚本执行结果。</summary>
public sealed record TieResult(bool Ok, string Output, string? Message)
{
    /// <summary>成功结果描述（OK 时输出内容，失败时错误信息）。</summary>
    public string? Description => Ok ? Output : Message;
}

/// <summary>
/// tie 脚本运行桥：进程调用 tiec.exe 编译 .tie 脚本（同目录 fptp_sdk.tie 依赖随拷）并执行。
/// <para>协议（templates/tie-module 同款）：</para>
/// <list type="bullet">
///   <item>输入：环境变量 <c>FPTP_TIE_INPUT</c> = base64(宿主文本)；</item>
///   <item>输出：stdout 单行 <c>FPTP_OK:&lt;base64&gt;</c> 或 <c>FPTP_ERR:&lt;base64&gt;</c>。</item>
/// </list>
/// 仅使用 tie 内联底座（get_env/println/str_len/str_char/算术），编译零 tie-interp 依赖。
/// tiec.exe 定位：<c>FPTP_TIE_HOME</c> 环境变量，或 <c>&lt;运行目录&gt;/tools/tie/tiec.exe</c>（随 NuGet 分发）。
/// </summary>
public static class TieRunner
{
    /// <summary>输入环境变量名。</summary>
    public const string InputEnvVar = "FPTP_TIE_INPUT";

    /// <summary>成功前缀。</summary>
    public const string OkPrefix = "FPTP_OK:";

    /// <summary>失败前缀。</summary>
    public const string ErrPrefix = "FPTP_ERR:";

    /// <summary>定位 tiec.exe；未找到返回 null。</summary>
    public static string? FindTiec()
    {
        string? home = Environment.GetEnvironmentVariable("FPTP_TIE_HOME");
        if (!string.IsNullOrEmpty(home))
        {
            string h = Path.Combine(home, "tiec.exe");
            if (File.Exists(h))
                return h;
        }
        foreach (string? root in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(AppContext.BaseDirectory) })
        {
            if (root is null)
                continue;
            string candidate = Path.Combine(root, "tools", "tie", "tiec.exe");
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>
    /// 编译并运行 tie 脚本，以协议文本进出（自动 base64 编解码）。
    /// 脚本约定顶层 <c>func process(src: string) -> string</c> 与 main 桥（见模板）。
    /// </summary>
    public static TieResult Run(string scriptPath, string input, int timeoutMs = 60_000)
    {
        ArgumentNullException.ThrowIfNull(scriptPath);
        ArgumentNullException.ThrowIfNull(input);
        string? tiec = FindTiec();
        if (tiec is null)
            return new TieResult(false, "", "未找到 tiec.exe（设置 FPTP_TIE_HOME 或随包 tools/tie/tiec.exe）");
        if (!File.Exists(scriptPath))
            return new TieResult(false, "", $"脚本不存在：{scriptPath}");

        string work = Path.Combine(Path.GetTempPath(), "fpsdk_tie_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            // tiec 编译产物输出到"输入脚本所在目录"，故先把脚本复制进临时工作目录。
            // tiec 按工作目录（CWD）解析 import：同目录依赖（fptp_sdk.tie 等）一并复制，
            // 否则 main.tie 的 import 会在临时目录解析失败。
            string baseName = Path.GetFileNameWithoutExtension(scriptPath);
            string scriptCopy = Path.Combine(work, baseName + ".tie");
            string exePath = Path.Combine(work, baseName + ".exe");
            string scriptDir = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
            foreach (string dep in Directory.GetFiles(scriptDir, "*.tie"))
            {
                if (string.Equals(dep, scriptPath, StringComparison.OrdinalIgnoreCase))
                    continue;   // 入口脚本下面单独复制
                File.Copy(dep, Path.Combine(work, Path.GetFileName(dep)), overwrite: true);
            }
            File.Copy(scriptPath, scriptCopy, overwrite: true);

            // 1) 编译：tiec.exe <script>
            string compileOut = RunProcess(tiec, "\"" + scriptCopy + "\"", work, env: null, timeoutMs);
            if (!File.Exists(exePath))
                return new TieResult(false, "", $"编译失败：\n{compileOut}");

            // 2) 运行：env 传 base64 输入，捕获 stdout
            string inputB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
            string stdout = RunProcess(exePath, "Output", work,
                env: new Dictionary<string, string> { [InputEnvVar] = inputB64 }, timeoutMs);

            return Parse(stdout);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); }
            catch { /* 清理失败忽略 */ }
        }
    }

    /// <summary>解析 FPTP_OK/FPTP_ERR 协议输出（自动 base64 解码）。</summary>
    public static TieResult Parse(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return new TieResult(false, "", "脚本无输出");
        string line = FirstLine(stdout);
        if (line.StartsWith(OkPrefix, StringComparison.Ordinal))
        {
            string decoded = DecodePayload(line.Substring(OkPrefix.Length));
            return new TieResult(true, decoded, null);
        }
        if (line.StartsWith(ErrPrefix, StringComparison.Ordinal))
        {
            string decoded = DecodePayload(line.Substring(ErrPrefix.Length));
            return new TieResult(false, "", decoded);
        }
        return new TieResult(false, "", $"无法识别的脚本输出：{line}");
    }

    private static string DecodePayload(string b64)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64.Trim()));
        }
        catch (FormatException)
        {
            return b64;   // 非 base64（容错直出）
        }
    }

    private static string FirstLine(string s)
    {
        int nl = s.IndexOf('\n');
        return nl < 0 ? s.Trim() : s.Substring(0, nl).Trim();
    }

    private static string RunProcess(string fileName, string args, string workDir,
        Dictionary<string, string>? env, int timeoutMs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (env != null)
        {
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        }
        using var p = new Process { StartInfo = psi };
        var outSb = new StringBuilder();
        var errSb = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) outSb.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) errSb.AppendLine(e.Data); };
        if (!p.Start())
            return "进程启动失败";
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(entireProcessTree: true); } catch { /* 忽略 */ }
            return "执行超时";
        }
        p.WaitForExit();
        return outSb + errSb.ToString();
    }
}