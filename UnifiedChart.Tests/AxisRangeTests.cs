namespace UnifiedChart.Tests
{
    using UnifiedChart;
    using Xunit;

    public class AxisRangeTests
    {
        [Fact]
        public void Calculate_通常データ_パディング付き範囲を返す()
        {
            var range = AxisRange.Calculate(null, null, 0, 10, hasData: true);
            Assert.True(range.Min < 0);
            Assert.True(range.Max > 10);
        }

        [Fact]
        public void Calculate_データなし_既定範囲を返す()
        {
            var range = AxisRange.Calculate(null, null, 0, 0, hasData: false);
            Assert.True(range.Min < range.Max);
        }

        [Fact]
        public void Calculate_単一点_有効な範囲を返す()
        {
            var range = AxisRange.Calculate(null, null, 5, 5, hasData: true);
            Assert.True(range.Min < 5);
            Assert.True(range.Max > 5);
        }

        [Fact]
        public void Calculate_同値ゼロ_有効な範囲を返す()
        {
            var range = AxisRange.Calculate(null, null, 0, 0, hasData: true);
            Assert.True(range.Min < 0);
            Assert.True(range.Max > 0);
        }

        [Fact]
        public void Calculate_NaNやInfinityを含む_既定範囲にフォールバックする()
        {
            var range1 = AxisRange.Calculate(null, null, double.NaN, 10, hasData: true);
            Assert.False(double.IsNaN(range1.Min));
            Assert.False(double.IsNaN(range1.Max));

            var range2 = AxisRange.Calculate(null, null, 0, double.PositiveInfinity, hasData: true);
            Assert.False(double.IsInfinity(range2.Max));
        }

        [Fact]
        public void Calculate_明示的な最小最大_そのまま使用する()
        {
            var range = AxisRange.Calculate(2, 8, 0, 10, hasData: true);
            Assert.Equal(2, range.Min);
            Assert.Equal(8, range.Max);
        }

        [Fact]
        public void Calculate_負の範囲_正しく処理する()
        {
            var range = AxisRange.Calculate(null, null, -10, -5, hasData: true);
            Assert.True(range.Min < -10);
            Assert.True(range.Max > -5);
        }

        [Fact]
        public void TranslateClamped_表示幅を維持して全体範囲内へ制限する()
        {
            var display = new AxisRange(2, 6);
            var bounds = new AxisRange(0, 10);

            var translated = display.TranslateClamped(20, bounds);

            Assert.Equal(6, translated.Min);
            Assert.Equal(10, translated.Max);
        }

        [Fact]
        public void ScrollClamped_負方向でも全体範囲を越えない()
        {
            var display = new AxisRange(0, 4);
            var bounds = new AxisRange(0, 10);

            var scrolled = display.ScrollClamped(bounds, -1);

            Assert.Equal(0, scrolled.Min);
            Assert.Equal(4, scrolled.Max);
        }
    }
}
