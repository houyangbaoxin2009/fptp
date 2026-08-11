using System.IO;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Dock.Model.Core;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.App.PluginHost;
using Osiris.App.ViewModels;
using Osiris.App.Views;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Osiris.CoreModule;
using Osiris.CoreModule.Controls;
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
            registry.Register(new ModuleRecord("osiris.core", "核心模块", "2.1.0.0",
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
            Assert.NotNull(ui.Canvas);                          // CoreModule SetCanvas 贡献
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

            // DockControl 已装配且画布经停靠文档渲染进视觉树
            Assert.NotNull(win.GetVisualDescendants().OfType<Dock.Avalonia.Controls.DockControl>().FirstOrDefault());
            Assert.NotNull(win.GetVisualDescendants().OfType<CanvasControl>().FirstOrDefault());
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

            // 打开模块管理/设置 → 作为 Dock 工具加入布局（可拖拽/浮动）
            vm.OpenModuleManagerCommand.Execute(null);
            vm.OpenSettingsCommand.Execute(null);
            var mgr = FindDockable(vm.DockFactory.Layout!, "moduleManager");
            var settings = FindDockable(vm.DockFactory.Layout!, "settings");
            Assert.NotNull(mgr);
            Assert.NotNull(settings);
            Assert.Equal("模块管理", mgr!.Title);
            Assert.Equal("设置", settings!.Title);
            Assert.NotNull(mgr.Context);   // ModuleManagerView 已注入
            Assert.NotNull(settings.Context); // SettingsView 已注入

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

    /// <summary>在停靠布局树中按 Id 查找 dockable（递归遍历 VisibleDockables）。</summary>
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
