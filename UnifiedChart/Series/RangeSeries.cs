namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 上限値と下限値の帯を塗りつぶして表示する範囲グラフの系列。
    /// </summary>
    public class RangeSeries : Series
    {
        public List<(double X, double Low, double High)> Points { get; } = new();

        public string Color { get; set; } = "SteelBlue";

        public string FillColor { get; set; } = "LightSkyBlue";

        public double StrokeThickness { get; set; } = 1;
    }
}
