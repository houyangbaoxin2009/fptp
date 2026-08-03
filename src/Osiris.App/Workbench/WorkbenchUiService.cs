using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Osiris.Core.Filters;
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

        /// <summary>按声明式参数描述自动生成对话框（Int=数值框，Choice/Color=下拉+色块）。</summary>
        public Osiris.Core.Plugins.FilterParameters PromptFilterParameters(
            IReadOnlyList<FilterParameterDescriptor> descriptors,
            Osiris.Core.Plugins.FilterParameters current)
        {
            if (descriptors == null || descriptors.Count == 0) return current;

            using (var dlg = new Form())
            {
                dlg.Text = "滤镜参数";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Font = new Font("Microsoft YaHei UI", 9F);

                int row = 0;
                int labelX = 16, ctrlX = 130, ctrlW = 200, rowH = 34, padTop = 16;
                var editors = new Dictionary<FilterParameterDescriptor, Control>();

                foreach (var d in descriptors)
                {
                    var lbl = new Label { Text = d.Label, AutoSize = true, Location = new Point(labelX, padTop + row * rowH + 6) };
                    dlg.Controls.Add(lbl);

                    switch (d.Kind)
                    {
                        case FilterParameterKind.Int:
                        {
                            var nud = new NumericUpDown
                            {
                                Location = new Point(ctrlX, padTop + row * rowH),
                                Width = ctrlW,
                                Minimum = d.Min,
                                Maximum = d.Max
                            };
                            nud.Value = Clamp(current.Get(d.Key, d.Min), d.Min, d.Max);
                            dlg.Controls.Add(nud);
                            editors[d] = nud;
                            break;
                        }
                        case FilterParameterKind.Color:
                        {
                            var combo = new ComboBox
                            {
                                Location = new Point(ctrlX, padTop + row * rowH),
                                Width = ctrlW - 34,
                                DropDownStyle = ComboBoxStyle.DropDownList
                            };
                            var preview = new Panel
                            {
                                Location = new Point(ctrlX + ctrlW - 28, padTop + row * rowH + 2),
                                Size = new Size(24, 24),
                                BorderStyle = BorderStyle.FixedSingle
                            };
                            FillColorCombo(combo, preview, d, current);
                            combo.SelectedIndexChanged += (s, e) =>
                                preview.BackColor = SelectedColor(combo, d);
                            dlg.Controls.Add(combo);
                            dlg.Controls.Add(preview);
                            editors[d] = combo;
                            break;
                        }
                        case FilterParameterKind.ColorPicker:
                        {
                            // 任意颜色：色块按钮，点击弹系统颜色对话框选任意色
                            var box = new Panel
                            {
                                Location = new Point(ctrlX, padTop + row * rowH + 2),
                                Size = new Size(24, 24),
                                BorderStyle = BorderStyle.FixedSingle
                            };
                            var btn = new Button
                            {
                                Location = new Point(ctrlX + 30, padTop + row * rowH),
                                Size = new Size(ctrlW - 30, 28),
                                Text = "选择颜色...",
                                UseVisualStyleBackColor = true
                            };
                            int cur = current.Get(d.Key, (int)Osiris.Core.Imaging.ColorUtil.PackBgra(0, 0, 0));
                            box.BackColor = ColorUtilToDrawing(cur);
                            btn.Click += (s, e) =>
                            {
                                using (var cd = new ColorDialog())
                                {
                                    cd.Color = ColorUtilToDrawing(cur);
                                    if (cd.ShowDialog(dlg) == DialogResult.OK)
                                    {
                                        cur = PackFromDrawing(cd.Color);
                                        box.BackColor = cd.Color;
                                    }
                                }
                            };
                            dlg.Controls.Add(box);
                            dlg.Controls.Add(btn);
                            editors[d] = box; // 值收集时读 box.BackColor
                            break;
                        }
                        case FilterParameterKind.Image:
                        {
                            // 图片路径：文本框 + 浏览按钮
                            var txt = new TextBox
                            {
                                Location = new Point(ctrlX, padTop + row * rowH),
                                Width = ctrlW - 92,
                                Text = current.Get<string>(d.Key, null) ?? ""
                            };
                            var browse = new Button
                            {
                                Location = new Point(ctrlX + ctrlW - 86, padTop + row * rowH),
                                Size = new Size(86, 28),
                                Text = "浏览..."
                            };
                            browse.Click += (s, e) =>
                            {
                                using (var ofd = new OpenFileDialog())
                                {
                                    ofd.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp";
                                    ofd.Title = "选择背景图片";
                                    if (ofd.ShowDialog(dlg) == DialogResult.OK)
                                        txt.Text = ofd.FileName;
                                }
                            };
                            dlg.Controls.Add(txt);
                            dlg.Controls.Add(browse);
                            editors[d] = txt; // 值收集时读 txt.Text
                            break;
                        }
                        default: // Choice
                        {
                            var combo = new ComboBox
                            {
                                Location = new Point(ctrlX, padTop + row * rowH),
                                Width = ctrlW,
                                DropDownStyle = ComboBoxStyle.DropDownList
                            };
                            FillChoiceCombo(combo, d, current);
                            dlg.Controls.Add(combo);
                            editors[d] = combo;
                            break;
                        }
                    }
                    row++;
                }

                int btnY = padTop + row * rowH + 12;
                var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(ctrlX, btnY), Width = 90 };
                var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(ctrlX + 104, btnY), Width = 90 };
                dlg.Controls.Add(ok);
                dlg.Controls.Add(cancel);
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                dlg.ClientSize = new Size(ctrlX + ctrlW + 32, btnY + 40);
                dlg.ShowInTaskbar = false;

                if (dlg.ShowDialog(_form) != DialogResult.OK) return null;

                // 收集用户确认的参数覆盖值
                var result = new Osiris.Core.Plugins.FilterParameters();
                foreach (var pair in editors)
                {
                    var d = pair.Key;
                    if (d.Kind == FilterParameterKind.Int)
                    {
                        result[d.Key] = (int)((NumericUpDown)pair.Value).Value;
                    }
                    else if (d.Kind == FilterParameterKind.ColorPicker)
                    {
                        result[d.Key] = PackFromDrawing(((Panel)pair.Value).BackColor);
                    }
                    else if (d.Kind == FilterParameterKind.Image)
                    {
                        result[d.Key] = ((TextBox)pair.Value).Text;
                    }
                    else
                    {
                        var combo = (ComboBox)pair.Value;
                        result[d.Key] = d.ChoiceValues[combo.SelectedIndex];
                    }
                }
                return result;
            }
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        /// <summary>BGRA 打包值 → System.Drawing.Color（色块预览用）。</summary>
        private static Color ColorUtilToDrawing(int bgra)
            => Color.FromArgb(Osiris.Core.Imaging.ColorUtil.R(bgra),
                              Osiris.Core.Imaging.ColorUtil.G(bgra),
                              Osiris.Core.Imaging.ColorUtil.B(bgra));

        /// <summary>System.Drawing.Color → BGRA 打包值（ColorPicker 参数值）。</summary>
        private static int PackFromDrawing(Color c)
            => Osiris.Core.Imaging.ColorUtil.PackBgra(c.R, c.G, c.B);

        /// <summary>颜色下拉：填充选项文本并选中当前值，同步色块。</summary>
        private static void FillColorCombo(ComboBox combo, Panel preview, FilterParameterDescriptor d,
                                           Osiris.Core.Plugins.FilterParameters current)
        {
            for (int i = 0; i < d.Choices.Length; i++) combo.Items.Add(d.Choices[i]);
            var cur = current.Get<object>(d.Key, null);
            int sel = 0;
            for (int i = 0; i < d.ChoiceValues.Length; i++)
                if (Equals(d.ChoiceValues[i], cur)) { sel = i; break; }
            combo.SelectedIndex = sel;
            preview.BackColor = SelectedColor(combo, d);
        }

        /// <summary>普通下拉：填充选项并选中当前值（按值相等匹配）。</summary>
        private static void FillChoiceCombo(ComboBox combo, FilterParameterDescriptor d,
                                            Osiris.Core.Plugins.FilterParameters current)
        {
            for (int i = 0; i < d.Choices.Length; i++) combo.Items.Add(d.Choices[i]);
            var cur = current.Get<object>(d.Key, null);
            int sel = 0;
            for (int i = 0; i < d.ChoiceValues.Length; i++)
                if (ValuesEqual(d.ChoiceValues[i], cur)) { sel = i; break; }
            combo.SelectedIndex = sel;
        }

        /// <summary>int[] 与 int[] 比较、int 与 int 比较（Choice 值可为组合，如宽高数组）。</summary>
        private static bool ValuesEqual(object a, object b)
        {
            if (a == null || b == null) return ReferenceEquals(a, b);
            if (a is int[] ia && b is int[] ib) return ia.Length == ib.Length && ia[0] == ib[0] && ia.Length > 1 && ia[1] == ib[1];
            return Equals(a, b);
        }

        /// <summary>选中颜色的 RGB（ChoiceValues 为 PackBgra 打包 int）。</summary>
        private static Color SelectedColor(ComboBox combo, FilterParameterDescriptor d)
        {
            if (combo.SelectedIndex < 0 || combo.SelectedIndex >= d.ChoiceValues.Length) return Color.White;
            int bgra = (int)d.ChoiceValues[combo.SelectedIndex];
            int a = (bgra >> 24) & 0xFF, r = (bgra >> 16) & 0xFF, g = (bgra >> 8) & 0xFF, b = bgra & 0xFF;
            if (a == 0) return Color.White;
            return Color.FromArgb(r, g, b);
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
                    {
                        item.ShortcutKeyDisplayString = m.ShortcutText;
                        // 解析真实快捷键并绑定（如 Ctrl+O、Ctrl+=、Ctrl+-），菜单显示与实际按键一致
                        item.ShortcutKeys = ParseShortcut(m.ShortcutText);
                    }
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

        /// <summary>解析快捷键文本（"Ctrl+O"、"Ctrl+="、"Ctrl+-"、"Ctrl+0"）为 Keys 组合。
        /// WinForms ShortcutKeys 要求带 Ctrl/Alt 修饰（裸键、纯 Shift 组合非法），不满足时返回 None（仅显示文本）。</summary>
        private static Keys ParseShortcut(string text)
        {
            var parts = text.Split('+');
            if (parts.Length == 0) return Keys.None;
            var keys = Keys.None;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].Trim().ToUpperInvariant())
                {
                    case "CTRL": keys |= Keys.Control; break;
                    case "SHIFT": keys |= Keys.Shift; break;
                    case "ALT": keys |= Keys.Alt; break;
                }
            }
            var key = parts[parts.Length - 1].Trim().ToUpperInvariant();
            switch (key)
            {
                case "0": keys |= Keys.D0; break;
                case "1": keys |= Keys.D1; break;
                case "2": keys |= Keys.D2; break;
                case "3": keys |= Keys.D3; break;
                case "4": keys |= Keys.D4; break;
                case "5": keys |= Keys.D5; break;
                case "6": keys |= Keys.D6; break;
                case "7": keys |= Keys.D7; break;
                case "8": keys |= Keys.D8; break;
                case "9": keys |= Keys.D9; break;
                case "=": keys |= Keys.Oemplus; break;
                case "+": keys |= Keys.Oemplus; break;
                case "-": keys |= Keys.OemMinus; break;
                default:
                    if (key.Length == 1 && key[0] >= 'A' && key[0] <= 'Z')
                        keys |= (Keys)(key[0] - 'A' + (int)Keys.A);
                    else
                        return Keys.None;
                    break;
            }
            // 无 Ctrl/Alt 修饰的裸键/纯 Shift 组合 WinForms 不允许设为 ShortcutKeys
            return (keys & (Keys.Control | Keys.Alt)) == 0 ? Keys.None : keys;
        }

        private void Execute(string commandId, object parameter)
        {
            var cmd = FindCommand(commandId);
            if (cmd != null && cmd.CanExecute(parameter))
                cmd.Execute(parameter);
        }
    }
}
