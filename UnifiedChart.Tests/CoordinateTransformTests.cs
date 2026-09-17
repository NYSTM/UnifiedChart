namespace UnifiedChart.Tests
{
    using UnifiedChart;
    using Xunit;

    public class CoordinateTransformTests
    {
        [Fact]
        public void ToScreenX_範囲の最小値_プロット領域の左端になる()
        {
            var area = new PlotRect(0, 0, 100, 200);
            var xRange = new AxisRange(0, 10);
            var yRange = new AxisRange(0, 10);
            var t = new CoordinateTransform(area, xRange, yRange);

            Assert.Equal(0, t.ToScreenX(0), 3);
            Assert.Equal(100, t.ToScreenX(10), 3);
        }

        [Fact]
        public void ToScreenY_Y軸は反転する()
        {
            var area = new PlotRect(0, 0, 100, 200);
            var xRange = new AxisRange(0, 10);
            var yRange = new AxisRange(0, 10);
            var t = new CoordinateTransform(area, xRange, yRange);

            // データ最大値(10)は画面上端(Top=0)、最小値(0)は画面下端(Bottom=200)になる
            Assert.Equal(200, t.ToScreenY(0), 3);
            Assert.Equal(0, t.ToScreenY(10), 3);
        }

        [Fact]
        public void 往復変換_元の値に戻る()
        {
            var area = new PlotRect(10, 20, 300, 150);
            var xRange = new AxisRange(-5, 5);
            var yRange = new AxisRange(0, 100);
            var t = new CoordinateTransform(area, xRange, yRange);

            var sx = t.ToScreenX(2.5);
            var sy = t.ToScreenY(42);

            Assert.Equal(2.5, t.ToDataX(sx), 3);
            Assert.Equal(42, t.ToDataY(sy), 3);
        }

        [Fact]
        public void HasSecondaryYAxis_Y2Range未指定の場合はfalse()
        {
            var area = new PlotRect(0, 0, 100, 200);
            var xRange = new AxisRange(0, 10);
            var yRange = new AxisRange(0, 10);
            var t = new CoordinateTransform(area, xRange, yRange);

            Assert.False(t.HasSecondaryYAxis);
        }

        [Fact]
        public void ToScreenY2_第2Y軸の範囲で変換される()
        {
            var area = new PlotRect(0, 0, 100, 200);
            var xRange = new AxisRange(0, 10);
            var yRange = new AxisRange(0, 10);
            var y2Range = new AxisRange(0, 100);
            var t = new CoordinateTransform(area, xRange, yRange, false, false, y2Range, false);

            Assert.True(t.HasSecondaryYAxis);
            Assert.Equal(200, t.ToScreenY2(0), 3);
            Assert.Equal(0, t.ToScreenY2(100), 3);
        }

        [Fact]
        public void ToDataY2_往復変換_元の値に戻る()
        {
            var area = new PlotRect(0, 0, 100, 200);
            var xRange = new AxisRange(0, 10);
            var yRange = new AxisRange(0, 10);
            var y2Range = new AxisRange(0, 100);
            var t = new CoordinateTransform(area, xRange, yRange, false, false, y2Range, false);

            var sy = t.ToScreenY2(55);
            Assert.Equal(55, t.ToDataY2(sy), 3);
        }
    }
}
