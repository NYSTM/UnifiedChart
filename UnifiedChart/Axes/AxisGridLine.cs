namespace UnifiedChart;

public sealed class AxisGridLine
{
    public double Value { get; set; }

    public string Color { get; set; } = "LightGray";

    public string DashStyle { get; set; } = "Dash";

    public float Thickness { get; set; } = 1;

    public bool IsVisible { get; set; } = true;
}
