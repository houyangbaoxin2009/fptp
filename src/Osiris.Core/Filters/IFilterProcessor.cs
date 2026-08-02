using System.Collections.Generic;
using System.Threading;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Core.Filters
{
    /// <summary>滤镜契约：PixelSurface 进，PixelSurface 出。</summary>
    public interface IFilterProcessor
    {
        string Id { get; }
        string DisplayName { get; }
        Plugins.FilterParameters Defaults { get; }
        /// <summary>参数声明式描述：壳据此生成参数对话框（空列表 = 无参数，直接执行）。</summary>
        IReadOnlyList<FilterParameterDescriptor> Parameters { get; }
        PixelSurface Apply(PixelSurface input, Plugins.FilterParameters p,
                           IProgress progress, CancellationToken ct);
    }
}
