namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 折れ線グラフの系列。
    /// </summary>
    public class LineSeries : Series
    {
        public List<(double X, double Y)> Points { get; } = new();
        public string Color { get; set; } = "Black";
        public string MarkerOutlineColor { get; set; } = "Black";
        public double StrokeThickness { get; set; } = 2.0;
        public LineDashStyle DashStyle { get; set; } = LineDashStyle.Solid;

        /// <summary>
        /// データ点にマーカーを表示するかどうか。
        /// </summary>
        public bool ShowMarkers { get; set; } = false;

        /// <summary>
        /// データ点に表示するマーカーの形状。
        /// </summary>
        public MarkerShape MarkerShape { get; set; } = MarkerShape.Circle;

        /// <summary>
        /// マーカーのサイズ。
        /// </summary>
        public double MarkerSize { get; set; } = 3.0;
    }
}
