namespace UnifiedChart.C1CWrapper;

public sealed class ChartLegend
{
    public bool Visible { get; set; } = true;

    public UnifiedChart.LegendPosition Position { get; set; } = UnifiedChart.LegendPosition.TopRight;
}
