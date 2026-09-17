namespace UnifiedChart;

public sealed class AxisValueLabel
{
    public double Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "Black";
    public bool IsVisible { get; set; } = true;
}
