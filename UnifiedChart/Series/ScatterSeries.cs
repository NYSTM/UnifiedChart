namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 散布図の系列。
    /// </summary>
    public class ScatterSeries : Series
    {
        public List<(double X, double Y)> Points { get; } = new();
        public string Color { get; set; } = "Black";
        public double MarkerSize { get; set; } = 3.0;
        public MarkerShape MarkerShape { get; set; } = MarkerShape.Circle;
    }
}
