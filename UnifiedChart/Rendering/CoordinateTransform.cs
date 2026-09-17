namespace UnifiedChart
{
    using System;

    /// <summary>
    /// データ座標とプロット領域内の画面座標を相互変換する。Y 軸は画面座標系（下向きが正）に反転する。
    /// </summary>
    public readonly struct CoordinateTransform
    {
        private readonly PlotRect plotArea;
        private readonly AxisRange xRange;
        private readonly AxisRange yRange;
        private readonly bool isXLogarithmic;
        private readonly bool isYLogarithmic;
        private readonly AxisRange? x2Range;
        private readonly bool isX2Logarithmic;
        private readonly AxisRange? y2Range;
        private readonly bool isY2Logarithmic;

        public CoordinateTransform(PlotRect plotArea, AxisRange xRange, AxisRange yRange)
            : this(plotArea, xRange, yRange, false)
        {
        }

        public double ToScreenXAxisOrigin(double dataY)
            => Math.Clamp(ToScreenY(dataY), plotArea.Top, plotArea.Bottom);

        public double ToScreenYAxisOrigin(double dataX)
            => Math.Clamp(ToScreenX(dataX), plotArea.Left, plotArea.Right);

        /// <summary>
        /// Y軸を対数(Log10)スケールとして扱う場合は isYLogarithmic に true を指定する。
        /// この場合 yRange は既に Log10 変換済みの範囲(Min/Max が Log10 値)として渡す。
        /// </summary>
        public CoordinateTransform(PlotRect plotArea, AxisRange xRange, AxisRange yRange, bool isYLogarithmic)
            : this(plotArea, xRange, yRange, false, isYLogarithmic)
        {
        }

        /// <summary>
        /// X軸・Y軸それぞれを対数(Log10)スケールとして扱う場合は対応するフラグに true を指定する。
        /// 対数指定した軸の range は既に Log10 変換済みの範囲(Min/Max が Log10 値)として渡す。
        /// </summary>
        public CoordinateTransform(PlotRect plotArea, AxisRange xRange, AxisRange yRange, bool isXLogarithmic, bool isYLogarithmic)
            : this(plotArea, xRange, yRange, isXLogarithmic, isYLogarithmic, null, false)
        {
        }

        /// <summary>
        /// セカンダリ(第2)Y軸の範囲を併せて指定するコンストラクタ。y2Range が null の場合は第2軸未使用。
        /// 対数指定した軸の range は既に Log10 変換済みの範囲(Min/Max が Log10 値)として渡す。
        /// </summary>
        public CoordinateTransform(PlotRect plotArea, AxisRange xRange, AxisRange yRange, bool isXLogarithmic, bool isYLogarithmic, AxisRange? y2Range, bool isY2Logarithmic)
            : this(plotArea, xRange, yRange, isXLogarithmic, isYLogarithmic, null, false, y2Range, isY2Logarithmic)
        {
        }

        /// <summary>
        /// セカンダリ(第2)X/Y軸の範囲を併せて指定するコンストラクタ。
        /// 対数指定した軸の range は既に Log10 変換済みの範囲として渡す。
        /// </summary>
        public CoordinateTransform(PlotRect plotArea, AxisRange xRange, AxisRange yRange, bool isXLogarithmic, bool isYLogarithmic, AxisRange? x2Range, bool isX2Logarithmic, AxisRange? y2Range, bool isY2Logarithmic)
        {
            this.plotArea = plotArea;
            this.xRange = xRange;
            this.yRange = yRange;
            this.isXLogarithmic = isXLogarithmic;
            this.isYLogarithmic = isYLogarithmic;
            this.x2Range = x2Range;
            this.isX2Logarithmic = isX2Logarithmic;
            this.y2Range = y2Range;
            this.isY2Logarithmic = isY2Logarithmic;
        }

        public AxisRange XRange => xRange;
        public AxisRange YRange => yRange;
        public bool IsXLogarithmic => isXLogarithmic;
        public bool IsYLogarithmic => isYLogarithmic;

        public bool HasSecondaryXAxis => x2Range.HasValue;

        public AxisRange X2Range => x2Range ?? default;

        public bool IsX2Logarithmic => isX2Logarithmic;

        /// <summary>
        /// セカンダリ(第2)Y軸が有効かどうか。
        /// </summary>
        public bool HasSecondaryYAxis => y2Range.HasValue;

        /// <summary>
        /// セカンダリ(第2)Y軸の範囲。HasSecondaryYAxis が false の場合は既定値を返す。
        /// </summary>
        public AxisRange Y2Range => y2Range ?? default;

        public bool IsY2Logarithmic => isY2Logarithmic;

        public double ToScreenX(double dataX)
        {
            var value = isXLogarithmic ? SafeLog10(dataX) : dataX;
            var span = xRange.Span == 0 ? 1 : xRange.Span;
            var ratio = (value - xRange.Min) / span;
            return plotArea.Left + ratio * plotArea.Width;
        }

        public double ToScreenX2(double dataX)
        {
            var range = x2Range ?? default;
            var value = isX2Logarithmic ? SafeLog10(dataX) : dataX;
            var span = range.Span == 0 ? 1 : range.Span;
            var ratio = (value - range.Min) / span;
            return plotArea.Left + ratio * plotArea.Width;
        }

        public double ToDataX2(double screenX)
        {
            var range = x2Range ?? default;
            var ratio = plotArea.Width == 0 ? 0 : (screenX - plotArea.Left) / plotArea.Width;
            var value = range.Min + ratio * range.Span;
            return isX2Logarithmic ? Math.Pow(10, value) : value;
        }

        public double ToScreenY(double dataY)
        {
            var value = isYLogarithmic ? SafeLog10(dataY) : dataY;
            var span = yRange.Span == 0 ? 1 : yRange.Span;
            var ratio = (value - yRange.Min) / span;
            return plotArea.Bottom - ratio * plotArea.Height;
        }

        public double ToDataX(double screenX)
        {
            var ratio = plotArea.Width == 0 ? 0 : (screenX - plotArea.Left) / plotArea.Width;
            var value = xRange.Min + ratio * xRange.Span;
            return isXLogarithmic ? Math.Pow(10, value) : value;
        }

        public double ToDataY(double screenY)
        {
            var ratio = plotArea.Height == 0 ? 0 : (plotArea.Bottom - screenY) / plotArea.Height;
            var value = yRange.Min + ratio * yRange.Span;
            return isYLogarithmic ? Math.Pow(10, value) : value;
        }

        /// <summary>
        /// セカンダリ(第2)Y軸のデータ値を画面Y座標に変換する。HasSecondaryYAxis が false の場合は結果は不定。
        /// </summary>
        public double ToScreenY2(double dataY)
        {
            var range = y2Range ?? default;
            var value = isY2Logarithmic ? SafeLog10(dataY) : dataY;
            var span = range.Span == 0 ? 1 : range.Span;
            var ratio = (value - range.Min) / span;
            return plotArea.Bottom - ratio * plotArea.Height;
        }

        /// <summary>
        /// セカンダリ(第2)Y軸の画面Y座標をデータ値に変換する。HasSecondaryYAxis が false の場合は結果は不定。
        /// </summary>
        public double ToDataY2(double screenY)
        {
            var range = y2Range ?? default;
            var ratio = plotArea.Height == 0 ? 0 : (plotArea.Bottom - screenY) / plotArea.Height;
            var value = range.Min + ratio * range.Span;
            return isY2Logarithmic ? Math.Pow(10, value) : value;
        }

        private static double SafeLog10(double value) => value > 0 ? Math.Log10(value) : double.NaN;
    }
}
