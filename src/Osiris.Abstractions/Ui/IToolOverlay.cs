namespace Osiris.Abstractions.Ui;

/// <summary>
/// 工具覆盖层画布：工具在文档像素坐标系上叠加绘制提示
/// （如套索多边形轮廓、选区预览），宿主把指令映射到渲染层。
/// </summary>
public interface IToolOverlay
{
    /// <summary>
    /// 绘制折线：按 points 顺序连线（相邻点直线相连）；
    /// closed 为 true 时自动从末点闭合回起点。
    /// </summary>
    void DrawPolyline(IReadOnlyList<Document.Point2> points, bool closed);
}
