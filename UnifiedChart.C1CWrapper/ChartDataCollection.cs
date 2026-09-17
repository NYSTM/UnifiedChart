namespace UnifiedChart.C1CWrapper;

public sealed class ChartDataCollection : List<ChartData>
{
    public List<ChartDataSeries> SeriesList
        => Count == 0 ? [] : this[0].SeriesList;
}
