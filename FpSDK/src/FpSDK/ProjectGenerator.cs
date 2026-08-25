using System.Text;

namespace FpSDK;

/// <summary>
/// 插件项目生成器：从模板目录生成标准插件骨架。
/// 模板目录：dotnet-module（.NET 程序集插件）/ tie-module（tie 脚本插件）。
/// 模板占位符：{{Name}} {{Id}} {{DisplayName}} {{Version}} {{MinHostVersion}}
/// {{Language}} {{EntryPoint}} {{AbstractionsRef}}。
/// 生成规则（dotnet 模板）：
///   - _Module_.csproj → &lt;Name&gt;.csproj；Module.cs → &lt;Name&gt;Module.cs；
///   - {{AbstractionsRef}} 依 RepoReference 注入 ProjectReference（仓库内 FpSDK）
///     或 PackageReference（NuGet 版 FpSDK）。
/// tie 模板（templates/tie-module）：module.json（type=script,language=tie）+
/// main.tie（自包含，TieRunner 桥），无 csproj。
/// </summary>
public static class ProjectGenerator
{
    /// <summary>支持的模板种类。</summary>
    public const string DotNetTemplate = "dotnet-module";
    public const string TieTemplate = "tie-module";

    /// <summary>生成选项。</summary>
    public sealed record Options(
        string Name,
        string Id,
        string DisplayName,
        string Version = "1.0.0.0",
        string MinHostVersion = "1.0.0",
        string Language = "dotnet",
        bool RepoReference = true,
        string FpSdkVersion = "1.0.0");

    /// <summary>从模板目录生成插件项目到目标目录；返回写入的文件相对路径列表。</summary>
    public static List<string> Generate(string templateRoot, string destDir, Options options)
    {
        ArgumentNullException.ThrowIfNull(templateRoot);
        ArgumentNullException.ThrowIfNull(destDir);
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(templateRoot))
            throw new DirectoryNotFoundException($"模板目录不存在：{templateRoot}");

        var files = new List<string>();
        foreach (string tmpl in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(templateRoot, tmpl);
            string targetRel = MapFileName(rel, options);
            string target = Path.Combine(destDir, targetRel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            string content = File.ReadAllText(tmpl, Encoding.UTF8);
            content = ReplaceTokens(content, options);
            File.WriteAllText(target, content, new UTF8Encoding(false));
            files.Add(targetRel);
        }
        return files;
    }

    private static string MapFileName(string rel, Options o) =>
        rel.Replace("_Module_.csproj", $"{o.Name}.csproj")
           .Replace("Module.cs", $"{o.Name}Module.cs");

    private static string ReplaceTokens(string content, Options o)
    {
        string abstractionsRef = o.RepoReference
            ? @"    <ProjectReference Include=""..\..\FpSDK\src\FpSDK\FpSDK.csproj"" />"
            : $"    <PackageReference Include=\"FpSDK\" Version=\"{o.FpSdkVersion}\" />";

        return content
            .Replace("{{Name}}", o.Name)
            .Replace("{{Id}}", o.Id)
            .Replace("{{DisplayName}}", o.DisplayName)
            .Replace("{{Version}}", o.Version)
            .Replace("{{MinHostVersion}}", o.MinHostVersion)
            .Replace("{{Language}}", o.Language)
            .Replace("{{EntryPoint}}", $"{o.Name}.dll")
            .Replace("{{AbstractionsRef}}", abstractionsRef);
    }
}