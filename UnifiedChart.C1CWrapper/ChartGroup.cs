namespace UnifiedChart.C1CWrapper;

public sealed class ChartGroup
{
    internal delegate void CoordinateLookupDelegate(int x, int y, CoordinateFocusEnum focus, out int seriesIndex, out int pointIndex, out int distance);

    internal CoordinateLookupDelegate? CoordinateLookup { get; set; }

    internal int GroupIndex { get; set; }
    public string Name { get; set; } = string.Empty;

    public Chart2DType ChartType { get; set; } = Chart2DType.Line;

    public double? MissingValueHole { get; set; }

    public bool DefaultDataSerializer { get; set; }

    public bool UseSecondaryYAxis { get; set; }

    public bool UseSecondaryXAxis { get; set; }

    public ChartDataCollection ChartData { get; } = new() { new ChartData() };

    public ChartData this[int index] => ChartData[index];

    public void CoordToDataIndex(int x, int y, CoordinateFocusEnum focus, out int seriesIndex, out int pointIndex, out int distance)
    {
        if (CoordinateLookup is null)
        {
            seriesIndex = pointIndex = distance = -1;
            return;
        }

        CoordinateLookup(x, y, focus, out seriesIndex, out pointIndex, out distance);
    }

    public void CoordToDataIndex(int x, int y, CoordianteFocusEnum focus, out int seriesIndex, out int pointIndex, out int distance)
        => CoordToDataIndex(x, y, (CoordinateFocusEnum)focus, out seriesIndex, out pointIndex, out distance);

    public ChartData AddNewChartData()
    {
        var data = new ChartData();
        ChartData.Add(data);
        return data;
    }
}
