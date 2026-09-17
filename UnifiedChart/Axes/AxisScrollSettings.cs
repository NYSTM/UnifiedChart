namespace UnifiedChart;

public sealed class AxisScrollSettings
{
    public double Scale { get; set; } = 1.0;
    public double? Unit { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool SynchronizeWithY2 { get; set; }
}
