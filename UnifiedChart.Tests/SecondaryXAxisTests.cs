namespace UnifiedChart.Tests;

using UnifiedChart;
using Xunit;

public class SecondaryXAxisTests
{
    [Fact]
    public void X2AxisはX2系列の範囲を自動計算する()
    {
        var model = new PlotModel { X2Axis = new Axis() };
        var primary = new LineSeries();
        primary.Points.Add((0, 10));
        primary.Points.Add((10, 20));
        var secondary = new LineSeries { UseSecondaryXAxis = true };
        secondary.Points.Add((100, 30));
        secondary.Points.Add((200, 40));
        model.ChartGroups[0].ChartData[0].SeriesList.Add(primary);
        model.ChartGroups[0].ChartData[0].SeriesList.Add(secondary);

        var transform = PlotRenderHelper.CreateTransform(model, new PlotRect(0, 0, 100, 100));

        Assert.True(transform.HasSecondaryXAxis);
        Assert.Equal(95, transform.X2Range.Min);
        Assert.Equal(205, transform.X2Range.Max);
        Assert.Equal(100, transform.ToDataX2(transform.ToScreenX2(100)), 6);
    }

    [Fact]
    public void X2Axis未設定の系列は主X軸を使う()
    {
        var model = new PlotModel();
        var series = new LineSeries { UseSecondaryXAxis = true };
        series.Points.Add((0, 0));
        series.Points.Add((10, 10));
        model.ChartGroups[0].ChartData[0].SeriesList.Add(series);

        var transform = PlotRenderHelper.CreateTransform(model, new PlotRect(0, 0, 100, 100));

        Assert.False(transform.HasSecondaryXAxis);
    }
}
