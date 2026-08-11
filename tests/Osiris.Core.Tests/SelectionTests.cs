using Osiris.Abstractions.Document;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// Selection 位图选区测试：矩形/多边形栅格化（黄金多边形用例）、求交、清空、克隆、越界防护。
/// </summary>
public class SelectionTests
{
    /// <summary>统计选区内被选中的像素总数（遍历全部像素调用 Contains）。</summary>
    private static int CountSelected(Selection selection)
    {
        int count = 0;
        for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
                if (selection.Contains(x, y))
                    count++;
        return count;
    }

    [Fact]
    public void SetRect_2x2Square_ContainsCorners_ExcludesEdges()
    {
        // 意图（黄金矩形用例）：2x2 正方形选区 SetRect(0,0,2,2) 后四个角点被选中，
        // 右/下边界（x==2 / y==2）不算选中（半开区间语义）。
        Selection selection = new(10, 10);
        selection.SetRect(0, 0, 2, 2);

        Assert.True(selection.Contains(0, 0));
        Assert.True(selection.Contains(1, 0));
        Assert.True(selection.Contains(0, 1));
        Assert.True(selection.Contains(1, 1));
        Assert.False(selection.Contains(2, 0));
        Assert.False(selection.Contains(0, 2));
        Assert.Equal(4, CountSelected(selection));
    }

    [Fact]
    public void SetPolygon_CenterTriangle_RasterizesExactly25Pixels()
    {
        // 意图（黄金多边形用例）：10x10 画布中心三角形 (5,1)(1,8)(9,8)，
        // 扫描线栅格化应恰好填满 25 像素（逐行计数：1+1+3+3+5+5+7）。
        Selection selection = new(10, 10);
        selection.SetPolygon([new Point2(5, 1), new Point2(1, 8), new Point2(9, 8)]);

        Assert.Equal(25, CountSelected(selection));

        // 关键点：顶角 / 重心附近 / 腰部像素应选中
        Assert.True(selection.Contains(5, 1));
        Assert.True(selection.Contains(5, 5));
        Assert.True(selection.Contains(4, 4));
        // 三角形外与水平底边外像素不应选中
        Assert.False(selection.Contains(1, 1));
        Assert.False(selection.Contains(0, 0));
        Assert.False(selection.Contains(5, 8)); // 底边为水平边，不参与栅格化
    }

    [Fact]
    public void Intersect_TwoRectangles_KeepsOnlyOverlap()
    {
        // 意图：两个 3x3 矩形偏移 (2,2) 后求交，交集只剩重合的 1 像素 (2,2)。
        Selection a = new(10, 10);
        Selection b = new(10, 10);
        a.SetRect(0, 0, 3, 3);
        b.SetRect(2, 2, 3, 3);

        a.Intersect(b);

        Assert.True(a.Contains(2, 2));
        Assert.False(a.Contains(0, 0));
        Assert.False(a.Contains(4, 4));
        Assert.Equal(1, CountSelected(a));
    }

    [Fact]
    public void Clear_RemovesAllSelectedPixels()
    {
        // 意图：Clear 后全部像素取消选中（掩码清零）。
        Selection selection = new(5, 5);
        selection.SetRect(0, 0, 5, 5);
        Assert.Equal(25, CountSelected(selection));

        selection.Clear();
        Assert.Equal(0, CountSelected(selection));
    }

    [Fact]
    public void Contains_OutOfBounds_ReturnsFalse()
    {
        // 意图：越界坐标查选区一律返回 false（不抛异常）。
        Selection selection = new(3, 3);
        Assert.False(selection.Contains(3, 0));
        Assert.False(selection.Contains(0, 3));
        Assert.False(selection.Contains(-1, 0));
        Assert.False(selection.Contains(0, -1));
    }

    [Fact]
    public void Intersect_DifferentSize_ThrowsArgumentException()
    {
        // 意图：尺寸不一致的选区求交是编程错误，应抛出 ArgumentException。
        Selection a = new(10, 10);
        Selection b = new(5, 5);
        Assert.Throws<ArgumentException>(() => a.Intersect(b));
    }

    [Fact]
    public void Clone_IsDeepCopy_ModifyingOriginalDoesNotAffectClone()
    {
        // 意图：Clone 为深拷贝——清空原选区不影响克隆体。
        Selection selection = new(10, 10);
        selection.SetRect(0, 0, 2, 2);
        Selection clone = selection.Clone();

        selection.Clear();

        Assert.Equal(0, CountSelected(selection));
        Assert.Equal(4, CountSelected(clone));
        Assert.True(clone.Contains(0, 0));
    }
}
