using System.Text;
using FpSDK;

namespace FpSdkCli;

/// <summary>
/// fpsdk —— fptp(Osiris) 插件开发脚手架。
/// 用法：
///   fpsdk new &lt;name&gt; [--path &lt;dir&gt;] [--repo|--sdk] [--id &lt;id&gt;] [--version &lt;v&gt;]
///   --repo（默认）：位于 fptp 仓库内，csproj 引本地 FpSDK 项目。
///   --sdk      ：外部模式，csproj 引 NuGet 包 FpSDK。
///   --id       ：模块 Id（默认取 name 小写）。--version 默认 1.0.0.0。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }
            if (args[0] == "--version")
            {
                Console.WriteLine("fpsdk 1.0.0");
                return 0;
            }
            if (args[0] != "new")
            {
                Console.Error.WriteLine($"未知命令：{args[0]}（支持：new）");
                return 1;
            }
            return RunNew(args.Skip(1).ToArray());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            return 2;
        }
    }

    private static int RunNew(string[] args)
    {
        string? name = null, path = null, id = null, version = null;
        bool repo = true;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--path" when i + 1 < args.Length: path = args[++i]; break;
                case "--id" when i + 1 < args.Length: id = args[++i]; break;
                case "--version" when i + 1 < args.Length: version = args[++i]; break;
                case "--repo": repo = true; break;
                case "--sdk": repo = false; break;
                case "-h" or "--help": PrintNewHelp(); return 0;
                default:
                    if (name is null && !args[i].StartsWith('-'))
                        name = args[i];
                    else
                    {
                        Console.Error.WriteLine($"未知参数：{args[i]}");
                        return 1;
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("缺少模块名：fpsdk new <name>");
            return 1;
        }

        string destDir = path ?? Path.Combine(Environment.CurrentDirectory, name);
        string templateRoot = Path.Combine(AppContext.BaseDirectory, "templates", "dotnet-module");

        var options = new ProjectGenerator.Options(
            Name: name,
            Id: id ?? NormalizeId(name),
            DisplayName: name,
            Version: version ?? "1.0.0.0",
            RepoReference: repo);

        List<string> files = ProjectGenerator.Generate(templateRoot, destDir, options);

        Console.WriteLine($"已生成插件项目 {name} → {destDir}");
        foreach (string f in files)
            Console.WriteLine($"  {f}");
        Console.WriteLine();
        Console.WriteLine("下一步：");
        Console.WriteLine($"  cd {destDir}");
        Console.WriteLine("  dotnet build  # 构建（确保宿主 FpSDK/仓库可见）");
        if (repo)
            Console.WriteLine("  提示：模块放置在 fptp 仓库 plugins/ 下时，宿主启动即自动扫描加载。");

        return 0;
    }

    /// <summary>模块 Id 规范化：小写、空白转点（如 "My Module" → "my.module"）。</summary>
    private static string NormalizeId(string name)
    {
        var sb = new StringBuilder();
        bool lastSep = false;
        foreach (char c in name.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (sb.Length > 0 && !lastSep) { sb.Append('.'); lastSep = true; }
            }
            else
            {
                sb.Append(char.ToLowerInvariant(c));
                lastSep = false;
            }
        }
        return sb.ToString();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("fpsdk —— fptp(Osiris) 插件开发脚手架");
        Console.WriteLine();
        Console.WriteLine("用法：fpsdk new <name> [选项]");
        Console.WriteLine();
        Console.WriteLine("选项：");
        Console.WriteLine("  --path <dir>     生成目录（默认 当前目录/<name>）");
        Console.WriteLine("  --repo           仓库内模式：引本地 FpSDK 项目（默认）");
        Console.WriteLine("  --sdk            外部模式：引 NuGet 包 FpSDK");
        Console.WriteLine("  --id <id>        模块 Id（默认 name 小写化）");
        Console.WriteLine("  --version <v>    模块版本（默认 1.0.0.0）");
    }

    private static void PrintNewHelp()
    {
        Console.WriteLine("用法：fpsdk new <name> [--path <dir>] [--repo|--sdk] [--id <id>] [--version <v>]");
    }
}