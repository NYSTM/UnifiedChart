namespace UnifiedChart.C1CWrapper;

public sealed class ChartDataSeries
{
    private int? _pointDataLength;

    public string Label { get; set; } = string.Empty;

    public SeriesDisplayEnum Display { get; set; } = SeriesDisplayEnum.Show;

    public ChartDataPointData PointData { get; }

    public ChartSymbolStyle SymbolStyle { get; } = new();

    public ChartFillStyle FillStyle { get; } = new();

    public string TooltipText { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public List<string> DataTypes { get; } = new();

    public ChartDataValues  X { get; } = new();

    public ChartDataValues Y { get; } = new();

    public List<string> CategoryLabels { get; } = new();

    public string Color { get; set; } = "Black";

    public string MarkerOutlineColor { get; set; } = "Black";

    public bool FillArea { get; set; }

    public double StrokeThickness { get; set; } = 2.0;

    public bool ConnectAcrossMissingValues { get; set; } = true;

    public bool ShowMarkers { get; set; }

    public UnifiedChart.MarkerShape MarkerShape
    {
        get => SymbolStyle.GetMarkerShape();
        set => SymbolStyle.Shape = value;
    }

    public double MarkerSize
    {
        get => SymbolStyle.Size;
        set => SymbolStyle.Size = value;
    }

    public ChartDataSeries()
    {
        PointData = new(() => Count, value => PointDataLength = value);
    }

    public int PointDataLength
    {
        get => Count;
        set => _pointDataLength = Math.Max(0, value);
    }

    public int Count
    {
        get
        {
            var availableCount = Math.Min(X.Count, Y.Count);
            return _pointDataLength.HasValue
                ? Math.Min(availableCount, _pointDataLength.Value)
                : availableCount;
        }
    }

    public void Add(double x, double y)
    {
        X.Add(x);
        Y.Add(y);
    }

    public void Clear()
    {
        X.Clear();
        Y.Clear();
    }

    public void CopyDataIn(IEnumerable<double> xValues, IEnumerable<double> yValues)
    {
        ArgumentNullException.ThrowIfNull(xValues);
        ArgumentNullException.ThrowIfNull(yValues);

        Clear();
        foreach (var x in xValues)
        {
            X.Add(x);
        }
        foreach (var y in yValues)
        {
            Y.Add(y);
        }
    }

    internal UnifiedChart.Series ToUnifiedSeries(Chart2DType chartType, double? missingValueHole = null)
    {
        UnifiedChart.Series series = chartType switch
        {
            Chart2DType.XYScatter => new UnifiedChart.ScatterSeries(),
            Chart2DType.Bar or Chart2DType.Column => new UnifiedChart.BarSeries(),
            Chart2DType.Area => new UnifiedChart.AreaSeries(),
            Chart2DType.Pie => new UnifiedChart.PieSeries(),
            Chart2DType.Radar => new UnifiedChart.RadarSeries(),
            _ => new UnifiedChart.LineSeries { ShowMarkers = chartType == Chart2DType.LineSymbols }
        };

        series.Name = Label;
        series.Visible = Display == SeriesDisplayEnum.Show;
        series.MissingValueHole = missingValueHole;
        series.ConnectAcrossMissingValues = ConnectAcrossMissingValues;

        switch (series)
        {
            case UnifiedChart.PieSeries pie:
                pie.IsDoughnut = chartType == Chart2DType.Doughnut;
                for (var i = 0; i < Math.Min(X.Count, Y.Count); i++)
                {
                    pie.Items.Add((X[i].ToString("0.##"), Y[i]));
                }
                break;
            case UnifiedChart.BarSeries bar:
                for (var i = 0; i < Count; i++)
                {
                    var category = i < X.Count ? X[i].ToString("0.##") : i.ToString();
                    bar.Items.Add((category, Y[i]));
                    bar.XValues.Add(i < X.Count ? X[i] : i);
                }
                    bar.Color = GetFillColorName();
                break;
            case UnifiedChart.AreaSeries area:
                AddPoints(area.Points);
                area.Color = GetOutlineColorName();
                area.FillColor = GetFillColorName();
                area.StrokeThickness = StrokeThickness;
                break;
            case UnifiedChart.ScatterSeries scatter:
                AddPoints(scatter.Points);
                scatter.Color = GetSymbolColor();
                scatter.MarkerShape = SymbolStyle.GetMarkerShape();
                scatter.MarkerSize = SymbolStyle.Size;
                break;
            case UnifiedChart.RadarSeries radar:
                AddPoints(radar.Points);
                radar.CategoryLabels.AddRange(CategoryLabels);
                var radarColor = FillStyle.GetOutlineColorName() ?? FillStyle.GetColor1Name() ?? GetSymbolColor();
                radar.Color = radarColor;
                radar.FillColor = radarColor;
                radar.FillArea = FillArea;
                radar.MarkerOutlineColor = string.Equals(MarkerOutlineColor, "Black", StringComparison.OrdinalIgnoreCase)
                    ? radarColor
                    : MarkerOutlineColor;
                radar.StrokeThickness = StrokeThickness;
                radar.ShowMarkers = ShowMarkers;
                radar.MarkerShape = SymbolStyle.GetMarkerShape();
                radar.MarkerSize = SymbolStyle.Size;
                break;
            case UnifiedChart.LineSeries line:
                AddPoints(line.Points);
                var lineColor = FillStyle.GetOutlineColorName() ?? FillStyle.GetColor1Name() ?? GetSymbolColor();
                line.Color = lineColor;
                line.MarkerOutlineColor = lineColor;
                line.StrokeThickness = StrokeThickness;
                line.ShowMarkers = ShowMarkers || chartType == Chart2DType.LineSymbols || chartType == Chart2DType.Radar;
                line.MarkerShape = SymbolStyle.GetMarkerShape();
                line.MarkerSize = SymbolStyle.Size;
                break;
        }

        series.TooltipText = TooltipText;

        return series;

        string GetSymbolColor() => SymbolStyle.GetColorName() ?? Color;

        string GetFillColorName() => FillStyle.GetColor1Name() ?? Color;

        string GetOutlineColorName() => FillStyle.GetOutlineColorName() ?? Color;

        void AddPoints(List<(double X, double Y)> points)
        {
            var count = Count;
            for (var i = 0; i < count; i++)
            {
                points.Add((X[i], Y[i]));
            }
        }
    }
}
