using Osiris.Abstractions.Document;
using Osiris.Core.Document;

namespace Osiris.Core.History;

/// <summary>
/// 滤镜应用命令：记录图层变换前后的不可变引用，撤销/重做仅做指针替换（COW 零拷贝）。
/// oldLayer 即变换前的 Layer 引用，newLayer 为滤镜输出后 with 派生的新引用——
/// 二者共享未变像素缓冲，历史栈因此以 O(1) 空间保存完整图层状态。
/// </summary>
public sealed class ApplyFilterCommand : IUndoableCommand
{
    private readonly DocumentService _service;
    private readonly string _layerId;
    private readonly Layer _oldLayer;
    private readonly Layer _newLayer;

    /// <summary>构造：绑定文档服务与图层变换前后引用。</summary>
    public ApplyFilterCommand(DocumentService service, string layerId, Layer oldLayer, Layer newLayer)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _layerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        _oldLayer = oldLayer ?? throw new ArgumentNullException(nameof(oldLayer));
        _newLayer = newLayer ?? throw new ArgumentNullException(nameof(newLayer));
        Name = $"滤镜: {newLayer.Name}";
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public void Execute(OsirisDocument document) => _service.ReplaceLayer(_layerId, _newLayer);

    /// <inheritdoc />
    public void Undo(OsirisDocument document) => _service.ReplaceLayer(_layerId, _oldLayer);

    /// <inheritdoc />
    public void Redo(OsirisDocument document) => _service.ReplaceLayer(_layerId, _newLayer);
}
