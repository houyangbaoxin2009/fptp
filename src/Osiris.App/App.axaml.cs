using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;
using Osiris.Core.Localization;
using Osiris.Core.Plugins;
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
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 模块基础设施：注册表 + 存储（与 CLI 共享同一注册表/配置/安全设置路径）
            var store = new JsonConfigStore();
            var registry = new ModuleRegistry(AppPaths.ModulesPath, AppPaths.SettingsPath, AppPaths.SecurePath, store);
            ModuleKind? currentKind = null; // 模块加载期间的调用方 Kind（无越权）
            var updater = new ModuleUpdater(registry, AppPaths.GetPluginDirectories(), () => currentKind);

            // 2. 宿主服务注册
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            services.Register<IModuleUpdater>(updater);

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

            // 5. 加载扩展模块（可卸载 ALC；禁用/Removed/版本不符自动跳过）
            foreach (var dir in AppPaths.GetPluginDirectories())
            {
                currentKind = null; // 加载期无特权调用
                ModuleLoader.LoadFromDirectory(dir, registry, host,
                    (name, ex) => Console.Error.WriteLine("模块加载失败 {0}: {1}", name, ex.Message));
                currentKind = null;
            }

            // 模块全部加载后：收集各模块贡献的交互工具（IToolPlugin）到工具宿主
            foreach (var module in registry.GetInstances().OfType<IToolPlugin>())
                toolHost.RegisterModule(module);

            // 6. 装配 UI：菜单/工具栏/面板/画布/状态栏
            viewModel.Rebuild(ui);
            // 语言切换：重新装配工作台（命令 DisplayName / 菜单路径 / 面板标题经 L10n 惰性翻译，Rebuild 即刷新）
            localization.LanguageChanged += (_, _) => Dispatcher.UIThread.Post(() => viewModel.Rebuild(ui));
            // 进度回调用 Dispatcher 回 UI 线程更新状态栏（模块可能在工作线程上报）
            host.Status.Changed += (percent, message) =>
                Dispatcher.UIThread.Post(() => viewModel.StatusText = $"{message} ({percent:0}%)");

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
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


