namespace UnifiedChart.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using UnifiedChart;

    public class PlotView : Control, IPlotView
    {
        static PlotView() => DefaultStyleKeyProperty.OverrideMetadata(typeof(PlotView), new FrameworkPropertyMetadata(typeof(PlotView)));

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(PlotModel), typeof(PlotView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnModelChanged));

        private AxisRange? zoomedXRange;
        private AxisRange? zoomedYRange;
        private AxisRange? zoomedY2Range;
        private bool isPanning;
        private Point panLastScreenPoint;
        private readonly ToolTip toolTip = new ToolTip { Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse };
        private string? lastTooltipText;
        private readonly List<(Series Series, Rect Rect)> legendItemRects = new();
        private string FontFamilyName => string.IsNullOrWhiteSpace(Model?.FontFamilyName) ? "Meiryo UI" : Model!.FontFamilyName;
        private Rect scrollXPreviousButton;
        private Rect scrollXNextButton;
        private Rect scrollYPreviousButton;
        private Rect scrollYNextButton;

        // ズーム／パンをスムーズに見せるためのアニメーション用状態。
        private AxisRange? animationTargetXRange;
        private AxisRange? animationTargetYRange;
        private AxisRange? animationStartXRange;
        private AxisRange? animationStartYRange;
        private readonly System.Windows.Threading.DispatcherTimer animationTimer =
            new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        private DateTime animationStartTime;
        private const int AnimationDurationMs = 150;

        public PlotView()
        {
            MouseWheel += HandleMouseWheelZoom;
            MouseLeftButtonDown += HandleMouseLeftButtonDown;
            MouseLeftButtonUp += HandleMouseLeftButtonUp;
            MouseMove += HandleMouseMove;
            MouseDoubleClick += (_, _) => ResetZoom();

            IsManipulationEnabled = true;
            ManipulationDelta += HandleManipulationDelta;

            animationTimer.Tick += AnimationTimer_Tick;
        }

        public PlotModel? Model
        {
            get => (PlotModel?)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PlotView)d).ResetZoom();
        }

        public void InvalidatePlot() => InvalidateVisual();

        public AxisRange? DisplayXRange => zoomedXRange;

        public AxisRange? DisplayYRange => zoomedYRange;

        public AxisRange? DisplayY2Range => zoomedY2Range;

        public void SetDisplayRange(AxisRange? xRange, AxisRange? yRange)
        {
            zoomedXRange = xRange;
            zoomedYRange = yRange;
            zoomedY2Range = null;
            InvalidateVisual();
        }

        public void ScrollX(double direction = 1) => ScrollRange(true, direction);

        public void ScrollY(double direction = 1) => ScrollRange(false, direction);

        /// <summary>
        /// 通常の描画が完了した後に発生するイベント。ユーザーコードから任意の図形を追加描画できる。
        /// </summary>
        public event EventHandler<CustomRenderEventArgs>? CustomRender;

        /// <summary>
        /// ズーム・パン状態をリセットする。
        /// </summary>
        public void ResetZoom()
        {
            zoomedXRange = null;
            zoomedYRange = null;
            zoomedY2Range = null;
            InvalidateVisual();
        }

        private PlotRect GetPlotArea()
        {
            var rightMargin = Model?.Y2Axis != null ? 48 : PlotLayout.DefaultRightMargin;
            var leftMargin = PlotLayout.DefaultLeftMargin + (string.IsNullOrEmpty(Model?.YAxis.Title) ? 0 : 14);
            var bottomMargin = PlotLayout.DefaultBottomMargin
                + (string.IsNullOrEmpty(Model?.XAxis.Title) ? 0 : 14)
                + (Model?.XAxis.IsDateTime == true ? 32 : 0);
            return new PlotRect(leftMargin, PlotLayout.DefaultTopMargin,
                Math.Max(0, ActualWidth - leftMargin - rightMargin),
                Math.Max(0, ActualHeight - PlotLayout.DefaultTopMargin - bottomMargin));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var theme = Model?.Theme ?? PlotTheme.Light;
            var backgroundBrush = (Brush)(new BrushConverter().ConvertFromString(theme.BackgroundColor) ?? Brushes.White);
            drawingContext.DrawRectangle(backgroundBrush, null, new Rect(0,0, ActualWidth, ActualHeight));
            if (Model == null)
            {
                var ft = new FormattedText("UnifiedChart: no data",
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamilyName), 12, Brushes.Gray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawingContext.DrawText(ft, new Point(4,4));
                return;
            }

            var radarSeries = Model.GetAllSeries().OfType<RadarSeries>().Where(s => s.Visible && s.Points.Count > 0).ToList();
            if (radarSeries.Count > 0)
            {
                var radarArea = GetPlotArea();
                DrawRadarChart(drawingContext, radarArea, radarSeries, theme, Model.ShowLegend, Model.RadarGridLevels, Model.RadarGridLabels, Model.RadarDirection);
                var radarTransform = PlotRenderHelper.CreateTransform(Model, radarArea, zoomedXRange, zoomedYRange);
                CustomRender?.Invoke(this, new CustomRenderEventArgs(drawingContext, radarArea, radarTransform, Model));
                return;
            }
            var titleBrush = (Brush)(new BrushConverter().ConvertFromString(theme.TitleColor) ?? Brushes.Black);
            var title = new FormattedText(Model.Title ?? string.Empty,
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamilyName), 14, titleBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var titleX = Model.TitleCompass.ToLowerInvariant() switch
            {
                "east" => Math.Max(4, ActualWidth - title.Width - 4),
                "north" or "south" => Math.Max(4, (ActualWidth - title.Width) / 2),
                _ => 4
            };
            var titleY = Model.TitleCompass.Equals("south", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(4, ActualHeight - title.Height - 4)
                : 4;
            drawingContext.DrawText(title, new Point(titleX, titleY));

            var pieSeries = Model.GetAllSeries().FirstOrDefault(s => s.Visible && s is PieSeries) as PieSeries;
            if (pieSeries != null)
            {
                var pieArea = GetPlotArea();
                DrawPieChart(drawingContext, pieArea, pieSeries, theme);
                var pieTransform = PlotRenderHelper.CreateTransform(Model, pieArea, zoomedXRange, zoomedYRange);
                CustomRender?.Invoke(this, new CustomRenderEventArgs(drawingContext, pieArea, pieTransform, Model));
                return;
            }

        private void DrawRadarChart(DrawingContext drawingContext, PlotRect plotArea, IReadOnlyList<RadarSeries> seriesList, PlotTheme theme, bool showLegend, int radarGridLevels, IReadOnlyList<string>? radarGridLabels, RadarDirection radarDirection)
        {
            var center = new Point(plotArea.Left + plotArea.Width / 2, plotArea.Top + plotArea.Height / 2);
            var radius = Math.Min(plotArea.Width, plotArea.Height) * 0.38;
            var pointCount = seriesList.Max(series => series.Points.Count);
            if (pointCount == 0 || radius <= 0)
            {
                return;
            }

            var values = seriesList
                .SelectMany(series => series.Points
                    .Where(point => !MissingValueContract.IsMissing(point.X, series.MissingValueHole)
                        && !MissingValueContract.IsMissing(point.Y, series.MissingValueHole))
                    .Select(point => point.Y))
                .ToArray();
            if (values.Length == 0)
            {
                return;
            }

            var minimum = Math.Min(0, values.Min());
            var maximum = values.Max();
            if (maximum <= minimum)
            {
                maximum = minimum + 1;
            }

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 128, 128, 128)), 1);
            var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(170, 128, 128, 128)), 1);
            var labelTypeface = new Typeface(FontFamilyName);
            var labelBrush = (Brush)(new BrushConverter().ConvertFromString(theme.AxisLabelColor) ?? Brushes.Black);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            var gridLevels = Math.Max(1, radarGridLevels);
            var hasCustomLabels = radarGridLabels is { Count: var labelCount } && labelCount == gridLevels;
            for (var level = 1; level <= gridLevels; level++)
            {
                var levelRadius = radius * level / gridLevels;
                var levelValue = minimum + (maximum - minimum) * level / gridLevels;
                drawingContext.DrawEllipse(null, gridPen, center, levelRadius, levelRadius);

                var valueText = new FormattedText(
                    hasCustomLabels ? radarGridLabels![level - 1] : levelValue.ToString("0.##"),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    labelTypeface,
                    10,
                    labelBrush,
                    dpi);
                var labelAngle = -Math.PI * 3 / 4;
                var labelCenterX = center.X + Math.Cos(labelAngle) * levelRadius;
                var labelCenterY = center.Y + Math.Sin(labelAngle) * levelRadius;
                drawingContext.DrawText(
                    valueText,
                    new Point(labelCenterX - valueText.Width / 2, labelCenterY - valueText.Height / 2));
            }

            var direction = radarDirection == RadarDirection.CounterClockwise ? -1 : 1;
            var outerPoints = CreateRadarPoints(center, radius, pointCount, direction);
            var categoryLabels = seriesList[0].CategoryLabels;
            var hasCategoryLabels = categoryLabels.Count == pointCount;
            for (var index = 0; index < pointCount; index++)
            {
                drawingContext.DrawLine(axisPen, center, outerPoints[index]);
                var label = hasCategoryLabels
                    ? categoryLabels[index]
                    : index < seriesList[0].Points.Count ? seriesList[0].Points[index].X.ToString("0.##") : index.ToString();
                var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, labelTypeface, 10, labelBrush, dpi);
                var labelVectorX = outerPoints[index].X - center.X;
                var labelVectorY = outerPoints[index].Y - center.Y;
                var labelVectorLength = Math.Max(1, Math.Sqrt(labelVectorX * labelVectorX + labelVectorY * labelVectorY));
                const double labelOffset = 8;
                drawingContext.DrawText(text, new Point(
                    outerPoints[index].X + labelVectorX / labelVectorLength * labelOffset - text.Width / 2,
                    outerPoints[index].Y + labelVectorY / labelVectorLength * labelOffset - text.Height / 2));
            }

            foreach (var series in seriesList)
            {
                var points = series.Points.Select((point, index) =>
                {
                    if (MissingValueContract.IsMissing(point.X, series.MissingValueHole)
                        || MissingValueContract.IsMissing(point.Y, series.MissingValueHole))
                    {
                        return (Index: index, Point: (Point?)null);
                    }

                    var normalized = Math.Clamp((point.Y - minimum) / (maximum - minimum), 0, 1);
                    var angle = -Math.PI / 2 + direction * index * Math.PI * 2 / pointCount;
                    return (Index: index, Point: (Point?)new Point(center.X + Math.Cos(angle) * radius * normalized, center.Y + Math.Sin(angle) * radius * normalized));
                }).Where(item => item.Point.HasValue).ToArray();
                if (points.Length == 0) continue;

                var color = ToBrush(series.Color);
                var fill = ToBrush(series.FillColor);
                if (series.FillArea && points.Length == series.Points.Count && points.Length >= 3)
                {
                    var geometry = new StreamGeometry();
                    using (var context = geometry.Open())
                    {
                        context.BeginFigure(points[0].Point!.Value, true, true);
                        for (var index = 1; index < points.Length; index++) context.LineTo(points[index].Point!.Value, true, false);
                    }
                    geometry.Freeze();
                    drawingContext.DrawGeometry(fill, new Pen(color, series.StrokeThickness), geometry);
                }
                else if (points.Length >= 2)
                {
                    for (var index = 1; index < points.Length; index++)
                    {
                        if (points[index].Index == points[index - 1].Index + 1)
                        {
                            drawingContext.DrawLine(new Pen(color, series.StrokeThickness), points[index - 1].Point!.Value, points[index].Point!.Value);
                        }
                    }

                    if (points.Length == series.Points.Count)
                    {
                        drawingContext.DrawLine(new Pen(color, series.StrokeThickness), points[^1].Point!.Value, points[0].Point!.Value);
                    }
                }

                if (series.ShowMarkers)
                {
                    foreach (var point in points) DrawMarker(drawingContext, color, series.MarkerShape, point.Point!.Value, Math.Max(1.0, series.MarkerSize * 1.5));
                }
            }

            if (showLegend)
            {
                var legendBrush = ToBrush(theme.LegendTextColor);
                var legendTypeface = new Typeface(FontFamilyName);
                var legendX = plotArea.Right - 110;
                var legendY = plotArea.Top + 8;
                foreach (var series in seriesList)
                {
                    DrawMarker(drawingContext, ToBrush(series.Color), series.MarkerShape, new Point(legendX + 6, legendY + 9), Math.Max(1.0, series.MarkerSize * 1.5));
                    var text = new FormattedText(series.Name, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, legendTypeface, 10, legendBrush, dpi);
                    drawingContext.DrawText(text, new Point(legendX + 18, legendY));
                    legendY += 18;
                }
            }
        }

        private static Point[] CreateRadarPoints(Point center, double radius, int pointCount, int direction)
        {
            var points = new Point[pointCount];
            for (var index = 0; index < pointCount; index++)
            {
                var angle = -Math.PI / 2 + direction * index * Math.PI * 2 / pointCount;
                points[index] = new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
            }
            return points;
        }

            var plotArea = GetPlotArea();
            var transform = PlotRenderHelper.CreateTransform(Model, plotArea, zoomedXRange, zoomedYRange);

            bool hasBarSeries = Model.GetAllSeries().Any(s => s.Visible && (s is BarSeries || s is BoxPlotSeries || s is StackedBarSeries));
            DrawAlarmZones(drawingContext, plotArea, transform, Model.XAxis, Model.YAxis, Model.Y2Axis);
            DrawAxisTicks(drawingContext, plotArea, transform, hasBarSeries, Model.ShowGridLines, theme, Model.XAxis, Model.YAxis, Model.Y2Axis);
            DrawAxisValueLabels(drawingContext, plotArea, transform, Model.XAxis, Model.YAxis, Model.Y2Axis);
            DrawAxisTitles(drawingContext, plotArea, Model.XAxis.Title, Model.YAxis.Title, theme);
            DrawScrollButtons(drawingContext, plotArea, Model.XAxis, Model.YAxis);

            foreach (var series in Model.GetAllSeries())
            {
                if (!series.Visible) continue;
                if (series is LineSeries line && line.Points.Count > 1)
                {
                    var brush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(line.Color) ? "Black" : line.Color) ?? Brushes.Black);
                    var pen = new Pen(brush, line.StrokeThickness) { DashStyle = ToWpfDashStyle(line.DashStyle) };
                    Point? previous = null;
                    foreach (var (x, y) in line.Points)
                    {
                        if (MissingValueContract.IsMissing(x, line.MissingValueHole) || MissingValueContract.IsMissing(y, line.MissingValueHole))
                        {
                            if (!line.ConnectAcrossMissingValues)
                            {
                                previous = null;
                            }
                            continue;
                        }

                        var screenY = line.UseSecondaryYAxis ? transform.ToScreenY2(y) : transform.ToScreenY(y);
                        var current = new Point(transform.ToScreenX(x), screenY);
                        if (previous.HasValue)
                        {
                            drawingContext.DrawLine(pen, previous.Value, current);
                        }
                        if (line.ShowMarkers)
                        {
                            DrawMarker(drawingContext, brush, line.MarkerShape, current, Math.Max(1.0, line.MarkerSize * 1.5));
                        }
                        previous = current;
                    }

                    if (line.ShowDataLabels)
                    {
                        DrawSeriesDataLabels(drawingContext, transform, line.Points, line.DataLabelFormat, line.UseSecondaryYAxis);
                    }
                }
                else if (series is AreaSeries area && area.Points.Count > 1)
                {
                    var fillColor = (Color)(ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(area.FillColor) ? "LightBlue" : area.FillColor) ?? Colors.LightBlue);
                    fillColor.A = 150;
                    var fillBrush = new SolidColorBrush(fillColor);
                    var strokeBrush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(area.Color) ? "Blue" : area.Color) ?? Brushes.Blue);
                    var pen = new Pen(strokeBrush, area.StrokeThickness) { DashStyle = ToWpfDashStyle(area.DashStyle) };
                    var baselineY = area.UseSecondaryYAxis ? transform.ToScreenY2(area.BaselineValue) : transform.ToScreenY(area.BaselineValue);

                    var segment = new List<Point>();
                    void FlushSegment()
                    {
                        if (segment.Count > 1)
                        {
                            var figure = new PathFigure { StartPoint = new Point(segment[0].X, baselineY), IsClosed = true };
                            foreach (var pt in segment)
                            {
                                figure.Segments.Add(new LineSegment(pt, true));
                            }
                            figure.Segments.Add(new LineSegment(new Point(segment[^1].X, baselineY), true));
                            var geometry = new PathGeometry(new[] { figure });
                            drawingContext.DrawGeometry(fillBrush, null, geometry);

                            for (int i = 1; i < segment.Count; i++)
                            {
                                drawingContext.DrawLine(pen, segment[i - 1], segment[i]);
                            }
                        }
                        segment = new List<Point>();
                    }

                    foreach (var (x, y) in area.Points)
                    {
                        if (MissingValueContract.IsMissing(x, area.MissingValueHole) || MissingValueContract.IsMissing(y, area.MissingValueHole))
                        {
                            FlushSegment();
                            continue;
                        }
                        var screenY = area.UseSecondaryYAxis ? transform.ToScreenY2(y) : transform.ToScreenY(y);
                        segment.Add(new Point(transform.ToScreenX(x), screenY));
                    }
                    FlushSegment();
                }
                else if (series is RangeSeries range && range.Points.Count > 1)
                {
                    var fillColor = (Color)(ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(range.FillColor) ? "LightSkyBlue" : range.FillColor) ?? Colors.LightSkyBlue);
                    fillColor.A = 150;
                    var fillBrush = new SolidColorBrush(fillColor);
                    var strokeBrush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(range.Color) ? "SteelBlue" : range.Color) ?? Brushes.SteelBlue);
                    var pen = new Pen(strokeBrush, range.StrokeThickness);

                    var topSegment = new List<Point>();
                    var bottomSegment = new List<Point>();
                    void FlushRangeSegment()
                    {
                        if (topSegment.Count > 1)
                        {
                            var figure = new PathFigure { StartPoint = topSegment[0], IsClosed = true };
                            for (int i = 1; i < topSegment.Count; i++)
                            {
                                figure.Segments.Add(new LineSegment(topSegment[i], true));
                            }
                            for (int i = bottomSegment.Count - 1; i >= 0; i--)
                            {
                                figure.Segments.Add(new LineSegment(bottomSegment[i], true));
                            }
                            var geometry = new PathGeometry(new[] { figure });
                            drawingContext.DrawGeometry(fillBrush, null, geometry);

                            for (int i = 1; i < topSegment.Count; i++)
                            {
                                drawingContext.DrawLine(pen, topSegment[i - 1], topSegment[i]);
                            }
                            for (int i = 1; i < bottomSegment.Count; i++)
                            {
                                drawingContext.DrawLine(pen, bottomSegment[i - 1], bottomSegment[i]);
                            }
                        }
                        topSegment = new List<Point>();
                        bottomSegment = new List<Point>();
                    }

                    foreach (var (x, low, high) in range.Points)
                    {
                        if (double.IsNaN(x) || double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(x) || double.IsInfinity(low) || double.IsInfinity(high))
                        {
                            FlushRangeSegment();
                            continue;
                        }
                        var screenX = transform.ToScreenX(x);
                        topSegment.Add(new Point(screenX, transform.ToScreenY(high)));
                        bottomSegment.Add(new Point(screenX, transform.ToScreenY(low)));
                    }
                    FlushRangeSegment();
                }
                else if (series is ScatterSeries scatter)
                {
                    var brush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(scatter.Color) ? theme.DefaultSeriesColor : scatter.Color) ?? Brushes.Black);
                    double size = scatter.MarkerSize;
                    foreach (var (x, y) in scatter.Points)
                    {
                        if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                        var cx = transform.ToScreenX(x);
                        var cy = scatter.UseSecondaryYAxis ? transform.ToScreenY2(y) : transform.ToScreenY(y);
                        DrawMarker(drawingContext, brush, scatter.MarkerShape, new Point(cx, cy), size);
                    }

                    if (scatter.ShowDataLabels)
                    {
                        DrawSeriesDataLabels(drawingContext, transform, scatter.Points, scatter.DataLabelFormat, scatter.UseSecondaryYAxis);
                    }
                }
                else if (series is BarSeries bar)
                {
                    var brush = (Brush)(new BrushConverter().ConvertFromString(theme.DefaultSeriesColor) ?? Brushes.SteelBlue);
                    var zeroY = transform.ToScreenY(0);
                    int count = bar.Items.Count;
                    if (count == 0) continue;
                    double slotWidth = plotArea.Width / count;
                    double barWidth = slotWidth * 0.6;
                    for (int i = 0; i < count; i++)
                    {
                        var value = bar.Items[i].Value;
                        if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                        var centerX = plotArea.Left + slotWidth * (i + 0.5);
                        var barY = transform.ToScreenY(value);
                        var top = Math.Min(barY, zeroY);
                        var height = Math.Abs(barY - zeroY);
                        drawingContext.DrawRectangle(brush, null, new Rect(centerX - barWidth / 2, top, barWidth, height));

                        if (bar.ShowDataLabels)
                        {
                            var dataLabelText = new FormattedText(PlotRenderHelper.FormatDataLabel(value, bar.DataLabelFormat),
                                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                new Typeface(FontFamilyName), 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                            drawingContext.DrawText(dataLabelText, new Point(centerX - dataLabelText.Width / 2, top - dataLabelText.Height - 2));
                        }
                    }

                    var labelTypeface = new Typeface(FontFamilyName);
                    var labelY = plotArea.Bottom + 2;
                    foreach (var (index, category) in PlotRenderHelper.GetVisibleBarCategoryLabels(bar, plotArea.Width))
                    {
                        var centerX = plotArea.Left + slotWidth * (index + 0.5);
                        var labelText = new FormattedText(category,
                            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            labelTypeface, 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        drawingContext.DrawText(labelText, new Point(centerX - labelText.Width / 2, labelY));
                    }
                }
                else if (series is StackedBarSeries stackedBar)
                {
                    var zeroY = transform.ToScreenY(0);
                    int count = stackedBar.Items.Count;
                    if (count == 0) continue;
                    double slotWidth = plotArea.Width / count;
                    double barWidth = slotWidth * 0.6;
                    for (int i = 0; i < count; i++)
                    {
                        var centerX = plotArea.Left + slotWidth * (i + 0.5);
                        var segments = PlotRenderHelper.CalculateStackedBarSegments(stackedBar, i);
                        foreach (var (valueIndex, start, end) in segments)
                        {
                            var brush = GetPaletteBrush(theme, valueIndex);
                            var startY = transform.ToScreenY(start);
                            var endY = transform.ToScreenY(end);
                            var top = Math.Min(startY, endY);
                            var height = Math.Abs(startY - endY);
                            drawingContext.DrawRectangle(brush, null, new Rect(centerX - barWidth / 2, top, barWidth, height));
                        }
                    }

                    var stackedLabelTypeface = new Typeface(FontFamilyName);
                    var stackedLabelY = plotArea.Bottom + 2;
                    foreach (var (index, category) in PlotRenderHelper.GetVisibleStackedBarCategoryLabels(stackedBar, plotArea.Width))
                    {
                        var centerX = plotArea.Left + slotWidth * (index + 0.5);
                        var labelText = new FormattedText(category,
                            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            stackedLabelTypeface, 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        drawingContext.DrawText(labelText, new Point(centerX - labelText.Width / 2, stackedLabelY));
                    }
                }
                else if (series is HistogramSeries histogram)
                {
                    var brush = (Brush)(new BrushConverter().ConvertFromString(theme.DefaultSeriesColor) ?? Brushes.MediumSeaGreen);
                    var pen = new Pen((Brush)(new BrushConverter().ConvertFromString(theme.BackgroundColor) ?? Brushes.White), 1);
                    var zeroY = transform.ToScreenY(0);
                    var bins = PlotRenderHelper.CalculateHistogramBins(histogram);
                    foreach (var bin in bins)
                    {
                        var left = transform.ToScreenX(bin.BinStart);
                        var right = transform.ToScreenX(bin.BinEnd);
                        var barY = transform.ToScreenY(bin.Count);
                        var top = Math.Min(barY, zeroY);
                        var height = Math.Abs(barY - zeroY);
                        var rectX = Math.Min(left, right);
                        var rectWidth = Math.Abs(right - left);
                        drawingContext.DrawRectangle(brush, pen, new Rect(rectX, top, rectWidth, height));
                    }
                }
                else if (series is BoxPlotSeries boxPlot)
                {
                    var brush = Brushes.LightSkyBlue;
                    var pen = new Pen(Brushes.Black, 1);
                    var medianPen = new Pen(Brushes.Firebrick, 2);
                    int count = boxPlot.Items.Count;
                    if (count == 0) continue;
                    double slotWidth = plotArea.Width / count;
                    double boxWidth = slotWidth * 0.5;
                    for (int i = 0; i < count; i++)
                    {
                        var item = boxPlot.Items[i];
                        if (double.IsNaN(item.Min) || double.IsNaN(item.Max) || double.IsInfinity(item.Min) || double.IsInfinity(item.Max)) continue;

                        var centerX = plotArea.Left + slotWidth * (i + 0.5);
                        var minY = transform.ToScreenY(item.Min);
                        var maxY = transform.ToScreenY(item.Max);
                        var q1Y = transform.ToScreenY(item.Q1);
                        var q3Y = transform.ToScreenY(item.Q3);
                        var medianY = transform.ToScreenY(item.Median);

                        // ひげ(Min-Max)
                        drawingContext.DrawLine(pen, new Point(centerX, minY), new Point(centerX, maxY));
                        drawingContext.DrawLine(pen, new Point(centerX - boxWidth / 4, minY), new Point(centerX + boxWidth / 4, minY));
                        drawingContext.DrawLine(pen, new Point(centerX - boxWidth / 4, maxY), new Point(centerX + boxWidth / 4, maxY));

                        // 箱(Q1-Q3)
                        var boxTop = Math.Min(q1Y, q3Y);
                        var boxHeight = Math.Abs(q1Y - q3Y);
                        drawingContext.DrawRectangle(brush, pen, new Rect(centerX - boxWidth / 2, boxTop, boxWidth, boxHeight));

                        // 中央値線
                        drawingContext.DrawLine(medianPen, new Point(centerX - boxWidth / 2, medianY), new Point(centerX + boxWidth / 2, medianY));
                    }

                    var boxLabelTypeface = new Typeface(FontFamilyName);
                    var boxLabelY = plotArea.Bottom + 2;
                    for (int i = 0; i < count; i++)
                    {
                        var category = boxPlot.Items[i].Category;
                        var centerX = plotArea.Left + slotWidth * (i + 0.5);
                        var labelText = new FormattedText(category,
                            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            boxLabelTypeface, 10, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        drawingContext.DrawText(labelText, new Point(centerX - labelText.Width / 2, boxLabelY));
                    }
                }
            }

            DrawAnnotations(drawingContext, transform, Model);
            DrawChartLabels(drawingContext, plotArea, transform, Model);
            DrawLegend(drawingContext, plotArea, Model);

            CustomRender?.Invoke(this, new CustomRenderEventArgs(drawingContext, plotArea, transform, Model));
        }

        private void DrawChartLabels(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, PlotModel model)
        {
            if (model.ChartLabels.Count == 0) return;

            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var occupied = new List<Rect>();
            foreach (var label in model.ChartLabels.Where(item => item.IsResolved && !string.IsNullOrEmpty(item.Text)))
            {
                var point = new Point(transform.ToScreenX(label.X), transform.ToScreenY(label.Y));
                var brush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(label.Color) ? "Black" : label.Color) ?? Brushes.Black);
                var text = new FormattedText(label.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, brush, dpi);
                var bounds = FindLabelBounds(point, text, label, plotArea, occupied);
                occupied.Add(bounds);

                if (label.ShowConnectionLine)
                {
                    drawingContext.DrawLine(new Pen(brush, 1), point, new Point(bounds.Left, bounds.Top + bounds.Height / 2));
                }

                drawingContext.DrawText(text, new Point(bounds.Left, bounds.Top));
            }
        }

        private static Rect FindLabelBounds(Point point, FormattedText text, UnifiedChart.ChartLabel label, PlotRect plotArea, List<Rect> occupied)
        {
            var result = ChartLabelPlacement.Find(
                point.X,
                point.Y,
                text.Width,
                text.Height,
                label,
                new LabelPlacementRect(plotArea.Left, plotArea.Top, plotArea.Width, plotArea.Height),
                occupied.Select(item => new LabelPlacementRect(item.Left, item.Top, item.Width, item.Height)).ToArray());
            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        /// <summary>
        /// PlotModel.Annotations に登録されたテキスト注釈を描画する。
        /// </summary>
        private void DrawAnnotations(DrawingContext drawingContext, CoordinateTransform transform, PlotModel model)
        {
            if (model.Annotations.Count == 0) return;

            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            foreach (var annotation in model.Annotations)
            {
                var x = transform.ToScreenX(annotation.X);
                var y = transform.ToScreenY(annotation.Y);
                var brush = (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(annotation.Color) ? "Black" : annotation.Color) ?? Brushes.Black);
                var text = new FormattedText(annotation.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, brush, dpi);
                drawingContext.DrawText(text, new Point(x, y));
            }
        }

        /// <summary>
        /// テーマの SeriesPalette からインデックスに対応するブラシュを取得する。範囲外は循環する。
        /// </summary>
        private static Brush GetPaletteBrush(PlotTheme theme, int index)
        {
            var palette = theme.SeriesPalette;
            if (palette == null || palette.Length == 0)
            {
                return (Brush)(new BrushConverter().ConvertFromString(theme.DefaultSeriesColor) ?? Brushes.SteelBlue);
            }
            var colorName = palette[index % palette.Length];
            return (Brush)(new BrushConverter().ConvertFromString(colorName) ?? Brushes.SteelBlue);
        }

        /// <summary>
        /// PieSeries を円グラフとして描画する。プロット領域の中央に円を配置し、右側に凡例(ラベルと割合)を表示する。
        /// </summary>
        private void DrawPieChart(DrawingContext drawingContext, PlotRect plotArea, PieSeries pie, PlotTheme theme)
        {
            var slices = PlotRenderHelper.CalculatePieSlices(pie);
            if (slices.Count == 0) return;

            double diameter = Math.Min(plotArea.Width, plotArea.Height) * 0.8;
            double radius = diameter / 2;
            var center = new Point(plotArea.Left + plotArea.Width / 2, plotArea.Top + plotArea.Height / 2);

            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var brush = GetPaletteBrush(theme, i);
                // CalculatePieSlices の角度は 0度=真上、時計回り。WPF 座標系での点計算に変換する。
                var startPoint = GetPointOnCircle(center, radius, slice.StartAngle);
                var endPoint = GetPointOnCircle(center, radius, slice.StartAngle + slice.SweepAngle);
                bool isLargeArc = slice.SweepAngle > 180.0;

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(center, true, true);
                    ctx.LineTo(startPoint, true, false);
                    ctx.ArcTo(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true, false);
                }
                geometry.Freeze();
                drawingContext.DrawGeometry(brush, new Pen(Brushes.White, 1), geometry);
            }

            var labelTypeface = new Typeface(FontFamilyName);
            var legendTextBrush = (Brush)(new BrushConverter().ConvertFromString(theme.LegendTextColor) ?? Brushes.Black);
            double legendX = center.X + radius + 16;
            double legendY = center.Y - radius;
            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var brush = GetPaletteBrush(theme, i);
                drawingContext.DrawRectangle(brush, null, new Rect(legendX, legendY, 12, 12));
                var text = $"{slice.Label}: {slice.Percentage:P1}";
                var labelText = new FormattedText(text,
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    labelTypeface, 11, legendTextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawingContext.DrawText(labelText, new Point(legendX + 16, legendY - 2));
                legendY += 18;
            }
        }

        private static Point GetPointOnCircle(Point center, double radius, double angleDegrees)
        {
            // 0度=真上、時計回りを基準とした角度からラジアンに変換(WPF 標準座標系で真上は -90度方向)
            double radians = (angleDegrees - 90.0) * Math.PI / 180.0;
            return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
        }

        /// <summary>
        /// Y軸・X軸の目盛り線とラベルを描画する。Bar系列がある場合、X軸はカテゴリラベル側で描画するため数値目盛りは省略する。
        /// </summary>
        private void DrawAxisTicks(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, bool hasBarSeries, bool showGridLines, PlotTheme theme, Axis xAxis, Axis yAxis, Axis? y2Axis)
        {
            var pen = new Pen((Brush)(new BrushConverter().ConvertFromString(theme.GridLineColor) ?? Brushes.LightGray), 1);
            var minorPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 128, 128, 128)), 1);
            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var labelBrush = (Brush)(new BrushConverter().ConvertFromString(theme.AxisLabelColor) ?? Brushes.Black);

            if (!transform.IsYLogarithmic && !yAxis.IsDateTime && showGridLines && yAxis.GridMajorVisible)
            {
                foreach (var tick in AxisTickHelper.GetMinorTicks(transform.YRange, yAxis))
                {
                    var y = transform.ToScreenY(tick);
                    drawingContext.DrawLine(minorPen, new Point(plotArea.Left, y), new Point(plotArea.Right, y));
                }
            }

            var yTicks = transform.IsYLogarithmic
                ? AxisTickHelper.GetLogTicks(transform.YRange)
                : (yAxis.IsDateTime ? AxisTickHelper.GetDateTimeTicks(transform.YRange) : AxisTickHelper.GetTicks(transform.YRange, yAxis));
            var yAxisOrigin = yAxis.Origin is double originX ? transform.ToScreenYAxisOrigin(originX) : plotArea.Left;
            foreach (var tick in yTicks)
            {
                var y = transform.ToScreenY(tick);
                var yTickStart = yAxis.TickDirection == AxisTickDirection.Inward || yAxis.TickDirection == AxisTickDirection.Cross ? yAxisOrigin : yAxisOrigin - 6;
                var yTickEnd = yAxis.TickDirection == AxisTickDirection.Outward ? yAxisOrigin - 6 : yAxisOrigin + 6;
                drawingContext.DrawLine(pen, new Point(yTickStart, y), new Point(yTickEnd, y));
                if (showGridLines && yAxis.GridMajorVisible)
                {
                    drawingContext.DrawLine(pen, new Point(plotArea.Left, y), new Point(plotArea.Right, y));
                }
                if (yAxis.TickLabelsVisible && yAxis.LabelMode == AxisLabelMode.Standard)
                {
                    var label = yAxis.IsDateTime ? DateTime.FromOADate(tick).ToString(yAxis.DateTimeFormat) : tick.ToString("0.###");
                    var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
                    drawingContext.DrawText(text, new Point(yAxisOrigin - text.Width - 2, y - text.Height / 2));
                }
            }

            if (!hasBarSeries)
            {
                if (!transform.IsXLogarithmic && !xAxis.IsDateTime && showGridLines && xAxis.GridMajorVisible)
                {
                    foreach (var tick in AxisTickHelper.GetMinorTicks(transform.XRange, xAxis))
                    {
                        var x = transform.ToScreenX(tick);
                        drawingContext.DrawLine(minorPen, new Point(x, plotArea.Top), new Point(x, plotArea.Bottom));
                    }
                }

                var xTicks = transform.IsXLogarithmic
                    ? AxisTickHelper.GetLogTicks(transform.XRange)
                    : (xAxis.IsDateTime ? AxisTickHelper.GetDateTimeTicks(transform.XRange) : AxisTickHelper.GetTicks(transform.XRange, xAxis));
                var xAxisOrigin = xAxis.Origin is double originY ? transform.ToScreenXAxisOrigin(originY) : plotArea.Bottom;
                foreach (var tick in xTicks)
                {
                    var x = transform.ToScreenX(tick);
                    var xTickStart = xAxis.TickDirection == AxisTickDirection.Inward || xAxis.TickDirection == AxisTickDirection.Cross ? xAxisOrigin : xAxisOrigin + 6;
                    var xTickEnd = xAxis.TickDirection == AxisTickDirection.Outward ? xAxisOrigin + 6 : xAxisOrigin - 6;
                    drawingContext.DrawLine(pen, new Point(x, xTickStart), new Point(x, xTickEnd));
                    if (showGridLines && xAxis.GridMajorVisible)
                    {
                        drawingContext.DrawLine(pen, new Point(x, plotArea.Top), new Point(x, plotArea.Bottom));
                    }
                    if (xAxis.TickLabelsVisible && xAxis.LabelMode == AxisLabelMode.Standard)
                    {
                        var label = xAxis.IsDateTime ? DateTime.FromOADate(tick).ToString(xAxis.DateTimeFormat) : tick.ToString("0.###");
                        var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
                        var labelPoint = new Point(x - text.Width / 2, xAxisOrigin + 2);
                        if (xAxis.IsDateTime)
                        {
                            drawingContext.PushTransform(new RotateTransform(45, x, xAxisOrigin + 2));
                            drawingContext.DrawText(text, labelPoint);
                            drawingContext.Pop();
                        }
                        else
                        {
                            drawingContext.DrawText(text, labelPoint);
                        }
                    }
                }
            }

            if (transform.HasSecondaryYAxis)
            {
                var y2Ticks = transform.IsY2Logarithmic
                    ? AxisTickHelper.GetLogTicks(transform.Y2Range)
                    : AxisTickHelper.GetTicks(transform.Y2Range);
                foreach (var tick in y2Ticks)
                {
                    var y = transform.ToScreenY2(tick);
                    if (y2Axis?.TickLabelsVisible == true && y2Axis.LabelMode == AxisLabelMode.Standard)
                    {
                        var label = tick.ToString("0.###");
                        var text = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
                        drawingContext.DrawText(text, new Point(plotArea.Right + 2, y - text.Height / 2));
                    }
                }
            }

            DrawAxisGridLines(drawingContext, plotArea, transform, xAxis, yAxis, y2Axis);
            DrawAxisMarkers(drawingContext, plotArea, transform, xAxis, yAxis, y2Axis, typeface, dpi);

        }

        private void DrawAxisValueLabels(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis yAxis, Axis? y2Axis)
        {
            var typeface = new Typeface("Segoe UI");
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (xAxis.TickLabelsVisible && xAxis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawXValueLabels(drawingContext, plotArea, transform, xAxis, typeface, dpi);
            }
            if (yAxis.TickLabelsVisible && yAxis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawYValueLabels(drawingContext, plotArea, transform, yAxis, typeface, dpi, false);
            }
            if (y2Axis is not null && transform.HasSecondaryYAxis && y2Axis.TickLabelsVisible && y2Axis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawYValueLabels(drawingContext, plotArea, transform, y2Axis, typeface, dpi, true);
            }
        }

        private static void DrawXValueLabels(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis axis, Typeface typeface, double dpi)
        {
            foreach (var label in axis.ValueLabels.Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text)))
            {
                var x = transform.ToScreenX(label.Value);
                if (x < plotArea.Left || x > plotArea.Right) continue;
                var text = new FormattedText(label.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Black, dpi);
                drawingContext.DrawText(text, new Point(x - text.Width / 2, plotArea.Bottom + 2));
            }
        }

        private static void DrawYValueLabels(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis axis, Typeface typeface, double dpi, bool secondary)
        {
            foreach (var label in axis.ValueLabels.Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text)))
            {
                var y = secondary ? transform.ToScreenY2(label.Value) : transform.ToScreenY(label.Value);
                if (y < plotArea.Top || y > plotArea.Bottom) continue;
                var text = new FormattedText(label.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Black, dpi);
                var x = secondary ? plotArea.Right + 2 : plotArea.Left - text.Width - 2;
                drawingContext.DrawText(text, new Point(x, y - text.Height / 2));
            }
        }

        private static void DrawAlarmZones(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis yAxis, Axis? y2Axis)
        {
            DrawZones(drawingContext, plotArea, transform, xAxis.AlarmZones, true, false, transform.XRange.Min, transform.XRange.Max);
            DrawZones(drawingContext, plotArea, transform, yAxis.AlarmZones, false, false, transform.YRange.Min, transform.YRange.Max);
            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                DrawZones(drawingContext, plotArea, transform, y2Axis.AlarmZones, false, true, transform.Y2Range.Min, transform.Y2Range.Max);
            }
        }

        private static void DrawZones(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, IEnumerable<AxisAlarmZone> zones, bool isX, bool isY2, double axisMinimum, double axisMaximum)
        {
            foreach (var zone in zones.Where(item => item.IsVisible))
            {
                var fillColor = (Color)(ColorConverter.ConvertFromString(string.IsNullOrWhiteSpace(zone.FillColor) ? "Yellow" : zone.FillColor) ?? Colors.Yellow);
                fillColor.A = zone.Opacity;
                var fill = new SolidColorBrush(fillColor);
                var border = new Pen(ToBrush(zone.BorderColor), zone.BorderThickness) { DashStyle = ToDashStyle(zone.BorderDashStyle) };
                foreach (var (dataFrom, dataTo) in AxisAlarmZoneResolver.Resolve(zone, axisMinimum, axisMaximum))
                {
                    var from = isX ? transform.ToScreenX(dataFrom) : (isY2 ? transform.ToScreenY2(dataFrom) : transform.ToScreenY(dataFrom));
                    var to = isX ? transform.ToScreenX(dataTo) : (isY2 ? transform.ToScreenY2(dataTo) : transform.ToScreenY(dataTo));
                    var rect = isX
                        ? new Rect(Math.Min(from, to), plotArea.Top, Math.Abs(to - from), plotArea.Height)
                        : new Rect(plotArea.Left, Math.Min(from, to), plotArea.Width, Math.Abs(to - from));
                    drawingContext.DrawRectangle(fill, border, rect);
                    if (!string.IsNullOrEmpty(zone.Text))
                    {
                        var text = new FormattedText(zone.Text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(FontFamilyName), 10, ToBrush(zone.BorderColor), 1);
                        drawingContext.DrawText(text, new Point(rect.Left, rect.Top));
                    }
                }
            }
        }

        private static DashStyle ToDashStyle(string dashStyle)
            => string.Equals(dashStyle, "Dash", StringComparison.OrdinalIgnoreCase) ? DashStyles.Dash
                : string.Equals(dashStyle, "Dot", StringComparison.OrdinalIgnoreCase) ? DashStyles.Dot
                : DashStyles.Solid;

        private static void DrawAxisGridLines(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis yAxis, Axis? y2Axis)
        {
            foreach (var line in yAxis.GridLines.Where(item => item.IsVisible))
            {
                var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                var y = transform.ToScreenY(line.Value);
                drawingContext.DrawLine(pen, new Point(plotArea.Left, y), new Point(plotArea.Right, y));
            }

            foreach (var line in xAxis.GridLines.Where(item => item.IsVisible))
            {
                var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                var x = transform.ToScreenX(line.Value);
                drawingContext.DrawLine(pen, new Point(x, plotArea.Top), new Point(x, plotArea.Bottom));
            }

            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                foreach (var line in y2Axis.GridLines.Where(item => item.IsVisible))
                {
                    var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                    var y = transform.ToScreenY2(line.Value);
                    drawingContext.DrawLine(pen, new Point(plotArea.Left, y), new Point(plotArea.Right, y));
                }
            }
        }

        private static void DrawAxisMarkers(DrawingContext drawingContext, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis yAxis, Axis? y2Axis, Typeface typeface, double dpi)
        {
            foreach (var marker in yAxis.Markers)
            {
                var y = transform.ToScreenY(marker.Value);
                var brush = ToBrush(marker.Color);
                var pen = new Pen(brush, 1);
                var x = plotArea.Left;
                var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, false);
                var tip = new Point(x + direction.X * 12, y + direction.Y * 12);
                if (marker.ShowConnectionLine) drawingContext.DrawLine(pen, new Point(x, y), tip);
                if (marker.ShowArrow) DrawArrow(drawingContext, pen, tip, new Point(x, y));
                DrawMarkerText(drawingContext, marker.Text, typeface, dpi, brush, tip, direction.X, direction.Y);
            }

            foreach (var marker in xAxis.Markers)
            {
                var x = transform.ToScreenX(marker.Value);
                var brush = ToBrush(marker.Color);
                var pen = new Pen(brush, 1);
                var y = plotArea.Bottom;
                var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, true);
                var tip = new Point(x + direction.X * 12, y + direction.Y * 12);
                if (marker.ShowConnectionLine) drawingContext.DrawLine(pen, new Point(x, y), tip);
                if (marker.ShowArrow) DrawArrow(drawingContext, pen, tip, new Point(x, y));
                DrawMarkerText(drawingContext, marker.Text, typeface, dpi, brush, tip, direction.X, direction.Y);
            }

            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                foreach (var marker in y2Axis.Markers)
                {
                    var y = transform.ToScreenY2(marker.Value);
                    var brush = ToBrush(marker.Color);
                    var pen = new Pen(brush, 1);
                    var x = plotArea.Right;
                    var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, false);
                    var tip = new Point(x + direction.X * 12, y + direction.Y * 12);
                    if (marker.ShowConnectionLine) drawingContext.DrawLine(pen, new Point(x, y), tip);
                    if (marker.ShowArrow) DrawArrow(drawingContext, pen, tip, new Point(x, y));
                    DrawMarkerText(drawingContext, marker.Text, typeface, dpi, brush, tip, direction.X, direction.Y);
                }
            }
        }

        private static void DrawMarkerText(DrawingContext drawingContext, string text, Typeface typeface, double dpi, Brush brush, Point tip, double directionX, double directionY)
        {
            if (string.IsNullOrEmpty(text)) return;
            var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, brush, dpi);
            var x = tip.X + directionX * 2 - (directionX == 0 ? formatted.Width / 2 : directionX < 0 ? formatted.Width : 0);
            var y = tip.Y + directionY * 2 - (directionY == 0 ? formatted.Height / 2 : directionY < 0 ? formatted.Height : 0);
            drawingContext.DrawText(formatted, new Point(x, y));
        }

        private static Pen CreatePen(string color, double thickness, string dashStyle)
        {
            var pen = new Pen(ToBrush(color), thickness);
            if (string.Equals(dashStyle, "Dash", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = DashStyles.Dash;
            else if (string.Equals(dashStyle, "Dot", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = DashStyles.Dot;
            return pen;
        }

        private static Brush ToBrush(string color)
            => (Brush)(new BrushConverter().ConvertFromString(string.IsNullOrWhiteSpace(color) ? "Black" : color) ?? Brushes.Black);

        private static void DrawArrow(DrawingContext drawingContext, Pen pen, Point tip, Point tail)
        {
            var dx = tip.X - tail.X;
            var dy = tip.Y - tail.Y;
            var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            var ux = dx / length;
            var uy = dy / length;
            var left = new Point(tip.X - ux * 6 - uy * 3, tip.Y - uy * 6 + ux * 3);
            var right = new Point(tip.X - ux * 6 + uy * 3, tip.Y - uy * 6 - ux * 3);
            drawingContext.DrawLine(pen, tip, left);
            drawingContext.DrawLine(pen, tip, right);
        }

        private void DrawScrollButtons(DrawingContext drawingContext, PlotRect plotArea, Axis xAxis, Axis yAxis)
        {
            scrollXPreviousButton = Rect.Empty;
            scrollXNextButton = Rect.Empty;
            scrollYPreviousButton = Rect.Empty;
            scrollYNextButton = Rect.Empty;
            var border = new Pen(Brushes.Gray, 1);
            var background = new SolidColorBrush(Color.FromArgb(220, 240, 240, 240));
            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            if (xAxis.ShowScrollButtons)
            {
                scrollXPreviousButton = new Rect(plotArea.Left, plotArea.Bottom + 18, 18, 18);
                scrollXNextButton = new Rect(plotArea.Right - 18, plotArea.Bottom + 18, 18, 18);
                DrawScrollButton(drawingContext, scrollXPreviousButton, "◀", border, background, typeface, dpi);
                DrawScrollButton(drawingContext, scrollXNextButton, "▶", border, background, typeface, dpi);
            }

            if (yAxis.ShowScrollButtons)
            {
                scrollYPreviousButton = new Rect(plotArea.Right + 28, plotArea.Top, 18, 18);
                scrollYNextButton = new Rect(plotArea.Right + 28, plotArea.Bottom - 18, 18, 18);
                DrawScrollButton(drawingContext, scrollYPreviousButton, "▲", border, background, typeface, dpi);
                DrawScrollButton(drawingContext, scrollYNextButton, "▼", border, background, typeface, dpi);
            }
        }

        private static void DrawScrollButton(DrawingContext drawingContext, Rect bounds, string text, Pen border, Brush background, Typeface typeface, double dpi)
        {
            drawingContext.DrawRectangle(background, border, bounds);
            var formattedText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Black, dpi);
            drawingContext.DrawText(formattedText, new Point(bounds.Left + (bounds.Width - formattedText.Width) / 2, bounds.Top + (bounds.Height - formattedText.Height) / 2));
        }

        /// <summary>
        /// X軸・Y軸のタイトル文字列(C1C の <c>Axis.Text</c> 相当)を描画する。
        /// X軸タイトルは数値目盛りラベルの下に中央揃えで、Y軸タイトルは数値目盛りラベルの左側に
        /// 90度回転させた縦書きで描画する。
        /// </summary>
        private void DrawAxisTitles(DrawingContext drawingContext, PlotRect plotArea, string xAxisTitle, string yAxisTitle, PlotTheme theme)
        {
            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var brush = (Brush)(new BrushConverter().ConvertFromString(theme.AxisLabelColor) ?? Brushes.Black);

            if (!string.IsNullOrEmpty(xAxisTitle))
            {
                var text = new FormattedText(xAxisTitle, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, brush, dpi);
                var x = plotArea.Left + plotArea.Width / 2 - text.Width / 2;
                var y = plotArea.Bottom + 16;
                drawingContext.DrawText(text, new Point(x, y));
            }

            if (!string.IsNullOrEmpty(yAxisTitle))
            {
                var text = new FormattedText(yAxisTitle, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, brush, dpi);
                drawingContext.PushTransform(new RotateTransform(-90, 4, plotArea.Top + plotArea.Height / 2));
                drawingContext.DrawText(text, new Point(4 - text.Width / 2, plotArea.Top + plotArea.Height / 2 - text.Height / 2));
                drawingContext.Pop();
            }
        }

        /// <summary>
        /// 各系列の名前と色を凡例としてプロット領域右上に描画する。
        /// </summary>
        private void DrawLegend(DrawingContext drawingContext, PlotRect plotArea, PlotModel model)
        {
            legendItemRects.Clear();
            if (!model.ShowLegend) return;
            var allSeries = model.GetAllSeries().ToList();
            if (allSeries.Count == 0) return;

            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            const double swatchSize = 10;
            const double padding = 4;
            const double lineHeight = 16;
            var theme = model.Theme;
            var legendTextBrush = (Brush)(new BrushConverter().ConvertFromString(theme.LegendTextColor) ?? Brushes.Black);

            var texts = allSeries.Select(s => new FormattedText(s.Name, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, s.Visible ? legendTextBrush : Brushes.Gray, dpi)).ToList();
            double maxTextWidth = texts.Max(t => t.Width);
            double legendWidth = swatchSize + padding * 3 + maxTextWidth;
            double legendHeight = padding * 2 + lineHeight * allSeries.Count;

            double legendX = model.LegendPosition is LegendPosition.TopLeft or LegendPosition.BottomLeft
                ? plotArea.Left + padding
                : plotArea.Right - legendWidth - padding;
            double legendY = model.LegendPosition is LegendPosition.BottomLeft or LegendPosition.BottomRight
                ? plotArea.Bottom - legendHeight - padding
                : plotArea.Top + padding;

            var backBrush = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
            var borderPen = new Pen((Brush)(new BrushConverter().ConvertFromString(theme.GridLineColor) ?? Brushes.LightGray), 1);
            drawingContext.DrawRectangle(backBrush, borderPen, new Rect(legendX, legendY, legendWidth, legendHeight));

            for (int i = 0; i < allSeries.Count; i++)
            {
                var series = allSeries[i];
                var itemY = legendY + padding + i * lineHeight;
                var colorName = GetSeriesColorName(series, theme);
                var brush = series.Visible ? (Brush)(new BrushConverter().ConvertFromString(colorName) ?? Brushes.Black) : Brushes.LightGray;
                drawingContext.DrawRectangle(brush, null, new Rect(legendX + padding, itemY + (lineHeight - swatchSize) / 2, swatchSize, swatchSize));
                drawingContext.DrawText(texts[i], new Point(legendX + padding * 2 + swatchSize, itemY));

                legendItemRects.Add((series, new Rect(legendX, itemY, legendWidth, lineHeight)));
            }
        }

        private static string GetSeriesColorName(Series series, PlotTheme theme) => series switch
        {
            LineSeries line => string.IsNullOrWhiteSpace(line.Color) ? theme.DefaultSeriesColor : line.Color,
            _ => theme.DefaultSeriesColor
        };

        private static DashStyle ToWpfDashStyle(LineDashStyle dashStyle) => dashStyle switch
        {
            LineDashStyle.Dash => DashStyles.Dash,
            LineDashStyle.Dot => DashStyles.Dot,
            LineDashStyle.DashDot => DashStyles.DashDot,
            _ => DashStyles.Solid
        };

        private static void DrawMarker(DrawingContext drawingContext, Brush brush, MarkerShape shape, Point center, double size)
        {
            double half = size / 2;
            switch (shape)
            {
                case MarkerShape.Dot:
                    drawingContext.DrawEllipse(brush, null, center, Math.Max(0.5, half / 2), Math.Max(0.5, half / 2));
                    break;
                case MarkerShape.Square:
                    drawingContext.DrawRectangle(brush, null, new Rect(center.X - half, center.Y - half, size, size));
                    break;
                case MarkerShape.Box:
                    drawingContext.DrawRectangle(null, new Pen(brush, Math.Max(1.0, size / 8)), new Rect(center.X - half, center.Y - half, size, size));
                    break;
                case MarkerShape.Diamond:
                    {
                        var figure = new PathFigure { StartPoint = new Point(center.X, center.Y - half), IsClosed = true };
                        figure.Segments.Add(new LineSegment(new Point(center.X + half, center.Y), true));
                        figure.Segments.Add(new LineSegment(new Point(center.X, center.Y + half), true));
                        figure.Segments.Add(new LineSegment(new Point(center.X - half, center.Y), true));
                        drawingContext.DrawGeometry(brush, null, new PathGeometry(new[] { figure }));
                    }
                    break;
                case MarkerShape.InvertedTri:
                    {
                        var figure = new PathFigure { StartPoint = new Point(center.X - half, center.Y - half), IsClosed = true };
                        figure.Segments.Add(new LineSegment(new Point(center.X + half, center.Y - half), true));
                        figure.Segments.Add(new LineSegment(new Point(center.X, center.Y + half), true));
                        drawingContext.DrawGeometry(brush, null, new PathGeometry(new[] { figure }));
                    }
                    break;
                case MarkerShape.Triangle:
                    {
                        var figure = new PathFigure { StartPoint = new Point(center.X, center.Y - half), IsClosed = true };
                        figure.Segments.Add(new LineSegment(new Point(center.X + half, center.Y + half), true));
                        figure.Segments.Add(new LineSegment(new Point(center.X - half, center.Y + half), true));
                        drawingContext.DrawGeometry(brush, null, new PathGeometry(new[] { figure }));
                    }
                    break;
                case MarkerShape.DiagCross:
                    {
                        var pen = new Pen(brush, Math.Max(1.0, size / 4));
                        drawingContext.DrawLine(pen, new Point(center.X - half, center.Y - half), new Point(center.X + half, center.Y + half));
                        drawingContext.DrawLine(pen, new Point(center.X - half, center.Y + half), new Point(center.X + half, center.Y - half));
                    }
                    break;
                case MarkerShape.Cross:
                    {
                        var pen = new Pen(brush, Math.Max(1.0, size / 4));
                        drawingContext.DrawLine(pen, new Point(center.X - half, center.Y - half), new Point(center.X + half, center.Y + half));
                        drawingContext.DrawLine(pen, new Point(center.X - half, center.Y + half), new Point(center.X + half, center.Y - half));
                    }
                    break;
                case MarkerShape.Plus:
                    {
                        var pen = new Pen(brush, Math.Max(1.0, size / 4));
                        drawingContext.DrawLine(pen, new Point(center.X - half, center.Y), new Point(center.X + half, center.Y));
                        drawingContext.DrawLine(pen, new Point(center.X, center.Y - half), new Point(center.X, center.Y + half));
                    }
                    break;
                case MarkerShape.Star:
                    {
                        var figure = new PathFigure { IsClosed = true };
                        for (int i = 0; i < 10; i++)
                        {
                            double angle = Math.PI / 2 * 3 + i * Math.PI / 5;
                            double radius = (i % 2 == 0) ? half : half / 2.5;
                            var point = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
                            if (i == 0)
                            {
                                figure.StartPoint = point;
                            }
                            else
                            {
                                figure.Segments.Add(new LineSegment(point, true));
                            }
                        }
                        drawingContext.DrawGeometry(brush, null, new PathGeometry(new[] { figure }));
                    }
                    break;
                case MarkerShape.Circle:
                default:
                    drawingContext.DrawEllipse(brush, null, center, half, half);
                    break;
            }
        }

        private void DrawSeriesDataLabels(DrawingContext drawingContext, CoordinateTransform transform, List<(double X, double Y)> points, string? format, bool useSecondaryYAxis = false)
        {
            var typeface = new Typeface(FontFamilyName);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            foreach (var (x, y) in points)
            {
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                var text = PlotRenderHelper.FormatDataLabel(y, format);
                var formattedText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Black, dpi);
                var screenX = transform.ToScreenX(x);
                var screenY = useSecondaryYAxis ? transform.ToScreenY2(y) : transform.ToScreenY(y);
                drawingContext.DrawText(formattedText, new Point(screenX - formattedText.Width / 2, screenY - formattedText.Height - 2));
            }
        }

        /// <summary>
        /// 現在の描画内容を PNG として保存します。
        /// </summary>
        public void SaveAsPng(string filePath)
        {
            var width = Math.Max(1, (int)ActualWidth);
            var height = Math.Max(1, (int)ActualHeight);
            var dpi = VisualTreeHelper.GetDpi(this);
            var renderBitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            renderBitmap.Render(this);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
            encoder.Save(stream);
        }

        /// <summary>
        /// 現在のグラフを印刷ダイアログ経由で印刷します。
        /// アスペクト比を維持したまま用紙の印刷可能領域に収まるようスケーリングして中央に配置します。
        /// </summary>
        public void Print()
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var contentWidth = Math.Max(1, ActualWidth);
            var contentHeight = Math.Max(1, ActualHeight);
            var printableWidth = printDialog.PrintableAreaWidth;
            var printableHeight = printDialog.PrintableAreaHeight;

            double scale = Math.Min(printableWidth / contentWidth, printableHeight / contentHeight);
            double scaledWidth = contentWidth * scale;
            double scaledHeight = contentHeight * scale;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var brush = new VisualBrush(this) { Stretch = Stretch.Uniform };
                var offsetX = (printableWidth - scaledWidth) / 2;
                var offsetY = (printableHeight - scaledHeight) / 2;
                dc.DrawRectangle(brush, null, new Rect(offsetX, offsetY, scaledWidth, scaledHeight));
            }

            printDialog.PrintVisual(visual, Model?.Title ?? "UnifiedChart");
        }

        private void HandleMouseWheelZoom(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (Model == null) return;

            var plotArea = GetPlotArea();
            var mousePos = e.GetPosition(this);
            var transform = PlotRenderHelper.CreateTransform(Model, plotArea, zoomedXRange, zoomedYRange);

            double factor = e.Delta > 0 ? 0.9 : 1.0 / 0.9;
            double dataX = transform.ToDataX(mousePos.X);
            double dataY = transform.ToDataY(mousePos.Y);

            var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
            var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);

            var newX = ZoomRange(currentX, dataX, factor);
            var newY = ZoomRange(currentY, dataY, factor);
            StartZoomAnimation(newX, newY);
        }

        /// <summary>
        /// 指定したズーム範囲まで軸範囲を滑らかにアニメーションさせる。
        /// </summary>
        private void StartZoomAnimation(AxisRange targetX, AxisRange targetY)
        {
            var plotArea = GetPlotArea();
            animationStartXRange = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
            animationStartYRange = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
            animationTargetXRange = targetX;
            animationTargetYRange = targetY;
            animationStartTime = DateTime.UtcNow;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (animationTargetXRange == null || animationTargetYRange == null ||
                animationStartXRange == null || animationStartYRange == null)
            {
                animationTimer.Stop();
                return;
            }

            var elapsedMs = (DateTime.UtcNow - animationStartTime).TotalMilliseconds;
            double t = Math.Min(1.0, elapsedMs / AnimationDurationMs);
            double eased = 1 - Math.Pow(1 - t, 3); // ease-out cubic

            zoomedXRange = LerpRange(animationStartXRange.Value, animationTargetXRange.Value, eased);
            zoomedYRange = LerpRange(animationStartYRange.Value, animationTargetYRange.Value, eased);
            InvalidateVisual();

            if (t >= 1.0)
            {
                animationTimer.Stop();
                animationTargetXRange = null;
                animationTargetYRange = null;
                animationStartXRange = null;
                animationStartYRange = null;
            }
        }

        private static AxisRange LerpRange(AxisRange from, AxisRange to, double t)
        {
            var min = from.Min + (to.Min - from.Min) * t;
            var max = from.Max + (to.Max - from.Max) * t;
            return new AxisRange(min, max);
        }

        /// <summary>
        /// タッチのピンチズーム/パン操作(Manipulation)に対応する。
        /// </summary>
        private void HandleManipulationDelta(object? sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (Model == null) return;

            var plotArea = GetPlotArea();
            var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
            var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
            var transform = new CoordinateTransform(plotArea, currentX, currentY);

            var origin = e.ManipulationOrigin;
            var scale = e.DeltaManipulation.Scale;

            // 拡大縮小(ピンチズーム)
            if (Math.Abs(scale.X - 1.0) > 0.0001 || Math.Abs(scale.Y - 1.0) > 0.0001)
            {
                double factorX = scale.X != 0 ? 1.0 / scale.X : 1.0;
                double factorY = scale.Y != 0 ? 1.0 / scale.Y : 1.0;
                double dataX = transform.ToDataX(origin.X);
                double dataY = transform.ToDataY(origin.Y);
                currentX = ZoomRange(currentX, dataX, factorX);
                currentY = ZoomRange(currentY, dataY, factorY);
            }

            // 平行移動(パン)
            var translation = e.DeltaManipulation.Translation;
            if (translation.X != 0 || translation.Y != 0)
            {
                var dxData = transform.ToDataX(translation.X) - transform.ToDataX(0);
                var dyData = transform.ToDataY(translation.Y) - transform.ToDataY(0);
                currentX = new AxisRange(currentX.Min - dxData, currentX.Max - dxData);
                currentY = new AxisRange(currentY.Min - dyData, currentY.Max - dyData);
            }

            zoomedXRange = currentX;
            zoomedYRange = currentY;
            InvalidateVisual();
            e.Handled = true;
        }

        private static AxisRange ZoomRange(AxisRange range, double pivot, double factor)
        {
            var newMin = pivot - (pivot - range.Min) * factor;
            var newMax = pivot + (range.Max - pivot) * factor;
            return new AxisRange(newMin, newMax);
        }

        private AxisRange GetAutoRange(PlotRect plotArea, bool isX)
            => PlotRenderHelper.GetAutoRange(Model!, plotArea, isX);

        private void ScrollRange(bool isX, double direction)
        {
            if (Model is null) return;

            var plotArea = GetPlotArea();
            var range = isX
                ? zoomedXRange ?? GetAutoRange(plotArea, true)
                : zoomedYRange ?? GetAutoRange(plotArea, false);
            var bounds = PlotRenderHelper.GetAutoRange(Model, plotArea, isX);
            var axis = isX ? Model.XAxis : Model.YAxis;
            if (!axis.Scroll.IsEnabled) return;
            var scrolled = AxisScrollHelper.Scroll(range, direction, axis.Scroll.Unit, axis.Scroll.Minimum ?? bounds.Min, axis.Scroll.Maximum ?? bounds.Max);
            if (isX) zoomedXRange = scrolled;
            else
            {
                zoomedYRange = scrolled;
                if (Model.Y2Axis is { Scroll.SynchronizeWithY2: true } y2Axis)
                {
                    var y2Bounds = PlotRenderHelper.GetAutoRange(Model, plotArea, false);
                    var y2Range = zoomedY2Range ?? y2Bounds;
                    zoomedY2Range = AxisScrollHelper.Scroll(y2Range, direction, y2Axis.Scroll.Unit, y2Axis.Scroll.Minimum ?? y2Bounds.Min, y2Axis.Scroll.Maximum ?? y2Bounds.Max);
                }
            }
            InvalidateVisual();
        }

        private void HandleMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            if (scrollXPreviousButton.Contains(position))
            {
                ScrollX(-1);
                e.Handled = true;
                return;
            }

            if (scrollXNextButton.Contains(position))
            {
                ScrollX(1);
                e.Handled = true;
                return;
            }

            if (scrollYPreviousButton.Contains(position))
            {
                ScrollY(1);
                e.Handled = true;
                return;
            }

            if (scrollYNextButton.Contains(position))
            {
                ScrollY(-1);
                e.Handled = true;
                return;
            }

            foreach (var (series, rect) in legendItemRects)
            {
                if (rect.Contains(position))
                {
                    series.Visible = !series.Visible;
                    InvalidateVisual();
                    return;
                }
            }

            isPanning = true;
            panLastScreenPoint = position;
            CaptureMouse();
        }

        private void HandleMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isPanning = false;
            ReleaseMouseCapture();
        }

        private void HandleMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Model == null) return;
            var currentPos = e.GetPosition(this);

            if (isPanning)
            {
                var plotArea = GetPlotArea();
                var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
                var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
                var transform = new CoordinateTransform(plotArea, currentX, currentY);

                var dxData = transform.ToDataX(currentPos.X) - transform.ToDataX(panLastScreenPoint.X);
                var dyData = transform.ToDataY(currentPos.Y) - transform.ToDataY(panLastScreenPoint.Y);

                var xBounds = PlotRenderHelper.GetAutoRange(Model, plotArea, true);
                var yBounds = PlotRenderHelper.GetAutoRange(Model, plotArea, false);
                zoomedXRange = currentX.TranslateClamped(-dxData, xBounds);
                zoomedYRange = currentY.TranslateClamped(-dyData, yBounds);
                panLastScreenPoint = currentPos;
                InvalidateVisual();
                return;
            }

            ShowNearestPointTooltip(currentPos);
        }

        private void ShowNearestPointTooltip(Point screenPoint)
        {
            if (Model == null) return;

            var plotArea = GetPlotArea();
            var transform = PlotRenderHelper.CreateTransform(Model, plotArea, zoomedXRange, zoomedYRange);

            const double thresholdPixels = 10.0;
            var nearest = PlotRenderHelper.FindNearestPointInfo(Model, transform, screenPoint.X, screenPoint.Y, thresholdPixels);

            if (nearest.HasValue)
            {
                var text = PlotRenderHelper.FormatTooltip(nearest.Value.Series, nearest.Value.X, nearest.Value.Y);
                if (text != lastTooltipText)
                {
                    lastTooltipText = text;
                    toolTip.Content = text;
                    toolTip.IsOpen = true;
                }
            }
            else if (lastTooltipText != null)
            {
                lastTooltipText = null;
                toolTip.IsOpen = false;
            }
        }
    }
}
