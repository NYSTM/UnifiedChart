namespace UnifiedChart.C1CWrapper;

public sealed class ChartValueLabel
{
    public double Value { get; set; }
    public double NumericValue
    {
        get => Value;
        set => Value = value;
    }
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "Black";
    public bool Visible { get; set; } = true;
}
