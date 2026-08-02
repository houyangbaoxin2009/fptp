using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Osiris.Core.Document;
using Osiris.Core.Filters;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Osiris.App.Workbench
{
    /// <summary>壳内置的文档级命令（基础设施，非业务功能）。</summary>
    internal static class WorkbenchCommands
    {
        /// <summary>打开图片为新文档（Id 与 Core.KnownCommands 共享）。</summary>
        internal sealed class OpenDocumentCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public OpenDocumentCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.OpenDocument;
            public string DisplayName => "打开(&O)...";

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                using (var dlg = new OpenFileDialog
                {
                    Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp|PNG|*.png|JPEG|*.jpg;*.jpeg|位图|*.bmp|WebP|*.webp"
                })
                {
                    if (dlg.ShowDialog(_form) != DialogResult.OK) return;
                    OpenFile(dlg.FileName);
                }
            }

            /// <summary>打开图片为新文档（菜单/拖放共用入口）。</summary>
            internal void OpenFile(string fileName)
            {
                try
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        var surface = new Osiris.Engine.Skia.ImageCodecSkia()
                            .Read(stream, Path.GetExtension(fileName));
                        var doc = new OsirisDocument(surface.Width, surface.Height);
                        var layer = new Layer(Path.GetFileName(fileName), surface.Width, surface.Height);
                        surface.Pixels.CopyTo(layer.Pixels.Pixels);
                        doc.Layers.Add(layer);
                        _form.LoadDocument(doc, Path.GetFileName(fileName), fileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_form, "打开失败: " + ex.Message, "Osiris",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>打印：合成当前文档，按页面可打印区域等比缩放居中打印（对齐 1.x）。</summary>
        internal sealed class PrintCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public PrintCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Print;
            public string DisplayName => "打印(&P)...";

            public bool CanExecute(object parameter) => _form.Document.Layers.Count > 0;

            public void Execute(object parameter)
            {
                using (var toPrint = _form.RenderToGdiBitmap())
                using (var pd = new System.Drawing.Printing.PrintDocument())
                using (var dlg = new PrintDialog { Document = pd })
                {
                    if (dlg.ShowDialog(_form) != DialogResult.OK) return;

                    pd.PrintPage += (s, ev) =>
                    {
                        // 按可打印区域等比缩放，居中打印
                        var bounds = ev.MarginBounds;
                        float scale = Math.Min(bounds.Width / (float)toPrint.Width,
                                               bounds.Height / (float)toPrint.Height);
                        int w = (int)(toPrint.Width * scale);
                        int h = (int)(toPrint.Height * scale);
                        int x = (int)(bounds.X + (bounds.Width - w) / 2);
                        int y = (int)(bounds.Y + (bounds.Height - h) / 2);
                        ev.Graphics.DrawImage(toPrint, x, y, w, h);
                        ev.HasMorePages = false;
                    };

                    try
                    {
                        _form.SetStatus("正在打印...");
                        pd.Print();
                        _form.SetStatus("打印完成");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(_form, "打印失败: " + ex.Message, "Osiris",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _form.SetStatus("打印失败");
                    }
                }
            }
        }

        /// <summary>撤销：优先撤销当前文档内历史；无历史时回退到上一个文档（裁切/排版生成的新文档可回到原图）。</summary>
        internal sealed class UndoCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public UndoCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Undo;
            public string DisplayName => "撤销(&U)";

            public bool CanExecute(object parameter)
                => _form.Document.History.CanUndo || _form.CanUndoDocument;

            public void Execute(object parameter)
            {
                if (_form.Document.History.CanUndo)
                    _form.Document.History.Undo(_form.Document);
                else
                    _form.UndoDocument();
            }
        }

        /// <summary>重做：优先重做当前文档内历史；无历史时前进到下一个文档。</summary>
        internal sealed class RedoCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public RedoCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Redo;
            public string DisplayName => "重做(&R)";

            public bool CanExecute(object parameter)
                => _form.Document.History.CanRedo || _form.CanRedoDocument;

            public void Execute(object parameter)
            {
                if (_form.Document.History.CanRedo)
                    _form.Document.History.Redo(_form.Document);
                else
                    _form.RedoDocument();
            }
        }

        /// <summary>画布视图缩放（放大/缩小/适应窗口/实际大小）。</summary>
        internal sealed class ZoomCommand : ICommand
        {
            public enum ZoomAction { In, Out, Fit, Actual }

            private readonly WorkbenchForm _form;
            private readonly ZoomAction _action;

            public ZoomCommand(WorkbenchForm form, ZoomAction action)
            {
                _form = form;
                _action = action;
            }

            public string Id => _action switch
            {
                ZoomAction.In => KnownCommands.ZoomIn,
                ZoomAction.Out => KnownCommands.ZoomOut,
                ZoomAction.Fit => KnownCommands.ZoomFit,
                _ => KnownCommands.ZoomActual
            };

            public string DisplayName => _action switch
            {
                ZoomAction.In => "放大(&I)",
                ZoomAction.Out => "缩小(&O)",
                ZoomAction.Fit => "适应窗口(&F)",
                _ => "实际大小(&A)"
            };

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                switch (_action)
                {
                    case ZoomAction.In: _form.ZoomIn(); break;
                    case ZoomAction.Out: _form.ZoomOut(); break;
                    case ZoomAction.Fit: _form.ZoomFitView(); break;
                    default: _form.ZoomActual(); break;
                }
            }
        }

        /// <summary>保存：合成当前文档写盘；无路径时转另存为。</summary>
        internal sealed class SaveCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public SaveCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Save;
            public string DisplayName => "保存(&S)";

            public bool CanExecute(object parameter) => _form.Document.Layers.Count > 0;

            public void Execute(object parameter)
            {
                if (string.IsNullOrEmpty(_form.CurrentPath))
                    new SaveAsCommand(_form).Execute(parameter);
                else
                    SaveTo(_form.CurrentPath);
            }

            /// <summary>把当前文档合成结果保存到指定路径。</summary>
            internal void SaveTo(string path)
            {
                using (var bmp = new Osiris.Engine.Skia.CanvasRenderer().Render(_form.Document))
                    Osiris.Engine.Skia.ImageCodecSkia.SaveComposite(bmp, path);
                _form.CurrentPath = path;
                _form.SetStatus("已保存: " + path);
            }
        }

        /// <summary>另存为：选择路径后保存。</summary>
        internal sealed class SaveAsCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public SaveAsCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.SaveAs;
            public string DisplayName => "另存为(&A)...";

            public bool CanExecute(object parameter) => _form.Document.Layers.Count > 0;

            public void Execute(object parameter)
            {
                using (var dlg = new SaveFileDialog
                {
                    Filter = "PNG|*.png|JPEG|*.jpg|位图|*.bmp|WebP|*.webp",
                    DefaultExt = "png",
                    FileName = Path.GetFileNameWithoutExtension(_form.CurrentPath ?? "未命名") + ".png"
                })
                {
                    if (dlg.ShowDialog(_form) != DialogResult.OK) return;
                    try
                    {
                        new SaveCommand(_form).SaveTo(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(_form, "保存失败: " + ex.Message, "Osiris",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>批量处理（1.x BatchBox 能力）：选目录 + 选项，后台逐张执行 裁切/灰度/换底/排版。</summary>
        internal sealed class BatchCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public BatchCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Batch;
            public string DisplayName => "批量处理(&B)...";

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                using (var dlg = new BatchDialog())
                {
                    if (dlg.ShowDialog(_form) != DialogResult.OK) return;
                    var options = dlg.Options;
                    var inputDir = dlg.InputDir;
                    var outputDir = dlg.OutputDir;

                    // 收集全部滤镜（BatchProcessor 按 Id 后缀匹配）
                    var filters = new List<IFilterProcessor>();
                    var registry = _form.PluginRegistry;
                    if (registry != null)
                        foreach (var plugin in registry.Loaded)
                            if (plugin is IFilterPlugin fp)
                                filters.AddRange(fp.Filters);
                    if (filters.Count == 0)
                    {
                        MessageBox.Show(_form, "未找到可用滤镜插件。", "Osiris",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var codec = new Osiris.Engine.Skia.ImageCodecSkia();
                    int done = 0;
                    var total = Directory.GetFiles(inputDir, "*.*")
                        .Where(BatchProcessor.IsImage).Count();
                    _form.SetStatus("批量处理: 0/" + total);

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            var result = BatchProcessor.Run(
                                inputDir, outputDir, options, filters,
                                file =>
                                {
                                    using (var stream = File.OpenRead(file))
                                        return codec.Read(stream, Path.GetExtension(file));
                                },
                                (surface, outFile) =>
                                {
                                    using (var stream = File.Create(outFile))
                                        codec.Write(surface, stream, ".png");
                                },
                                null,
                                (file, ok, ex) =>
                                {
                                    done++;
                                    _form.SetStatus(string.Format("批量处理: {0}/{1} {2}", done, total,
                                        Path.GetFileName(file)));
                                });
                            _form.BeginInvoke(new Action(() =>
                            {
                                _form.SetStatus(string.Format("批量完成: {0} 成功, {1} 失败", result.Succeeded, result.Failed));
                                MessageBox.Show(_form,
                                    string.Format("批量完成: {0} 成功, {1} 失败\n输出目录: {2}",
                                        result.Succeeded, result.Failed, outputDir),
                                    "Osiris",
                                    result.Failed == 0 ? MessageBoxButtons.OK : MessageBoxButtons.OK,
                                    result.Failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                            }));
                        }
                        catch (Exception ex)
                        {
                            _form.BeginInvoke(new Action(() =>
                            {
                                _form.SetStatus("批量处理失败");
                                MessageBox.Show(_form, "批量处理失败: " + ex.Message, "Osiris",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    });
                }
            }
        }

        /// <summary>批量处理选项对话框：输入/输出目录 + 裁切/灰度/换底勾选 + 排版相纸下拉。</summary>
        private sealed class BatchDialog : Form
        {
            private readonly TextBox _inputBox = new TextBox();
            private readonly TextBox _outputBox = new TextBox();
            private readonly CheckBox _crop = new CheckBox { Text = "智能裁切" };
            private readonly CheckBox _gray = new CheckBox { Text = "灰度" };
            private readonly CheckBox _bg = new CheckBox { Text = "换底色" };
            private readonly ComboBox _paper = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

            public string InputDir => _inputBox.Text.Trim();
            public string OutputDir => _outputBox.Text.Trim();

            public BatchOptions Options => new BatchOptions
            {
                Crop = _crop.Checked,
                Grayscale = _gray.Checked,
                ReplaceBackground = _bg.Checked,
                LayoutPaper = _paper.SelectedIndex > 0 ? (string)_paper.SelectedItem : null
            };

            public BatchDialog()
            {
                Text = "批量处理";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterParent;
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
                ShowInTaskbar = false;

                int labelX = 16, ctrlX = 90, ctrlW = 260, rowH = 34, padTop = 16;
                int row = 0;

                _inputBox.Location = new System.Drawing.Point(ctrlX, padTop + row * rowH);
                _inputBox.Width = ctrlW;
                var inBtn = new Button { Text = "浏览...", Location = new System.Drawing.Point(ctrlX + ctrlW + 6, padTop + row * rowH - 2), Width = 76 };
                inBtn.Click += (s, e) => PickDir(_inputBox);
                Controls.Add(new Label { Text = "输入目录", AutoSize = true, Location = new System.Drawing.Point(labelX, padTop + row * rowH + 6) });
                Controls.Add(_inputBox);
                Controls.Add(inBtn);
                row++;

                _outputBox.Location = new System.Drawing.Point(ctrlX, padTop + row * rowH);
                _outputBox.Width = ctrlW;
                var outBtn = new Button { Text = "浏览...", Location = new System.Drawing.Point(ctrlX + ctrlW + 6, padTop + row * rowH - 2), Width = 76 };
                outBtn.Click += (s, e) => PickDir(_outputBox);
                Controls.Add(new Label { Text = "输出目录", AutoSize = true, Location = new System.Drawing.Point(labelX, padTop + row * rowH + 6) });
                Controls.Add(_outputBox);
                Controls.Add(outBtn);
                row++;

                _crop.Checked = true;
                _crop.Location = new System.Drawing.Point(ctrlX, padTop + row * rowH + 2);
                _gray.Location = new System.Drawing.Point(ctrlX + 100, padTop + row * rowH + 2);
                _bg.Location = new System.Drawing.Point(ctrlX + 190, padTop + row * rowH + 2);
                Controls.Add(_crop);
                Controls.Add(_gray);
                Controls.Add(_bg);
                row++;

                _paper.Items.Add("不排版");
                foreach (var name in LayoutProcessor.PaperPresets.Keys) _paper.Items.Add(name);
                _paper.SelectedIndex = 0;
                _paper.Location = new System.Drawing.Point(ctrlX, padTop + row * rowH);
                _paper.Width = 120;
                Controls.Add(new Label { Text = "排版相纸", AutoSize = true, Location = new System.Drawing.Point(labelX, padTop + row * rowH + 6) });
                Controls.Add(_paper);
                row++;

                int btnY = padTop + row * rowH + 8;
                var ok = new Button { Text = "开始", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(ctrlX, btnY), Width = 90 };
                var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(ctrlX + 104, btnY), Width = 90 };
                Controls.Add(ok);
                Controls.Add(cancel);
                AcceptButton = ok;
                CancelButton = cancel;
                ClientSize = new System.Drawing.Size(ctrlX + ctrlW + 100, btnY + 40);
            }

            private void PickDir(TextBox box)
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (System.IO.Directory.Exists(box.Text)) dlg.SelectedPath = box.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) box.Text = dlg.SelectedPath;
                }
            }
        }
    }
}
