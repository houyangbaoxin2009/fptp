using System;
using System.IO;
using System.Windows.Forms;
using Osiris.Core.Document;
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
                    try
                    {
                        using (var stream = File.OpenRead(dlg.FileName))
                        {
                            var surface = new Osiris.Engine.Skia.ImageCodecSkia()
                                .Read(stream, Path.GetExtension(dlg.FileName));
                            var doc = new OsirisDocument(surface.Width, surface.Height);
                            var layer = new Layer(Path.GetFileName(dlg.FileName), surface.Width, surface.Height);
                            surface.Pixels.CopyTo(layer.Pixels.Pixels);
                            doc.Layers.Add(layer);
                            _form.LoadDocument(doc, Path.GetFileName(dlg.FileName));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(_form, "打开失败: " + ex.Message, "Osiris",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>撤销（操作当前文档历史栈，刷新画布由 History.Changed 事件驱动）。</summary>
        internal sealed class UndoCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public UndoCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Undo;
            public string DisplayName => "撤销(&U)";

            public bool CanExecute(object parameter) => _form.Document.History.CanUndo;

            public void Execute(object parameter) => _form.Document.History.Undo(_form.Document);
        }

        /// <summary>重做（操作当前文档历史栈，刷新画布由 History.Changed 事件驱动）。</summary>
        internal sealed class RedoCommand : ICommand
        {
            private readonly WorkbenchForm _form;

            public RedoCommand(WorkbenchForm form) { _form = form; }

            public string Id => KnownCommands.Redo;
            public string DisplayName => "重做(&R)";

            public bool CanExecute(object parameter) => _form.Document.History.CanRedo;

            public void Execute(object parameter) => _form.Document.History.Redo(_form.Document);
        }
    }
}
