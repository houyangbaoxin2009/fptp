using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Dock.Model.Core;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;
using Osiris.App.PluginHost;
using Osiris.App.ViewModels;
using Osiris.App.Views;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Osiris.CoreModule;
using Osiris.CoreModule.Controls;
using Osiris.CoreModule.ViewModels;
using CoreModuleType = Osiris.CoreModule.CoreModule;

namespace Osiris.App.Tests;

/// <summary>
/// 工作台冒烟测试：复刻 App 组合根逻辑（标准模块 → 扩展模块 → Rebuild），
/// 验证壳把模块贡献装配为菜单树/工具栏/面板/画布，并确认 Menu 容器 Style 方案真实生成菜单项。
/// </summary>
public class WorkbenchSmokeTests
{
    /// <summary>构造隔离的注册表（临时目录，避免污染 %APPDATA%/Fptp）。</summary>
    private static ModuleRegistry CreateTestRegistry(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), "osiris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var store = new JsonConfigStore();
        return new ModuleRegistry(
            Path.Combine(dir, "modules.json"),
            Path.Combine(dir, "settings.json"),
            Path.Combine(dir, "secure.json"),
            store);
    }

    [AvaloniaFact]
    public void Workbench_AssemblesCoreModuleContributions()
    {
        var registry = CreateTestRegistry(out string dir);
        try
        {
            // 宿主服务 + UI 收集 + 宿主上下文（与 App.axaml.cs 同构）
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);

            // 标准模块（CoreModule）：注册文档服务/画布/命令/菜单/图层面板
            var core = new CoreModuleType();
            core.Initialize(host);
            registry.Register(new ModuleRecord("osiris.core", "核心模块", "1.0.0",
                ModuleKind.Standard, ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, null, null));

            // 工作台装配
            var vm = new MainWindowViewModel(registry, services);
            vm.Rebuild(ui);

            // ---- VM 层断言 ----
            Assert.Equal(3, vm.Menus.Count);                    // 文件 / 编辑 / 视图
            Assert.Equal("文件", vm.Menus[0].Title);
            Assert.Equal(3, vm.Menus[0].Children.Count);        // 打开 / 保存 / 导出
            Assert.Equal(2, vm.Menus[1].Children.Count);        // 撤销 / 重做
            Assert.Equal(2, vm.Menus[2].Children.Count);        // 缩放适应 / 实际大小
            Assert.NotNull(ui.Canvas);                          // CoreModule SetCanvas 贡献（画布状态 VM）
            Assert.IsType<CanvasDocumentViewModel>(ui.Canvas);
            Assert.Equal(ui.Canvas, vm.CanvasContent);
            Assert.Single(vm.RightPanels);                      // "图层"面板（默认右侧）
            Assert.Empty(vm.ToolbarItems);                      // CoreModule 未贡献工具栏

            // ---- Dock 停靠布局断言：画布注入中央文档、模块面板注入右侧停靠区 ----
            Assert.NotNull(vm.DockFactory.Layout);
            Assert.Equal(ui.Canvas, FindDockable(vm.DockFactory.Layout, "osiris.canvas")?.Context);
            Assert.NotNull(FindDockable(vm.DockFactory.Layout, "panel.图层"));

            // ---- 窗口渲染断言（Menu 容器 Style 方案是否真实生成菜单项）----
            var win = new MainWindow(vm);
            win.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs(); // 冲刷 Dock 延迟内容物化队列
            var menu = win.GetVisualDescendants().OfType<Menu>().FirstOrDefault();
            Assert.NotNull(menu);
            Assert.Equal(3, menu.Items.Count);                  // 顶层数据项已绑定
            // Items 集合装的是数据项（MenuNodeViewModel），实际容器 MenuItem 在视觉树中生成
            var menuItems = menu.GetVisualDescendants().OfType<MenuItem>().ToList();
            Assert.Equal(3, menuItems.Count);                   // Style Selector="MenuItem" 容器已生成
            Assert.Equal("文件", menuItems[0].Header);          // Style Setter 的 Header 绑定已生效
            Assert.Equal("编辑", menuItems[1].Header);
            Assert.Equal("视图", menuItems[2].Header);

            // DockControl 已装配且画布经停靠文档渲染进视觉树（CanvasControl 由模板生成并绑定 VM）
            Assert.NotNull(win.GetVisualDescendants().OfType<Dock.Avalonia.Controls.DockControl>().FirstOrDefault());
            var canvasControl = win.GetVisualDescendants().OfType<CanvasControl>().FirstOrDefault();
            Assert.NotNull(canvasControl);
            Assert.Same(ui.Canvas, canvasControl.ViewModel);     // 模板生成的控件绑定同一画布 VM
            win.Close();
        }
        finally
        {
            // 清理临时注册表目录
            try { Directory.Delete(dir, recursive: true); } catch { /* 忽略清理失败 */ }
        }
    }

    [AvaloniaFact]
    public void AppHostContext_ResolvesActiveDocument_AfterCoreModuleLoads()
    {
        var registry = CreateTestRegistry(out string dir);
        try
        {
            var services = new ServiceRegistry();
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);

            // 加载前：无 DocumentService，ActiveDocument 为 null
            Assert.Null(host.ActiveDocument);

            var core = new CoreModuleType();
            core.Initialize(host);

            // 加载后：DocumentService 经注册表懒解析（同一实例）
            var documents = services.Get<Osiris.Core.Document.DocumentService>();
            Assert.NotNull(documents);
            Assert.Null(host.ActiveDocument); // 尚未打开文档
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    [AvaloniaFact]
    public void DockToolWindows_OpenAndClose_AsDockableTools()
    {
        var registry = CreateTestRegistry(out string dir);
        try
        {
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            var vm = new MainWindowViewModel(registry, services);

            // 打开模块管理 → 作为 Dock 工具加入布局（可拖拽/浮动）
            vm.OpenModuleManagerCommand.Execute(null);
            var mgr = FindDockable(vm.DockFactory.Layout!, "moduleManager");
            Assert.NotNull(mgr);
            Assert.Equal("模块管理", mgr!.Title);
            Assert.NotNull(mgr.Context); // ModuleManagerView 已注入

            // 设置改独立窗口：不再注册为 Dock 工具（不得停靠工作区）
            vm.OpenSettingsCommand.Execute(null);
            Assert.Null(FindDockable(vm.DockFactory.Layout!, "settings"));

            // 重复打开不重建（激活既有）
            vm.OpenModuleManagerCommand.Execute(null);
            Assert.Same(mgr, FindDockable(vm.DockFactory.Layout!, "moduleManager"));

            // 关闭 → 从布局移除
            vm.CloseToolWindow("moduleManager");
            Assert.Null(FindDockable(vm.DockFactory.Layout!, "moduleManager"));

            // 再次打开 → 重建新窗口
            vm.OpenModuleManagerCommand.Execute(null);
            var mgr2 = FindDockable(vm.DockFactory.Layout!, "moduleManager");
            Assert.NotNull(mgr2);
            Assert.NotSame(mgr, mgr2);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// 画布浮动崩溃回归测试：Dock 浮动/移动画布时模板每次生成新的 CanvasControl 并绑定同一 VM。
    /// 同一 VM 生成两个 CanvasControl 各自挂载到不同父级（模拟浮动窗口 + 原停靠区共存），
    /// 不得出现 "CanvasControl already has a visual parent" 双父级异常。
    /// </summary>
    [AvaloniaFact]
    public void CanvasViewModel_SharedByMultipleControls_NoDoubleParentCrash()
    {
        var registry = CreateTestRegistry(out string dir);
        try
        {
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);
            var core = new CoreModuleType();
            core.Initialize(host);
            var vm = new MainWindowViewModel(registry, services);
            vm.Rebuild(ui);

            // 同一画布 VM（Dock 文档 Context）绑定两个控件实例——模拟 Dock 浮动语义：
            // 模板每次重建 CanvasControl，共用状态，各挂各的父级（无双父级）。
            var canvasVm = Assert.IsType<CanvasDocumentViewModel>(ui.Canvas);
            var w1 = new Window { Content = new CanvasControl { ViewModel = canvasVm } };
            var w2 = new Window { Content = new CanvasControl { ViewModel = canvasVm } };
            w1.Show();
            w2.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            // 两个控件都成功挂载（无异常），且共享同一状态源
            Assert.NotNull(w1.Content);
            Assert.NotNull(w2.Content);
            Assert.Same(canvasVm, ((CanvasControl)w1.Content).ViewModel);
            Assert.Same(canvasVm, ((CanvasControl)w2.Content).ViewModel);

            // 状态经 VM 共享：一个控件改视口，另一个读同一值
            canvasVm.ZoomAt(10, 10, 2.5);
            Assert.Equal(2.5, ((CanvasControl)w1.Content).Scale);
            Assert.Equal(2.5, ((CanvasControl)w2.Content).Scale);

            // 模拟浮动：把 w1 的控件摘下来（Avalonia 会先移除旧父级）再挂到新父级——同一实例复用
            var floating = (CanvasControl)w1.Content;
            w1.Content = null;
            w1.Close();
            var w3 = new Window { Content = floating };
            w3.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.True(floating.IsAttachedToVisualTree()); // 同一实例成功迁移挂载到新父级（无双父级异常）
            w2.Close();
            w3.Close();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    /// <summary>在停靠布局树中按 Id 查找 dockable（递归遍历 VisibleDockables）。</summary>
    /// <summary>仓库 plugins/bin 定位（加载 fptm 扩展模块用）。</summary>
    private static string PluginsBinPath
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "plugins", "bin");
                if (File.Exists(Path.Combine(candidate, "Fptp.Plugins.Builtin.dll")))
                    return candidate;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("未找到仓库 plugins/bin 目录（需先构建 Fptp.Plugins.Builtin 项目）。");
        }
    }

    [AvaloniaFact]
    public void ToolActivation_LoadsFptmTools_ActivatesOnCanvasViewModel()
    {
        // 意图：工具激活链路（画笔失效回归）——加载扩展模块 → 注册工具到 ToolHostService →
        // ActivateTool 后画布 VM 的 ActiveTool 必须非空且为指定工具（画布事件路由依赖此链路）。
        var registry = CreateTestRegistry(out string dir);
        try
        {
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);

            var core = new CoreModuleType();
            core.Initialize(host);
            registry.Register(new ModuleRecord("osiris.core", "核心模块", "1.0.0",
                ModuleKind.Standard, ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, null, null));

            // 加载扩展模块（itool 提供 9 个工具）
            var errors = new List<string>();
            ModuleLoader.LoadFromDirectory(PluginsBinPath, registry, host,
                (name, ex) => errors.Add($"{name}: {ex.Message}"));
            Assert.Empty(errors);

            var vm = new MainWindowViewModel(registry, services);
            vm.Rebuild(ui);
            Assert.NotNull(vm.CanvasViewModel); // 画布 VM 必须存在（ActivateTool 目标）

            var toolHost = new ToolHostService(() => vm.CanvasViewModel);
            foreach (var module in registry.GetInstances().OfType<ITool>())
                toolHost.RegisterModule(module);
            Assert.Equal(9, toolHost.Tools.Count); // itool 9 个工具已注册

            // 激活铅笔 → 画布 VM.ActiveTool 必须同步
            toolHost.ActivateTool("pencil");
            Assert.NotNull(vm.CanvasViewModel!.ActiveTool);
            Assert.Equal("pencil", vm.CanvasViewModel!.ActiveTool!.Id);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    [AvaloniaFact]
    public void PencilTool_DrawsOnCanvasClick_EndToEnd()
    {
        // 意图：画笔端到端回归（"各种笔完全没用"）——真实 MainWindow + Dock 渲染 →
        // 激活铅笔 → headless 鼠标点击画布 → 断言首层像素被铅笔着色。
        var registry = CreateTestRegistry(out string dir);
        try
        {
            var services = new ServiceRegistry();
            services.Register<IModuleRegistry>(registry);
            var ui = new AppUiService();
            var host = new AppHostContext(services, ui);

            var core = new CoreModuleType();
            core.Initialize(host);
            registry.Register(new ModuleRecord("osiris.core", "核心模块", "1.0.0",
                ModuleKind.Standard, ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, null, null));

            var errors = new List<string>();
            ModuleLoader.LoadFromDirectory(PluginsBinPath, registry, host,
                (name, ex) => errors.Add($"{name}: {ex.Message}"));
            Assert.Empty(errors);

            var vm = new MainWindowViewModel(registry, services);
            var window = new MainWindow(vm);
            window.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            vm.Rebuild(ui);

            var toolHost = new ToolHostService(() => vm.CanvasViewModel);
            foreach (var module in registry.GetInstances().OfType<ITool>())
                toolHost.RegisterModule(module);
            services.Register<IToolHostService>(toolHost);
            Assert.NotNull(vm.CanvasViewModel);

            // 打开白底文档（DocumentService 由 CoreModule 注册）
            var docs = services.Get<IDocumentService>() ?? throw new InvalidOperationException("无 IDocumentService");
            docs.OpenDocument(Osiris.Abstractions.Document.PixelSurface.Create(64, 64));

            // 激活铅笔并点击画布中央
            toolHost.ActivateTool("pencil");
            var canvas = window.GetVisualDescendants().OfType<CanvasControl>().FirstOrDefault();
            Assert.NotNull(canvas);
            Assert.NotNull(canvas!.ViewModel); // 绑定必须成功（否则 ActiveTool 路由失效）
            Assert.NotNull(canvas.ViewModel!.ActiveTool);

            // 画布视口 1:1 时控件坐标 ≈ 文档像素坐标；点击中心
            double cx = canvas.Bounds.Width / 2, cy = canvas.Bounds.Height / 2;
            window.MouseDown(new Avalonia.Point(cx, cy), Avalonia.Input.MouseButton.Left);
            window.MouseMove(new Avalonia.Point(cx + 10, cy), Avalonia.Input.RawInputModifiers.None);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            // 实时反馈断言：MouseMove 后（未 MouseUp）图层已含笔迹（预览通道生效）
            var docAfterMove = docs.Document!;
            ReadOnlySpan<byte> moved = docAfterMove.Layers[0].Pixels.Row(docAfterMove.Height / 2);
            Assert.False(moved[docAfterMove.Width / 2 * 4] == 255,
                "MouseMove 后（松手前）画布应实时显示笔迹（预览通道未生效）。");

            window.MouseUp(new Avalonia.Point(cx + 10, cy), Avalonia.Input.MouseButton.Left);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            // 断言：文档首层中心像素被铅笔（黑色）着色（白色底 → 非白）
            var doc = docs.Document!;
            ReadOnlySpan<byte> row = doc.Layers[0].Pixels.Row(doc.Height / 2);
            int i = doc.Width / 2 * 4;
            Assert.False(row[i] == 255 && row[i + 1] == 255 && row[i + 2] == 255,
                "点击画布中心后像素应被铅笔着色（仍为白色说明事件未到达工具）。");
            window.Close();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private static IDockable? FindDockable(IDockable root, string id)
    {
        if (root.Id == id)
            return root;
        if (root is IDock dock && dock.VisibleDockables is not null)
        {
            foreach (var child in dock.VisibleDockables)
            {
                if (FindDockable(child, id) is { } found)
                    return found;
            }
        }
        return null;
    }
}


