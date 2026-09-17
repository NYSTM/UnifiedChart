namespace UnifiedChart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// PlotModel から座標変換を構築するための共通ヘルパー。UI 非依存。
    /// </summary>
    public static class PlotRenderHelper
    {
        /// <summary>
        /// データラベル表示用に数値を書式化する。formatがnullまたは空の場合は"0.##"を使用する。
        /// </summary>
        public static string FormatDataLabel(double value, string? format)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return string.Empty;
            var effectiveFormat = string.IsNullOrEmpty(format) ? "0.##" : format;
            return value.ToString(effectiveFormat, System.Globalization.CultureInfo.CurrentCulture);
        }

        public static string FormatTooltip(Series series, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(series.TooltipText))
            {
                return $"{series.Name}: ({x:0.###}, {y:0.###})";
            }

            try
            {
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    series.TooltipText,
                    x,
                    y,
                    series.Name);
            }
            catch (FormatException)
            {
                return $"{series.Name}: ({x:0.###}, {y:0.###})";
            }
        }

        /// <summary>
        /// 実データ空間の範囲(Min/Maxが実値)をLog10空間の範囲に変換する。0以下の値は安全な最小値に丸める。
        /// </summary>
        private static AxisRange ToLogSpaceRange(AxisRange dataRange)
        {
            const double minPositive = 1e-9;
            var min = dataRange.Min > 0 ? dataRange.Min : minPositive;
            var max = dataRange.Max > 0 ? dataRange.Max : minPositive;
            if (max <= min) max = min * 10;
            return new AxisRange(Math.Log10(min), Math.Log10(max));
        }

        public static CoordinateTransform CreateTransform(PlotModel model, PlotRect plotArea)
            => CreateTransform(model, plotArea, null, null);

        /// <summary>
        /// 指定軸のデータ全体を含む自動表示範囲を返す。固定Min/Maxも適用される。
        /// </summary>
        public static AxisRange GetAutoRange(PlotModel model, PlotRect plotArea, bool isX)
        {
            var transform = CreateTransform(model, plotArea);
            return isX
                ? new AxisRange(transform.ToDataX(plotArea.Left), transform.ToDataX(plotArea.Right))
                : new AxisRange(transform.ToDataY(plotArea.Bottom), transform.ToDataY(plotArea.Top));
        }

        /// <summary>
        /// ズーム状態など、明示的な軸範囲を指定して座標変換を構築する。
        /// zoomedXRange / zoomedYRange が null の場合はデータから自動計算する。
        /// </summary>
        public static CoordinateTransform CreateTransform(PlotModel model, PlotRect plotArea, AxisRange? zoomedXRange, AxisRange? zoomedYRange, AxisRange? zoomedY2Range = null)
        {
            if (zoomedXRange.HasValue && zoomedYRange.HasValue)
            {
                bool isXLog = model.XAxis.IsLogarithmic;
                bool isYLog = model.YAxis.IsLogarithmic;
                var xRangeForZoom = isXLog ? ToLogSpaceRange(zoomedXRange.Value) : zoomedXRange.Value;
                var yRangeForZoom = isYLog ? ToLogSpaceRange(zoomedYRange.Value) : zoomedYRange.Value;
                var (y2RangeForZoom, isY2LogForZoom) = CalculateY2Range(model, zoomedY2Range);
                var x2RangeForZoom = model.X2Axis is null
                    ? (AxisRange?)null
                    : CalculateXRange(model, model.X2Axis, useSecondary: true, model.X2Axis.IsLogarithmic);
                return new CoordinateTransform(plotArea, xRangeForZoom, yRangeForZoom, isXLog, isYLog, x2RangeForZoom, model.X2Axis?.IsLogarithmic == true, y2RangeForZoom, isY2LogForZoom);
            }

            double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity;
            double yMin = double.PositiveInfinity, yMax = double.NegativeInfinity;
            bool hasData = false;

            foreach (var series in model.GetAllSeries())
            {
                if (!series.Visible) continue;
                if (series is LineSeries line)
                {
                    bool useY2 = series.UseSecondaryYAxis;
                    foreach (var (x, y) in line.Points)
                    {
                        if (MissingValueContract.IsMissing(x, series.MissingValueHole) || MissingValueContract.IsMissing(y, series.MissingValueHole)) continue;
                        hasData = true;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (useY2) continue;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }
                else if (series is AreaSeries area)
                {
                    bool useY2 = series.UseSecondaryYAxis;
                    foreach (var (x, y) in area.Points)
                    {
                        if (MissingValueContract.IsMissing(x, series.MissingValueHole) || MissingValueContract.IsMissing(y, series.MissingValueHole)) continue;
                        hasData = true;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (useY2) continue;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                    if (!useY2 && hasData && !double.IsNaN(area.BaselineValue) && !double.IsInfinity(area.BaselineValue))
                    {
                        if (area.BaselineValue < yMin) yMin = area.BaselineValue;
                        if (area.BaselineValue > yMax) yMax = area.BaselineValue;
                    }
                }
                else if (series is ScatterSeries scatter)
                {
                    bool useY2 = series.UseSecondaryYAxis;
                    foreach (var (x, y) in scatter.Points)
                    {
                        if (MissingValueContract.IsMissing(x, series.MissingValueHole) || MissingValueContract.IsMissing(y, series.MissingValueHole)) continue;
                        hasData = true;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (useY2) continue;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }
                else if (series is RangeSeries range)
                {
                    foreach (var (x, low, high) in range.Points)
                    {
                        if (double.IsNaN(x) || double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(x) || double.IsInfinity(low) || double.IsInfinity(high)) continue;
                        hasData = true;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (low < yMin) yMin = low;
                        if (high > yMax) yMax = high;
                    }
                }
                else if (series is BarSeries bar)
                {
                    for (int i = 0; i < bar.Items.Count; i++)
                    {
                        var value = bar.Items[i].Value;
                        if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                        hasData = true;
                        double x = i < bar.XValues.Count ? bar.XValues[i] : i;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        // 棒グラフは 0 を基準に描画するため、0 も範囲に含める
                        if (0 < yMin) yMin = 0;
                        if (0 > yMax) yMax = 0;
                        if (value < yMin) yMin = value;
                        if (value > yMax) yMax = value;
                    }
                }
                else if (series is StackedBarSeries stackedBar)
                {
                    for (int i = 0; i < stackedBar.Items.Count; i++)
                    {
                        double positiveSum = 0, negativeSum = 0;
                        foreach (var value in stackedBar.Items[i].Values)
                        {
                            if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                            if (value >= 0) positiveSum += value;
                            else negativeSum += value;
                        }
                        hasData = true;
                        double x = i;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        // 積み上げ棒グラフは 0 を基準に描画するため、0 も範囲に含める
                        if (0 < yMin) yMin = 0;
                        if (0 > yMax) yMax = 0;
                        if (negativeSum < yMin) yMin = negativeSum;
                        if (positiveSum > yMax) yMax = positiveSum;
                    }
                }
                else if (series is HistogramSeries histogram)
                {
                    var bins = CalculateHistogramBins(histogram);
                    foreach (var bin in bins)
                    {
                        hasData = true;
                        if (bin.BinStart < xMin) xMin = bin.BinStart;
                        if (bin.BinEnd > xMax) xMax = bin.BinEnd;
                        // ヒストグラムは 0 を基準に描画するため、0 も範囲に含める
                        if (0 < yMin) yMin = 0;
                        if (0 > yMax) yMax = 0;
                        if (bin.Count < yMin) yMin = bin.Count;
                        if (bin.Count > yMax) yMax = bin.Count;
                    }
                }
                else if (series is BoxPlotSeries boxPlot)
                {
                    for (int i = 0; i < boxPlot.Items.Count; i++)
                    {
                        var item = boxPlot.Items[i];
                        if (double.IsNaN(item.Min) || double.IsNaN(item.Max) || double.IsInfinity(item.Min) || double.IsInfinity(item.Max)) continue;
                        hasData = true;
                        double x = i;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (item.Min < yMin) yMin = item.Min;
                        if (item.Max > yMax) yMax = item.Max;
                    }
                }
            }

            bool isXLogarithmic = model.XAxis.IsLogarithmic;
            bool isX2Logarithmic = model.X2Axis?.IsLogarithmic == true;
            bool isYLogarithmic = model.YAxis.IsLogarithmic;

            var xRangeForTransform = CalculateXRange(model, model.XAxis, useSecondary: false, isXLogarithmic);
            var x2RangeForTransform = model.X2Axis is null
                ? (AxisRange?)null
                : CalculateXRange(model, model.X2Axis, useSecondary: true, isX2Logarithmic);

            if (!isYLogarithmic)
            {
                var linearYRange = AxisRange.Calculate(model.YAxis.Minimum, model.YAxis.Maximum, hasData ? yMin : 0, hasData ? yMax : 1, hasData);
                var (y2Range, isY2Log) = CalculateY2Range(model);
                return new CoordinateTransform(plotArea, zoomedXRange ?? xRangeForTransform, zoomedYRange ?? linearYRange, isXLogarithmic, false, x2RangeForTransform, isX2Logarithmic, y2Range, isY2Log);
            }

            // 対数軸: 0以下の値は無視し、正の値のみを Log10 空間で範囲計算する。
            double logYMin = double.PositiveInfinity, logYMax = double.NegativeInfinity;
            bool hasPositiveY = false;
            foreach (var series in model.GetAllSeries())
            {
                if (!series.Visible || series.UseSecondaryYAxis) continue;
                foreach (var y in EnumerateYValues(series))
                {
                    if (double.IsNaN(y) || double.IsInfinity(y) || y <= 0) continue;
                    var logY = Math.Log10(y);
                    hasPositiveY = true;
                    if (logY < logYMin) logYMin = logY;
                    if (logY > logYMax) logYMax = logY;
                }
            }

            double? explicitLogMin = model.YAxis.Minimum.HasValue && model.YAxis.Minimum.Value > 0 ? Math.Log10(model.YAxis.Minimum.Value) : (double?)null;
            double? explicitLogMax = model.YAxis.Maximum.HasValue && model.YAxis.Maximum.Value > 0 ? Math.Log10(model.YAxis.Maximum.Value) : (double?)null;
            var yRange = AxisRange.Calculate(explicitLogMin, explicitLogMax, hasPositiveY ? logYMin : 0, hasPositiveY ? logYMax : 1, hasPositiveY);
            var (y2RangeForLog, isY2LogForLog) = CalculateY2Range(model);
            return new CoordinateTransform(plotArea, zoomedXRange ?? xRangeForTransform, zoomedYRange ?? yRange, isXLogarithmic, true, x2RangeForTransform, isX2Logarithmic, y2RangeForLog, isY2LogForLog);
        }

        private static AxisRange CalculateXRange(PlotModel model, Axis axis, bool useSecondary, bool logarithmic)
        {
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            bool hasData = false;
            foreach (var series in model.GetAllSeries())
            {
                if (!series.Visible || series.UseSecondaryXAxis != useSecondary) continue;
                foreach (var x in EnumerateXValues(series))
                {
                    if (double.IsNaN(x) || double.IsInfinity(x) || (logarithmic && x <= 0)) continue;
                    var value = logarithmic ? Math.Log10(x) : x;
                    hasData = true;
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }

            if (!logarithmic)
            {
                return AxisRange.Calculate(axis.Minimum, axis.Maximum, hasData ? min : 0, hasData ? max : 1, hasData);
            }

            double? explicitMin = axis.Minimum is > 0 ? Math.Log10(axis.Minimum.Value) : null;
            double? explicitMax = axis.Maximum is > 0 ? Math.Log10(axis.Maximum.Value) : null;
            return AxisRange.Calculate(explicitMin, explicitMax, hasData ? min : 0, hasData ? max : 1, hasData);
        }

        /// <summary>
        /// PlotModel.Y2Axis が設定されている場合、UseSecondaryYAxis=true の系列のY値から第2軸の範囲を計算する。
        /// Y2Axis が未設定の場合は null を返す。
        /// </summary>
        private static (AxisRange? Range, bool IsLogarithmic) CalculateY2Range(PlotModel model, AxisRange? zoomedY2Range = null)
        {
            if (model.Y2Axis == null) return (null, false);

            var y2Axis = model.Y2Axis;
            double y2Min = double.PositiveInfinity, y2Max = double.NegativeInfinity;
            bool hasY2Data = false;

            if (!y2Axis.IsLogarithmic)
            {
                foreach (var series in model.GetAllSeries())
                {
                    if (!series.Visible || !series.UseSecondaryYAxis) continue;
                    foreach (var y in EnumerateYValues(series))
                    {
                        if (double.IsNaN(y) || double.IsInfinity(y)) continue;
                        hasY2Data = true;
                        if (y < y2Min) y2Min = y;
                        if (y > y2Max) y2Max = y;
                    }
                }
                var range = zoomedY2Range ?? CalculateY2LinearRange(y2Axis, y2Min, y2Max, hasY2Data);
                return (range, false);
            }

            else
            {
                foreach (var series in model.GetAllSeries())
                {
                    if (!series.Visible || !series.UseSecondaryYAxis) continue;
                    foreach (var y in EnumerateYValues(series))
                    {
                        if (double.IsNaN(y) || double.IsInfinity(y) || y <= 0) continue;
                        var logY = Math.Log10(y);
                        hasY2Data = true;
                        if (logY < y2Min) y2Min = logY;
                        if (logY > y2Max) y2Max = logY;
                    }
                }

                double? explicitLogMin = y2Axis.Minimum.HasValue && y2Axis.Minimum.Value > 0 ? Math.Log10(y2Axis.Minimum.Value) : (double?)null;
                double? explicitLogMax = y2Axis.Maximum.HasValue && y2Axis.Maximum.Value > 0 ? Math.Log10(y2Axis.Maximum.Value) : (double?)null;
                var range = zoomedY2Range ?? AxisRange.Calculate(explicitLogMin, explicitLogMax, hasY2Data ? y2Min : 0, hasY2Data ? y2Max : 1, hasY2Data);
                return (range, true);
            }
        }

        private static AxisRange CalculateY2LinearRange(Axis axis, double dataMin, double dataMax, bool hasData)
        {
            var range = AxisRange.Calculate(axis.Minimum, axis.Maximum, hasData ? dataMin : 0, hasData ? dataMax : 1, hasData);
            if (!hasData || axis.Minimum.HasValue || axis.Maximum.HasValue)
            {
                return range;
            }

            if (dataMin == dataMax)
            {
                return range;
            }

            return new AxisRange(Math.Floor(dataMin), Math.Ceiling(dataMax));
        }

        /// <summary>
        /// 系列からX値(対数軸範囲計算に必要な値)を列挙する。UI非依存の内部ヘルパー。
        /// </summary>
        private static IEnumerable<double> EnumerateXValues(Series series)
        {
            switch (series)
            {
                case LineSeries line:
                    foreach (var (x, _) in line.Points) yield return x;
                    break;
                case AreaSeries area:
                    foreach (var (x, _) in area.Points) yield return x;
                    break;
                case ScatterSeries scatter:
                    foreach (var (x, _) in scatter.Points) yield return x;
                    break;
                case RangeSeries range:
                    foreach (var (x, _, _) in range.Points) yield return x;
                    break;
                case HistogramSeries histogram:
                    foreach (var bin in CalculateHistogramBins(histogram))
                    {
                        yield return bin.BinStart;
                        yield return bin.BinEnd;
                    }
                    break;
            }
        }

        /// <summary>
        /// 系列からY値(または対数軸範囲計算に必要な値)を列挙する。UI非依存の内部ヘルパー。
        /// </summary>
        private static IEnumerable<double> EnumerateYValues(Series series)
        {
            switch (series)
            {
                case LineSeries line:
                    foreach (var (_, y) in line.Points) yield return y;
                    break;
                case AreaSeries area:
                    foreach (var (_, y) in area.Points) yield return y;
                    if (!double.IsNaN(area.BaselineValue) && !double.IsInfinity(area.BaselineValue)) yield return area.BaselineValue;
                    break;
                case ScatterSeries scatter:
                    foreach (var (_, y) in scatter.Points) yield return y;
                    break;
                case RangeSeries range:
                    foreach (var (_, low, high) in range.Points)
                    {
                        yield return low;
                        yield return high;
                    }
                    break;
                case BarSeries bar:
                    foreach (var item in bar.Items) yield return item.Value;
                    break;
                case StackedBarSeries stackedBar:
                    foreach (var item in stackedBar.Items)
                    {
                        double positiveSum = 0, negativeSum = 0;
                        foreach (var value in item.Values)
                        {
                            if (double.IsNaN(value) || double.IsInfinity(value)) continue;
                            if (value >= 0) positiveSum += value;
                            else negativeSum += value;
                        }
                        yield return positiveSum;
                        yield return negativeSum;
                    }
                    break;
                case HistogramSeries histogram:
                    foreach (var bin in CalculateHistogramBins(histogram)) yield return bin.Count;
                    break;
                case BoxPlotSeries boxPlot:
                    foreach (var item in boxPlot.Items)
                    {
                        yield return item.Min;
                        yield return item.Max;
                    }
                    break;
            }
        }

        /// <summary>
        /// ヒストグラムの区間(ビン)を計算する。Values を Min/Max の範囲で BinCount 個の等幅区間に分割し、
        /// 区間ごとの度数を返す。NaN/Infinity は無視する。
        /// </summary>
        /// <param name="histogram">対象の HistogramSeries。</param>
        public static IReadOnlyList<(double BinStart, double BinEnd, int Count)> CalculateHistogramBins(HistogramSeries histogram)
        {
            var result = new List<(double BinStart, double BinEnd, int Count)>();
            var values = histogram.Values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();
            int binCount = Math.Max(1, histogram.BinCount);

            if (values.Count == 0)
            {
                return result;
            }

            double min = values.Min();
            double max = values.Max();

            if (min == max)
            {
                // 全て同値の場合は単一区間にまとめる
                result.Add((min, max, values.Count));
                return result;
            }

            double binWidth = (max - min) / binCount;
            var counts = new int[binCount];

            foreach (var value in values)
            {
                int index = (int)((value - min) / binWidth);
                if (index >= binCount) index = binCount - 1;
                if (index < 0) index = 0;
                counts[index]++;
            }

            for (int i = 0; i < binCount; i++)
            {
                double binStart = min + binWidth * i;
                double binEnd = binStart + binWidth;
                result.Add((binStart, binEnd, counts[i]));
            }

            return result;
        }

        /// <summary>
        /// BarSeries のカテゴリラベルのうち、重ならずに表示できるものを間引いて返す。
        /// </summary>
        /// <param name="bar">対象の BarSeries。</param>
        /// <param name="plotAreaWidth">プロット領域の幅（画面座標）。</param>
        /// <param name="approximateLabelWidth">ラベル1件あたりのおおよその表示幅（画面座標）。</param>
        public static IEnumerable<(int Index, string Category)> GetVisibleBarCategoryLabels(BarSeries bar, double plotAreaWidth, double approximateLabelWidth = 40)
        {
            int count = bar.Items.Count;
            if (count == 0) yield break;

            double slotWidth = plotAreaWidth / count;
            int step = slotWidth <= 0 ? count : Math.Max(1, (int)Math.Ceiling(approximateLabelWidth / slotWidth));

            for (int i = 0; i < count; i += step)
            {
                yield return (i, bar.Items[i].Category);
            }
        }

        /// <summary>
        /// StackedBarSeries のカテゴリラベルのうち、重ならずに表示できるものを間引いて返す。
        /// </summary>
        /// <param name="stackedBar">対象の StackedBarSeries。</param>
        /// <param name="plotAreaWidth">プロット領域の幅（画面座標）。</param>
        /// <param name="approximateLabelWidth">ラベル1件あたりのおおよその表示幅（画面座標）。</param>
        public static IEnumerable<(int Index, string Category)> GetVisibleStackedBarCategoryLabels(StackedBarSeries stackedBar, double plotAreaWidth, double approximateLabelWidth = 40)
        {
            int count = stackedBar.Items.Count;
            if (count == 0) yield break;

            double slotWidth = plotAreaWidth / count;
            int step = slotWidth <= 0 ? count : Math.Max(1, (int)Math.Ceiling(approximateLabelWidth / slotWidth));

            for (int i = 0; i < count; i += step)
            {
                yield return (i, stackedBar.Items[i].Category);
            }
        }

        /// <summary>
        /// StackedBarSeries の指定カテゴリについて、正負それぞれの積み上げセグメント（開始値・終了値・元のインデックス）を計算する。
        /// NaN/Infinity の値は無視する。
        /// </summary>
        public static IReadOnlyList<(int ValueIndex, double Start, double End)> CalculateStackedBarSegments(StackedBarSeries stackedBar, int itemIndex)
        {
            var result = new List<(int ValueIndex, double Start, double End)>();
            if (itemIndex < 0 || itemIndex >= stackedBar.Items.Count) return result;

            double positiveCursor = 0, negativeCursor = 0;
            var values = stackedBar.Items[itemIndex].Values;
            for (int i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (double.IsNaN(value) || double.IsInfinity(value)) continue;

                if (value >= 0)
                {
                    double start = positiveCursor;
                    double end = positiveCursor + value;
                    result.Add((i, start, end));
                    positiveCursor = end;
                }
                else
                {
                    double end = negativeCursor;
                    double start = negativeCursor + value;
                    result.Add((i, start, end));
                    negativeCursor = start;
                }
            }

            return result;
        }

        /// <summary>
        /// マウス直下（画面座標）に最も近いデータ点を探索する。
        /// LineSeries/ScatterSeries を対象とし、点群が X 昇順であれば二分探索で候補範囲を絞り込み、
        /// そうでない場合は線形探索にフォールバックする。
        /// </summary>
        public static (string SeriesName, double X, double Y)? FindNearestPoint(PlotModel model, CoordinateTransform transform, double screenX, double screenY, double thresholdPixels)
        {
            var nearest = FindNearestPointInfo(model, transform, screenX, screenY, thresholdPixels);
            return nearest.HasValue
                ? (nearest.Value.Series.Name, nearest.Value.X, nearest.Value.Y)
                : null;
        }

        public static (Series Series, int DataIndex, double X, double Y)? FindNearestPointInfo(PlotModel model, CoordinateTransform transform, double screenX, double screenY, double thresholdPixels)
            => FindNearestPointInfo(model, transform, screenX, screenY, thresholdPixels, model.GetAllSeries());

        public static (Series Series, int DataIndex, double X, double Y)? FindNearestPointInfo(PlotModel model, CoordinateTransform transform, double screenX, double screenY, double thresholdPixels, IEnumerable<Series> seriesSource)
        {
            double bestDistSq = double.PositiveInfinity;
            (double X, double Y)? bestPoint = null;
            Series? bestSeries = null;
            var bestDataIndex = -1;

            foreach (var series in seriesSource)
            {
                if (!series.Visible) continue;
                IReadOnlyList<(double X, double Y)>? points = series switch
                {
                    LineSeries line => line.Points,
                    ScatterSeries scatter => scatter.Points,
                    _ => null
                };
                if (points == null || points.Count == 0) continue;

                double dataX = transform.ToDataX(screenX);
                var candidateIndices = IsSortedAscendingByX(points)
                    ? GetCandidateIndicesNearX(points, dataX)
                    : Enumerable.Range(0, points.Count);

                foreach (var i in candidateIndices)
                {
                    var (x, y) = points[i];
                    if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                    var sx = transform.ToScreenX(x);
                    var sy = transform.ToScreenY(y);
                    var dx = sx - screenX;
                    var dy = sy - screenY;
                    var distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestPoint = (x, y);
                        bestSeries = series;
                        bestDataIndex = i;
                    }
                }
            }

            if (bestPoint.HasValue && bestDistSq <= thresholdPixels * thresholdPixels)
            {
                return (bestSeries!, bestDataIndex, bestPoint.Value.X, bestPoint.Value.Y);
            }
            return null;
        }

        private static bool IsSortedAscendingByX(IReadOnlyList<(double X, double Y)> points)
        {
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X < points[i - 1].X) return false;
            }
            return true;
        }

        /// <summary>
        /// 二分探索で dataX に近い位置を求め、その前後の候補インデックスを返す。
        /// </summary>
        private static IEnumerable<int> GetCandidateIndicesNearX(IReadOnlyList<(double X, double Y)> points, double dataX)
        {
            int lo = 0, hi = points.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (points[mid].X < dataX) lo = mid + 1;
                else hi = mid;
            }

            const int window = 2;
            int start = Math.Max(0, lo - window);
            int end = Math.Min(points.Count - 1, lo + window);
            for (int i = start; i <= end; i++)
            {
                yield return i;
            }
        }

        /// <summary>
        /// 円グラフの各スライスの開始角度・終了角度(度、時計回り、0度=真上)・割合を計算する。
        /// 負の値や NaN/Infinity の値は無視する。合計が 0 以下の場合は空リストを返す。
        /// </summary>
        public static IReadOnlyList<(string Label, double Value, double StartAngle, double SweepAngle, double Percentage)> CalculatePieSlices(PieSeries pie)
        {
            var result = new List<(string Label, double Value, double StartAngle, double SweepAngle, double Percentage)>();
            var validItems = pie.Items
                .Where(item => !double.IsNaN(item.Value) && !double.IsInfinity(item.Value) && item.Value > 0)
                .ToList();

            double total = validItems.Sum(item => item.Value);
            if (total <= 0)
            {
                return result;
            }

            double currentAngle = 0;
            foreach (var item in validItems)
            {
                double percentage = item.Value / total;
                double sweepAngle = percentage * 360.0;
                result.Add((item.Label, item.Value, currentAngle, sweepAngle, percentage));
                currentAngle += sweepAngle;
            }

            return result;
        }
    }
}
