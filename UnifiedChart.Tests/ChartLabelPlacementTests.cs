namespace UnifiedChart.Tests;

public class ChartLabelPlacementTests
{
    [Fact]
    public void Autoは候補順の最初で領域内かつ未重複の位置を選ぶ()
    {
        var label = new ChartLabel();
        var result = ChartLabelPlacement.Find(
            50,
            50,
            10,
            10,
            label,
            new LabelPlacementRect(0, 0, 100, 100),
            Array.Empty<LabelPlacementRect>());

        Assert.Equal(58, result.Left);
        Assert.Equal(32, result.Top);
    }

    [Fact]
    public void Autoは領域外候補より領域内候補を優先する()
    {
        var label = new ChartLabel();
        var result = ChartLabelPlacement.Find(
            95,
            5,
            20,
            20,
            label,
            new LabelPlacementRect(0, 0, 100, 100),
            Array.Empty<LabelPlacementRect>());

        Assert.True(result.Left >= 0);
        Assert.True(result.Top >= 0);
        Assert.True(result.Right <= 100);
        Assert.True(result.Bottom <= 100);
    }

    [Fact]
    public void Autoは重複を最小化する候補へフォールバックする()
    {
        var label = new ChartLabel();
        var occupied = new[] { new LabelPlacementRect(58, 32, 10, 10) };
        var result = ChartLabelPlacement.Find(
            50,
            50,
            10,
            10,
            label,
            new LabelPlacementRect(0, 0, 100, 100),
            occupied);

        Assert.NotEqual(32, result.Top);
    }
}
