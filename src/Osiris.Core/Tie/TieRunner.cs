using System.Diagnostics;
using System.Text;

namespace Osiris.Core.Tie;

/// <summary>tie 脚本执行结果（v2 帧桥解析结果，FpSDK.TieRunner 同构）。</summary>
public sealed record TieResult(bool Ok, string Output, string? Message)
{
    /// <summary>成功结果描述（OK 时输出内容，失败时错误信息）。</summary>
    public string? Description => Ok ? Output : Message;
}

/// <summary>
/// tie 脚本运行桥（宿主侧，Osiris.Core）：进程调用 tiec.exe 编译 .tie 脚本
/// （同目录 fptp_sdk.tie 等依赖随拷）并执行。协议 v2 = tink 帧桥（stdin/stdout 行帧流）。
/// <para>协议（与 FpSDK.TieRunner / templates/tie-module 同款，fptp.tie-bridge.v2）：</para>
/// <list type="bullet">
///   <item>通道：stdin/stdout 文本流，<c>\n</c> 定界，每行一条消息；</item>
///   <item>消息：<c>base64(帧)</c>，帧 = <c>[len:u32 BE][payload:len 字节][crc:u32 BE]</c>，
///       crc = CRC32-IEEE(payload)，校验向量 crc32("123456789")==0xCBF43926（tink 帧协议）；</item>
///   <item>输入（宿主→脚本）：stdin 若干行，每行 payload = 协议文本(UTF-8)，可多帧流；</item>
///   <item>输出（脚本→宿主）：stdout 若干行，payload = <c>[tag:1][正文(UTF-8)]</c>，
///       tag 0x00=OK（正文=结果）、0x01=ERR（正文=错误消息）；</item>
///   <item>终帧：宿主写完输入后关闭 stdin → 脚本 read_line() EOF 退出。</item>
/// </list>
/// tiec.exe 定位：<c>FPTP_TIE_HOME</c> 环境变量，或 <c>&lt;运行目录&gt;/tools/tie/tiec.exe</c>（随产品分发）。
/// 编译所需 LLVM 显式接管（tiec 同级 llvm/ 或 FPTP_TIE_HOME/bin/llvm），避免静默回退旧版 LLVM 产出坏 exe。
/// </summary>
public static class TieRunner
{
    /// <summary>定位 tiec.exe；未找到返回 null。</summary>
    public static string? FindTiec()
    {
        string? home = Environment.GetEnvironmentVariable("FPTP_TIE_HOME");
        if (!string.IsNullOrEmpty(home))
        {
            // 发行根布局：<home>/bin/tiec.exe（tiec 同级 bin/llvm 由 FindLlvmHome 发现）
            string h = Path.Combine(home, "bin", "tiec.exe");
            if (File.Exists(h))
                return h;
            // 直连路径：<home>/tiec.exe
            string h2 = Path.Combine(home, "tiec.exe");
            if (File.Exists(h2))
                return h2;
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
    /// 编译并运行 tie 脚本（v2 帧桥）：stdin 写输入帧行 base64(帧[协议文本])，stdout 读应答帧。
    /// 脚本约定顶层 <c>func main()</c> 调 fptp.bridge(process)（见 fptp_sdk.tie 模板）。
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

        string work = Path.Combine(Path.GetTempPath(), "osiris_tie_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            // tiec 编译产物输出到"输入脚本所在目录"，故先把脚本复制进临时工作目录。
            // tiec 按工作目录（CWD）解析 import + 工程清单（module.json/langs 等）：
            // 插件目录全部文件递归复制（保留相对路径），排除二进制产物，
            // 否则裸目录下编译出的 exe（read_line 走 tie-interp 桥）会静默无输出。
            string baseName = Path.GetFileNameWithoutExtension(scriptPath);
            string scriptCopy = Path.Combine(work, baseName + ".tie");
            string exePath = Path.Combine(work, baseName + ".exe");
            string scriptDir = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
            foreach (string dep in Directory.EnumerateFiles(scriptDir, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFullPath(dep), scriptPath, StringComparison.OrdinalIgnoreCase))
                    continue;   // 入口脚本下面单独复制
                string ext = Path.GetExtension(dep).ToLowerInvariant();
                if (ext is ".exe" or ".lib" or ".obj" or ".o" or ".pdb")
                    continue;   // 跳过编译产物
                string rel = Path.GetRelativePath(scriptDir, dep);
                string target = Path.Combine(work, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(dep, target, overwrite: true);
            }
            File.Copy(scriptPath, scriptCopy, overwrite: true);

            // 1) 编译：tiec.exe <script> -o <exe>（显式输出路径，产物落临时目录）。
            //    tiec 依赖同版本 LLVM：显式 TIE_LLVM_HOME（tiec 同级 llvm/ 或 FPTP_TIE_HOME/bin/llvm），
            //    避免回退 PATH/D:\LLVM 旧版 LLVM 链接出静默无输出的 exe。
            string? llvmHome = FindLlvmHome(tiec);
            var compileEnv = llvmHome is null ? null : new Dictionary<string, string> { ["TIE_LLVM_HOME"] = llvmHome };
            string compileOut = RunProcess(tiec, "\"" + scriptCopy + "\" -o \"" + exePath + "\"", work, inputLine: null, timeoutMs, compileEnv);
            if (!File.Exists(exePath))
                return new TieResult(false, "", $"编译失败：\n{compileOut}");

            // 2) 运行（v2 帧桥）：stdin 写一条输入帧行 base64(帧[协议文本]) → 关闭 stdin（脚本 read_line EOF 退出）；
            //    stdout 逐行读取应答帧并解析（CRC 校验 + tag）。
            string outputLine = Convert.ToBase64String(Tink.Encode(Encoding.UTF8.GetBytes(input)));
            string stdout = RunProcess(exePath, "Output", work, outputLine, timeoutMs);

            return Parse(stdout);
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); }
            catch { /* 清理失败忽略 */ }
        }
    }

    /// <summary>
    /// 解析脚本 stdout（v2 帧桥）：逐行 base64(帧) 解码 → CRC 校验 → 读 payload 首字节 tag。
    /// 第一帧 tag=OK → 成功（正文=结果）；tag=ERR → 失败（正文=错误消息）；无合法帧 → 失败（原始输出）。
    /// </summary>
    public static TieResult Parse(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return new TieResult(false, "", "脚本无输出");
        foreach (string raw in stdout.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0)
                continue;
            byte[]? frame;
            try { frame = Convert.FromBase64String(line); }
            catch (FormatException) { continue; }   // 非 base64 行（诊断输出等）跳过
            byte[]? payload = Tink.TryDecode(frame, 0, out _);
            if (payload is null)
                continue;   // CRC 不符/长度不符 → 跳过
            byte tag = payload[0];
            string text = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
            return tag == Tink.OkTag
                ? new TieResult(true, text, null)
                : new TieResult(false, "", text);
        }
        return new TieResult(false, "", $"无法识别的脚本输出：{stdout.Trim()}");
    }

    /// <summary>定位与 tiec 匹配的 LLVM 根（含 bin/clang 等）；找不到返回 null（tiec 按自身回退链）。</summary>
    private static string? FindLlvmHome(string tiecPath)
    {
        string tiecDir = Path.GetDirectoryName(Path.GetFullPath(tiecPath))!;
        // 发行布局：tiec 同级 llvm/（bin/tiec.exe + bin/llvm）
        if (Directory.Exists(Path.Combine(tiecDir, "llvm", "bin")))
            return Path.Combine(tiecDir, "llvm");
        // FPTP_TIE_HOME 指向发行根：<home>/bin/llvm（bin/tiec.exe 同级另有 llvm 时上面已兜住）
        string? home = Environment.GetEnvironmentVariable("FPTP_TIE_HOME");
        if (!string.IsNullOrEmpty(home) && Directory.Exists(Path.Combine(home, "bin", "llvm", "bin")))
            return Path.Combine(home, "bin", "llvm");
        return null;
    }

    private static string RunProcess(string fileName, string args, string workDir,
        string? inputLine, int timeoutMs, Dictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = inputLine is not null,
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
        // 异步读先行，避免子进程输出塞满管道而死锁
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        // v2 帧桥：写入输入帧行后立即关闭 stdin（脚本 read_line 收到 EOF 退出循环）
        if (inputLine is not null)
        {
            p.StandardInput.Write(inputLine);
            p.StandardInput.WriteLine();
            p.StandardInput.Close();
        }
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(entireProcessTree: true); } catch { /* 忽略 */ }
            return "执行超时";
        }
        p.WaitForExit();
        return outSb + errSb.ToString();
    }
}