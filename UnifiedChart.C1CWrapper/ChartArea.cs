namespace UnifiedChart.C1CWrapper;

public sealed class ChartArea
{
    public ChartArea()
    {
        Axes = new(new ChartAxis(new UnifiedChart.Axis()), new ChartAxis(new UnifiedChart.Axis()));
    }

    public ChartHeader Header { get; } = new();

    public ChartFooter Footer { get; } = new();

    public float TitleFontSize { get; set; } = 12f;
    public float FooterFontSize { get; set; } = 9f;
    public float AxisLabelFontSize { get; set; } = 8f;
    public float AxisTitleFontSize { get; set; } = 9f;
    public float DataLabelFontSize { get; set; } = 8f;
    public float LegendFontSize { get; set; } = 8f;
    public float ValueLabelFontSize { get; set; } = 8f;
    public float AlarmZoneFontSize { get; set; } = 8f;

    public double PlotMarginLeft { get; set; } = UnifiedChart.PlotLayout.DefaultLeftMargin;
    public double PlotMarginTop { get; set; } = UnifiedChart.PlotLayout.DefaultTopMargin;
    public double PlotMarginRight { get; set; } = UnifiedChart.PlotLayout.DefaultRightMargin;
    public double PlotMarginBottom { get; set; } = UnifiedChart.PlotLayout.DefaultBottomMargin;

    public System.Drawing.Point? LocationDefault { get; set; }

    public System.Drawing.Size? SizeDefault { get; set; }

    public ChartAxes Axes { get; }

    public ChartAxis AxisX => Axes.X;

    public ChartAxis AxisY => Axes.Y;

    public ChartAxis? AxisX2 => Axes.X2;

    public ChartAxis? X2Axis
    {
        get => Axes.X2;
        set => Axes.X2 = value;
    }

    public ChartAxis? AxisY2 => Axes.Y2;

    public ChartAxis? Y2Axis
    {
        get => Axes.Y2;
        set => Axes.Y2 = value;
    }

    public ChartAxis EnableSecondaryXAxis(ChartGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.UseSecondaryXAxis = true;
        return Axes.X2 ??= new ChartAxis();
    }

    public ChartLegend Legend { get; } = new();

    public ChartLabels ChartLabels { get; } = new();

    public List<ChartGroup> ChartGroups { get; } = new() { new ChartGroup() };

    public List<ChartGroup> ChartGroup => ChartGroups;

    public bool AutoAssignSecondaryYAxis { get; set; }

    internal void ApplyLayout(System.Windows.Forms.Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (LocationDefault is System.Drawing.Point location)
        {
            control.Location = location;
        }

        if (SizeDefault is System.Drawing.Size size)
        {
            control.Size = size;
        }
    }

    public ChartGroup this[int index] => ChartGroups[index];

    public ChartGroup AddNewChartGroup()
    {
        var group = new ChartGroup();
        ChartGroups.Add(group);
        return group;
    }

    public ChartAxis EnableSecondaryYAxis(ChartGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var axis = Axes.Y2 ??= new ChartAxis();
        group.UseSecondaryYAxis = true;
        return axis;
    }

    internal void ApplyTo(UnifiedChart.PlotModel model)
    {
        model.Title = !Header.Visible || Header.Compass.Equals("West", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : Header.Text;
        model.TitleCompass = Header.Compass;
        model.Footer = Footer.Visible ? Footer.Text : string.Empty;
        model.FooterCompass = Footer.Compass;
        model.ShowFooter = Footer.Visible;
        model.TitleFontSize = TitleFontSize;
        model.FooterFontSize = FooterFontSize;
        model.AxisLabelFontSize = AxisLabelFontSize;
        model.AxisTitleFontSize = AxisTitleFontSize;
        model.DataLabelFontSize = DataLabelFontSize;
        model.LegendFontSize = LegendFontSize;
        model.ValueLabelFontSize = ValueLabelFontSize;
        model.AlarmZoneFontSize = AlarmZoneFontSize;
        model.PlotMarginLeft = PlotMarginLeft;
        model.PlotMarginTop = PlotMarginTop;
        model.PlotMarginRight = PlotMarginRight;
        model.PlotMarginBottom = PlotMarginBottom;
        CopyAxis(AxisX, model.XAxis);
        var hasSecondaryXAxis = Axes.X2 is not null || ChartGroups.Any(group => group.UseSecondaryXAxis && HasData(group));
        model.X2Axis = hasSecondaryXAxis ? new UnifiedChart.Axis() : null;
        if (model.X2Axis is not null)
        {
            if (Axes.X2 is not null)
            {
                CopyAxis(Axes.X2, model.X2Axis);
            }
        }
        CopyAxis(AxisY, model.YAxis);
        var hasExplicitSecondaryGroup = ChartGroups.Any(group => group.UseSecondaryYAxis && HasData(group));
        var hasImplicitSecondaryGroup = AutoAssignSecondaryYAxis && ChartGroups
            .Skip(1)
            .Any(HasData);
        var hasSecondaryYAxis = AxisY2 is not null || hasExplicitSecondaryGroup || hasImplicitSecondaryGroup;
        model.Y2Axis = hasSecondaryYAxis ? new UnifiedChart.Axis() : null;
        if (AxisY2 is not null && model.Y2Axis is not null)
        {
            CopyAxis(AxisY2, model.Y2Axis!);
        }
        model.ShowLegend = Legend.Visible;
        model.LegendPosition = Legend.Position;
        model.ChartLabels.Clear();
        foreach (var sourceLabel in ChartLabels)
        {
            model.ChartLabels.Add(new UnifiedChart.ChartLabel
            {
                SeriesName = sourceLabel.SeriesName,
                DataIndex = sourceLabel.DataIndex,
                Text = sourceLabel.Text,
                Color = sourceLabel.Color,
                Auto = sourceLabel.Compass == LabelCompassEnum.Auto,
                ShowConnectionLine = sourceLabel.ShowConnectionLine,
                OffsetX = sourceLabel.OffsetX,
                OffsetY = sourceLabel.OffsetY
            });
        }
        model.ChartGroups.Clear();

        var seriesIndex = 0;
        var hasPrimaryDataGroup = false;
        foreach (var sourceGroup in ChartGroups)
        {
            var targetGroup = new UnifiedChart.ChartGroup();
            targetGroup.ChartData.Clear();
            model.ChartGroups.Add(targetGroup);

            var hasData = HasData(sourceGroup);
            var useSecondaryYAxis = sourceGroup.UseSecondaryYAxis;
            if (!useSecondaryYAxis && AutoAssignSecondaryYAxis && hasData && hasPrimaryDataGroup)
            {
                useSecondaryYAxis = true;
            }
            if (hasData && !useSecondaryYAxis)
            {
                hasPrimaryDataGroup = true;
            }

            foreach (var sourceData in sourceGroup.ChartData)
            {
                var targetData = new UnifiedChart.ChartData();
                targetGroup.ChartData.Add(targetData);

                foreach (var dataSeries in sourceData.SeriesList)
                {
                    var series = dataSeries.ToUnifiedSeries(sourceGroup.ChartType, sourceGroup.MissingValueHole);
                    if (ChartGroups.Count > 1 || sourceGroup.ChartType is Chart2DType.LineSymbols or Chart2DType.Radar)
                    {
                        ApplyDefaultSeriesStyle(series, dataSeries, seriesIndex++);
                    }
                    series.UseSecondaryYAxis = useSecondaryYAxis && model.Y2Axis is not null;
                    series.UseSecondaryXAxis = sourceGroup.UseSecondaryXAxis && model.X2Axis is not null;
                    targetData.SeriesList.Add(series);
                }
            }

        }

        ResolveChartLabels(model);
    }

    private static bool HasData(ChartGroup group)
        => group.ChartData.Any(data => data.SeriesList.Any(series =>
            series.Display == SeriesDisplayEnum.Show && series.Count > 0));

    private static void ApplyDefaultSeriesStyle(UnifiedChart.Series series, ChartDataSeries source, int seriesIndex)
    {
        if (series is not UnifiedChart.LineSeries line)
        {
            return;
        }

        if (!string.Equals(source.Color, "Black", StringComparison.OrdinalIgnoreCase)
            || source.MarkerShape != UnifiedChart.MarkerShape.Circle
            || source.SymbolStyle.GetColorName() is not null)
        {
            return;
        }

        var palette = new[] { "SteelBlue", "SeaGreen", "IndianRed", "Goldenrod" };
        line.Color = palette[seriesIndex % palette.Length];
        line.MarkerOutlineColor = line.Color;
        line.MarkerShape = (seriesIndex % 4) switch
        {
            0 => UnifiedChart.MarkerShape.Square,
            1 => UnifiedChart.MarkerShape.Circle,
            2 => UnifiedChart.MarkerShape.Diamond,
            _ => UnifiedChart.MarkerShape.Triangle
        };
        line.MarkerSize = 6;
        line.StrokeThickness = 2;
        line.ShowMarkers = true;
    }

    private void ResolveChartLabels(UnifiedChart.PlotModel model)
    {
        foreach (var label in model.ChartLabels)
        {
            var series = model.GetAllSeries().FirstOrDefault(item =>
                string.Equals(item.Name, label.SeriesName, StringComparison.Ordinal));
            if (series is null)
            {
                continue;
            }

            var points = series switch
            {
                UnifiedChart.LineSeries line => line.Points,
                UnifiedChart.ScatterSeries scatter => scatter.Points,
                UnifiedChart.AreaSeries area => area.Points,
                _ => null
            };
            if (points is null || label.DataIndex < 0 || label.DataIndex >= points.Count)
            {
                continue;
            }

            label.X = points[label.DataIndex].X;
            label.Y = points[label.DataIndex].Y;
            label.IsResolved = true;
        }
    }

    private static void CopyAxis(ChartAxis source, UnifiedChart.Axis target)
    {
        target.Title = source.Text;
        target.Minimum = source.Min;
        target.Maximum = source.Max;
        target.Origin = source.Origin;
        target.TickDirection = source.TickDirection;
        target.TickLabelsVisible = source.TickLabelsVisible;
        target.LabelMode = source.LabelMode;
        target.MajorUnit = source.UnitMajor;
        target.MinorUnit = source.UnitMinor;
        target.AutoMajor = source.AutoMajor;
        target.AutoMinor = source.AutoMinor;
        target.GridMajorVisible = source.GridMajorVisible;
        target.GridMajorSpacing = source.GridMajorSpacing;
        target.GridMajorThickness = source.GridMajorThickness;
        target.GridMinorVisible = source.GridMinorVisible;
        target.GridMinorSpacing = source.GridMinorSpacing;
        target.GridMinorThickness = source.GridMinorThickness;
        target.AxisLineThickness = source.AxisLineThickness;
        target.IsLogarithmic = source.IsLogarithmic;
        target.IsDateTime = source.IsDateTime;
        target.DateTimeFormat = source.DateTimeFormat;
        target.Scroll.Scale = source.ScrollBar.Scale;
        target.Scroll.Unit = source.ScrollUnit;
        target.Scroll.Minimum = source.ScrollMin;
        target.Scroll.Maximum = source.ScrollMax;
        target.Scroll.IsEnabled = source.ScrollEnabled;
        target.Scroll.SynchronizeWithY2 = source.SynchronizeWithY2;
        target.Markers.Clear();
        target.Markers.AddRange(source.Markers.Select(marker => new UnifiedChart.AxisMarker
        {
            Value = marker.Value,
            Text = marker.Text,
            Color = marker.Color,
            ArrowDirection = marker.ArrowDirection,
            ShowArrow = marker.ShowArrow,
            ShowConnectionLine = marker.ShowConnectionLine
        }));
        target.GridLines.Clear();
        target.GridLines.AddRange(source.GridLines.Select(line => new UnifiedChart.AxisGridLine
        {
            Value = line.Value,
            Color = line.Color,
            DashStyle = line.DashStyle,
            Thickness = line.Thickness,
            IsVisible = line.Visible
        }));
        target.AlarmZones.Clear();
        target.AlarmZones.AddRange(source.AlarmZones.Select(zone => new UnifiedChart.AxisAlarmZone
        {
            Mode = zone.Mode,
            LowerLimit = zone.LowerLimit,
            UpperLimit = zone.UpperLimit,
            From = zone.From,
            To = zone.To,
            FillColor = zone.FillColor,
            Opacity = zone.Opacity,
            BorderColor = zone.BorderColor,
            BorderThickness = zone.BorderThickness,
            BorderDashStyle = zone.BorderDashStyle,
            Text = zone.Text,
            IsVisible = zone.Visible
        }));
        target.ValueLabels.Clear();
        target.ValueLabels.AddRange(source.ValueLabels.Select(label => new UnifiedChart.AxisValueLabel
        {
            Value = label.Value,
            Text = label.Text,
            Color = label.Color,
            IsVisible = label.Visible
        }));
    }
}
