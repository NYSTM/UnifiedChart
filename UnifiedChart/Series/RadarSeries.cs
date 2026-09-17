namespace UnifiedChart;

public sealed class RadarSeries : Series
{
    public List<(double X, double Y)> Points { get; } = new();

    public List<string> CategoryLabels { get; } = new();

    public string Color { get; set; } = "SteelBlue";

    public string FillColor { get; set; } = "Transparent";

    public bool FillArea { get; set; }

    public string MarkerOutlineColor { get; set; } = "SteelBlue";

    public MarkerShape MarkerShape { get; set; } = MarkerShape.Circle;

    public double MarkerSize { get; set; } = 3.0;

    public double StrokeThickness { get; set; } = 2.0;

    public bool ShowMarkers { get; set; } = true;
}
