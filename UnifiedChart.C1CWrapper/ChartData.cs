namespace UnifiedChart.C1CWrapper;

public sealed class ChartData
{
    public List<ChartDataSeries> SeriesList { get; } = new();

    public ChartDataSeries this[int index] => SeriesList[index];

    public ChartDataSeries? FindByLabel(string label)
        => SeriesList.FirstOrDefault(series => string.Equals(series.Label, label, StringComparison.Ordinal));

    public void Clear()
        => SeriesList.Clear();

    public int Count => SeriesList.Count;

    public ChartDataSeries AddNewSeries()
    {
        var series = new ChartDataSeries();
        SeriesList.Add(series);
        return series;
    }
}
