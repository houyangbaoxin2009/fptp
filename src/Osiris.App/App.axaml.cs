using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;
using Osiris.Core.Localization;
using Osiris.Core.Plugins;
using Osiris.Core.Security;
using Osiris.Core.Storage;
using Osiris.CoreModule.Services;
// CoreModule 类与其所在命名空间 Osiris.CoreModule 同名：简单名会被 Osiris 命名空间下的子命名空间遮蔽，
// 故用别名（别名标识符不与任何命名空间冲突）。
using CoreModuleType = Osiris.CoreModule.CoreModule;
using Osiris.Engine.Skia;
using Osiris.App.PluginHost;
using Osiris.App.ViewModels;
using Osiris.App.Views;

namespace Osiris.App;

/// <summary>
/// 应用根：模块运行时组装。
/// 壳只做：构建模块注册表（与 CLI 共享 %APPDATA%/Fptp）→ 加载标准模块（CoreModule 静态）→
/// 加载扩展模块（ALC）→ 把模块贡献装配到空工作台。
/// </summary>
public partial class App : Application
{
    // 工作台装配引用（外部模块异步确认加载后 RefreshWorkbench 复用；生命周期=应用）
    private ModuleRegistry? _registry;
    private ToolHostService? _toolHost;
    private MainWindowViewModel? _viewModel;
    private AppUiService? _ui;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 0. 权限检测：管理员权限下运行插件（ALC 任意代码）可破坏系统。
            //    弹警告让用户知情决策（降权重启 / 仍要继续）——恶意提权注入场景下用户至少看到警告。
            bool elevated = ElevationGuard.IsElevated();
            if (elevated)
                ShowElevationWarning(desktop); // 非阻塞：选择"降权重启"则启动普通权限实例并退出当前

            // 1. 模块基础设施：注册表 + 存储（与 CLI 共享同一注册表/配置/安全设置路径）
            var store = new JsonConfigStore();
            var registry = new ModuleRegistry(AppPaths.ModulesPath, AppPaths.SettingsPath, AppPaths.SecurePath, store);
            ModuleKind? currentKind = null; // 模块加载期间的调用方 Kind（无越权）
            var updater = new ModuleUpdater(registry, AppPaths.GetPluginDirectories(), () => currentKind);

            // 2. 宿主服务注册
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            services.Register<IModuleUpdater>(updater);
            // 模块签名校验（哈希白名单防篡改）：内置名单 trusted-modules.json（随产品分发，构建后生成）
            // ∪ 用户名单（%APPDATA%/Fptp/trusted-modules.json，外部模块确认加载后写入）。
            // 无内置名单（开发模式）降级放行；外部模块哈希未信任 → 拒绝加载。
            var trustStore = new ModuleTrustStore();
            services.Register<IModuleSignatureValidator>(new TrustedModuleSignatureValidator(trustStore));
            services.Register<ModuleTrustStore>(trustStore);

            // ---- 本地化：语言包服务（语言 id 为 BCP-47 小写形式，如 zh-cn / en-us）----
            // 从注册表读取界面语言配置（osiris.core.language，默认 zh-cn），加载语言包并注入静态门面 L10n，
            // 使模块/视图/命令任意处经 L10n.T("中文原文") 获取翻译；未命中返回原文，零破坏。
            var localization = new JsonLocalizationService();
            string language = registry.GetConfig("osiris.core", "language", "zh-cn") ?? "zh-cn";
            localization.LoadLanguage(language);
            services.Register<ILocalizationService>(localization);
            L10n.SetService(localization);

            services.Register<Func<string, PixelSurface?>>(SkiaCodec.Decode);
            services.Register<Func<string, PixelSurface, bool>>(SaveByExtension);
            // 滤镜解析器（CoreModule batch 命令与 fptp.filters 滤镜窗口共用）：
            // 从注册表已实例化模块收集全部 IFilterPlugin 的滤镜，按 Id 匹配（大小写不敏感）。
            services.Register<Func<string, IFilterProcessor?>>(id => registry.GetInstances()
                .OfType<IFilterPlugin>()
                .SelectMany(m => m.Filters)
                .FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)));
            // 全部滤镜枚举（滤镜窗口列表数据源）。
            services.Register<Func<IReadOnlyList<IFilterProcessor>>>(() => registry.GetInstances()
                .OfType<IFilterPlugin>()
                .SelectMany(m => m.Filters)
                .ToList());

            // 3. 工作台装配（壳零业务 UI）
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);
            var viewModel = new MainWindowViewModel(registry, services);
            var window = new MainWindow(viewModel);

            // 工具宿主：聚合模块工具（IToolPlugin）并路由到画布（工具窗口点击即切换当前工具）
            var toolHost = new ToolHostService(() => viewModel.CanvasViewModel);
            services.Register<IToolHostService>(toolHost);

            // 保存工作台引用（外部模块异步确认加载后 RefreshWorkbench 复用）
            _registry = registry;
            _toolHost = toolHost;
            _viewModel = viewModel;
            _ui = ui;

            // 壳级入口：模块管理 + 设置（壳的职责——模块运行时管理；不属任何模块）
            ui.RegisterCommand(new ShellCommand("osiris.shell.moduleManager", "模块管理(&M)...",
                () => viewModel.OpenModuleManagerCommand.Execute(null)));
            ui.RegisterCommand(new ShellCommand("osiris.shell.settings", "设置(&S)...",
                () => viewModel.OpenSettingsCommand.Execute(null)));
            ui.AddMenu("工具/模块管理", "osiris.shell.moduleManager", 9998);
            ui.AddMenu("工具/设置", "osiris.shell.settings", 9999);

            // 4. 加载标准模块（随产品分发，静态引用；登记 Standard 记录）
            var core = new CoreModuleType();
            core.Initialize(host);
            registry.Register(new ModuleRecord("osiris.core", "核心模块", "1.0.0",
                ModuleKind.Standard, ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, null, null));

            // 主窗口先显示（空工作台），外部模块确认后异步加载再装配——保证确认弹窗有窗口宿主
            desktop.MainWindow = window;

            // 5. 加载扩展模块（可卸载 ALC；禁用/Removed/版本不符自动跳过）：
            //    内置目录（程序集旁 plugins/，随产品分发=可信）同步加载；
            //    外部目录（%APPDATA%/Fptp/modules/，用户手动安装=不可信来源）弹确认后异步加载。
            foreach (var dir in AppPaths.GetPluginDirectories())
            {
                if (IsExternalModuleDir(dir))
                {
                    // 外部模块：确认后加载（不阻塞主流程；拒绝则跳过该目录全部模块）
                    _ = LoadExternalModulesAsync(dir, registry, host, window, RefreshWorkbench);
                    continue;
                }
                ModuleLoader.LoadFromDirectory(dir, registry, host,
                    (name, ex) => Console.Error.WriteLine("模块加载失败 {0}: {1}", name, ex.Message));
            }

            // 6. 装配 UI（内置模块 + 标准模块）：菜单/工具栏/面板/画布/状态栏
            RefreshWorkbench();
            // 语言切换：重新装配工作台（命令 DisplayName / 菜单路径 / 面板标题经 L10n 惰性翻译，Rebuild 即刷新）
            localization.LanguageChanged += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                RefreshWorkbench();
                window.Title = L10n.T("Osiris 1.0.0 - 模块化图像工作台"); // 主窗口标题同步刷新
            });
            // 进度回调用 Dispatcher 回 UI 线程更新状态栏（模块可能在工作线程上报）
            host.Status.Changed += (percent, message) =>
                Dispatcher.UIThread.Post(() => viewModel.StatusText = $"{message} ({percent:0}%)");
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>收集全部已加载模块的交互工具到工具宿主，并重装配工作台（幂等：外部模块确认加载后再次调用）。</summary>
    private void RefreshWorkbench()
    {
        // 字段仅在 OnFrameworkInitializationCompleted 内赋值后调用，此处非空
        ModuleRegistry registry = _registry!;
        ToolHostService toolHost = _toolHost!;
        MainWindowViewModel viewModel = _viewModel!;
        AppUiService ui = _ui!;

        foreach (var module in registry.GetInstances().OfType<IToolPlugin>())
            toolHost.RegisterModule(module);
        viewModel.Rebuild(ui);
    }

    /// <summary>是否为外部模块目录（%APPDATA%/Fptp/modules/，用户手动安装的不可信来源）。</summary>
    private static bool IsExternalModuleDir(string dir)
        => string.Equals(
            Path.GetFullPath(dir),
            Path.GetFullPath(Path.Combine(AppPaths.AppDataDir, "modules")),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 外部模块加载：弹确认框（列出模块清单）→ 用户确认后把模块哈希写入信任名单 →
    /// 后台加载 → 回 UI 线程重装配。用户拒绝 → 跳过该目录全部模块。
    /// 设计：外部来源模块可能是恶意代码，加载前必须知情确认；确认后信任名单持久化，下次启动自动通过。
    /// </summary>
    private static async Task LoadExternalModulesAsync(
        string dir, ModuleRegistry registry, IHostContext host, Window owner, Action refresh)
    {
        // 枚举目录下模块清单（id/name），供确认框展示
        var modules = ModuleLoader.EnumerateManifests(dir);
        if (modules.Count == 0)
            return; // 无模块可加载，无需确认

        bool approved = await ConfirmExternalModulesAsync(owner, modules);
        if (!approved)
            return;

        // 确认加载 → 记录信任（主 DLL 哈希写入用户信任名单，后续启动自动通过校验）
        var trustStore = host.Services.Get<ModuleTrustStore>();
        if (trustStore is not null)
        {
            foreach ((string id, _) in modules)
            {
                string? entry = ModuleLoader.ReadEntryPoint(dir);
                string? hash = entry is null ? null : Osiris.Core.Security.HashUtil.Sha256File(Path.Combine(dir, entry));
                if (hash is not null)
                    trustStore.Trust(id, hash);
            }
        }

        // 后台加载（模块 Initialize 可能耗时；UI 线程不阻塞）
        await Task.Run(() => ModuleLoader.LoadFromDirectory(dir, registry, host,
            (name, ex) => Console.Error.WriteLine("模块加载失败 {0}: {1}", name, ex.Message)));
        Dispatcher.UIThread.Post(() => refresh());
    }

    /// <summary>外部模块确认对话框：展示模块清单，返回用户是否同意加载。</summary>
    private static Task<bool> ConfirmExternalModulesAsync(Window owner, IReadOnlyList<(string Id, string Name)> modules)
    {
        var dlg = new Window
        {
            Title = L10n.T("外部模块确认"),
            Width = 520,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = L10n.T("检测到外部安装的模块（可能来自不可信来源）。\n恶意模块是可执行代码，可窃取文件或破坏系统。\n请确认以下模块来源可信后才允许加载："),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 13,
        });

        var list = new ListBox { Height = 120, Margin = new Thickness(0, 4) };
        foreach ((string id, string name) in modules)
            list.Items.Add(new TextBlock { Text = $"{name}（{id}）" });
        panel.Children.Add(list);

        var approve = new Button { Content = L10n.T("确认加载"), MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Right };
        var reject = new Button { Content = L10n.T("拒绝加载"), MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Right };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(approve);
        row.Children.Add(reject);
        panel.Children.Add(row);
        dlg.Content = panel;

        var tcs = new TaskCompletionSource<bool>();
        approve.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        reject.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(false); // 关闭窗口视为拒绝
        dlg.Show(owner);
        return tcs.Task;
    }

    /// <summary>按扩展名保存图片（.jpg/.jpeg → JPEG，其余 → PNG）。</summary>
    private static bool SaveByExtension(string path, PixelSurface surface)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".jpg" or ".jpeg")
        {
            SkiaCodec.EncodeJpeg(surface, path);
            return true;
        }
        if (ext is ".png" or "")
        {
            SkiaCodec.EncodePng(surface, path);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 管理员权限警告：非阻塞弹出提示窗（"以普通权限重新启动" / "仍要继续"）。
    /// 选择降权重启：经 explorer.exe 启动普通权限实例（explorer 令牌非提权），随后退出当前提权进程。
    /// 设计：知情同意而非强制拒绝——用户可能有合法管理员需求；恶意提权注入场景下警告仍可见。
    /// </summary>
    private static void ShowElevationWarning(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var dlg = new Window
        {
            Title = L10n.T("权限警告"),
            Width = 480,
            Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        panel.Children.Add(new TextBlock
        {
            Text = L10n.T("检测到以管理员权限运行。\n插件（模块）是可执行代码，管理员权限下恶意插件可修改系统文件、安装驱动、破坏系统安全。\n建议以普通权限运行本程序。"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 13,
        });
        var restart = new Button
        {
            Content = L10n.T("以普通权限重新启动"),
            MinWidth = 160,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        restart.Click += (_, _) =>
        {
            // 经 explorer 启动普通权限实例（explorer 以用户令牌运行，不继承本进程提权令牌）
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{Environment.ProcessPath}\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"降权重启失败：{ex.Message}");
            }
            desktop.Shutdown(); // 关闭当前提权实例
        };
        var continueBtn = new Button
        {
            Content = L10n.T("仍要继续"),
            MinWidth = 120,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        continueBtn.Click += (_, _) => dlg.Close();

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(restart);
        row.Children.Add(continueBtn);
        panel.Children.Add(row);
        dlg.Content = panel;

        if (desktop.MainWindow is { } owner)
            dlg.Show(owner);
        else
            dlg.Show();
    }
}

/// <summary>
/// 壳级命令：lambda 包装（模块管理/设置等壳自身入口，非模块贡献）。
/// 实现 Abstractions 命令契约，供工作台菜单绑定。
/// DisplayName 惰性翻译：存中文原文（key），访问时经 L10n.T 取当前语言文本——语言切换后 Rebuild 即时刷新。
/// </summary>
internal sealed class ShellCommand : Osiris.Abstractions.Ui.ICommand
{
    private readonly string _id;
    private readonly string _displayNameKey;
    private readonly Action _action;

    public ShellCommand(string id, string displayNameKey, Action action)
    {
        _id = id;
        _displayNameKey = displayNameKey;
        _action = action;
    }

    public string Id => _id;

    public string DisplayName => Osiris.Abstractions.Localization.L10n.T(_displayNameKey);

    public void Execute(object? parameter) => _action();
}


