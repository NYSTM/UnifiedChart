namespace UnifiedChart.C1CWrapper;

public sealed class ChartAxis
{
    public ChartAxis()
        : this(new UnifiedChart.Axis())
    {
    }

    public double? Origin
    {
        get => InnerAxis.Origin;
        set => InnerAxis.Origin = value;
    }

    public AnnotationMethodEnum AnnoMethod
    {
        get => InnerAxis.LabelMode == UnifiedChart.AxisLabelMode.ValueLabels
            ? AnnotationMethodEnum.ValueLabels
            : _annotationMethod;
        set
        {
            _annotationMethod = value;
            if (value == AnnotationMethodEnum.ValueLabels)
            {
                InnerAxis.LabelMode = UnifiedChart.AxisLabelMode.ValueLabels;
            }
            else if (value == AnnotationMethodEnum.None)
            {
                InnerAxis.LabelMode = UnifiedChart.AxisLabelMode.Standard;
            }
        }
    }

    private AnnotationMethodEnum _annotationMethod = AnnotationMethodEnum.None;
    public int AnnotationRotation { get; set; }
    public UnifiedChart.AxisTickDirection TickDirection
    {
        get => InnerAxis.TickDirection;
        set => InnerAxis.TickDirection = value;
    }

    public bool TickLabelsVisible
    {
        get => InnerAxis.TickLabelsVisible;
        set => InnerAxis.TickLabelsVisible = value;
    }

    public UnifiedChart.AxisLabelMode LabelMode
    {
        get => InnerAxis.LabelMode;
        set => InnerAxis.LabelMode = value;
    }

    public bool ChartValueLabelsEnabled
    {
        get => InnerAxis.ChartValueLabelsEnabled;
        set => InnerAxis.ChartValueLabelsEnabled = value;
    }

    public List<ChartAxisMarker> Markers { get; } = new();

    public List<ChartGridLine> GridLines { get; } = new();

    public List<ChartAlarmZone> AlarmZones { get; } = new();

    public List<ChartValueLabel> ValueLabels { get; } = new();

    public double? UnitMajor
    {
        get => InnerAxis.MajorUnit;
        set => InnerAxis.MajorUnit = value;
    }

    public double? UnitMinor
    {
        get => InnerAxis.MinorUnit;
        set => InnerAxis.MinorUnit = value;
    }

    public bool AutoMajor
    {
        get => InnerAxis.AutoMajor;
        set => InnerAxis.AutoMajor = value;
    }

    public bool AutoMinor
    {
        get => InnerAxis.AutoMinor;
        set => InnerAxis.AutoMinor = value;
    }

    public bool AutoMin
    {
        get => InnerAxis.AutoMinimum;
        set => InnerAxis.AutoMinimum = value;
    }

    public bool AutoMax
    {
        get => InnerAxis.AutoMaximum;
        set => InnerAxis.AutoMaximum = value;
    }

    public bool GridMajorVisible
    {
        get => InnerAxis.GridMajorVisible;
        set => InnerAxis.GridMajorVisible = value;
    }

    public double? GridMajorSpacing
    {
        get => InnerAxis.GridMajorSpacing;
        set => InnerAxis.GridMajorSpacing = value;
    }

    public double GridMajorThickness
    {
        get => InnerAxis.GridMajorThickness;
        set => InnerAxis.GridMajorThickness = value;
    }

    public bool GridMinorVisible
    {
        get => InnerAxis.GridMinorVisible;
        set => InnerAxis.GridMinorVisible = value;
    }

    public double? GridMinorSpacing
    {
        get => InnerAxis.GridMinorSpacing;
        set => InnerAxis.GridMinorSpacing = value;
    }

    public double GridMinorThickness
    {
        get => InnerAxis.GridMinorThickness;
        set => InnerAxis.GridMinorThickness = value;
    }

    public double AxisLineThickness
    {
        get => InnerAxis.AxisLineThickness;
        set => InnerAxis.AxisLineThickness = value;
    }

    public string Compass
    {
        get => InnerAxis.Compass;
        set => InnerAxis.Compass = value ?? string.Empty;
    }

    public bool ShowScrollButtons
    {
        get => InnerAxis.ShowScrollButtons;
        set => InnerAxis.ShowScrollButtons = value;
    }

    public double? ScrollUnit
    {
        get => InnerAxis.Scroll.Unit;
        set => InnerAxis.Scroll.Unit = value;
    }

    public double? ScrollMin
    {
        get => InnerAxis.Scroll.Minimum;
        set => InnerAxis.Scroll.Minimum = value;
    }

    public double? ScrollMax
    {
        get => InnerAxis.Scroll.Maximum;
        set => InnerAxis.Scroll.Maximum = value;
    }

    public bool ScrollEnabled
    {
        get => InnerAxis.Scroll.IsEnabled;
        set => InnerAxis.Scroll.IsEnabled = value;
    }

    public bool SynchronizeWithY2
    {
        get => InnerAxis.Scroll.SynchronizeWithY2;
        set => InnerAxis.Scroll.SynchronizeWithY2 = value;
    }

    internal ChartAxis(UnifiedChart.Axis axis)
    {
        InnerAxis = axis;
        ScrollBar = new AxisScrollBar(axis.Scroll);
    }

    internal UnifiedChart.Axis InnerAxis { get; }

    public AxisScrollBar ScrollBar { get; }

    public string Text
    {
        get => InnerAxis.Title;
        set => InnerAxis.Title = value ?? string.Empty;
    }

    public double? Min
    {
        get => InnerAxis.Minimum;
        set => InnerAxis.Minimum = value;
    }

    public double? Max
    {
        get => InnerAxis.Maximum;
        set => InnerAxis.Maximum = value;
    }

    public bool IsLogarithmic
    {
        get => InnerAxis.IsLogarithmic;
        set => InnerAxis.IsLogarithmic = value;
    }

    public bool IsDateTime
    {
        get => InnerAxis.IsDateTime;
        set => InnerAxis.IsDateTime = value;
    }

    public string DateTimeFormat
    {
        get => InnerAxis.DateTimeFormat;
        set => InnerAxis.DateTimeFormat = value ?? "yyyy/MM/dd";
    }
}
