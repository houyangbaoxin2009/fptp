using FpSDK;
using Xunit;

namespace FpSdk.Tests;

/// <summary>
/// tie 2026.1-preview.4 集成测试：
/// - tie 模板生成回环（module.json type=script / main.tie + fptp_sdk.tie / 无 csproj）；
/// - TieRunner 端到端：真实编译运行 main.tie（import fptp_sdk.tie），base64 中文协议文本往返；
/// - ticc 随包分发可达性（AppContext.BaseDirectory/tools/tie）。
/// </summary>
public class TieTests
{
    private static string FindTemplateRoot(string template)
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "..", "..", "..", "..", "..", "templates", template);
            if (Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException($"找不到模板目录 templates/{template}");
    }

    private static string NewTempDir()
    {
        string p = Path.Combine(Path.GetTempPath(), $"fpstie_{Guid.NewGuid():N}");
        Directory.CreateDirectory(p);
        return p;
    }

    private static void CleanUp(string? tmp = null)
    {
        if (tmp != null && Directory.Exists(tmp))
            Directory.Delete(tmp, recursive: true);
    }

    [Fact]
    public void TieVersion_BindsHarbor()
    {
        Assert.Equal("Harbor-2026.1-preview.5", TieVersion.Harbor);
        Assert.Equal("fptp.tie-bridge.v2", TieVersion.BridgeProtocol);
    }

    [Fact]
    public void Generate_TieMode_CreatesScriptManifest()
    {
        string tmp = NewTempDir();
        try
        {
            var files = ProjectGenerator.Generate(FindTemplateRoot(ProjectGenerator.TieTemplate), tmp,
                new ProjectGenerator.Options(Name: "TieProbe", Id: "tie.probe",
                    DisplayName: "tie 探头", Language: "tie"));

            Assert.Contains("main.tie", files);
            Assert.Contains("fptp_sdk.tie", files);   // 运行桥已抽为库，随模板分发
            Assert.Contains("module.json", files);
            Assert.Contains(Path.Combine("std", "tink.tie"), files);   // tink 帧层随模板分发
            Assert.Contains(Path.Combine("rdu", "crc.tie"), files);    // tink 依赖 crc（内联）
            Assert.DoesNotContain(files, f => f.EndsWith(".csproj"));

            // module.json：tie 脚本清单约定
            string manifestJson = File.ReadAllText(Path.Combine(tmp, "module.json"));
            Assert.Contains("\"type\": \"script\"", manifestJson);
            Assert.Contains("\"language\": \"tie\"", manifestJson);
            Assert.Contains("\"entryPoint\": \"TieProbe.tie\"", manifestJson);
            Assert.Contains("\"id\": \"tie.probe\"", manifestJson);
            Assert.Contains("\"name\": \"tie 探头\"", manifestJson);

            // main.tie：import 运行桥库 + process 豁口（v2 帧桥已移至 fptp_sdk.tie）
            string main = File.ReadAllText(Path.Combine(tmp, "main.tie"));
            Assert.Contains("import \"fptp_sdk.tie\"", main);
            Assert.Contains("func process(src: string) -> string", main);
            Assert.Contains("fptp.bridge(process)", main);
            // token 已替换
            Assert.DoesNotContain("{{Name}}", main.Replace("tie 探头", ""));

            // fptp_sdk.tie：v2 帧桥（import std/tink.tie + 帧解码 + 应答）
            string sdk = File.ReadAllText(Path.Combine(tmp, "fptp_sdk.tie"));
            Assert.Contains("import \"std/tink.tie\" as tink", sdk);
            Assert.Contains("namespace fptp", sdk);
            Assert.Contains("func bridge(proc: fn(string) -> string)", sdk);
            Assert.Contains("tink.frame_next(bytes, 0)", sdk);
            Assert.Contains("func base64_decode(s: string) -> table<i64>", sdk);
            Assert.Contains("func base64_encode(bytes: table<i64>) -> string", sdk);
        }
        finally
        {
            CleanUp(tmp);
        }
    }

    [Fact]
    public void Tiec_IsShippedWithPackage()
    {
        // tiec.exe 已随 FpSDK/测试输出分发（tools/tie/）
        string? tiec = TieRunner.FindTiec();
        Assert.True(tiec is not null, "未找到随包 tiec.exe（构建后 tools\\tie\\tiec.exe 应已复制到输出）");
    }

    [Fact]
    public void TieRunner_RunsScript_ChineseRoundtrip()
    {
        string? tiec = TieRunner.FindTiec();
        if (tiec is null)
            return;   // 运行桥不可用（未随包分发）时跳过

        string tmp = NewTempDir();
        try
        {
            ProjectGenerator.Generate(FindTemplateRoot(ProjectGenerator.TieTemplate), tmp,
                new ProjectGenerator.Options(Name: "Echo", Id: "tie.echo", DisplayName: "回声"));

            string mainTie = Path.Combine(tmp, "main.tie");
            TieResult result = TieRunner.Run(mainTie, "你好，tie 2026.1-pre4！\n第二行：中文 ✓");

            Assert.True(result.Ok, result.Message);
            Assert.Equal("你好，tie 2026.1-pre4！\n第二行：中文 ✓", result.Output);
        }
        finally
        {
            CleanUp(tmp);
        }
    }

    [Fact]
    public void TieRunner_ErrPath_ReplyErrProtocol()
    {
        string? tiec = TieRunner.FindTiec();
        if (tiec is null)
            return;   // 运行桥不可用（未随包分发）时跳过

        string tmp = NewTempDir();
        try
        {
            ProjectGenerator.Generate(FindTemplateRoot(ProjectGenerator.TieTemplate), tmp,
                new ProjectGenerator.Options(Name: "FailProbe", Id: "tie.fail", DisplayName: "失败探头"));

            // 覆盖 main.tie：直接走 fptp_sdk.tie 的失败应答（tiec 不接受带 BOM 的文件头）
            // 注意：桥的字节模型按 0-255 码点串处理，协议文本用 ASCII 才能稳定往返
            string mainTie = Path.Combine(tmp, "main.tie");
            File.WriteAllText(mainTie,
                "type tie<logic>\n\nimport \"fptp_sdk.tie\" as fptp\n\nfunc main() {\n    fptp.reply_err(\"probe_err\")\n}\n",
                new System.Text.UTF8Encoding(false));

            TieResult result = TieRunner.Run(mainTie, "输入无关紧要");
            Assert.False(result.Ok);
            Assert.Equal("probe_err", result.Message);
        }
        finally
        {
            CleanUp(tmp);
        }
    }

    [Fact]
    public void TieRunner_Parse_FrameProtocol()
    {
        // v2 帧桥解析：OK 帧 → 成功；ERR 帧 → 失败；非法行跳过/失败。
        byte[] okFrame = Tink.Encode(Tink.TagPayload(Tink.OkTag, "结果"));
        Assert.True(TieRunner.Parse(Convert.ToBase64String(okFrame)).Ok);

        byte[] errFrame = Tink.Encode(Tink.TagPayload(Tink.ErrTag, "失败了"));
        var err = TieRunner.Parse(Convert.ToBase64String(errFrame));
        Assert.False(err.Ok);
        Assert.Equal("失败了", err.Message);

        Assert.False(TieRunner.Parse("junk").Ok);                       // 非法行 → 失败
        Assert.False(TieRunner.Parse("").Ok);                           // 空输出 → 失败
        Assert.True(TieRunner.Parse("诊断行\n" + Convert.ToBase64String(okFrame)).Ok);   // 非帧行在前：取第一个合法帧
    }

    [Fact]
    public void TieSdk_DataTools_ParsesAndBuildsTieData()
    {
        // 意图：fptp_sdk.tie 数据工具（data_get_int/data_get/data_get_bool/data_has/data_make/data_escape）
        // 在真实 tiec 端到端下对 tie:data 顶层表文本取值/构造正确（含转义还原与逃逸）。
        string? tiec = TieRunner.FindTiec();
        if (tiec is null)
            return;   // 运行桥不可用（未随包分发）时跳过

        string tmp = NewTempDir();
        try
        {
            ProjectGenerator.Generate(FindTemplateRoot(ProjectGenerator.TieTemplate), tmp,
                new ProjectGenerator.Options(Name: "DataProbe", Id: "tie.data", DisplayName: "数据工具探头"));

            // 覆盖 main.tie：用数据工具解析/构造 tie:data（ASCII 协议文本，tiec 不接受带 BOM 的文件头）
            string mainTie = Path.Combine(tmp, "main.tie");
            File.WriteAllText(mainTie,
                """
                type tie<logic>

                import "fptp_sdk.tie" as fptp

                func main() {
                    var txt = "[\"width\": 640, \"name\": \"a\\\"b\", \"ok\": true]"
                    var w = fptp.data_get_int(txt, "width", 0)
                    var nm = fptp.data_get(txt, "name", "")
                    var ok = fptp.data_get_bool(txt, "ok", false)
                    var miss = fptp.data_get(txt, "nope", "fb")
                    var has = fptp.data_has(txt, "width")
                    var made = fptp.data_make("result", "he said \"hi\" \\ ok")
                    fptp.reply_ok(to_string(w) + "|" + nm + "|" + to_string(ok) + "|" + miss + "|" + to_string(has) + "|" + made)
                }
                """,
                new System.Text.UTF8Encoding(false));

            TieResult result = TieRunner.Run(mainTie, "输入无关紧要");

            Assert.True(result.Ok, result.Message);
            string[] parts = result.Output.Split('|');
            Assert.Equal("640", parts[0]);                          // data_get_int
            Assert.Equal("a\"b", parts[1]);                          // 转义还原
            Assert.Equal("-1", parts[2]);                            // tie bool true → to_string -1
            Assert.Equal("fb", parts[3]);                            // 未命中 → fallback
            Assert.Equal("-1", parts[4]);                            // data_has true
            Assert.Equal("[\"result\": \"he said \\\"hi\\\" \\\\ ok\"]", parts[5]);   // data_make 转义
        }
        finally
        {
            CleanUp(tmp);
        }
    }
}