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
        PixelSurface Apply(PixelSurface input, Plugins.FilterParameters p,
                           IProgress progress, CancellationToken ct);
    }
}
