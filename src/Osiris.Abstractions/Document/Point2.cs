namespace Osiris.Abstractions.Document;

/// <summary>
/// 文档像素坐标（二维整数点）。
/// 用于选区多边形顶点、图层偏移等文档坐标系中的位置描述。
/// </summary>
public readonly record struct Point2(int X, int Y);
