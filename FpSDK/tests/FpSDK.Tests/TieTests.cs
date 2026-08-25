using FpSDK;
using Xunit;

namespace FpSdk.Tests;

/// <summary>
/// tie 2026.1-preview.4 集成测试：
/// - tie 模板生成回环（module.json type=script / main.tie / 无 csproj）；
/// - TieRunner 端到端：真实编译运行 main.tie，base64 中文协议文本往返；
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
        Assert.Equal("Harbor-2026.1-preview.4", TieVersion.Harbor);
        Assert.Equal("fptp.tie-bridge.v1", TieVersion.BridgeProtocol);
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
            Assert.Contains("module.json", files);
            Assert.DoesNotContain(files, f => f.EndsWith(".csproj"));

            // module.json：tie 脚本清单约定
            string manifestJson = File.ReadAllText(Path.Combine(tmp, "module.json"));
            Assert.Contains("\"type\": \"script\"", manifestJson);
            Assert.Contains("\"language\": \"tie\"", manifestJson);
            Assert.Contains("\"entryPoint\": \"TieProbe.tie\"", manifestJson);
            Assert.Contains("\"id\": \"tie.probe\"", manifestJson);
            Assert.Contains("\"name\": \"tie 探头\"", manifestJson);

            // main.tie：进程桥约定 + 模板豁口
            string main = File.ReadAllText(Path.Combine(tmp, "main.tie"));
            Assert.Contains("func process(src: string) -> string", main);
            Assert.Contains("FPTP_OK:", main);
            Assert.Contains("base64_decode", main);
            // token 已替换
            Assert.DoesNotContain("{{Name}}", main.Replace("tie 探头", ""));
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
    public void TieRunner_Parse_Protocol()
    {
        Assert.True(TieRunner.Parse("FPTP_OK:" + Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("结果"))).Ok);
        var err = TieRunner.Parse("FPTP_ERR:" + Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("失败了")));
        Assert.False(err.Ok);
        Assert.Equal("失败了", err.Message);
        Assert.False(TieRunner.Parse("junk").Ok);
    }
}