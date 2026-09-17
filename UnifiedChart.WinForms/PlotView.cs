namespace UnifiedChart.WinForms
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using UnifiedChart;

    public class PlotView : UserControl, IPlotView
    {
        private PlotModel? model;
        private AxisRange? zoomedXRange;
        private AxisRange? zoomedYRange;
        private AxisRange? zoomedY2Range;
        private bool isPanning;
        private Point panLastScreenPoint;
        private readonly ToolTip toolTip = new ToolTip();
        private string? lastTooltipText;
        private readonly List<(Series Series, RectangleF Rect)> legendItemRects = new();
        private string FontFamilyName => string.IsNullOrWhiteSpace(Model?.FontFamilyName) ? "Meiryo UI" : Model!.FontFamilyName;
        private RectangleF scrollXPreviousButton;
        private RectangleF scrollXNextButton;
        private RectangleF scrollYPreviousButton;
        private RectangleF scrollYNextButton;

        // ズーム／パンをスムーズに見せるためのアニメーション用状態。
        private AxisRange? animationTargetXRange;
        private AxisRange? animationTargetYRange;
        private AxisRange? animationStartXRange;
        private AxisRange? animationStartYRange;
        private AxisRange? animationTargetY2Range;
        private AxisRange? animationStartY2Range;
        private readonly System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
        private DateTime animationStartTime;
        private const int AnimationDurationMs = 150;

        // WM_GESTURE によるピンチズーム用の直前距離。
        private long lastGestureZoomDistance;

        public PlotView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private static void DrawX2ValueLabels(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis axis, Font font)
        {
            foreach (var label in axis.ValueLabels.Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text)))
            {
                var x = (float)transform.ToScreenX2(label.Value);
                if (x < plotArea.Left || x > plotArea.Right) continue;
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(label.Color)));
                var size = g.MeasureString(label.Text, font);
                g.DrawString(label.Text, font, brush, x - size.Width / 2, (float)plotArea.Top - size.Height - 8);
            }
        }

        public PlotModel? Model
        {
            get => model;
            set
            {
                model = value;
                UnifiedChart.ChartLogger.Write("PlotView.Model設定", model);
                ResetZoom();
                Invalidate();
            }
        }
        public void InvalidatePlot()
        {
            UnifiedChart.ChartLogger.Write("PlotView.InvalidatePlot", model);
            Invalidate();
        }

        public AxisRange? DisplayXRange => zoomedXRange;

        public AxisRange? DisplayYRange => zoomedYRange;

        public AxisRange? DisplayY2Range => zoomedY2Range;

        public event EventHandler? AxisScroll;

        public int CoordToDataIndex(Point screenPoint)
            => TryCoordToDataIndex(screenPoint, out var dataIndex) ? dataIndex : -1;

        public bool TryCoordToDataIndex(Point screenPoint, out int dataIndex)
        {
            dataIndex = -1;
            if (Model is null)
            {
                return false;
            }

            var plotArea = GetPlotArea();
            var transform = GetTransform(plotArea);
            var nearest = PlotRenderHelper.FindNearestPointInfo(Model, transform, screenPoint.X, screenPoint.Y, 10.0);
            if (!nearest.HasValue)
            {
                return false;
            }

            dataIndex = nearest.Value.DataIndex;
            return true;
        }

        public void CoordToDataIndex(int x, int y, int groupIndex, int focus, out int seriesIndex, out int pointIndex, out int distance)
        {
            TryCoordToDataIndex(x, y, groupIndex, focus, out seriesIndex, out pointIndex, out distance);
        }

        private bool TryCoordToDataIndex(int x, int y, int groupIndex, int focus, out int seriesIndex, out int pointIndex, out int distance)
        {
            seriesIndex = -1;
            pointIndex = -1;
            distance = -1;
            if (Model is null || groupIndex < 0 || groupIndex >= Model.ChartGroups.Count)
            {
                return false;
            }

            var plotArea = GetPlotArea();
            var transform = GetTransform(plotArea);
            var series = Model.ChartGroups[groupIndex].ChartData.SelectMany(data => data.SeriesList).ToList();
            var nearest = PlotRenderHelper.FindNearestPointInfo(Model, transform, x, y, double.PositiveInfinity, series);
            if (!nearest.HasValue)
            {
                return false;
            }

            var dx = Math.Abs(transform.ToScreenX(nearest.Value.X) - x);
            var dy = Math.Abs(transform.ToScreenY(nearest.Value.Y) - y);
            if (focus == 0 && dx > 10 ||
                focus == 1 && dy > 10 ||
                focus == 2 && Math.Sqrt(dx * dx + dy * dy) > 10)
            {
                return false;
            }

            seriesIndex = series.IndexOf(nearest.Value.Series);
            pointIndex = nearest.Value.DataIndex;
            distance = (int)Math.Round(focus switch
            {
                0 => dx,
                1 => dy,
                _ => Math.Sqrt(dx * dx + dy * dy)
            });
            return true;
        }

        public void SetDisplayRange(AxisRange? xRange, AxisRange? yRange)
        {
            zoomedXRange = xRange;
            zoomedYRange = yRange;
            zoomedY2Range = null;
            UnifiedChart.ChartLogger.Write("PlotView.SetDisplayRange", model, $"X={xRange}, Y={yRange}");
            Invalidate();
        }

        public void ScrollX(double direction = 1) => ScrollAxis(true, direction);

        public void ScrollY(double direction = 1) => ScrollAxis(false, direction);

        /// <summary>
        /// 通常の描画が完了した後に発生するイベント。ユーザーコードから任意の図形を追加描画できる。
        /// </summary>
        public event EventHandler<CustomRenderEventArgs>? CustomRender;

        /// <summary>
        /// ズーム・パン状態をリセットし、データ全体が表示される状態に戻す。
        /// </summary>
        public void ResetZoom()
        {
            zoomedXRange = null;
            zoomedYRange = null;
            zoomedY2Range = null;
            animationTargetY2Range = null;
            animationStartY2Range = null;
            UnifiedChart.ChartLogger.Write("PlotView.ResetZoom", model);
            Invalidate();
        }

        private CoordinateTransform GetTransform(PlotRect plotArea)
        {
            if (Model == null) return new CoordinateTransform(plotArea, new AxisRange(0, 1), new AxisRange(0, 1));
            return PlotRenderHelper.CreateTransform(Model, plotArea, zoomedXRange, zoomedYRange, zoomedY2Range);
        }

        /// <summary>
        /// 軸タイトルの有無を考慮したプロット領域を計算する。軸タイトルが設定されている場合は
        /// 対応する余白(下側/左側)を追加で確保する。
        /// </summary>
        private PlotRect GetPlotArea()
        {
            var rightMargin = Model?.Y2Axis != null ? Math.Max(48, Model.PlotMarginRight) : Model?.PlotMarginRight ?? PlotLayout.DefaultRightMargin;
            var leftMargin = (Model?.PlotMarginLeft ?? PlotLayout.DefaultLeftMargin)
                + (string.IsNullOrEmpty(Model?.YAxis.Title) ? 0 : Model?.AxisTitleFontSize + 5 ?? 14);
            if(Model is not null)
            {
                using var axisFont = new Font(FontFamilyName, Model.AxisLabelFontSize);
                var yLabelWidth = Model.YAxis.ValueLabels
                    .Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text))
                    .Select(item => TextRenderer.MeasureText(item.Text, axisFont).Width)
                    .DefaultIfEmpty(0)
                    .Max();
                    leftMargin = Math.Max(leftMargin, (float)Model.PlotMarginLeft + yLabelWidth + 6);
            }

            var topMargin = (Model?.PlotMarginTop ?? PlotLayout.DefaultTopMargin) + (Model?.X2Axis != null ? 24 : 0);
            var bottomMargin = (Model?.PlotMarginBottom ?? PlotLayout.DefaultBottomMargin)
                + (string.IsNullOrEmpty(Model?.XAxis.Title) ? 0 : Model?.AxisTitleFontSize + 5 ?? 14)
                + (string.Equals(Model?.TitleCompass, "South", StringComparison.OrdinalIgnoreCase) ? 24 : 0)
                + (Model?.XAxis.IsDateTime == true ? 32 : 0);
            return PlotLayout.CalculatePlotArea(ClientSize.Width, ClientSize.Height,
                topMargin: topMargin, leftMargin: leftMargin, rightMargin: rightMargin, bottomMargin: bottomMargin);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UnifiedChart.ChartLogger.Write("PlotView.OnPaint", model, $"Size={ClientSize.Width}x{ClientSize.Height}");
            var theme = Model?.Theme ?? PlotTheme.Light;
            e.Graphics.Clear(ColorTranslator.FromHtml(SafeColorName(theme.BackgroundColor)));
            if (Model == null)
            {
                using var f = new Font(FontFamilyName, Model?.AxisLabelFontSize ?? 9);
                TextRenderer.DrawText(e.Graphics, "UnifiedChart: no data", f, ClientRectangle, Color.Gray);
                return;
            }
            using var font = new Font(FontFamilyName, Model.TitleFontSize);
            using var titleBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.TitleColor)));
            DrawTitle(e.Graphics, Model.Title ?? string.Empty, Model.TitleCompass, font, titleBrush);
            using var footerFont = new Font(FontFamilyName, Model.FooterFontSize);
            DrawFooter(e.Graphics, Model.Footer ?? string.Empty, Model.FooterCompass, footerFont, titleBrush);

            var pieSeries = Model.GetAllSeries().FirstOrDefault(s => s.Visible && s is PieSeries) as PieSeries;
            if (pieSeries != null)
            {
                var pieArea = GetPlotArea();
                DrawPieChart(e.Graphics, pieArea, pieSeries, theme, FontFamilyName, Model.LegendFontSize);
                var pieTransform = GetTransform(pieArea);
                CustomRender?.Invoke(this, new CustomRenderEventArgs(e.Graphics, pieArea, pieTransform, Model));
                return;
            }

            var plotArea = GetPlotArea();
            var radarSeries = Model.GetAllSeries().OfType<RadarSeries>().Where(s => s.Points.Count > 0).ToList();
            if (radarSeries.Count > 0)
            {
                DrawRadarChart(e.Graphics, plotArea, radarSeries, theme, FontFamilyName, false, Model.RadarGridLevels, Model.RadarGridLabels, Model.RadarDirection, Model.ValueLabelFontSize, Model.LegendFontSize);
                var radarTransform = GetTransform(plotArea);
                DrawLegend(e.Graphics, plotArea, Model);
                CustomRender?.Invoke(this, new CustomRenderEventArgs(e.Graphics, plotArea, radarTransform, Model));
                return;
            }

            var transform = GetTransform(plotArea);

            bool hasBarSeries = Model.GetAllSeries().Any(s => s.Visible && (s is BarSeries || s is BoxPlotSeries || s is StackedBarSeries));
            DrawAlarmZones(e.Graphics, plotArea, transform, Model.XAxis, Model.X2Axis, Model.YAxis, Model.Y2Axis, Model.AlarmZoneFontSize);
            DrawAxisTicks(e.Graphics, plotArea, transform, hasBarSeries, Model.ShowGridLines, theme, Model.XAxis, Model.X2Axis, Model.YAxis, Model.Y2Axis, FontFamilyName, Model.AxisLabelFontSize);
            DrawAxisValueLabels(e.Graphics, plotArea, transform, Model.XAxis, Model.X2Axis, Model.YAxis, Model.Y2Axis, FontFamilyName, Model.ValueLabelFontSize);
            DrawAxisTitles(e.Graphics, plotArea, Model.XAxis.Title, Model.X2Axis?.Title, Model.YAxis.Title, theme, FontFamilyName, Model.AxisTitleFontSize);
            DrawScrollButtons(e.Graphics, plotArea, Model.XAxis, Model.YAxis, Model.AxisLabelFontSize);

            foreach (var series in Model.GetAllSeries())
            {
                if (!series.Visible) continue;
                if (series is LineSeries line && line.Points.Count > 0)
                {
                    using var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(line.Color)), (float)line.StrokeThickness);
                    pen.DashStyle = ToGdiDashStyle(line.DashStyle);
                    using var markerBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(line.Color)));
                    using var markerOutlinePen = new Pen(ColorTranslator.FromHtml(SafeColorName(line.MarkerOutlineColor)), 1f);
                    PointF? previous = null;
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
                        var current = new PointF((float)ScreenX(transform, line, x), (float)screenY);
                        if (previous.HasValue)
                        {
                            e.Graphics.DrawLine(pen, previous.Value, current);
                        }
                        if (line.ShowMarkers)
                        {
                            DrawMarker(e.Graphics, markerBrush, markerOutlinePen, line.MarkerShape, current.X, current.Y, (float)Math.Max(1.0, line.MarkerSize * 1.5));
                        }
                        previous = current;
                    }

                    if (line.ShowDataLabels)
                    {
                        DrawSeriesDataLabels(e.Graphics, transform, line.Points, line.DataLabelFormat, line.UseSecondaryYAxis, line.UseSecondaryXAxis, FontFamilyName, Model.DataLabelFontSize);
                    }
                }

                else if (series is AreaSeries area && area.Points.Count > 0)
                {
                    using var fillBrush = new SolidBrush(Color.FromArgb(150, ColorTranslator.FromHtml(SafeColorName(area.FillColor))));
                    using var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(area.Color)), (float)area.StrokeThickness);
                    pen.DashStyle = ToGdiDashStyle(area.DashStyle);
                    var baselineY = area.UseSecondaryYAxis ? (float)transform.ToScreenY2(area.BaselineValue) : (float)transform.ToScreenY(area.BaselineValue);

                    var segment = new List<PointF>();
                    void FlushSegment()
                    {
                        if (segment.Count > 1)
                        {
                            var polygon = new List<PointF> { new PointF(segment[0].X, baselineY) };
                            polygon.AddRange(segment);
                            polygon.Add(new PointF(segment[^1].X, baselineY));
                            e.Graphics.FillPolygon(fillBrush, polygon.ToArray());
                            e.Graphics.DrawLines(pen, segment.ToArray());
                        }
                        else if (segment.Count == 1)
                        {
                            var point = segment[0];
                            const float pointSize = 4f;
                            using var pointBrush = new SolidBrush(pen.Color);
                            e.Graphics.FillEllipse(pointBrush, point.X - pointSize / 2, point.Y - pointSize / 2, pointSize, pointSize);
                        }
                        segment = new List<PointF>();
                    }

                    foreach (var (x, y) in area.Points)
                    {
                        if (MissingValueContract.IsMissing(x, area.MissingValueHole) || MissingValueContract.IsMissing(y, area.MissingValueHole))
                        {
                            FlushSegment();
                            continue;
                        }
                        var screenY = area.UseSecondaryYAxis ? transform.ToScreenY2(y) : transform.ToScreenY(y);
                        segment.Add(new PointF((float)ScreenX(transform, area, x), (float)screenY));
                    }
                    FlushSegment();
                }
                else if (series is RangeSeries range && range.Points.Count > 0)
                {
                    using var fillBrush = new SolidBrush(Color.FromArgb(150, ColorTranslator.FromHtml(SafeColorName(range.FillColor))));
                    using var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(range.Color)), (float)range.StrokeThickness);

                    var topSegment = new List<PointF>();
                    var bottomSegment = new List<PointF>();
                    void FlushRangeSegment()
                    {
                        if (topSegment.Count > 1)
                        {
                            var polygon = new List<PointF>(topSegment);
                            for (int i = bottomSegment.Count - 1; i >= 0; i--)
                            {
                                polygon.Add(bottomSegment[i]);
                            }
                            e.Graphics.FillPolygon(fillBrush, polygon.ToArray());
                            e.Graphics.DrawLines(pen, topSegment.ToArray());
                            e.Graphics.DrawLines(pen, bottomSegment.ToArray());
                        }
                        else if (topSegment.Count == 1)
                        {
                            var point = topSegment[0];
                            const float pointSize = 4f;
                            using var pointBrush = new SolidBrush(pen.Color);
                            e.Graphics.FillEllipse(pointBrush, point.X - pointSize / 2, point.Y - pointSize / 2, pointSize, pointSize);
                        }
                        topSegment = new List<PointF>();
                        bottomSegment = new List<PointF>();
                    }

                    foreach (var (x, low, high) in range.Points)
                    {
                        if (double.IsNaN(x) || double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(x) || double.IsInfinity(low) || double.IsInfinity(high))
                        {
                            FlushRangeSegment();
                            continue;
                        }
                        var screenX = (float)ScreenX(transform, range, x);
                        topSegment.Add(new PointF(screenX, (float)transform.ToScreenY(high)));
                        bottomSegment.Add(new PointF(screenX, (float)transform.ToScreenY(low)));
                    }
                    FlushRangeSegment();
                }
                else if (series is ScatterSeries scatter)
                {
                    using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(scatter.Color)));
                    float size = (float)scatter.MarkerSize;
                    foreach (var (x, y) in scatter.Points)
                    {
                        if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                        var cx = (float)ScreenX(transform, scatter, x);
                        var cy = scatter.UseSecondaryYAxis ? (float)transform.ToScreenY2(y) : (float)transform.ToScreenY(y);
                        DrawMarker(e.Graphics, brush, null, scatter.MarkerShape, cx, cy, size);
                    }

                    if (scatter.ShowDataLabels)
                    {
                        DrawSeriesDataLabels(e.Graphics, transform, scatter.Points, scatter.DataLabelFormat, scatter.UseSecondaryYAxis, scatter.UseSecondaryXAxis, FontFamilyName, Model.DataLabelFontSize);
                    }
                }
                else if (series is BarSeries bar)
                {
                    using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(bar.Color)));
                    var zeroY = (float)transform.ToScreenY(0);
                    int count = bar.Items.Count;
                    if (count == 0) continue;
                    var xValues = bar.XValues.Count == count
                        ? bar.XValues
                        : Enumerable.Range(0, count).Select(static index => (double)index).ToList();
                    var xStep = xValues.Count > 1
                        ? xValues.Zip(xValues.Skip(1), static (left, right) => right - left).Where(static step => step > 0).DefaultIfEmpty(1).Min()
                        : 1;
                    var barWidth = Math.Abs((float)(ScreenX(transform, bar, xValues[0] + xStep * 0.6) - ScreenX(transform, bar, xValues[0])));
                    for (int i = 0; i < count; i++)
                    {
                        var value = bar.Items[i].Value;
                        if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                        var centerX = (float)ScreenX(transform, bar, xValues[i]);
                        var barY = (float)transform.ToScreenY(value);
                        var top = Math.Min(barY, zeroY);
                        var height = Math.Abs(barY - zeroY);
                        e.Graphics.FillRectangle(brush, centerX - barWidth / 2, top, barWidth, height);

                        if (bar.ShowDataLabels)
                        {
                            var text = PlotRenderHelper.FormatDataLabel(value, bar.DataLabelFormat);
                            using var dataLabelFont = new Font(FontFamilyName, Model.DataLabelFontSize);
                            var textSize = e.Graphics.MeasureString(text, dataLabelFont);
                            e.Graphics.DrawString(text, dataLabelFont, Brushes.Black, centerX - textSize.Width / 2, top - textSize.Height - 2);
                        }
                    }

                    using var labelFont = new Font(FontFamilyName, Model.AxisLabelFontSize);
                    var labelY = (float)plotArea.Bottom + 2;
                    if (bar.XValues.Count != count)
                    {
                        foreach (var (index, category) in PlotRenderHelper.GetVisibleBarCategoryLabels(bar, plotArea.Width))
                        {
                            var centerX = (float)ScreenX(transform, bar, xValues[index]);
                            var size = e.Graphics.MeasureString(category, labelFont);
                            e.Graphics.DrawString(category, labelFont, Brushes.Black, centerX - size.Width / 2, labelY);
                        }
                    }
                }
                else if (series is StackedBarSeries stackedBar)
                {
                    var zeroY = (float)transform.ToScreenY(0);
                    int count = stackedBar.Items.Count;
                    if (count == 0) continue;
                    float slotWidth = (float)plotArea.Width / count;
                    float barWidth = slotWidth * 0.6f;
                    for (int i = 0; i < count; i++)
                    {
                        var centerX = (float)plotArea.Left + slotWidth * (i + 0.5f);
                        var segments = PlotRenderHelper.CalculateStackedBarSegments(stackedBar, i);
                        foreach (var (valueIndex, start, end) in segments)
                        {
                            using var brush = new SolidBrush(GetPaletteColor(theme, valueIndex));
                            var startY = (float)transform.ToScreenY(start);
                            var endY = (float)transform.ToScreenY(end);
                            var top = Math.Min(startY, endY);
                            var height = Math.Abs(startY - endY);
                            e.Graphics.FillRectangle(brush, centerX - barWidth / 2, top, barWidth, height);
                        }
                    }

                    using var labelFont2 = new Font(FontFamilyName, Model.AxisLabelFontSize);
                    var labelY2 = (float)plotArea.Bottom + 2;
                    foreach (var (index, category) in PlotRenderHelper.GetVisibleStackedBarCategoryLabels(stackedBar, plotArea.Width))
                    {
                        var centerX = (float)plotArea.Left + slotWidth * (index + 0.5f);
                        var size = e.Graphics.MeasureString(category, labelFont2);
                        e.Graphics.DrawString(category, labelFont2, Brushes.Black, centerX - size.Width / 2, labelY2);
                    }
                }
                else if (series is HistogramSeries histogram)
                {
                    using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.DefaultSeriesColor)));
                    using var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(theme.BackgroundColor)), 1);
                    var zeroY = (float)transform.ToScreenY(0);
                    var bins = PlotRenderHelper.CalculateHistogramBins(histogram);
                    foreach (var bin in bins)
                    {
                        var left = (float)ScreenX(transform, histogram, bin.BinStart);
                        var right = (float)ScreenX(transform, histogram, bin.BinEnd);
                        var barY = (float)transform.ToScreenY(bin.Count);
                        var top = Math.Min(barY, zeroY);
                        var height = Math.Abs(barY - zeroY);
                        e.Graphics.FillRectangle(brush, Math.Min(left, right), top, Math.Abs(right - left), height);
                        e.Graphics.DrawRectangle(pen, Math.Min(left, right), top, Math.Abs(right - left), height);
                    }
                }
                else if (series is BoxPlotSeries boxPlot)
                {
                    using var brush = new SolidBrush(ColorTranslator.FromHtml("LightSkyBlue"));
                    using var pen = new Pen(Color.Black, 1);
                    using var medianPen = new Pen(Color.Firebrick, 2);
                    int count = boxPlot.Items.Count;
                    if (count == 0) continue;
                    float slotWidth = (float)plotArea.Width / count;
                    float boxWidth = slotWidth * 0.5f;
                    for (int i = 0; i < count; i++)
                    {
                        var item = boxPlot.Items[i];
                        if (double.IsNaN(item.Min) || double.IsNaN(item.Max) || double.IsInfinity(item.Min) || double.IsInfinity(item.Max)) continue;

                        var centerX = (float)plotArea.Left + slotWidth * (i + 0.5f);
                        var minY = (float)transform.ToScreenY(item.Min);
                        var maxY = (float)transform.ToScreenY(item.Max);
                        var q1Y = (float)transform.ToScreenY(item.Q1);
                        var q3Y = (float)transform.ToScreenY(item.Q3);
                        var medianY = (float)transform.ToScreenY(item.Median);

                        // ひげ(Min-Max)
                        e.Graphics.DrawLine(pen, centerX, minY, centerX, maxY);
                        e.Graphics.DrawLine(pen, centerX - boxWidth / 4, minY, centerX + boxWidth / 4, minY);
                        e.Graphics.DrawLine(pen, centerX - boxWidth / 4, maxY, centerX + boxWidth / 4, maxY);

                        // 箱(Q1-Q3)
                        var boxTop = Math.Min(q1Y, q3Y);
                        var boxHeight = Math.Abs(q1Y - q3Y);
                        e.Graphics.FillRectangle(brush, centerX - boxWidth / 2, boxTop, boxWidth, boxHeight);
                        e.Graphics.DrawRectangle(pen, centerX - boxWidth / 2, boxTop, boxWidth, boxHeight);

                        // 中央値線
                        e.Graphics.DrawLine(medianPen, centerX - boxWidth / 2, medianY, centerX + boxWidth / 2, medianY);
                    }

                    using var labelFont = new Font(FontFamilyName, Model.AxisLabelFontSize);
                    var labelY = (float)plotArea.Bottom + 2;
                    for (int i = 0; i < count; i++)
                    {
                        var category = boxPlot.Items[i].Category;
                        var centerX = (float)plotArea.Left + slotWidth * (i + 0.5f);
                        var size = e.Graphics.MeasureString(category, labelFont);
                        e.Graphics.DrawString(category, labelFont, Brushes.Black, centerX - size.Width / 2, labelY);
                    }
                }
            }

            DrawAnnotations(e.Graphics, transform, Model, FontFamilyName, Model.ValueLabelFontSize);
            DrawChartLabels(e.Graphics, plotArea, transform, Model, FontFamilyName, Model.ValueLabelFontSize);
            DrawLegend(e.Graphics, plotArea, Model);

            CustomRender?.Invoke(this, new CustomRenderEventArgs(e.Graphics, plotArea, transform, Model));
        }

        private static void DrawChartLabels(Graphics g, PlotRect plotArea, CoordinateTransform transform, PlotModel model, string fontFamilyName, float fontSize)
        {
            if (model.ChartLabels.Count == 0) return;

            using var font = new Font(fontFamilyName, fontSize);
            var occupied = new List<RectangleF>();
            foreach (var label in model.ChartLabels.Where(item => item.IsResolved && !string.IsNullOrEmpty(item.Text)))
            {
                var point = new PointF((float)transform.ToScreenX(label.X), (float)transform.ToScreenY(label.Y));
                var size = g.MeasureString(label.Text, font);
                var bounds = FindLabelBounds(point, size, label, plotArea, occupied);
                occupied.Add(bounds);

                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(label.Color)));
                if (label.ShowConnectionLine)
                {
                    using var linePen = new Pen(brush, 1);
                    g.DrawLine(linePen, point, new PointF(bounds.Left, bounds.Top + bounds.Height / 2));
                }

                g.DrawString(label.Text, font, brush, bounds.Location);
            }
        }

        private static RectangleF FindLabelBounds(PointF point, SizeF size, ChartLabel label, PlotRect plotArea, List<RectangleF> occupied)
        {
            var result = ChartLabelPlacement.Find(
                point.X,
                point.Y,
                size.Width,
                size.Height,
                label,
                new LabelPlacementRect(plotArea.Left, plotArea.Top, plotArea.Width, plotArea.Height),
                occupied.Select(item => new LabelPlacementRect(item.Left, item.Top, item.Width, item.Height)).ToArray());
            return new RectangleF((float)result.Left, (float)result.Top, (float)result.Width, (float)result.Height);
        }

        /// <summary>
        /// PlotModel.Annotations に登録されたテキスト注釈を描画する。
        /// </summary>
        private static void DrawAnnotations(Graphics g, CoordinateTransform transform, PlotModel model, string fontFamilyName, float fontSize)
        {
            if (model.Annotations.Count == 0) return;

            using var font = new Font(fontFamilyName, fontSize);
            foreach (var annotation in model.Annotations)
            {
                var x = (float)transform.ToScreenX(annotation.X);
                var y = (float)transform.ToScreenY(annotation.Y);
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(annotation.Color)));
                g.DrawString(annotation.Text, font, brush, x, y);
            }
        }

        /// <summary>
        /// テーマの SeriesPalette からインデックスに対応する色を取得する。範囲外は循環する。
        /// </summary>
        private static Color GetPaletteColor(PlotTheme theme, int index)
        {
            var palette = theme.SeriesPalette;
            if (palette == null || palette.Length == 0) return ColorTranslator.FromHtml(SafeColorName(theme.DefaultSeriesColor));
            return ColorTranslator.FromHtml(SafeColorName(palette[index % palette.Length]));
        }

        /// <summary>
        /// PieSeries を円グラフとして描画する。プロット領域の中央に円を配置し、右側に凡例(ラベルと割合)を表示する。
        /// </summary>
        private static void DrawPieChart(Graphics g, PlotRect plotArea, PieSeries pie, PlotTheme theme, string fontFamilyName, float fontSize)
        {
            var slices = PlotRenderHelper.CalculatePieSlices(pie);
            if (slices.Count == 0) return;

            float diameter = (float)Math.Min(plotArea.Width, plotArea.Height) * 0.8f;
            float centerX = (float)(plotArea.Left + plotArea.Width / 2);
            float centerY = (float)(plotArea.Top + plotArea.Height / 2);
            var rect = new RectangleF(centerX - diameter / 2, centerY - diameter / 2, diameter, diameter);

            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                using var brush = new SolidBrush(GetPaletteColor(theme, i));
                // GDI+ の角度は 0度=右(3時方向)基準のため、CalculatePieSlices の 0度=真上 からの補正として -90 する
                g.FillPie(brush, rect, (float)slice.StartAngle - 90f, (float)slice.SweepAngle);
                g.DrawPie(Pens.White, rect, (float)slice.StartAngle - 90f, (float)slice.SweepAngle);
            }

            using var labelFont = new Font(fontFamilyName, fontSize);
            using var legendTextBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.LegendTextColor)));
            float legendX = rect.Right + 16;
            float legendY = rect.Top;
            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                using var brush = new SolidBrush(GetPaletteColor(theme, i));
                g.FillRectangle(brush, legendX, legendY, 12, 12);
                var text = $"{slice.Label}: {slice.Percentage:P1}";
                g.DrawString(text, labelFont, legendTextBrush, legendX + 16, legendY - 2);
                legendY += 18;
            }
        }

        /// <summary>
        /// Y軸・X軸の目盛り線とラベルを描画する。
        /// </summary>
        private static void DrawAxisTicks(Graphics g, PlotRect plotArea, CoordinateTransform transform, bool hasBarSeries, bool showGridLines, PlotTheme theme, Axis xAxis, Axis? x2Axis, Axis yAxis, Axis? y2Axis, string fontFamilyName, float fontSize)
        {
            using var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(theme.GridLineColor)), (float)Math.Max(0.1, Math.Max(xAxis.GridMajorThickness, yAxis.GridMajorThickness)));
            var minorThickness = (float)Math.Max(0.1, Math.Max(xAxis.GridMinorThickness, yAxis.GridMinorThickness));
            using var minorPen = new Pen(Color.FromArgb(90, pen.Color), minorThickness);
            using var font = new Font(fontFamilyName, fontSize);
            using var labelBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.AxisLabelColor)));

            if (!transform.IsYLogarithmic && !yAxis.IsDateTime && showGridLines && yAxis.GridMinorVisible)
            {
                foreach (var tick in AxisTickHelper.GetMinorTicks(transform.YRange, yAxis))
                {
                    var y = (float)transform.ToScreenY(tick);
                    g.DrawLine(minorPen, (float)plotArea.Left, y, (float)plotArea.Right, y);
                }
            }

            var yTicks = transform.IsYLogarithmic
                ? AxisTickHelper.GetLogTicks(transform.YRange)
                : (yAxis.IsDateTime ? AxisTickHelper.GetDateTimeTicks(transform.YRange) : AxisTickHelper.GetTicks(transform.YRange, yAxis));
            foreach (var tick in yTicks)
            {
                var y = (float)transform.ToScreenY(tick);
                var yAxisOrigin = yAxis.Origin is double originX ? (float)transform.ToScreenYAxisOrigin(originX) : (float)plotArea.Left;
                var yTickStart = yAxis.TickDirection == AxisTickDirection.Inward || yAxis.TickDirection == AxisTickDirection.Cross ? yAxisOrigin : yAxisOrigin - 6;
                var yTickEnd = yAxis.TickDirection == AxisTickDirection.Outward ? yAxisOrigin - 6 : yAxisOrigin + 6;
                g.DrawLine(pen, yTickStart, y, yTickEnd, y);
                if (showGridLines && yAxis.GridMajorVisible)
                {
                    g.DrawLine(pen, (float)plotArea.Left, y, (float)plotArea.Right, y);
                }
                if (yAxis.TickLabelsVisible && yAxis.LabelMode == AxisLabelMode.Standard)
                {
                    var label = yAxis.IsDateTime ? DateTime.FromOADate(tick).ToString(yAxis.DateTimeFormat) : tick.ToString("0.###");
                    var size = g.MeasureString(label, font);
                    g.DrawString(label, font, labelBrush, yAxisOrigin - size.Width - 2, y - size.Height / 2);
                }
            }

            {
                if (!transform.IsXLogarithmic && !xAxis.IsDateTime && showGridLines && xAxis.GridMinorVisible)
                {
                    foreach (var tick in AxisTickHelper.GetMinorTicks(transform.XRange, xAxis))
                    {
                        var x = (float)transform.ToScreenX(tick);
                        g.DrawLine(minorPen, x, (float)plotArea.Top, x, (float)plotArea.Bottom);
                    }
                }

                var xTicks = transform.IsXLogarithmic
                    ? AxisTickHelper.GetLogTicks(transform.XRange)
                    : (xAxis.IsDateTime ? AxisTickHelper.GetDateTimeTicks(transform.XRange) : AxisTickHelper.GetTicks(transform.XRange, xAxis));
                foreach (var tick in xTicks)
                {
                    var x = (float)transform.ToScreenX(tick);
                    var xAxisOrigin = xAxis.Origin is double originY ? (float)transform.ToScreenXAxisOrigin(originY) : (float)plotArea.Bottom;
                    var xTickStart = xAxis.TickDirection == AxisTickDirection.Inward || xAxis.TickDirection == AxisTickDirection.Cross ? xAxisOrigin : xAxisOrigin + 6;
                    var xTickEnd = xAxis.TickDirection == AxisTickDirection.Outward ? xAxisOrigin + 6 : xAxisOrigin - 6;
                    g.DrawLine(pen, x, xTickStart, x, xTickEnd);
                    if (showGridLines && xAxis.GridMajorVisible)
                    {
                        g.DrawLine(pen, x, (float)plotArea.Top, x, (float)plotArea.Bottom);
                    }
                    if (xAxis.TickLabelsVisible && xAxis.LabelMode == AxisLabelMode.Standard)
                    {
                        var label = xAxis.IsDateTime ? DateTime.FromOADate(tick).ToString(xAxis.DateTimeFormat) : tick.ToString("0.###");
                        var size = g.MeasureString(label, font);
                        var labelLeft = x - size.Width / 2;
                        if (xAxis.IsDateTime)
                        {
                            var state = g.Save();
                            g.TranslateTransform(x, xAxisOrigin + 2);
                            g.RotateTransform(45);
                            g.DrawString(label, font, labelBrush, -size.Width / 2, 0);
                            g.Restore(state);
                        }
                        else
                        {
                            g.DrawString(label, font, labelBrush, labelLeft, xAxisOrigin + 2);
                        }
                    }
                }
            }

            if (transform.HasSecondaryYAxis)
            {
                var y2Ticks = transform.IsY2Logarithmic
                    ? AxisTickHelper.GetLogTicks(transform.Y2Range)
                    : AxisTickHelper.GetTicks(transform.Y2Range, y2Axis ?? new Axis(), 10);
                foreach (var tick in y2Ticks)
                {
                    var y = (float)transform.ToScreenY2(tick);
                    if (y2Axis?.TickLabelsVisible == true && y2Axis.LabelMode == AxisLabelMode.Standard)
                    {
                        var label = tick.ToString("0.###");
                        var size = g.MeasureString(label, font);
                        g.DrawString(label, font, labelBrush, (float)plotArea.Right + 2, y - size.Height / 2);
                    }
                }
            }

            if (x2Axis is not null && transform.HasSecondaryXAxis)
            {
                var x2Ticks = transform.IsX2Logarithmic
                    ? AxisTickHelper.GetLogTicks(transform.X2Range)
                    : AxisTickHelper.GetTicks(transform.X2Range, x2Axis, 10);
                foreach (var tick in x2Ticks)
                {
                    var x = (float)transform.ToScreenX2(tick);
                    g.DrawLine(pen, x, (float)plotArea.Top - 6, x, (float)plotArea.Top);
                    if (showGridLines && x2Axis.GridMajorVisible)
                    {
                        g.DrawLine(pen, x, (float)plotArea.Top, x, (float)plotArea.Bottom);
                    }
                    if (x2Axis.TickLabelsVisible && x2Axis.LabelMode == AxisLabelMode.Standard)
                    {
                        var label = tick.ToString("0.###");
                        var size = g.MeasureString(label, font);
                        g.DrawString(label, font, labelBrush, x - size.Width / 2, (float)plotArea.Top - size.Height - 8);
                    }
                }
            }

            DrawAxisGridLines(g, plotArea, transform, xAxis, x2Axis, yAxis, y2Axis);
            DrawAxisLines(g, plotArea, transform, xAxis, x2Axis, yAxis, y2Axis);
            DrawAxisMarkers(g, plotArea, transform, xAxis, yAxis, y2Axis, font);

        }

        private static void DrawAxisLines(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis? x2Axis, Axis yAxis, Axis? y2Axis)
        {
            using var xPen = new Pen(Color.Black, (float)Math.Max(0.1, xAxis.AxisLineThickness));
            using var yPen = new Pen(Color.Black, (float)Math.Max(0.1, yAxis.AxisLineThickness));
            g.DrawLine(yPen, (float)plotArea.Left, (float)plotArea.Top, (float)plotArea.Left, (float)plotArea.Bottom);
            g.DrawLine(xPen, (float)plotArea.Left, (float)plotArea.Bottom, (float)plotArea.Right, (float)plotArea.Bottom);

            if (transform.HasSecondaryXAxis && x2Axis is not null)
            {
                using var x2Pen = new Pen(Color.Black, (float)Math.Max(0.1, x2Axis.AxisLineThickness));
                g.DrawLine(x2Pen, (float)plotArea.Left, (float)plotArea.Top, (float)plotArea.Right, (float)plotArea.Top);
            }

            if (transform.HasSecondaryYAxis && y2Axis is not null)
            {
                using var y2Pen = new Pen(Color.Black, (float)Math.Max(0.1, y2Axis.AxisLineThickness));
                g.DrawLine(y2Pen, (float)plotArea.Right, (float)plotArea.Top, (float)plotArea.Right, (float)plotArea.Bottom);
            }
        }

        private static void DrawAlarmZones(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis? x2Axis, Axis yAxis, Axis? y2Axis, float fontSize)
        {
            DrawZones(g, plotArea, transform, xAxis.AlarmZones, true, false, transform.XRange.Min, transform.XRange.Max, fontSize);
            if (x2Axis is not null && transform.HasSecondaryXAxis)
            {
                DrawZones(g, plotArea, transform, x2Axis.AlarmZones, true, false, transform.X2Range.Min, transform.X2Range.Max, fontSize);
            }
            DrawZones(g, plotArea, transform, yAxis.AlarmZones, false, false, transform.YRange.Min, transform.YRange.Max, fontSize);
            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                DrawZones(g, plotArea, transform, y2Axis.AlarmZones, false, true, transform.Y2Range.Min, transform.Y2Range.Max, fontSize);
            }
        }

        private static void DrawAxisValueLabels(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis? x2Axis, Axis yAxis, Axis? y2Axis, string fontFamilyName, float fontSize)
        {
            using var font = new Font(fontFamilyName, fontSize);
            if (xAxis.TickLabelsVisible && xAxis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawXValueLabels(g, plotArea, transform, xAxis, font);
            }
            if (x2Axis is not null && transform.HasSecondaryXAxis && x2Axis.TickLabelsVisible && x2Axis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawX2ValueLabels(g, plotArea, transform, x2Axis, font);
            }
            if (yAxis.TickLabelsVisible && yAxis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawYValueLabels(g, plotArea, transform, yAxis, font, false);
            }
            if (y2Axis is not null && transform.HasSecondaryYAxis && y2Axis.TickLabelsVisible && y2Axis.LabelMode == AxisLabelMode.ValueLabels)
            {
                DrawYValueLabels(g, plotArea, transform, y2Axis, font, true);
            }
        }

        private static void DrawXValueLabels(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis axis, Font font)
        {
            foreach (var label in axis.ValueLabels.Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text)))
            {
                var x = (float)transform.ToScreenX(label.Value);
                if (x < plotArea.Left || x > plotArea.Right) continue;
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(label.Color)));
                var size = g.MeasureString(label.Text, font);
                g.DrawString(label.Text, font, brush, x - size.Width / 2, (float)plotArea.Bottom + 2);
            }
        }

        private static void DrawYValueLabels(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis axis, Font font, bool secondary)
        {
            foreach (var label in axis.ValueLabels.Where(item => item.IsVisible && !string.IsNullOrEmpty(item.Text)))
            {
                var y = (float)(secondary ? transform.ToScreenY2(label.Value) : transform.ToScreenY(label.Value));
                if (y < plotArea.Top || y > plotArea.Bottom) continue;
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(label.Color)));
                var size = g.MeasureString(label.Text, font);
                var x = secondary ? (float)plotArea.Right + 2 : (float)plotArea.Left - size.Width - 2;
                g.DrawString(label.Text, font, brush, x, y - size.Height / 2);
            }
        }

        private static void DrawZones(Graphics g, PlotRect plotArea, CoordinateTransform transform, IEnumerable<AxisAlarmZone> zones, bool isX, bool isY2, double axisMinimum, double axisMaximum, float fontSize)
        {
            foreach (var zone in zones.Where(item => item.IsVisible))
            {
                var fillColor = ColorTranslator.FromHtml(SafeColorName(zone.FillColor));
                using var fill = new SolidBrush(Color.FromArgb(zone.Opacity, fillColor));
                using var border = new Pen(ColorTranslator.FromHtml(SafeColorName(zone.BorderColor)), zone.BorderThickness);
                ApplyDashStyle(border, zone.BorderDashStyle);
                foreach (var (dataFrom, dataTo) in AxisAlarmZoneResolver.Resolve(zone, axisMinimum, axisMaximum))
                {
                    var from = isX ? transform.ToScreenX(dataFrom) : (isY2 ? transform.ToScreenY2(dataFrom) : transform.ToScreenY(dataFrom));
                    var to = isX ? transform.ToScreenX(dataTo) : (isY2 ? transform.ToScreenY2(dataTo) : transform.ToScreenY(dataTo));
                    var rect = isX
                        ? new RectangleF((float)Math.Min(from, to), (float)plotArea.Top, (float)Math.Abs(to - from), (float)plotArea.Height)
                        : new RectangleF((float)plotArea.Left, (float)Math.Min(from, to), (float)plotArea.Width, (float)Math.Abs(to - from));
                    g.FillRectangle(fill, rect);
                    if (!string.Equals(zone.BorderColor, "Transparent", StringComparison.OrdinalIgnoreCase)) g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                    if (!string.IsNullOrEmpty(zone.Text))
                    {
                        using var font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize);
                        g.DrawString(zone.Text, font, border.Brush, rect.Location);
                    }
                }
            }
        }

        private static void ApplyDashStyle(Pen pen, string dashStyle)
        {
            if (string.Equals(dashStyle, "Dash", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            else if (string.Equals(dashStyle, "Dot", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        }

        private static void DrawAxisGridLines(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis? x2Axis, Axis yAxis, Axis? y2Axis)
        {
            foreach (var line in yAxis.GridLines.Where(item => item.IsVisible))
            {
                using var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                var y = (float)transform.ToScreenY(line.Value);
                g.DrawLine(pen, (float)plotArea.Left, y, (float)plotArea.Right, y);
            }

            if (x2Axis is not null && transform.HasSecondaryXAxis)
            {
                foreach (var line in x2Axis.GridLines.Where(item => item.IsVisible))
                {
                    using var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                    var x = (float)transform.ToScreenX2(line.Value);
                    g.DrawLine(pen, x, (float)plotArea.Top, x, (float)plotArea.Bottom);
                }
            }

            foreach (var line in xAxis.GridLines.Where(item => item.IsVisible))
            {
                using var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                var x = (float)transform.ToScreenX(line.Value);
                g.DrawLine(pen, x, (float)plotArea.Top, x, (float)plotArea.Bottom);
            }

            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                foreach (var line in y2Axis.GridLines.Where(item => item.IsVisible))
                {
                    using var pen = CreatePen(line.Color, line.Thickness, line.DashStyle);
                    var y = (float)transform.ToScreenY2(line.Value);
                    g.DrawLine(pen, (float)plotArea.Left, y, (float)plotArea.Right, y);
                }
            }
        }

        private static void DrawAxisMarkers(Graphics g, PlotRect plotArea, CoordinateTransform transform, Axis xAxis, Axis yAxis, Axis? y2Axis, Font font)
        {
            foreach (var marker in yAxis.Markers)
            {
                var y = (float)transform.ToScreenY(marker.Value);
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(marker.Color)));
                using var pen = new Pen(brush, 1);
                var x = (float)plotArea.Left;
                var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, false);
                var tip = new PointF(x + (float)(direction.X * 12), y + (float)(direction.Y * 12));
                if (marker.ShowConnectionLine) g.DrawLine(pen, x, y, tip.X, tip.Y);
                if (marker.ShowArrow) DrawArrow(g, pen, tip, new PointF(x, y));
                DrawMarkerText(g, marker.Text, font, brush, tip, direction.X, direction.Y);
            }

            foreach (var marker in xAxis.Markers)
            {
                var x = (float)transform.ToScreenX(marker.Value);
                using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(marker.Color)));
                using var pen = new Pen(brush, 1);
                var y = (float)plotArea.Bottom;
                var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, true);
                var tip = new PointF(x + (float)(direction.X * 12), y + (float)(direction.Y * 12));
                if (marker.ShowConnectionLine) g.DrawLine(pen, x, y, tip.X, tip.Y);
                if (marker.ShowArrow) DrawArrow(g, pen, tip, new PointF(x, y));
                DrawMarkerText(g, marker.Text, font, brush, tip, direction.X, direction.Y);
            }

            if (y2Axis is not null && transform.HasSecondaryYAxis)
            {
                foreach (var marker in y2Axis.Markers)
                {
                    var y = (float)transform.ToScreenY2(marker.Value);
                    using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(marker.Color)));
                    using var pen = new Pen(brush, 1);
                    var x = (float)plotArea.Right;
                    var direction = AxisMarkerDirection.Resolve(marker.ArrowDirection, false);
                    var tip = new PointF(x + (float)(direction.X * 12), y + (float)(direction.Y * 12));
                    if (marker.ShowConnectionLine) g.DrawLine(pen, x, y, tip.X, tip.Y);
                    if (marker.ShowArrow) DrawArrow(g, pen, tip, new PointF(x, y));
                    DrawMarkerText(g, marker.Text, font, brush, tip, direction.X, direction.Y);
                }
            }
        }

        private static void DrawMarkerText(Graphics g, string text, Font font, Brush brush, PointF tip, double directionX, double directionY)
        {
            if (string.IsNullOrEmpty(text)) return;
            var size = g.MeasureString(text, font);
            var x = tip.X + (float)(directionX * 2) - (directionX == 0 ? size.Width / 2 : directionX < 0 ? size.Width : 0);
            var y = tip.Y + (float)(directionY * 2) - (directionY == 0 ? size.Height / 2 : directionY < 0 ? size.Height : 0);
            g.DrawString(text, font, brush, x, y);
        }

        private static Pen CreatePen(string color, float thickness, string dashStyle)
        {
            var pen = new Pen(ColorTranslator.FromHtml(SafeColorName(color)), thickness);
            if (string.Equals(dashStyle, "Dash", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            else if (string.Equals(dashStyle, "Dot", StringComparison.OrdinalIgnoreCase)) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            return pen;
        }

        private static void DrawArrow(Graphics g, Pen pen, PointF tip, PointF tail)
        {
            var dx = tip.X - tail.X;
            var dy = tip.Y - tail.Y;
            var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            var ux = dx / length;
            var uy = dy / length;
            var left = new PointF((float)(tip.X - ux * 6 - uy * 3), (float)(tip.Y - uy * 6 + ux * 3));
            var right = new PointF((float)(tip.X - ux * 6 + uy * 3), (float)(tip.Y - uy * 6 - ux * 3));
            g.DrawLine(pen, tip, left);
            g.DrawLine(pen, tip, right);
        }

        private void DrawScrollButtons(Graphics g, PlotRect plotArea, Axis xAxis, Axis yAxis, float fontSize)
        {
            scrollXPreviousButton = RectangleF.Empty;
            scrollXNextButton = RectangleF.Empty;
            scrollYPreviousButton = RectangleF.Empty;
            scrollYNextButton = RectangleF.Empty;

            using var brush = new SolidBrush(Color.FromArgb(220, SystemColors.Control));
            using var pen = new Pen(SystemColors.ControlDark);
            using var font = new Font(FontFamilyName, fontSize);

            if (xAxis.ShowScrollButtons)
            {
                scrollXPreviousButton = new RectangleF((float)plotArea.Left, (float)plotArea.Bottom + 18, 18, 18);
                scrollXNextButton = new RectangleF((float)plotArea.Right - 18, (float)plotArea.Bottom + 18, 18, 18);
                DrawButton(g, scrollXPreviousButton, "◀", brush, pen, font);
                DrawButton(g, scrollXNextButton, "▶", brush, pen, font);
            }

            if (yAxis.ShowScrollButtons)
            {
                scrollYPreviousButton = new RectangleF((float)plotArea.Right + 28, (float)plotArea.Top, 18, 18);
                scrollYNextButton = new RectangleF((float)plotArea.Right + 28, (float)plotArea.Bottom - 18, 18, 18);
                DrawButton(g, scrollYPreviousButton, "▲", brush, pen, font);
                DrawButton(g, scrollYNextButton, "▼", brush, pen, font);
            }
        }

        private static void DrawButton(Graphics g, RectangleF bounds, string text, Brush brush, Pen pen, Font font)
        {
            g.FillRectangle(brush, bounds);
            g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            TextRenderer.DrawText(g, text, font, Rectangle.Round(bounds), SystemColors.ControlText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>
        /// X軸・Y軸のタイトル文字列(C1C の <c>Axis.Text</c> 相当)を描画する。
        /// X軸タイトルは数値目盛りラベルの下に中央揃えで、Y軸タイトルは数値目盛りラベルの左側に
        /// 90度回転させた縦書きで描画する。
        /// </summary>
        private static void DrawAxisTitles(Graphics g, PlotRect plotArea, string xAxisTitle, string? x2AxisTitle, string yAxisTitle, PlotTheme theme, string fontFamilyName, float fontSize)
        {
            using var font = new Font(fontFamilyName, fontSize, FontStyle.Bold);
            using var brush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.AxisLabelColor)));

            if (!string.IsNullOrEmpty(xAxisTitle))
            {
                var size = g.MeasureString(xAxisTitle, font);
                var x = (float)(plotArea.Left + plotArea.Width / 2 - size.Width / 2);
                var y = (float)plotArea.Bottom + 16;
                g.DrawString(xAxisTitle, font, brush, x, y);
            }

            if (!string.IsNullOrEmpty(x2AxisTitle))
            {
                var size = g.MeasureString(x2AxisTitle, font);
                var x = (float)(plotArea.Left + plotArea.Width / 2 - size.Width / 2);
                g.DrawString(x2AxisTitle, font, brush, x, (float)plotArea.Top - size.Height - 16);
            }

            if (!string.IsNullOrEmpty(yAxisTitle))
            {
                var size = g.MeasureString(yAxisTitle, font);
                var state = g.Save();
                g.TranslateTransform(4, (float)(plotArea.Top + plotArea.Height / 2 + size.Width / 2));
                g.RotateTransform(-90);
                g.DrawString(yAxisTitle, font, brush, 0, 0);
                g.Restore(state);
            }
        }

        private static void DrawTitle(Graphics g, string title, string compass, Font font, Brush brush)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            var bounds = g.VisibleClipBounds;
            var size = g.MeasureString(title, font);
            var position = compass.ToLowerInvariant() switch
            {
                "east" => new PointF(bounds.Right - size.Width - 4, bounds.Top + 4),
                "north" => new PointF(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Top + 4),
                "south" => new PointF(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Bottom - size.Height - 4),
                _ => new PointF(bounds.Left + 4, bounds.Top + 4)
            };
            g.DrawString(title, font, brush, position);
        }

        private static void DrawFooter(Graphics g, string footer, string compass, Font font, Brush brush)
        {
            if (string.IsNullOrEmpty(footer))
            {
                return;
            }

            var bounds = g.VisibleClipBounds;
            var size = g.MeasureString(footer, font);
            var position = compass.ToLowerInvariant() switch
            {
                "east" => new PointF(bounds.Right - size.Width - 4, bounds.Bottom - size.Height - 4),
                "west" => new PointF(bounds.Left + 4, bounds.Bottom - size.Height - 4),
                "north" => new PointF(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Bottom - size.Height - 4),
                _ => new PointF(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Bottom - size.Height - 4)
            };
            g.DrawString(footer, font, brush, position);
        }

        /// <summary>
        /// 各系列の名前と色を凡例としてプロット領域右上に描画する。
        /// 非表示系列はグレーアウトして表示し、クリックでの表示切替に使用する矩形を記録する。
        /// </summary>
        private void DrawLegend(Graphics g, PlotRect plotArea, PlotModel model)
        {
            legendItemRects.Clear();
            if (!model.ShowLegend) return;
            var allSeries = model.GetAllSeries().ToList();
            if (allSeries.Count == 0) return;

            using var font = new Font(FontFamilyName, model.LegendFontSize);
            const float swatchSize = 10;
            const float padding = 4;
            const float lineHeight = 16;

            float maxTextWidth = allSeries.Max(s => g.MeasureString(s.Name, font).Width);
            float legendWidth = swatchSize + padding * 3 + maxTextWidth;
            float legendHeight = padding * 2 + lineHeight * allSeries.Count;

            float legendX = model.LegendPosition switch
            {
                LegendPosition.TopLeft or LegendPosition.BottomLeft => (float)plotArea.Left + padding,
                _ => (float)plotArea.Right - legendWidth - padding
            };
            float legendY = model.LegendPosition switch
            {
                LegendPosition.BottomLeft or LegendPosition.BottomRight => (float)plotArea.Bottom - legendHeight - padding,
                _ => (float)plotArea.Top + padding
            };

            var theme = model.Theme;
            using var backBrush = new SolidBrush(Color.FromArgb(230, ColorTranslator.FromHtml(SafeColorName(theme.BackgroundColor))));
            using var borderPen = new Pen(ColorTranslator.FromHtml(SafeColorName(theme.GridLineColor)));
            g.FillRectangle(backBrush, legendX, legendY, legendWidth, legendHeight);
            g.DrawRectangle(borderPen, legendX, legendY, legendWidth, legendHeight);

            for (int i = 0; i < allSeries.Count; i++)
            {
                var series = allSeries[i];
                var itemY = legendY + padding + i * lineHeight;
                var colorName = GetSeriesColorName(series, theme);
                using var textBrush = new SolidBrush(series.Visible ? ColorTranslator.FromHtml(SafeColorName(theme.LegendTextColor)) : Color.Gray);
                using var brush = new SolidBrush(series.Visible ? ColorTranslator.FromHtml(SafeColorName(colorName)) : Color.LightGray);
                var swatchCenterX = legendX + padding + swatchSize / 2;
                var swatchCenterY = itemY + lineHeight / 2;
                if (series is LineSeries line)
                {
                    using var outlinePen = new Pen(series.Visible
                        ? ColorTranslator.FromHtml(SafeColorName(line.MarkerOutlineColor))
                        : Color.Gray, 1f);
                    DrawMarker(g, brush, outlinePen, line.MarkerShape, swatchCenterX, swatchCenterY, swatchSize);
                }
                else if (series is RadarSeries radar)
                {
                    using var outlinePen = new Pen(series.Visible
                        ? ColorTranslator.FromHtml(SafeColorName(radar.MarkerOutlineColor))
                        : Color.Gray, 1f);
                    DrawMarker(g, brush, outlinePen, radar.MarkerShape, swatchCenterX, swatchCenterY, swatchSize);
                }
                else
                {
                    g.FillRectangle(brush, legendX + padding, itemY + (lineHeight - swatchSize) / 2, swatchSize, swatchSize);
                }
                g.DrawString(series.Name, font, textBrush, legendX + padding * 2 + swatchSize, itemY);

                legendItemRects.Add((series, new RectangleF(legendX, itemY, legendWidth, lineHeight)));
            }
        }

        private static string GetSeriesColorName(Series series, PlotTheme theme) => series switch
        {
            LineSeries line => line.Color,
            RadarSeries radar => radar.Color,
            _ => theme.DefaultSeriesColor
        };

        /// <summary>
        /// 現在の描画内容を PNG として保存します。
        /// </summary>
        public void SaveAsPng(string filePath)
        {
            var width = Math.Max(1, ClientSize.Width);
            var height = Math.Max(1, ClientSize.Height);
            using var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var rect = new Rectangle(0, 0, width, height);
                var message = new PaintEventArgs(graphics, rect);
                InvokePaint(this, message);
            }
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private static string SafeColorName(string name) => string.IsNullOrWhiteSpace(name) ? "Black" : name;

        private static void DrawMarker(Graphics graphics, Brush brush, Pen? outlinePen, MarkerShape shape, float cx, float cy, float size)
        {
            float half = size / 2f;
            switch (shape)
            {
                case MarkerShape.Dot:
                    var dotSize = Math.Max(1f, size / 2f);
                    graphics.FillEllipse(brush, cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize);
                    break;
                case MarkerShape.Square:
                    graphics.FillRectangle(brush, cx - half, cy - half, size, size);
                    if (outlinePen is not null) graphics.DrawRectangle(outlinePen, cx - half, cy - half, size, size);
                    break;
                case MarkerShape.Box:
                    if (outlinePen is not null)
                    {
                        graphics.DrawRectangle(outlinePen, cx - half, cy - half, size, size);
                    }
                    break;
                case MarkerShape.Diamond:
                    graphics.FillPolygon(brush, new[]
                    {
                        new PointF(cx, cy - half),
                        new PointF(cx + half, cy),
                        new PointF(cx, cy + half),
                        new PointF(cx - half, cy)
                    });
                    break;
                case MarkerShape.InvertedTri:
                    graphics.FillPolygon(brush, new[]
                    {
                        new PointF(cx - half, cy - half),
                        new PointF(cx + half, cy - half),
                        new PointF(cx, cy + half)
                    });
                    break;
                case MarkerShape.Triangle:
                    graphics.FillPolygon(brush, new[]
                    {
                        new PointF(cx, cy - half),
                        new PointF(cx + half, cy + half),
                        new PointF(cx - half, cy + half)
                    });
                    break;
                case MarkerShape.Cross:
                    using (var pen = new Pen(brush, Math.Max(1f, size / 4f)))
                    {
                        graphics.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                        graphics.DrawLine(pen, cx - half, cy + half, cx + half, cy - half);
                    }
                    break;
                case MarkerShape.DiagCross:
                    using (var pen = new Pen(brush, Math.Max(1f, size / 4f)))
                    {
                        graphics.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                        graphics.DrawLine(pen, cx - half, cy + half, cx + half, cy - half);
                    }
                    break;
                case MarkerShape.Plus:
                    using (var pen = new Pen(brush, Math.Max(1f, size / 4f)))
                    {
                        graphics.DrawLine(pen, cx - half, cy, cx + half, cy);
                        graphics.DrawLine(pen, cx, cy - half, cx, cy + half);
                    }
                    break;
                case MarkerShape.Star:
                    {
                        var points = new PointF[10];
                        for (int i = 0; i < 10; i++)
                        {
                            double angle = Math.PI / 2 * 3 + i * Math.PI / 5;
                            float radius = (i % 2 == 0) ? half : half / 2.5f;
                            points[i] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
                        }
                        graphics.FillPolygon(brush, points);
                    }
                    break;
                case MarkerShape.Circle:
                default:
                    graphics.FillEllipse(brush, cx - half, cy - half, size, size);
                    if (outlinePen is not null) graphics.DrawEllipse(outlinePen, cx - half, cy - half, size, size);
                    break;
            }
        }

        private static System.Drawing.Drawing2D.DashStyle ToGdiDashStyle(LineDashStyle dashStyle) => dashStyle switch
        {
            LineDashStyle.Dash => System.Drawing.Drawing2D.DashStyle.Dash,
            LineDashStyle.Dot => System.Drawing.Drawing2D.DashStyle.Dot,
            LineDashStyle.DashDot => System.Drawing.Drawing2D.DashStyle.DashDot,
            _ => System.Drawing.Drawing2D.DashStyle.Solid
        };

        private static double ScreenX(CoordinateTransform transform, Series series, double x)
            => series.UseSecondaryXAxis && transform.HasSecondaryXAxis
                ? transform.ToScreenX2(x)
                : transform.ToScreenX(x);

        private static void DrawSeriesDataLabels(Graphics graphics, CoordinateTransform transform, List<(double X, double Y)> points, string? format, bool useSecondaryYAxis = false, bool useSecondaryXAxis = false, string fontFamilyName = "Meiryo UI", float fontSize = 8f)
        {
            using var labelFont = new Font(fontFamilyName, fontSize);
            foreach (var (x, y) in points)
            {
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                var text = PlotRenderHelper.FormatDataLabel(y, format);
                var screenX = (float)(useSecondaryXAxis && transform.HasSecondaryXAxis ? transform.ToScreenX2(x) : transform.ToScreenX(x));
                var screenY = useSecondaryYAxis ? (float)transform.ToScreenY2(y) : (float)transform.ToScreenY(y);
                var size = graphics.MeasureString(text, labelFont);
                graphics.DrawString(text, labelFont, Brushes.Black, screenX - size.Width / 2, screenY - size.Height - 2);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Model == null) return;

            var plotArea = GetPlotArea();
            var transform = GetTransform(plotArea);

            double factor = e.Delta > 0 ? 0.9 : 1.0 / 0.9;
            double dataX = transform.ToDataX(e.X);
            double dataY = transform.ToDataY(e.Y);
            double dataY2 = transform.HasSecondaryYAxis ? transform.ToDataY2(e.Y) : 0;

            var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
            var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
            var currentY2 = transform.HasSecondaryYAxis ? zoomedY2Range ?? GetAutoY2Range(plotArea) : (AxisRange?)null;

            var newX = ZoomRange(currentX, dataX, factor);
            var newY = ZoomRange(currentY, dataY, factor);
            var newY2 = currentY2.HasValue ? ZoomRange(currentY2.Value, dataY2, factor) : (AxisRange?)null;
            StartZoomAnimation(newX, newY, newY2);
        }

        /// <summary>
        /// 指定したズーム範囲まで軸範囲を滑らかにアニメーションさせる。
        /// </summary>
        private void StartZoomAnimation(AxisRange targetX, AxisRange targetY, AxisRange? targetY2 = null)
        {
            var plotArea = GetPlotArea();
            animationStartXRange = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
            animationStartYRange = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
            animationTargetXRange = targetX;
            animationTargetYRange = targetY;
            animationStartY2Range = zoomedY2Range ?? (targetY2.HasValue ? GetAutoY2Range(plotArea) : null);
            animationTargetY2Range = targetY2;
            animationStartTime = DateTime.UtcNow;
            animationTimer.Start();
        }

        private AxisRange GetAutoY2Range(PlotRect plotArea)
        {
            var transform = PlotRenderHelper.CreateTransform(Model!, plotArea);
            return transform.Y2Range;
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
            if (animationStartY2Range.HasValue && animationTargetY2Range.HasValue)
            {
                zoomedY2Range = LerpRange(animationStartY2Range.Value, animationTargetY2Range.Value, eased);
            }
            Invalidate();

            if (t >= 1.0)
            {
                animationTimer.Stop();
                animationTargetXRange = null;
                animationTargetYRange = null;
                animationStartXRange = null;
                animationStartYRange = null;
                animationTargetY2Range = null;
                animationStartY2Range = null;
            }
        }

        private static AxisRange LerpRange(AxisRange from, AxisRange to, double t)
        {
            var min = from.Min + (to.Min - from.Min) * t;
            var max = from.Max + (to.Max - from.Max) * t;
            return new AxisRange(min, max);
        }

        private static AxisRange ZoomRange(AxisRange range, double pivot, double factor)
        {
            var newMin = pivot - (pivot - range.Min) * factor;
            var newMax = pivot + (range.Max - pivot) * factor;
            return new AxisRange(newMin, newMax);
        }

        private AxisRange GetAutoRange(PlotRect plotArea, bool isX)
        {
            var transform = PlotRenderHelper.CreateTransform(Model!, plotArea);
            // ToData(Left/Right) から現在の自動範囲を逆算する
            return isX
                ? new AxisRange(transform.ToDataX(plotArea.Left), transform.ToDataX(plotArea.Right))
                : new AxisRange(transform.ToDataY(plotArea.Bottom), transform.ToDataY(plotArea.Top));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                if (scrollXPreviousButton.Contains(e.Location))
                {
                    ScrollAxis(isX: true, direction: -1);
                    return;
                }

                if (scrollXNextButton.Contains(e.Location))
                {
                    ScrollAxis(isX: true, direction: 1);
                    return;
                }

                if (scrollYPreviousButton.Contains(e.Location))
                {
                    ScrollAxis(isX: false, direction: 1);
                    return;
                }

                if (scrollYNextButton.Contains(e.Location))
                {
                    ScrollAxis(isX: false, direction: -1);
                    return;
                }

                foreach (var (series, rect) in legendItemRects)
                {
                    if (rect.Contains(e.Location))
                    {
                        series.Visible = !series.Visible;
                        Invalidate();
                        return;
                    }
                }

                isPanning = true;
                panLastScreenPoint = e.Location;
            }
        }

        private void ScrollAxis(bool isX, double direction)
        {
            if (Model is null)
            {
                return;
            }

            UnifiedChart.ChartLogger.Write(
                isX ? "PlotView.ScrollX" : "PlotView.ScrollY",
                Model,
                $"Direction={direction}");

            var plotArea = GetPlotArea();
            var range = isX
                ? zoomedXRange ?? GetAutoRange(plotArea, isX: true)
                : zoomedYRange ?? GetAutoRange(plotArea, isX: false);
            var bounds = PlotRenderHelper.GetAutoRange(Model, plotArea, isX);
            var axis = isX ? Model.XAxis : Model.YAxis;
            if (!axis.Scroll.IsEnabled) return;
            var minimum = axis.Scroll.Minimum ?? bounds.Min;
            var maximum = axis.Scroll.Maximum ?? bounds.Max;
            var scrolled = AxisScrollHelper.Scroll(range, direction, axis.Scroll.Unit, minimum, maximum);

            if (isX)
            {
                zoomedXRange = scrolled;
            }
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

            Invalidate();
            AxisScroll?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isPanning && Model != null)
            {
                var plotArea = GetPlotArea();
                var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
                var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
                var transform = new CoordinateTransform(plotArea, currentX, currentY);

                var dxData = transform.ToDataX(e.X) - transform.ToDataX(panLastScreenPoint.X);
                var dyData = transform.ToDataY(e.Y) - transform.ToDataY(panLastScreenPoint.Y);

                var xBounds = PlotRenderHelper.GetAutoRange(Model, plotArea, true);
                var yBounds = PlotRenderHelper.GetAutoRange(Model, plotArea, false);
                zoomedXRange = currentX.TranslateClamped(-dxData, xBounds);
                zoomedYRange = currentY.TranslateClamped(-dyData, yBounds);
                panLastScreenPoint = e.Location;
                UnifiedChart.ChartLogger.Write("PlotView.Pan", Model, $"X={zoomedXRange}, Y={zoomedYRange}");
                Invalidate();
                return;
            }

            ShowNearestPointTooltip(e.Location);
        }

        private void ShowNearestPointTooltip(Point screenPoint)
        {
            if (Model == null) return;

            var plotArea = GetPlotArea();
            var transform = GetTransform(plotArea);

            const double thresholdPixels = 10.0;
            var nearest = PlotRenderHelper.FindNearestPointInfo(Model, transform, screenPoint.X, screenPoint.Y, thresholdPixels);

            string? text = nearest.HasValue
                ? PlotRenderHelper.FormatTooltip(nearest.Value.Series, nearest.Value.X, nearest.Value.Y)
                : null;

            if (text != lastTooltipText)
            {
                lastTooltipText = text;
                toolTip.SetToolTip(this, text ?? string.Empty);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPanning = false;
        }

        // --- WM_GESTURE によるピンチズーム/パン(タッチ操作)対応 ---

        private const int WM_GESTURE = 0x0119;
        private const int GID_ZOOM = 3;
        private const int GID_PAN = 4;
        private const int GC_ZOOM = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct GESTUREINFO
        {
            public int cbSize;
            public int dwFlags;
            public int dwID;
            public IntPtr hwndTarget;
            public POINTS ptsLocation;
            public int dwInstanceID;
            public int dwSequenceID;
            public long ullArguments;
            public int cbExtraArgs;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTS
        {
            public short x;
            public short y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

        [DllImport("user32.dll")]
        private static extern bool CloseGestureInfoHandle(IntPtr hGestureInfo);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GESTURE && Model != null)
            {
                if (HandleGestureMessage(m))
                {
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private bool HandleGestureMessage(Message m)
        {
            var gi = new GESTUREINFO();
            gi.cbSize = Marshal.SizeOf(typeof(GESTUREINFO));
            if (!GetGestureInfo(m.LParam, ref gi))
            {
                return false;
            }

            try
            {
                if (gi.dwID == GID_ZOOM)
                {
                    var location = PointToClient(new Point(gi.ptsLocation.x, gi.ptsLocation.y));
                    long distance = gi.ullArguments & 0xFFFFFF; // 下位24bitが距離

                    if ((gi.dwFlags & GC_ZOOM) != 0 && lastGestureZoomDistance != 0 && distance != 0)
                    {
                        var plotArea = GetPlotArea();
                        var transform = GetTransform(plotArea);
                        double factor = (double)lastGestureZoomDistance / distance;
                        double dataX = transform.ToDataX(location.X);
                        double dataY = transform.ToDataY(location.Y);

                        var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
                        var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);

                        zoomedXRange = ZoomRange(currentX, dataX, factor);
                        zoomedYRange = ZoomRange(currentY, dataY, factor);
                        Invalidate();
                    }

                    lastGestureZoomDistance = distance;
                    return true;
                }
                else if (gi.dwID == GID_PAN)
                {
                    var location = PointToClient(new Point(gi.ptsLocation.x, gi.ptsLocation.y));
                    if (isPanning)
                    {
                        var plotArea = GetPlotArea();
                        var currentX = zoomedXRange ?? GetAutoRange(plotArea, isX: true);
                        var currentY = zoomedYRange ?? GetAutoRange(plotArea, isX: false);
                        var transform = new CoordinateTransform(plotArea, currentX, currentY);

                        var dxData = transform.ToDataX(location.X) - transform.ToDataX(panLastScreenPoint.X);
                        var dyData = transform.ToDataY(location.Y) - transform.ToDataY(panLastScreenPoint.Y);

                        zoomedXRange = new AxisRange(currentX.Min - dxData, currentX.Max - dxData);
                        zoomedYRange = new AxisRange(currentY.Min - dyData, currentY.Max - dyData);
                        Invalidate();
                    }

                    panLastScreenPoint = location;
                    isPanning = true;
                    return true;
                }
            }
            finally
            {
                CloseGestureInfoHandle(m.LParam);
            }

            return false;
        }

        /// <summary>
        /// �現在のグラフを印刷ダイアログ経由で印刷します。
        /// </summary>
        public void Print()
        {
            using var printDialog = new PrintDialog();
            using var document = new System.Drawing.Printing.PrintDocument();
            document.PrintPage += OnPrintPage;
            printDialog.Document = document;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                document.Print();
            }
        }

        private void OnPrintPage(object? sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            if (e.Graphics == null) return;
            var bounds = e.MarginBounds;
            var contentWidth = Math.Max(1, ClientSize.Width);
            var contentHeight = Math.Max(1, ClientSize.Height);
            using var bitmap = new Bitmap(contentWidth, contentHeight);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var rect = new Rectangle(0, 0, contentWidth, contentHeight);
                var message = new PaintEventArgs(graphics, rect);
                InvokePaint(this, message);
            }

            // アスペクト比を維持したまま用紙の余白領域に収まるサイズを計算し、中央に配置する
            double scale = Math.Min((double)bounds.Width / contentWidth, (double)bounds.Height / contentHeight);
            int scaledWidth = (int)(contentWidth * scale);
            int scaledHeight = (int)(contentHeight * scale);
            int offsetX = bounds.Left + (bounds.Width - scaledWidth) / 2;
            int offsetY = bounds.Top + (bounds.Height - scaledHeight) / 2;

            e.Graphics.DrawImage(bitmap, new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight));
            e.HasMorePages = false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            ResetZoom();
        }

        private static void DrawRadarChart(Graphics graphics, PlotRect plotArea, IReadOnlyList<RadarSeries> seriesList, PlotTheme theme, string fontFamilyName, bool showLegend, int radarGridLevels, IReadOnlyList<string>? radarGridLabels, RadarDirection radarDirection, float labelFontSize, float legendFontSize)
        {
            var center = new PointF((float)(plotArea.X + plotArea.Width / 2), (float)(plotArea.Y + plotArea.Height / 2));
            var radius = (float)(Math.Min(plotArea.Width, plotArea.Height) * 0.38);
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

            using var gridPen = new Pen(Color.FromArgb(150, Color.Gray), 1f);
            using var axisPen = new Pen(Color.FromArgb(170, Color.Gray), 1f);
            using var labelFont = new Font(fontFamilyName, labelFontSize);
            using var labelBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.AxisLabelColor)));

            var gridLevels = Math.Max(1, radarGridLevels);
            var hasCustomLabels = radarGridLabels is { Count: var labelCount } && labelCount == gridLevels;
            for (var level = 1; level <= gridLevels; level++)
            {
                var levelRadius = radius * level / gridLevels;
                var levelValue = minimum + (maximum - minimum) * level / gridLevels;
                graphics.DrawEllipse(
                    gridPen,
                    center.X - levelRadius,
                    center.Y - levelRadius,
                    levelRadius * 2,
                    levelRadius * 2);

                var valueLabel = hasCustomLabels
                    ? radarGridLabels![level - 1]
                    : levelValue.ToString("0.##");
                var valueSize = graphics.MeasureString(valueLabel, labelFont);
                var labelAngle = -Math.PI * 3 / 4;
                var labelCenterX = center.X + (float)(Math.Cos(labelAngle) * levelRadius);
                var labelCenterY = center.Y + (float)(Math.Sin(labelAngle) * levelRadius);
                graphics.DrawString(
                    valueLabel,
                    labelFont,
                    labelBrush,
                    labelCenterX - valueSize.Width / 2,
                    labelCenterY - valueSize.Height / 2);
            }

            var direction = radarDirection == RadarDirection.CounterClockwise ? -1 : 1;
            var outerPoints = CreateRadarPoints(center, radius, pointCount, direction);
            var categoryLabels = seriesList[0].CategoryLabels;
            var hasCategoryLabels = categoryLabels.Count == pointCount;
            for (var index = 0; index < pointCount; index++)
            {
                graphics.DrawLine(axisPen, center, outerPoints[index]);
                var label = hasCategoryLabels
                    ? categoryLabels[index]
                    : index < seriesList[0].Points.Count
                    ? seriesList[0].Points[index].X.ToString("0.##")
                    : index.ToString();
                var labelSize = graphics.MeasureString(label, labelFont);
                var labelVectorX = outerPoints[index].X - center.X;
                var labelVectorY = outerPoints[index].Y - center.Y;
                var labelVectorLength = Math.Max(1f, MathF.Sqrt(labelVectorX * labelVectorX + labelVectorY * labelVectorY));
                var labelOffset = 8f;
                var labelPoint = new PointF(
                    outerPoints[index].X + labelVectorX / labelVectorLength * labelOffset - labelSize.Width / 2,
                    outerPoints[index].Y + labelVectorY / labelVectorLength * labelOffset - labelSize.Height / 2);
                graphics.DrawString(label, labelFont, labelBrush, labelPoint);
            }

            foreach (var series in seriesList)
            {
                if (!series.Visible)
                {
                    continue;
                }

                var points = new List<(int Index, PointF Point)>(series.Points.Count);
                foreach (var (point, index) in series.Points.Select((point, index) => (point, index)))
                {
                    if (MissingValueContract.IsMissing(point.X, series.MissingValueHole)
                        || MissingValueContract.IsMissing(point.Y, series.MissingValueHole))
                    {
                        continue;
                    }

                    var normalized = Math.Clamp((point.Y - minimum) / (maximum - minimum), 0, 1);
                    var angle = -Math.PI / 2 + direction * index * Math.PI * 2 / pointCount;
                    points.Add((index, new PointF(
                        center.X + (float)(Math.Cos(angle) * radius * normalized),
                        center.Y + (float)(Math.Sin(angle) * radius * normalized))));
                }

                if (points.Count == 0)
                {
                    continue;
                }

                var color = ColorTranslator.FromHtml(SafeColorName(series.Color));
                var fillColor = ColorTranslator.FromHtml(SafeColorName(series.FillColor));
                using var fillBrush = new SolidBrush(Color.FromArgb(55, fillColor));
                using var pen = new Pen(color, (float)series.StrokeThickness);
                var drawablePoints = points.Select(item => item.Point).ToArray();
                if (series.FillArea && points.Count >= 3 && points.Count == series.Points.Count)
                {
                    graphics.FillPolygon(fillBrush, drawablePoints);
                    graphics.DrawPolygon(pen, drawablePoints);
                }
                else if (points.Count >= 2)
                {
                    for (var index = 1; index < points.Count; index++)
                    {
                        if (points[index].Index == points[index - 1].Index + 1)
                        {
                            graphics.DrawLine(pen, points[index - 1].Point, points[index].Point);
                        }
                    }

                    if (points.Count == series.Points.Count)
                    {
                        graphics.DrawLine(pen, drawablePoints[^1], drawablePoints[0]);
                    }
                }

                if (!series.ShowMarkers)
                {
                    continue;
                }

                using var markerBrush = new SolidBrush(color);
                using var markerPen = new Pen(ColorTranslator.FromHtml(SafeColorName(series.MarkerOutlineColor)), 1f);
                foreach (var (_, point) in points)
                {
                    DrawMarker(graphics, markerBrush, markerPen, series.MarkerShape, point.X, point.Y, (float)Math.Max(1.0, series.MarkerSize * 1.5));
                }
            }

            if (showLegend)
            {
                using var legendFont = new Font(fontFamilyName, legendFontSize);
                using var legendBrush = new SolidBrush(ColorTranslator.FromHtml(SafeColorName(theme.LegendTextColor)));
                var legendX = (float)(plotArea.Right - 110);
                var legendY = (float)(plotArea.Top + 8);
                foreach (var series in seriesList)
                {
                    var color = ColorTranslator.FromHtml(SafeColorName(series.Color));
                    using var swatchBrush = new SolidBrush(color);
                    var outlineColor = string.IsNullOrWhiteSpace(series.MarkerOutlineColor)
                        ? color
                        : ColorTranslator.FromHtml(SafeColorName(series.MarkerOutlineColor));
                    using var swatchPen = new Pen(outlineColor, 1f);
                    DrawMarker(graphics, swatchBrush, swatchPen, series.MarkerShape, legendX + 6, legendY + 9, Math.Max(1f, (float)series.MarkerSize * 1.5f));
                    graphics.DrawString(series.Name, legendFont, legendBrush, legendX + 18, legendY);
                    legendY += 18;
                }
            }
        }

        private static void DrawRadarOutline(Graphics graphics, Pen pen, PointF[] points)
        {
            if (points.Length >= 3)
            {
                graphics.DrawPolygon(pen, points);
            }
            else if (points.Length == 2)
            {
                graphics.DrawLine(pen, points[0], points[1]);
            }
            else if (points.Length == 1)
            {
                graphics.DrawEllipse(pen, points[0].X - 1, points[0].Y - 1, 2, 2);
            }
        }

        private static PointF[] CreateRadarPoints(PointF center, float radius, int pointCount, int direction)
        {
            var points = new PointF[pointCount];
            for (var index = 0; index < pointCount; index++)
            {
                var angle = -Math.PI / 2 + direction * index * Math.PI * 2 / pointCount;
                points[index] = new PointF(
                    center.X + (float)(Math.Cos(angle) * radius),
                    center.Y + (float)(Math.Sin(angle) * radius));
            }

            return points;
        }
    }
}
