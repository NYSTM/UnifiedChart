namespace UnifiedChart.C1CWrapper;

public sealed class AxisScrollBar
{
    private readonly UnifiedChart.AxisScrollSettings settings;

    internal AxisScrollBar(UnifiedChart.AxisScrollSettings settings)
    {
        this.settings = settings;
    }

    public double Scale
    {
        get => settings.Scale;
        set => settings.Scale = value;
    }

    public bool Visible
    {
        get => settings.IsEnabled;
        set => settings.IsEnabled = value;
    }

    public double? Unit
    {
        get => settings.Unit;
        set => settings.Unit = value;
    }

    public double? Min
    {
        get => settings.Minimum;
        set => settings.Minimum = value;
    }

    public double? Max
    {
        get => settings.Maximum;
        set => settings.Maximum = value;
    }
}
