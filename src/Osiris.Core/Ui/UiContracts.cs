using System;
using System.Collections.Generic;

namespace Osiris.Core.Ui
{
    /// <summary>命令：UI 元素只绑定命令 Id，不直接调逻辑（命令总线）。</summary>
    public interface ICommand
    {
        string Id { get; }
        string DisplayName { get; }
        /// <summary>当前是否可执行（UI 据此灰显）。</summary>
        bool CanExecute(object parameter);
        /// <summary>执行命令。</summary>
        void Execute(object parameter);
    }

    /// <summary>菜单项贡献：模组注册到壳，壳映射为 WinForms MenuStrip 项。</summary>
    public sealed class MenuContribution
    {
        /// <summary>菜单位置路径，如 "文件/打开"。壳按路径建树。</summary>
        public string Path { get; }
        public string CommandId { get; }
        /// <summary>快捷键文本（壳解析），如 "Ctrl+O"，可为 null。</summary>
        public string ShortcutText { get; }
        /// <summary>排序权重，越小越靠前。</summary>
        public int Order { get; }

        public MenuContribution(string path, string commandId, string shortcutText = null, int order = 100)
        {
            Path = path;
            CommandId = commandId;
            ShortcutText = shortcutText;
            Order = order;
        }
    }

    /// <summary>工具栏按钮贡献。</summary>
    public sealed class ToolbarContribution
    {
        public string CommandId { get; }
        public string IconKey { get; }
        public int Order { get; }

        public ToolbarContribution(string commandId, string iconKey = null, int order = 100)
        {
            CommandId = commandId;
            IconKey = iconKey;
            Order = order;
        }
    }

    /// <summary>停靠面板内容工厂：模组返回任意内容（壳映射为控件）。</summary>
    public sealed class PanelContribution
    {
        public string Id { get; }
        public string Title { get; }
        /// <summary>面板位置：Left/Right/Bottom。</summary>
        public PanelSide Side { get; }
        /// <summary>面板内容工厂：壳调用创建内容控件。</summary>
        public Func<object> ContentFactory { get; }
        public int Order { get; }

        public PanelContribution(string id, string title, PanelSide side,
                                 Func<object> contentFactory, int order = 100)
        {
            Id = id;
            Title = title;
            Side = side;
            ContentFactory = contentFactory;
            Order = order;
        }
    }

    /// <summary>
    /// 列表面板内容（纯数据契约，不泄漏 WinForms）：历史面板等以列表展示的停靠面板。
    /// 壳映射为列表控件；模组经 Items 提供数据、Changed 通知刷新、SelectedIndexChanged 感知选中。
    /// </summary>
    public sealed class ListPanelContent
    {
        /// <summary>列表项数据（每次刷新时壳调用）。</summary>
        public Func<IReadOnlyList<string>> Items { get; set; }
        /// <summary>当前选中索引（-1 = 无选中），点击跳转后壳回写。</summary>
        public int SelectedIndex { get; set; } = -1;
        /// <summary>选中变化回调（壳触发，模组据此跳转历史等）。</summary>
        public Action<int> SelectedIndexChanged { get; set; }
        /// <summary>数据变化通知（壳刷新列表）。</summary>
        public event Action Changed;

        public void NotifyChanged() => Changed?.Invoke();
    }

    public enum PanelSide
    {
        Left,
        Right,
        Bottom
    }

    /// <summary>壳与模组共享的预定义命令 Id。</summary>
    public static class KnownCommands
    {
        /// <summary>打开图片文档（壳实现，菜单由模组贡献）。</summary>
        public const string OpenDocument = "workbench.openDocument";
        /// <summary>撤销（壳实现，操作当前文档历史）。</summary>
        public const string Undo = "workbench.undo";
        /// <summary>重做（壳实现，操作当前文档历史）。</summary>
        public const string Redo = "workbench.redo";
    }

    /// <summary>UI 服务：模组在 Initialize 时通过此接口贡献 UI 资源。</summary>
    public interface IUiService
    {
        /// <summary>注册命令。</summary>
        void RegisterCommand(ICommand command);
        /// <summary>贡献菜单项。</summary>
        void AddMenu(MenuContribution contribution);
        /// <summary>贡献工具栏按钮。</summary>
        void AddToolbar(ToolbarContribution contribution);
        /// <summary>贡献停靠面板。</summary>
        void AddPanel(PanelContribution contribution);
        /// <summary>
        /// 激活交互工具（套索/画笔等）：壳只路由鼠标事件与覆盖层，工具由模组自持实例。
        /// 传入 null 取消激活。
        /// </summary>
        void ActivateTool(Plugins.IEditorTool tool);
    }
}
