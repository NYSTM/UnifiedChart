namespace UnifiedChart.Tests
{
    using System.Linq;
    using UnifiedChart;
    using Xunit;

    public class AxisTickHelperTests
    {
        [Fact]
        public void GetTicks_0から100の範囲で概ね5個程度の目盛りを返す()
        {
            var range = new AxisRange(0, 100);
            var ticks = AxisTickHelper.GetTicks(range, desiredTickCount: 5).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t => Assert.InRange(t, 0, 100));
            // 0, 20, 40, 60, 80, 100 のような等間隔になるはず
            Assert.True(ticks.Count >= 4 && ticks.Count <= 7);
        }

        [Fact]
        public void GetTicks_範囲がMin以上Max以下に収まる()
        {
            var range = new AxisRange(3.3, 17.8);
            var ticks = AxisTickHelper.GetTicks(range, desiredTickCount: 5).ToList();

            Assert.All(ticks, t => Assert.InRange(t, range.Min, range.Max));
        }

        [Fact]
        public void GetTicks_MinとMaxが同じ場合は単一値を返す()
        {
            var range = new AxisRange(5, 5);
            var ticks = AxisTickHelper.GetTicks(range).ToList();

            Assert.Single(ticks);
            Assert.Equal(5, ticks[0]);
        }

        [Fact]
        public void GetTicks_NaNやInfinityを含む場合は例外にならず単一値を返す()
        {
            var range = new AxisRange(double.NaN, 10);
            var ticks = AxisTickHelper.GetTicks(range).ToList();

            Assert.Single(ticks);
        }

        [Fact]
        public void GetTicks_負の範囲でも等間隔に計算される()
        {
            var range = new AxisRange(-50, 50);
            var ticks = AxisTickHelper.GetTicks(range, desiredTickCount: 5).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t => Assert.InRange(t, -50, 50));
        }

        [Fact]
        public void GetTicks_小さい範囲でもきれいな間隔になる()
        {
            var range = new AxisRange(0, 1);
            var ticks = AxisTickHelper.GetTicks(range, desiredTickCount: 5).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t => Assert.InRange(t, 0, 1));
        }

        [Fact]
        public void GetLogTicks_1から1000のLog10範囲で10のべき乗の目盛を返す()
        {
            var logRange = new AxisRange(0, 3); // Log10(1)=0, Log10(1000)=3
            var ticks = AxisTickHelper.GetLogTicks(logRange).ToList();

            Assert.Contains(1.0, ticks);
            Assert.Contains(10.0, ticks);
            Assert.Contains(100.0, ticks);
            Assert.Contains(1000.0, ticks);
        }

        [Fact]
        public void GetLogTicks_目盛はすべてLog10範囲内に収まる()
        {
            var logRange = new AxisRange(0.5, 2.5);
            var ticks = AxisTickHelper.GetLogTicks(logRange).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t =>
            {
                var logValue = System.Math.Log10(t);
                Assert.InRange(logValue, logRange.Min - 1e-6, logRange.Max + 1e-6);
            });
        }

        [Fact]
        public void GetLogTicks_MinとMaxが同じ場合は単一値を返す()
        {
            var logRange = new AxisRange(2, 2);
            var ticks = AxisTickHelper.GetLogTicks(logRange).ToList();

            Assert.Single(ticks);
            Assert.Equal(100.0, ticks[0], 3);
        }

        [Fact]
        public void GetDateTimeTicks_数日間の範囲では日単位の目盛を返す()
        {
            var min = new System.DateTime(2024, 1, 1).ToOADate();
            var max = new System.DateTime(2024, 1, 10).ToOADate();
            var range = new AxisRange(min, max);

            var ticks = AxisTickHelper.GetDateTimeTicks(range, desiredTickCount: 5).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t => Assert.InRange(t, min, max));
        }

        [Fact]
        public void GetDateTimeTicks_数年間の範囲では年単位の目盛を返す()
        {
            var min = new System.DateTime(2015, 1, 1).ToOADate();
            var max = new System.DateTime(2024, 1, 1).ToOADate();
            var range = new AxisRange(min, max);

            var ticks = AxisTickHelper.GetDateTimeTicks(range, desiredTickCount: 5).ToList();

            Assert.NotEmpty(ticks);
            Assert.All(ticks, t =>
            {
                var date = System.DateTime.FromOADate(t);
                Assert.Equal(1, date.Month);
                Assert.Equal(1, date.Day);
            });
        }

        [Fact]
        public void GetDateTimeTicks_MinとMaxが同じ場合は単一値を返す()
        {
            var value = new System.DateTime(2024, 5, 1).ToOADate();
            var range = new AxisRange(value, value);

            var ticks = AxisTickHelper.GetDateTimeTicks(range).ToList();

            Assert.Single(ticks);
            Assert.Equal(value, ticks[0]);
        }
    }
}
