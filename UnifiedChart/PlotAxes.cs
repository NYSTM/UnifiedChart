namespace UnifiedChart;

public sealed class PlotAxes
{
    public Axis XAxis { get; } = new();

    public Axis? X2Axis { get; set; }

    public Axis YAxis { get; } = new();

    public Axis? Y2Axis { get; set; }
}
