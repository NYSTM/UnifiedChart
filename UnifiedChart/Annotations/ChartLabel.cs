namespace UnifiedChart;

/// <summary>
/// 系列のデータ点に結合して表示する任意ラベル。
/// </summary>
public sealed class ChartLabel
{
    public string SeriesName { get; set; } = string.Empty;

    public int DataIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Color { get; set; } = "Black";

    public bool Auto { get; set; } = true;

    public bool ShowConnectionLine { get; set; } = true;

    public double OffsetX { get; set; } = 8;

    public double OffsetY { get; set; } = -8;

    public double X { get; set; }

    public double Y { get; set; }

    public bool IsResolved { get; set; }
}
