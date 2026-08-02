using System;
using System.IO;
using Osiris.Core.Imaging;

namespace Osiris.Core.IO
{
    /// <summary>图片导入器：从流解码为 PixelSurface。</summary>
    public interface IDocumentImporter
    {
        string Id { get; }
        /// <summary>支持的扩展名（含点，小写）。</summary>
        string[] Extensions { get; }
        bool CanRead(string extension);
        PixelSurface Read(Stream stream, string extension);
    }

    /// <summary>图片导出器：将 PixelSurface 编码到流。</summary>
    public interface IDocumentExporter
    {
        string Id { get; }
        string[] Extensions { get; }
        bool CanWrite(string extension);
        void Write(PixelSurface surface, Stream stream, string extension);
    }

    /// <summary>编解码注册表：按扩展名查导入/导出器。</summary>
    public static class CodecRegistry
    {
        private static readonly System.Collections.Generic.List<IDocumentImporter> Importers =
            new System.Collections.Generic.List<IDocumentImporter>();
        private static readonly System.Collections.Generic.List<IDocumentExporter> Exporters =
            new System.Collections.Generic.List<IDocumentExporter>();

        /// <summary>注册导入器。</summary>
        public static void Register(IDocumentImporter importer)
        {
            if (importer == null) throw new ArgumentNullException(nameof(importer));
            lock (Importers) { if (!Importers.Contains(importer)) Importers.Add(importer); }
        }

        /// <summary>注册导出器。</summary>
        public static void Register(IDocumentExporter exporter)
        {
            if (exporter == null) throw new ArgumentNullException(nameof(exporter));
            lock (Exporters) { if (!Exporters.Contains(exporter)) Exporters.Add(exporter); }
        }

        /// <summary>按扩展名查找导入器，找不到返回 null。</summary>
        public static IDocumentImporter FindImporter(string extension)
        {
            var ext = Normalize(extension);
            lock (Importers)
            {
                foreach (var imp in Importers)
                    if (imp.CanRead(ext)) return imp;
            }
            return null;
        }

        /// <summary>按扩展名查找导出器，找不到返回 null。</summary>
        public static IDocumentExporter FindExporter(string extension)
        {
            var ext = Normalize(extension);
            lock (Exporters)
            {
                foreach (var exp in Exporters)
                    if (exp.CanWrite(ext)) return exp;
            }
            return null;
        }

        private static string Normalize(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return string.Empty;
            return extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        }
    }
}
