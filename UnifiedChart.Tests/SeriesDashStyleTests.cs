namespace UnifiedChart.Tests
{
    using Xunit;

    public class SeriesDashStyleTests
    {
        [Fact]
        public void LineSeries_DashStyleの既定値はSolid()
        {
            var line = new LineSeries();

            Assert.Equal(LineDashStyle.Solid, line.DashStyle);
        }

        [Fact]
        public void LineSeries_DashStyleを変更できる()
        {
            var line = new LineSeries { DashStyle = LineDashStyle.DashDot };

            Assert.Equal(LineDashStyle.DashDot, line.DashStyle);
        }

        [Fact]
        public void AreaSeries_DashStyleの既定値はSolid()
        {
            var area = new AreaSeries();

            Assert.Equal(LineDashStyle.Solid, area.DashStyle);
        }

        [Fact]
        public void AreaSeries_DashStyleを変更できる()
        {
            var area = new AreaSeries { DashStyle = LineDashStyle.Dot };

            Assert.Equal(LineDashStyle.Dot, area.DashStyle);
        }
    }
}
