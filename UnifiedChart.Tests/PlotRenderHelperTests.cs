namespace UnifiedChart.Tests
{
    using System.Linq;
    using UnifiedChart;
    using Xunit;

    public class PlotRenderHelperTests
    {
        [Fact]
        public void GetVisibleBarCategoryLabels_全件表示できる幅ならすべて返す()
        {
            var bar = new BarSeries();
            bar.Items.Add(("A", 1));
            bar.Items.Add(("B", 2));
            bar.Items.Add(("C", 3));

            var result = PlotRenderHelper.GetVisibleBarCategoryLabels(bar, plotAreaWidth: 300, approximateLabelWidth: 10).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(("A", 0), (result[0].Category, result[0].Index));
        }

        [Fact]
        public void GetVisibleBarCategoryLabels_狭い場合は間引かれる()
        {
            var bar = new BarSeries();
            for (int i = 0; i < 10; i++)
            {
                bar.Items.Add((i.ToString(), i));
            }

            // plotAreaWidth=100, 10件 -> slotWidth=10, approximateLabelWidth=40 -> step = ceil(40/10) = 4
            var result = PlotRenderHelper.GetVisibleBarCategoryLabels(bar, plotAreaWidth: 100, approximateLabelWidth: 40).ToList();

            Assert.True(result.Count < 10);
            Assert.Equal(0, result[0].Index);
            Assert.Equal(4, result[1].Index);
        }

        [Fact]
        public void GetVisibleBarCategoryLabels_空データなら空を返す()
        {
            var bar = new BarSeries();
            var result = PlotRenderHelper.GetVisibleBarCategoryLabels(bar, plotAreaWidth: 100).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void FindNearestPoint_閾値内の最近傍点を返す()
        {
            var model = new PlotModel();
            var line = new LineSeries();
            line.Points.Add((0, 0));
            line.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            var screenX = transform.ToScreenX(10);
            var screenY = transform.ToScreenY(10);

            var nearest = PlotRenderHelper.FindNearestPoint(model, transform, screenX, screenY, thresholdPixels: 5);

            Assert.NotNull(nearest);
            Assert.Equal(10, nearest!.Value.X);
            Assert.Equal(10, nearest.Value.Y);
        }

        [Fact]
        public void FindNearestPointInfo_データインデックスを返す()
        {
            var model = new PlotModel();
            var line = new LineSeries();
            line.Points.Add((0, 0));
            line.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);
            var nearest = PlotRenderHelper.FindNearestPointInfo(
                model,
                transform,
                transform.ToScreenX(10),
                transform.ToScreenY(10),
                thresholdPixels: 5);

            Assert.True(nearest.HasValue);
            Assert.Equal(1, nearest.Value.DataIndex);
        }

        [Fact]
        public void FindNearestPoint_閾値外ならnullを返す()
        {
            var model = new PlotModel();
            var line = new LineSeries();
            line.Points.Add((0, 0));
            line.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            // データ範囲外の画面座標を指定し、どの点からも遠いことを保証する
            var nearest = PlotRenderHelper.FindNearestPoint(model, transform, -1000, -1000, thresholdPixels: 5);

            Assert.Null(nearest);
        }

        [Fact]
        public void FindNearestPoint_非単調なX座標でも線形探索で正しく見つかる()
        {
            var model = new PlotModel();
            var scatter = new ScatterSeries();
            scatter.Points.Add((5, 5));
            scatter.Points.Add((1, 1));
            scatter.Points.Add((3, 3));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(scatter);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            var screenX = transform.ToScreenX(1);
            var screenY = transform.ToScreenY(1);

            var nearest = PlotRenderHelper.FindNearestPoint(model, transform, screenX, screenY, thresholdPixels: 5);

            Assert.NotNull(nearest);
            Assert.Equal(1, nearest!.Value.X);
            Assert.Equal(1, nearest.Value.Y);
        }

        [Fact]
        public void FindNearestPoint_非表示シリーズは無視される()
        {
            var model = new PlotModel();
            var line = new LineSeries { Visible = false };
            line.Points.Add((5, 5));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            var screenX = transform.ToScreenX(5);
            var screenY = transform.ToScreenY(5);

            var nearest = PlotRenderHelper.FindNearestPoint(model, transform, screenX, screenY, thresholdPixels: 5);

            Assert.Null(nearest);
        }

        [Fact]
        public void CalculateHistogramBins_通常データを均等なビンに分割する()
        {
            var histogram = new HistogramSeries { BinCount = 5 };
            for (int i = 0; i < 100; i++)
            {
                histogram.Values.Add(i / 10.0); // 0.0 ~ 9.9
            }

            var bins = PlotRenderHelper.CalculateHistogramBins(histogram);

            Assert.Equal(5, bins.Count);
            Assert.Equal(100, bins.Sum(b => b.Count));
            Assert.Equal(0.0, bins[0].BinStart, 3);
            Assert.Equal(9.9, bins[^1].BinEnd, 3);
        }

        [Fact]
        public void CalculateHistogramBins_データが空なら空リストを返す()
        {
            var histogram = new HistogramSeries();

            var bins = PlotRenderHelper.CalculateHistogramBins(histogram);

            Assert.Empty(bins);
        }

        [Fact]
        public void CalculateHistogramBins_全て同値なら単一ビンにまとめる()
        {
            var histogram = new HistogramSeries();
            histogram.Values.Add(5);
            histogram.Values.Add(5);
            histogram.Values.Add(5);

            var bins = PlotRenderHelper.CalculateHistogramBins(histogram);

            Assert.Single(bins);
            Assert.Equal(3, bins[0].Count);
        }

        [Fact]
        public void CalculateHistogramBins_NaNとInfinityは無視される()
        {
            var histogram = new HistogramSeries { BinCount = 2 };
            histogram.Values.Add(1);
            histogram.Values.Add(double.NaN);
            histogram.Values.Add(2);
            histogram.Values.Add(double.PositiveInfinity);

            var bins = PlotRenderHelper.CalculateHistogramBins(histogram);

            Assert.Equal(2, bins.Sum(b => b.Count));
        }

        [Fact]
        public void CreateTransform_BoxPlotSeriesのMinMaxがY軸範囲に反映される()
        {
            var model = new PlotModel();
            var boxPlot = new BoxPlotSeries();
            boxPlot.Items.Add(("A", 1, 3, 5, 7, 10));
            boxPlot.Items.Add(("B", -2, 0, 2, 4, 6));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(boxPlot);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Min <= -2);
            Assert.True(transform.YRange.Max >= 10);
        }

        [Fact]
        public void CreateTransform_BoxPlotSeriesのNaNは無視される()
        {
            var model = new PlotModel();
            var boxPlot = new BoxPlotSeries();
            boxPlot.Items.Add(("A", double.NaN, 3, 5, 7, double.NaN));
            boxPlot.Items.Add(("B", 1, 2, 3, 4, 5));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(boxPlot);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Min <= 1);
            Assert.True(transform.YRange.Max >= 5);
        }

        [Fact]
        public void CalculatePieSlices_割合と角度が合計に一致する()
        {
            var pie = new PieSeries();
            pie.Items.Add(("A", 25));
            pie.Items.Add(("B", 75));

            var slices = PlotRenderHelper.CalculatePieSlices(pie);

            Assert.Equal(2, slices.Count);
            Assert.Equal(0.25, slices[0].Percentage, 3);
            Assert.Equal(0.75, slices[1].Percentage, 3);
            Assert.Equal(90.0, slices[0].SweepAngle, 3);
            Assert.Equal(270.0, slices[1].SweepAngle, 3);
            Assert.Equal(0.0, slices[0].StartAngle, 3);
            Assert.Equal(90.0, slices[1].StartAngle, 3);
        }

        [Fact]
        public void CalculatePieSlices_負の値やNaNは無視される()
        {
            var pie = new PieSeries();
            pie.Items.Add(("A", 10));
            pie.Items.Add(("B", -5));
            pie.Items.Add(("C", double.NaN));
            pie.Items.Add(("D", 10));

            var slices = PlotRenderHelper.CalculatePieSlices(pie);

            Assert.Equal(2, slices.Count);
            Assert.Equal(0.5, slices[0].Percentage, 3);
            Assert.Equal(0.5, slices[1].Percentage, 3);
        }

        [Fact]
        public void CalculatePieSlices_合計が0以下なら空リストを返す()
        {
            var pie = new PieSeries();
            pie.Items.Add(("A", 0));
            pie.Items.Add(("B", -5));

            var slices = PlotRenderHelper.CalculatePieSlices(pie);

            Assert.Empty(slices);
        }

        [Fact]
        public void CreateTransform_AreaSeriesの点とBaselineValueがY軸範囲に反映される()
        {
            var model = new PlotModel();
            var area = new AreaSeries { BaselineValue = 0 };
            area.Points.Add((0, 5));
            area.Points.Add((10, -3));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(area);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Min <= -3);
            Assert.True(transform.YRange.Max >= 5);
        }

        [Fact]
        public void CreateTransform_AreaSeriesのNaNは無視される()
        {
            var model = new PlotModel();
            var area = new AreaSeries();
            area.Points.Add((0, double.NaN));
            area.Points.Add((1, 4));
            area.Points.Add((2, double.PositiveInfinity));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(area);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Max >= 4);
        }

        [Fact]
        public void CreateTransform_StackedBarSeriesの積み上げ合計がY軸範囲に反映される()
        {
            var model = new PlotModel();
            var stackedBar = new StackedBarSeries();
            stackedBar.Items.Add(("A", new System.Collections.Generic.List<double> { 3, 5 }));
            stackedBar.Items.Add(("B", new System.Collections.Generic.List<double> { -2, -4 }));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(stackedBar);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Max >= 8);
            Assert.True(transform.YRange.Min <= -6);
        }

        [Fact]
        public void CalculateStackedBarSegments_正の値は下から積み上がる()
        {
            var stackedBar = new StackedBarSeries();
            stackedBar.Items.Add(("A", new System.Collections.Generic.List<double> { 3, 5, 2 }));

            var segments = PlotRenderHelper.CalculateStackedBarSegments(stackedBar, 0);

            Assert.Equal(3, segments.Count);
            Assert.Equal((0, 0.0, 3.0), segments[0]);
            Assert.Equal((1, 3.0, 8.0), segments[1]);
            Assert.Equal((2, 8.0, 10.0), segments[2]);
        }

        [Fact]
        public void CalculateStackedBarSegments_負の値は別方向に積み上がりNaNは無視される()
        {
            var stackedBar = new StackedBarSeries();
            stackedBar.Items.Add(("A", new System.Collections.Generic.List<double> { 4, double.NaN, -3, -2 }));

            var segments = PlotRenderHelper.CalculateStackedBarSegments(stackedBar, 0);

            Assert.Equal(3, segments.Count);
            Assert.Equal((0, 0.0, 4.0), segments[0]);
            Assert.Equal((2, -3.0, 0.0), segments[1]);
            Assert.Equal((3, -5.0, -3.0), segments[2]);
        }

        [Fact]
        public void CreateTransform_RangeSeriesのLowHighがY軸範囲に反映される()
        {
            var model = new PlotModel();
            var range = new RangeSeries();
            range.Points.Add((0, -3, 5));
            range.Points.Add((1, -1, 8));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(range);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Min <= -3);
            Assert.True(transform.YRange.Max >= 8);
        }

        [Fact]
        public void CreateTransform_RangeSeriesのNaNは無視される()
        {
            var model = new PlotModel();
            var range = new RangeSeries();
            range.Points.Add((0, double.NaN, double.NaN));
            range.Points.Add((1, 2, 6));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(range);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.YRange.Min <= 2);
            Assert.True(transform.YRange.Max >= 6);
        }

        [Fact]
        public void CreateTransform_対数Y軸が有効な場合はLog10空間の範囲になる()
        {
            var model = new PlotModel();
            model.YAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((0, 1));
            line.Points.Add((1, 100));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.IsYLogarithmic);
            // Log10(1)=0, Log10(100)=2 を含む範囲になるはず
            Assert.True(transform.YRange.Min <= 0);
            Assert.True(transform.YRange.Max >= 2);
        }

        [Fact]
        public void CreateTransform_対数Y軸で0以下の値は無視される()
        {
            var model = new PlotModel();
            model.YAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((0, -5));
            line.Points.Add((1, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.IsYLogarithmic);
            Assert.True(transform.YRange.Max >= 1);
        }

        [Fact]
        public void CreateTransform_対数Y軸でToScreenYとToDataYが往復変換できる()
        {
            var model = new PlotModel();
            model.YAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((0, 1));
            line.Points.Add((1, 1000));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            var screenY = transform.ToScreenY(100);
            var dataY = transform.ToDataY(screenY);

            Assert.Equal(100, dataY, 1);
        }

        [Fact]
        public void CreateTransform_対数X軸が有効な場合はLog10空間の範囲になる()
        {
            var model = new PlotModel();
            model.XAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((1, 0));
            line.Points.Add((100, 1));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.IsXLogarithmic);
            Assert.True(transform.XRange.Min <= 0);
            Assert.True(transform.XRange.Max >= 2);
        }

        [Fact]
        public void CreateTransform_対数X軸で0以下の値は無視される()
        {
            var model = new PlotModel();
            model.XAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((-5, 0));
            line.Points.Add((10, 1));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.IsXLogarithmic);
            Assert.True(transform.XRange.Max >= 1);
        }

        [Fact]
        public void CreateTransform_対数X軸でToScreenXとToDataXが往復変換できる()
        {
            var model = new PlotModel();
            model.XAxis.IsLogarithmic = true;
            var line = new LineSeries();
            line.Points.Add((1, 0));
            line.Points.Add((1000, 1));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            var screenX = transform.ToScreenX(100);
            var dataX = transform.ToDataX(screenX);

            Assert.Equal(100, dataX, 1);
        }

        [Fact]
        public void CreateTransform_Y2Axis未設定なら第2Y軸は無効()
        {
            var model = new PlotModel();
            var line = new LineSeries();
            line.Points.Add((0, 0));
            line.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.False(transform.HasSecondaryYAxis);
        }

        [Fact]
        public void CreateTransform_UseSecondaryYAxis系列は第2Y軸範囲に反映され主軸から除外される()
        {
            var model = new PlotModel();
            model.Y2Axis = new Axis();

            var primaryLine = new LineSeries();
            primaryLine.Points.Add((0, 0));
            primaryLine.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(primaryLine);

            var secondaryLine = new LineSeries { UseSecondaryYAxis = true };
            secondaryLine.Points.Add((0, 1000));
            secondaryLine.Points.Add((10, 2000));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(secondaryLine);

            var plotArea = new PlotRect(0, 0, 100, 100);
            var transform = PlotRenderHelper.CreateTransform(model, plotArea);

            Assert.True(transform.HasSecondaryYAxis);
            // 主軸の範囲は secondaryLine の値(1000,2000)を含まない
            Assert.True(transform.YRange.Max < 1000);
            // 第2軸の範囲は secondaryLine の値を含む
            Assert.True(transform.Y2Range.Min <= 1000);
            Assert.True(transform.Y2Range.Max >= 2000);
        }

        [Fact]
        public void CreateTransform_ズーム指定時は第2Y軸にも表示範囲を適用する()
        {
            var model = new PlotModel { Y2Axis = new Axis() };
            var primaryLine = new LineSeries();
            primaryLine.Points.Add((0, 0));
            primaryLine.Points.Add((10, 10));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(primaryLine);

            var secondaryLine = new LineSeries { UseSecondaryYAxis = true };
            secondaryLine.Points.Add((0, 100));
            secondaryLine.Points.Add((10, 200));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(secondaryLine);

            var zoomedY2 = new AxisRange(120, 180);
            var transform = PlotRenderHelper.CreateTransform(
                model,
                new PlotRect(0, 0, 100, 100),
                new AxisRange(2, 8),
                new AxisRange(2, 8),
                zoomedY2);

            Assert.Equal(zoomedY2.Min, transform.Y2Range.Min);
            Assert.Equal(zoomedY2.Max, transform.Y2Range.Max);
            Assert.Equal(100, transform.ToScreenY2(120));
            Assert.Equal(0, transform.ToScreenY2(180));
        }
    }
}
