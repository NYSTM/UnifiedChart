namespace UnifiedChart;

public sealed class ChartData
{
    public List<Series> SeriesList { get; } = new();

    public Series AddSeries(Series series)
    {
        ArgumentNullException.ThrowIfNull(series);
        SeriesList.Add(series);
        return series;
    }
}
