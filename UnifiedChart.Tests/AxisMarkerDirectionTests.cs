namespace UnifiedChart.Tests;

public class AxisMarkerDirectionTests
{
    [Theory]
    [InlineData("East", 1, 0)]
    [InlineData("West", -1, 0)]
    [InlineData("North", 0, -1)]
    [InlineData("South", 0, 1)]
    public void 指定方向を画面座標ベクトルへ変換する(string value, double expectedX, double expectedY)
    {
        var result = AxisMarkerDirection.Resolve(value, false);

        Assert.Equal(expectedX, result.X);
        Assert.Equal(expectedY, result.Y);
    }

    [Fact]
    public void AutoはY軸で外側方向の既定値を返す()
    {
        var result = AxisMarkerDirection.Resolve("Auto", false);

        Assert.Equal((-1, 0), result);
    }
}
