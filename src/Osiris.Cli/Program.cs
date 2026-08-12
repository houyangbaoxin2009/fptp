using Osiris.Abstractions.Cli;
using Osiris.Abstractions.Localization;
using Osiris.Core.Localization;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Osiris.Cli;

/// <summary>
/// Osiris 命令行宿主（模块化 CLI，架构 4.9 / 8.1 / 10 节）：
/// 与 GUI 同为"模块宿主"——使用同一个 ModuleRegistry（%APPDATA%/Fptp 下
/// modules.json / settings.json / secure.json 与 GUI 同路径，跨进程共享模块状态与安全设置），
/// 同一权限模型（Standard 不可禁用/卸载、Security 设置只读、MinHostVersion 校验由 ModuleLoader 完成）。
/// 差异点：CLI 无文档/无 UI（ActiveDocument=null、Ui=null，模块据此跳过 UI 注册）；
/// CLI 不静态引用 CoreModule 标准模块（保持轻量），只经 ALC 加载扩展模块；
/// 扩展模块贡献的 ICliCommandProvider.Commands 动态挂载为根命令的子命令（batch 等）。
/// </summary>
internal static class Program
{
    /// <summary>入口：加载模块 → 收集命令 → 挂载子命令 → 解析执行 → 返回进程退出码。</summary>
    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            // 顶层兜底：任何未捕获异常都以错误退出码结束（错误信息打印到 stderr）
            Console.Error.WriteLine($"osiris: 未处理的异常: {ex.Message}");
            return 1;
        }
    }

    /// <summary>宿主启动流程（与 GUI 启动流程同构：注册表 → 宿主上下文 → 加载 → 收集命令）。</summary>
    private static int Run(string[] args)
    {
        // 1) 配置存储 + 模块注册表：与 GUI 完全同一路径（modules.json/settings.json/secure.json），
        //    因此 CLI 读到的模块启用/禁用状态、用户配置、安全设置都与 GUI 一致——共享状态，CLI 无特权。
        var store = new JsonConfigStore();
        var registry = new ModuleRegistry(
            CliEnvironment.ModulesPath, CliEnvironment.SettingsPath, CliEnvironment.SecurePath, store);

        // 1.5) 本地化：与 GUI 共享同一语言配置（osiris.core.language，默认 zh-cn），
        //      加载语言包并注入 L10n 门面——CLI 帮助/错误文本与 GUI 同语言。
        var localization = new JsonLocalizationService();
        localization.LoadLanguage(registry.GetConfig("osiris.core", "language", "zh-cn") ?? "zh-cn");
        L10n.SetService(localization);

        // 2) CLI 宿主上下文：预注册 IModuleRegistry / IModuleUpdater，控制台进度，Ui=null
        var context = new CliHostContext(registry);

        // 3) 加载扩展模块：程序集旁 plugins/ + 用户 %APPDATA%/Fptp/modules/（与 GUI 扫描的同一批目录）。
        //    CLI 不加载 Standard（CoreModule 未引用）——kind=standard 清单亦经 LoadFromDirectory 登记进
        //    注册表，只是不参与 CLI 命令收集（catalog 只收 Extension）。
        int loaded = LoadExtensionModules(registry, context);

        // 4) 收集全部模块贡献的 CLI 子命令（禁用/卸载模块自动跳过）
        var catalog = new CliCommandCatalog(registry);
        IReadOnlyList<CliCommandDescriptor> commands = catalog.Collect(
            (name, ex) => Console.Error.WriteLine($"osiris: 收集 CLI 命令失败 [{name}]: {ex.Message}"));

        // 5) 无模块可加载 → 打印错误；有模块但均未贡献命令 → 打印帮助
        if (loaded == 0)
        {
            Console.Error.WriteLine("osiris: 未找到任何可加载的扩展模块。");
            Console.Error.WriteLine($"  已扫描目录: {CliEnvironment.ExtensionDirectory}");
            Console.Error.WriteLine($"              {CliEnvironment.UserModuleDirectory}");
            return 1;
        }
        if (commands.Count == 0)
        {
            Console.Error.WriteLine($"osiris: 已加载 {loaded} 个模块，但均未贡献 CLI 子命令。");
            Console.Error.WriteLine("用法: osiris <module-command> [options]");
            return 1;
        }

        // 6) 装配根命令并挂载全部子命令（重复子命令名取第一个并告警——System.CommandLine 禁止同名）
        var root = new RootCommand("Osiris 命令行宿主（模块化）：子命令由已加载的扩展模块动态贡献。");
        var mounted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CliCommandDescriptor command in commands)
        {
            if (!mounted.Add(command.Name))
            {
                Console.Error.WriteLine($"osiris: 忽略重复的子命令名: {command.Name}");
                continue;
            }
            root.Add(CliCommandMounter.Mount(command));
        }

        // 用户不带子命令直接运行根命令 → 打印可用子命令列表（替代 System.CommandLine 默认帮助）
        root.SetAction(_ => PrintAvailableCommands(root));

        // 7) 解析并执行：System.CommandLine 自动处理 --help / 解析错误（打印错误+帮助，退出码 1）
        ParseResult parseResult = root.Parse(args);
        return parseResult.Invoke();
    }

    /// <summary>加载两个扩展模块目录（统一走 Core ModuleLoader，逐模块失败不中断整体）。</summary>
    private static int LoadExtensionModules(ModuleRegistry registry, CliHostContext context)
    {
        // 与 GUI 相同的两个扩展模块目录；目录不存在时 LoadFromDirectory 返回 0（幂等）。
        // 禁用/卸载/MinHostVersion/ALC 卸载纪律全部由 ModuleLoader 统一处理——CLI 无特权差异。
        string[] directories = [CliEnvironment.ExtensionDirectory, CliEnvironment.UserModuleDirectory];
        int loaded = 0;
        foreach (string directory in directories)
        {
            int count = ModuleLoader.LoadFromDirectory(directory, registry, context,
                (name, ex) => Console.Error.WriteLine($"osiris: 模块加载失败 [{name}]: {ex.Message}"));
            loaded += count;
        }
        return loaded;
    }

    /// <summary>根命令无子命令参数时打印可用子命令列表（任务要求的 "osiris &lt;module-command&gt;" 帮助面）。</summary>
    private static int PrintAvailableCommands(RootCommand root)
    {
        Console.WriteLine("osiris <module-command> [options]");
        Console.WriteLine();
        Console.WriteLine("可用子命令（由已加载的扩展模块动态贡献）：");
        foreach (Command sub in root.Subcommands)
            Console.WriteLine($"  {sub.Name,-20} {sub.Description}");
        return 0;
    }
}
