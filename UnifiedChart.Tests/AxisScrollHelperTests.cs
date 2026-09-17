namespace UnifiedChart.Tests;

public class AxisScrollHelperTests
{
    [Fact]
    public void 明示単位で境界内へスクロールする()
    {
        var result = AxisScrollHelper.Scroll(new AxisRange(20, 40), 1, 5, 0, 100);

        Assert.Equal(new AxisRange(25, 45), result);
    }

    [Fact]
    public void 最大境界を越えない()
    {
        var result = AxisScrollHelper.Scroll(new AxisRange(80, 100), 1, 10, 0, 100);

        Assert.Equal(new AxisRange(80, 100), result);
    }
}
