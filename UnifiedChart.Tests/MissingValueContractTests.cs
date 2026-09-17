namespace UnifiedChart.Tests;

public class MissingValueContractTests
{
    [Fact]
    public void Hole値は欠損判定されるが入力値は変更されない()
    {
        Assert.True(MissingValueContract.IsMissing(100, 100));
        Assert.False(MissingValueContract.IsMissing(100, null));
    }

    [Fact]
    public void Hole値で線分を分割する()
    {
        var points = new[]
        {
            (0d, 1d),
            (1d, 100d),
            (2d, 3d),
            (3d, 4d)
        };

        var segments = MissingValueContract.SplitSegments(points, 100).ToArray();

        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { (0d, 1d) }, segments[0]);
        Assert.Equal(new[] { (2d, 3d), (3d, 4d) }, segments[1]);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NaNと無限大は欠損判定される(double value)
    {
        Assert.True(MissingValueContract.IsMissing(value, null));
    }

    [Fact]
    public void NaNを含む点で線分を分割する()
    {
        var points = new[]
        {
            (0d, 1d),
            (1d, double.NaN),
            (2d, 3d)
        };

        var segments = MissingValueContract.SplitSegments(points, null).ToArray();

        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { (0d, 1d) }, segments[0]);
        Assert.Equal(new[] { (2d, 3d) }, segments[1]);
    }

    [Fact]
    public void Hole値を含む点で線分を分割する()
    {
        var points = new[]
        {
            (0d, 1d),
            (1d, 100d),
            (2d, 3d)
        };

        var segments = MissingValueContract.SplitSegments(points, 100).ToArray();

        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { (0d, 1d) }, segments[0]);
        Assert.Equal(new[] { (2d, 3d) }, segments[1]);
    }
}
