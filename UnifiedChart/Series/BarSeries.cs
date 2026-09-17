namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 縦横の棒グラフの系列。
    /// </summary>
    public class BarSeries : Series
    {
        public List<(string Category, double Value)> Items { get; } = new();
        public List<double> XValues { get; } = new();
        public string Color { get; set; } = "SteelBlue";
    }
}
