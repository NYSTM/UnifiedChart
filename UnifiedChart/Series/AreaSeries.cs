namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 面グラフの系列。折れ線と同様の点列を持ち、基準値(BaselineValue)から各点までの領域を塗りつぶして描画する。
    /// </summary>
    public class AreaSeries : Series
    {
        public List<(double X, double Y)> Points { get; } = new();
        public string Color { get; set; } = "Blue";
        public string FillColor { get; set; } = "LightBlue";
        public double StrokeThickness { get; set; } = 2;
        public double BaselineValue { get; set; } = 0;
        public LineDashStyle DashStyle { get; set; } = LineDashStyle.Solid;
    }
}
