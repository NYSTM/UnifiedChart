namespace UnifiedChart
{
    /// <summary>
    /// 描画領域を表す単純な矩形（UI 非依存）。
    /// </summary>
    public readonly struct PlotRect
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }

        public PlotRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;
    }
}
