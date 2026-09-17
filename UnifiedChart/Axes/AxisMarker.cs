namespace UnifiedChart;

public sealed class AxisMarker
{
    public double Value { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Color { get; set; } = "Black";

    public string ArrowDirection { get; set; } = "Auto";

    public bool ShowArrow { get; set; } = true;

    public bool ShowConnectionLine { get; set; } = true;
}
