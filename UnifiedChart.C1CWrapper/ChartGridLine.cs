namespace UnifiedChart.C1CWrapper;

public sealed class ChartGridLine
{
    public double Value { get; set; }

    public string Color { get; set; } = "LightGray";

    public string DashStyle { get; set; } = "Dash";

    public float Thickness { get; set; } = 1;

    public bool Visible { get; set; } = true;
}
