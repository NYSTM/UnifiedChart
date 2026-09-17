namespace UnifiedChart.Tests
{
    using Xunit;

    public class PlotModelTests
    {
        [Fact]
        public void RadarSeriesの塗りつぶし既定値は無効()
        {
            var series = new RadarSeries();

            Assert.False(series.FillArea);
        }

        [Fact]
        public void RadarSeriesのマーカーと線の既定値はLineSeriesと同じ()
        {
            var radar = new RadarSeries();
            var line = new LineSeries();

            Assert.Equal(line.MarkerSize, radar.MarkerSize);
            Assert.Equal(line.StrokeThickness, radar.StrokeThickness);
        }

        [Fact]
        public void ShowGridLines_既定値はtrue()
        {
            var model = new PlotModel();

            Assert.True(model.ShowGridLines);
        }

    [Fact]
    public void RadarDirectionの既定値は反時計回り()
    {
        var model = new PlotModel();

        Assert.Equal(RadarDirection.CounterClockwise, model.RadarDirection);
    }

    [Fact]
    public void RadarDirectionは時計回りへ変更できる()
    {
        var model = new PlotModel { RadarDirection = RadarDirection.Clockwise };

        Assert.Equal(RadarDirection.Clockwise, model.RadarDirection);
    }

        [Fact]
        public void FontFamilyName_既定値はSegoeUI()
        {
            var model = new PlotModel();

            Assert.Equal("Meiryo UI", model.FontFamilyName);
        }

        [Fact]
        public void FontFamilyName_変更できる()
        {
            var model = new PlotModel { FontFamilyName = "Meiryo" };

            Assert.Equal("Meiryo", model.FontFamilyName);
        }

        [Fact]
        public void 表示設定の既定値は従来の固定値を維持する()
        {
            var model = new PlotModel();

            Assert.Equal(12f, model.TitleFontSize);
            Assert.Equal(9f, model.FooterFontSize);
            Assert.Equal(8f, model.AxisLabelFontSize);
            Assert.Equal(9f, model.AxisTitleFontSize);
            Assert.Equal(8f, model.DataLabelFontSize);
            Assert.Equal(8f, model.LegendFontSize);
            Assert.Equal(8f, model.ValueLabelFontSize);
            Assert.Equal(48d, model.PlotMarginLeft);
            Assert.Equal(32d, model.PlotMarginTop);
            Assert.Equal(16d, model.PlotMarginRight);
            Assert.Equal(32d, model.PlotMarginBottom);
        }

        [Fact]
        public void ShowGridLines_falseに設定できる()
        {
            var model = new PlotModel { ShowGridLines = false };

            Assert.False(model.ShowGridLines);
        }

        [Fact]
        public void LegendPosition_既定値はTopRight()
        {
            var model = new PlotModel();

            Assert.Equal(LegendPosition.TopRight, model.LegendPosition);
        }

        [Fact]
        public void LegendPosition_変更できる()
        {
            var model = new PlotModel { LegendPosition = LegendPosition.BottomLeft };

            Assert.Equal(LegendPosition.BottomLeft, model.LegendPosition);
        }

        [Fact]
        public void ShowLegend_既定値はtrue()
        {
            var model = new PlotModel();

            Assert.True(model.ShowLegend);
        }

        [Fact]
        public void ShowLegend_falseに設定できる()
        {
            var model = new PlotModel { ShowLegend = false };

            Assert.False(model.ShowLegend);
        }

        [Fact]
        public void Annotations_既定は空で追加できる()
        {
            var model = new PlotModel();

            Assert.Empty(model.Annotations);

            model.Annotations.Add(new TextAnnotation { X = 1, Y = 2, Text = "note", Color = "Red" });

            Assert.Single(model.Annotations);
            Assert.Equal("note", model.Annotations[0].Text);
        }

        [Fact]
        public void ChartGroups_ChartData_SeriesListの階層を保持する()
        {
            var model = new PlotModel();
            var group = new ChartGroup();
            var data = new ChartData();
            var series = new LineSeries();

            data.SeriesList.Add(series);
            group.ChartData.Add(data);
            model.ChartGroups.Add(group);

            Assert.Same(series, model.ChartGroups[1].ChartData[1].SeriesList[0]);
            Assert.Same(series, Assert.Single(model.GetAllSeries()));
        }
    }
}
