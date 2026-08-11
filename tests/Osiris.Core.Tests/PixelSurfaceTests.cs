using Osiris.Abstractions.Document;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// PixelSurface / PixelSurfaceEditor 契约测试：
/// 创建语义、COW（写时复制）核心不变量、行访问越界防护、脏矩形合并。
/// </summary>
public class PixelSurfaceTests
{
    [Fact]
    public void Create_WidthHeight_RowBytesAndInitialTransparentBlack()
    {
        // 意图：Create 默认创建全透明黑画布（BGRA 预乘全零），尺寸与行字节数正确。
        PixelSurface surface = PixelSurface.Create(3, 2);

        Assert.Equal(3, surface.Width);
        Assert.Equal(2, surface.Height);
        Assert.Equal(3 * 4, surface.RowBytes);
        Assert.Equal(3 * 2 * 4, surface.Pixels.Length);
        Assert.All(surface.Pixels.ToArray(), b => Assert.Equal(0, b)); // 初始全透明黑
    }

    [Fact]
    public void CreateEditor_WritePixel_Commit_ReturnsNewSurface_OriginalUnchanged()
    {
        // 意图：COW 核心断言——编辑器写像素后 Commit 返回新实例，源实例保持不变。
        PixelSurface original = PixelSurface.Create(2, 2);
        PixelSurfaceEditor editor = original.CreateEditor();

        // 写第一个像素为不透明红（BGRA 预乘：B=0,G=0,R=255,A=255）
        editor.Row(0)[0] = 0;
        editor.Row(0)[1] = 0;
        editor.Row(0)[2] = 255;
        editor.Row(0)[3] = 255;

        PixelSurface committed = editor.Commit();

        // Commit 返回的是新实例（非同一引用），且新实例携带修改后的像素
        Assert.False(ReferenceEquals(committed, original));
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, committed.Row(0)[..4].ToArray());
        // 源实例保持初始透明黑不变（编辑未提交前不影响原数据）
        Assert.All(original.Pixels.ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void CreateEditor_AfterCommit_CanContinueEditing()
    {
        // 意图：Commit 后编辑器仍持有独立缓冲，可再次修改并再次 Commit（重复提交语义）。
        PixelSurfaceEditor editor = PixelSurface.Create(2, 1).CreateEditor();
        editor.Row(0)[2] = 10; // R=10

        PixelSurface first = editor.Commit();
        editor.Row(0)[2] = 200; // 再次修改并提交
        PixelSurface second = editor.Commit();

        Assert.Equal(10, first.Row(0)[2]);
        Assert.Equal(200, second.Row(0)[2]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Row_OutOfBounds_ThrowsArgumentOutOfRangeException(int y)
    {
        // 意图：y 越界（负数或 ≥ 高度）时按行访问必须抛 ArgumentOutOfRangeException。
        PixelSurface surface = PixelSurface.Create(2, 2);
        PixelSurfaceEditor editor = surface.CreateEditor();

        Assert.Throws<ArgumentOutOfRangeException>(() => surface.Row(y));
        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Row(y));
    }

    [Fact]
    public void MarkDirty_TwoRects_UnionsToBoundingBox()
    {
        // 意图：脏矩形合并——(0,0,1,1) 与 (2,3,2,2) 合并为恰好包围两者的 (0,0,4,5)。
        PixelSurfaceEditor editor = PixelSurface.Create(10, 10).CreateEditor();
        editor.MarkDirty(0, 0, 1, 1);
        editor.MarkDirty(2, 3, 2, 2);

        DirtyRect? dirty = editor.DirtyRect;
        Assert.NotNull(dirty);
        Assert.Equal(new DirtyRect(0, 0, 4, 5), dirty.Value);
    }

    [Fact]
    public void MarkAllDirty_CoversWholeCanvas()
    {
        // 意图：MarkAllDirty 后脏矩形应覆盖整幅画布。
        PixelSurfaceEditor editor = PixelSurface.Create(5, 3).CreateEditor();
        editor.MarkAllDirty();

        Assert.Equal(new DirtyRect(0, 0, 5, 3), editor.DirtyRect);
    }

    [Fact]
    public void DirtyRect_Union_WithEmptyReturnsOther()
    {
        // 意图：空脏矩形与任意矩形合并直接返回另一方（DirtyRect.Union 语义）。
        DirtyRect empty = new(0, 0, 0, 0);
        DirtyRect rect = new(2, 3, 4, 5);

        Assert.Equal(rect, empty.Union(rect));
        Assert.Equal(rect, rect.Union(empty));
        Assert.Equal(rect, new DirtyRect(2, 3, 4, 5).Union(new DirtyRect(2, 3, 4, 5)));
        // 重合矩形合并等于自身
        Assert.Equal(new DirtyRect(2, 3, 4, 5), new DirtyRect(2, 3, 4, 5).Union(new DirtyRect(2, 3, 4, 5)));
        // 右/下边界为开区间：点 (5,7) 在 (2,3,4,5) 外（Right=6,Bottom=8）
        Assert.True(rect.Contains(5, 7));
        Assert.False(rect.Contains(6, 8));
    }
}
