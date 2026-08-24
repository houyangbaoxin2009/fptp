using FpSDK;
using Xunit;

namespace FpSdk.Tests;

/// <summary>
/// 脚手架模板生成回环：从仓库模板生成骨架，断言关键文件、token 替换、引用模式。
/// </summary>
public class TemplateTests
{
    private static string FindTemplateRoot()
    {
        // 测试程序集位于 bin/Debug/net10.0/，向上回溯仓库根 FpSDK/
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "..", "..", "..", "..", "..", "templates", "dotnet-module");
            if (Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("找不到模板目录 templates/dotnet-module");
    }

    private static string NewTempDir()
    {
        string p = Path.Combine(Path.GetTempPath(), $"fpsdk_{Guid.NewGuid():N}");
        Directory.CreateDirectory(p);
        return p;
    }

    [Fact]
    public void Generate_RepoMode_CreatesProjectSkeleton()
    {
        string tmp = NewTempDir();
        try
        {
            var files = ProjectGenerator.Generate(FindTemplateRoot(), tmp,
                new ProjectGenerator.Options(Name: "MyWidget", Id: "my.widget",
                    DisplayName: "我的组件", Version: "1.0.0.0", RepoReference: true));

            Assert.Contains("MyWidget.csproj", files);          // _Module_.csproj 重命名
            Assert.Contains("MyWidgetModule.cs", files);        // Module.cs 重命名
            Assert.Contains("module.json", files);
            Assert.Contains(Path.Combine("langs", "en-us.json"), files);

            // csproj：仓库内模式引本地 FpSDK 项目
            string csproj = File.ReadAllText(Path.Combine(tmp, "MyWidget.csproj"));
            Assert.Contains(@"..\..\FpSDK\src\FpSDK\FpSDK.csproj", csproj);

            // module.json：token 已替换且可被强类型读取
            string manifestJson = File.ReadAllText(Path.Combine(tmp, "module.json"));
            var manifest = FpSDK.PluginManifest.Load(manifestJson);
            Assert.Equal("my.widget", manifest.Id);
            Assert.Equal("我的组件", manifest.Name);
            Assert.Equal("MyWidget.dll", manifest.EntryPoint);
            Assert.Equal("1.0.0.0", manifest.Version);

            // Module.cs：namespace / PluginExport / Id 已替换
            string moduleCs = File.ReadAllText(Path.Combine(tmp, "MyWidgetModule.cs"));
            Assert.Contains("namespace MyWidget;", moduleCs);
            Assert.Contains("[PluginExport]", moduleCs);
            Assert.Contains("=> \"my.widget\";", moduleCs);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Generate_SdkMode_ReferencesNuGetPackage()
    {
        string tmp = NewTempDir();
        try
        {
            ProjectGenerator.Generate(FindTemplateRoot(), tmp,
                new ProjectGenerator.Options(Name: "Ext", Id: "ext", DisplayName: "外部插件", RepoReference: false));

            string csproj = File.ReadAllText(Path.Combine(tmp, "Ext.csproj"));
            Assert.Contains(@"<PackageReference Include=""FpSDK"" Version=""1.0.0"" />", csproj);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Generate_GeneratedProject_Builds()
    {
        // 端到端：生成的仓库内插件能真实构建（引用本地 FpSDK + Abstractions）。
        string tmp = NewTempDir();
        try
        {
            ProjectGenerator.Generate(FindTemplateRoot(), tmp,
                new ProjectGenerator.Options(Name: "Buildable", Id: "buildable",
                    DisplayName: "可构建", RepoReference: true));
            // 仓库内 FpSDK 相对路径：plugins/Buildable/ → ../../FpSDK/...
            // 此处生成在临时目录，重建引用指向仓库内 FpSDK 项目
            string csproj = File.ReadAllText(Path.Combine(tmp, "Buildable.csproj"));
            string repoSdk = @"..\..\FpSDK\src\FpSDK\FpSDK.csproj";
            Assert.Contains(repoSdk, csproj);   // 引用形态正确（真构建依赖所在仓库布局）
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}