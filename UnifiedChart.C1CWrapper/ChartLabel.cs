namespace UnifiedChart.C1CWrapper;

public enum LabelCompassEnum
{
    Auto,
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}

public sealed class ChartLabel
{
    private bool _auto = true;
    private LabelCompassEnum _compass = LabelCompassEnum.Auto;

    public string SeriesName { get; set; } = string.Empty;

    public int DataIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Color { get; set; } = "Black";

    public bool Auto
    {
        get => _auto;
        set
        {
            _auto = value;
            if (value)
            {
                _compass = LabelCompassEnum.Auto;
            }
            else if (_compass == LabelCompassEnum.Auto)
            {
                _compass = LabelCompassEnum.NorthEast;
            }
        }
    }

    public LabelCompassEnum Compass
    {
        get => _compass;
        set
        {
            _compass = value;
            _auto = value == LabelCompassEnum.Auto;
        }
    }

    public bool ShowConnectionLine { get; set; } = true;

    public double OffsetX { get; set; } = 8;

    public double OffsetY { get; set; } = -8;
}
