using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Osiris.Core.Ui;

namespace Osiris.App.Workbench
{
    /// <summary>WinForms 实现的 UI 服务：收集模组贡献，装配到壳控件。</summary>
    internal sealed class WorkbenchUiService : IUiService
    {
        private readonly WorkbenchForm _form;
        private readonly List<ICommand> _commands = new List<ICommand>();
        private readonly List<MenuContribution> _menus = new List<MenuContribution>();
        private readonly List<ToolbarContribution> _toolbars = new List<ToolbarContribution>();
        private readonly List<PanelContribution> _panels = new List<PanelContribution>();

        public WorkbenchUiService(WorkbenchForm form)
        {
            _form = form;
        }

        public void RegisterCommand(ICommand command)
        {
            lock (_commands)
            {
                _commands.RemoveAll(c => c.Id == command.Id);
                _commands.Add(command);
            }
        }

        public void AddMenu(MenuContribution contribution)
        {
            lock (_menus) { _menus.Add(contribution); }
        }

        public void AddToolbar(ToolbarContribution contribution)
        {
            lock (_toolbars) { _toolbars.Add(contribution); }
        }

        public void AddPanel(PanelContribution contribution)
        {
            lock (_panels) { _panels.Add(contribution); }
            _form.AddPanelInternal(contribution);
        }

        /// <summary>激活/取消交互工具（壳只路由鼠标事件与覆盖层）。</summary>
        public void ActivateTool(Osiris.Core.Plugins.IEditorTool tool)
        {
            _form.SetActiveTool(tool);
        }

        /// <summary>用新文档替换当前文档（排版相纸等模组生成结果）。</summary>
        public void LoadDocument(Osiris.Core.Document.OsirisDocument doc, string title)
        {
            _form.LoadDocument(doc, title);
        }

        /// <summary>把全部已注册资源装配到窗体（菜单树/工具栏/状态栏）。</summary>
        internal void ApplyTo(WorkbenchForm form)
        {
            // 菜单树
            form.MenuStrip.SuspendLayout();
            form.MenuStrip.Items.Clear();
            var sortedMenus = new List<MenuContribution>(_menus);
            sortedMenus.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var m in sortedMenus)
                BuildMenuTree(form.MenuStrip, m);
            form.MenuStrip.ResumeLayout();

            // 工具栏
            form.ToolStrip.Items.Clear();
            var sortedToolbars = new List<ToolbarContribution>(_toolbars);
            sortedToolbars.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var t in sortedToolbars)
            {
                var cmd = FindCommand(t.CommandId);
                if (cmd == null) continue;
                var btn = new ToolStripButton(cmd.DisplayName)
                {
                    Tag = t.CommandId,
                    DisplayStyle = string.IsNullOrEmpty(t.IconKey)
                        ? ToolStripItemDisplayStyle.Text
                        : ToolStripItemDisplayStyle.ImageAndText
                };
                btn.Click += (s, e) => Execute(t.CommandId, null);
                form.ToolStrip.Items.Add(btn);
            }
        }

        /// <summary>菜单路径 "文件/打开" → 前 N-1 段建中间节点，最后一段挂命令叶子。</summary>
        private void BuildMenuTree(MenuStrip strip, MenuContribution m)
        {
            var segments = m.Path.Split('/');
            ToolStripMenuItem parent = null;
            for (int i = 0; i < segments.Length; i++)
            {
                var isLeaf = i == segments.Length - 1;
                if (!isLeaf)
                {
                    parent = parent == null
                        ? FindOrCreateTop(strip, segments[i])
                        : FindOrCreateChild(parent, segments[i]);
                    continue;
                }

                // 叶子：绑定命令；命令不存在则灰显禁用
                var item = new ToolStripMenuItem(m.CommandId);
                var cmd = FindCommand(m.CommandId);
                if (cmd != null)
                {
                    item.Text = cmd.DisplayName;
                    item.Tag = m.CommandId;
                    item.Click += (s, e) => Execute(m.CommandId, null);
                    if (!string.IsNullOrEmpty(m.ShortcutText))
                        item.ShortcutKeyDisplayString = m.ShortcutText;
                }
                else
                {
                    item.Text = m.Path;
                    item.Enabled = false;
                }

                if (parent == null)
                    strip.Items.Add(item);
                else
                    parent.DropDownItems.Add(item);
            }
        }

        private static ToolStripMenuItem FindOrCreateTop(MenuStrip strip, string text)
        {
            foreach (ToolStripMenuItem item in strip.Items)
                if (item.Text == text) return item;
            var created = new ToolStripMenuItem(text);
            strip.Items.Add(created);
            return created;
        }

        private static ToolStripMenuItem FindOrCreateChild(ToolStripMenuItem parent, string text)
        {
            foreach (ToolStripMenuItem item in parent.DropDownItems)
                if (item.Text == text) return item;
            var created = new ToolStripMenuItem(text);
            parent.DropDownItems.Add(created);
            return created;
        }

        private ICommand FindCommand(string id)
        {
            lock (_commands)
            {
                foreach (var c in _commands)
                    if (c.Id == id) return c;
            }
            return null;
        }

        private void Execute(string commandId, object parameter)
        {
            var cmd = FindCommand(commandId);
            if (cmd != null && cmd.CanExecute(parameter))
                cmd.Execute(parameter);
        }
    }
}
