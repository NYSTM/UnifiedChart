namespace UnifiedChart.C1CWrapper;

public sealed class ChartAlarmZone
{
    public UnifiedChart.AlarmZoneMode Mode { get; set; } = UnifiedChart.AlarmZoneMode.Explicit;

    public double? LowerLimit { get; set; }

    public double? UpperLimit { get; set; }

    public double From { get; set; }

    public double To { get; set; }

    public string FillColor { get; set; } = "Yellow";

    public byte Opacity { get; set; } = 64;

    public string BorderColor { get; set; } = "Transparent";

    public float BorderThickness { get; set; } = 1;

    public string BorderDashStyle { get; set; } = "Solid";

    public string Text { get; set; } = string.Empty;

    public bool Visible { get; set; } = true;
}
