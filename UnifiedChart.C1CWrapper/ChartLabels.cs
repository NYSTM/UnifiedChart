namespace UnifiedChart.C1CWrapper;

public sealed class ChartLabels : List<ChartLabel>
{
    public ChartLabel Add(string seriesName, int dataIndex, string text)
    {
        var label = new ChartLabel
        {
            SeriesName = seriesName ?? string.Empty,
            DataIndex = dataIndex,
            Text = text ?? string.Empty
        };
        Add(label);
        return label;
    }
}
