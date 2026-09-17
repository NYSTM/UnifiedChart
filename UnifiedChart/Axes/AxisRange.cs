namespace UnifiedChart
{
    using System;

    /// <summary>
    /// 軸の表示範囲（最小値・最大値）。データから自動計算するためのユーティリティを提供する。
    /// </summary>
    public readonly struct AxisRange
    {
        public double Min { get; }
        public double Max { get; }

        public AxisRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Span => Max - Min;

        /// <summary>
        /// 表示幅を維持したまま、指定した量だけ範囲を移動する。移動先は全体範囲内に制限する。
        /// </summary>
        public AxisRange TranslateClamped(double offset, AxisRange bounds)
        {
            if (Min >= Max || bounds.Min >= bounds.Max)
            {
                return bounds;
            }

            var span = Span;
            if (span >= bounds.Span)
            {
                return bounds;
            }

            var min = Math.Clamp(Min + offset, bounds.Min, bounds.Max - span);
            return new AxisRange(min, min + span);
        }

        /// <summary>
        /// 表示範囲を全体範囲の割合で移動する。正の値は最大方向へ移動する。
        /// </summary>
        public AxisRange ScrollClamped(AxisRange bounds, double fraction = 0.2)
            => TranslateClamped(Span * fraction, bounds);

        /// <summary>
        /// 指定した最小・最大の候補値から、パディングを加えた表示範囲を計算する。
        /// データが空、単一点、同値の場合も安全な範囲を返す。NaN・Infinity は無視する。
        /// </summary>
        public static AxisRange Calculate(double? explicitMin, double? explicitMax, double dataMin, double dataMax, bool hasData, double paddingRatio = 0.05)
        {
            if (!hasData || double.IsNaN(dataMin) || double.IsNaN(dataMax) || double.IsInfinity(dataMin) || double.IsInfinity(dataMax))
            {
                dataMin = 0;
                dataMax = 1;
            }

            if (dataMin == dataMax)
            {
                var delta = dataMin == 0 ? 1.0 : Math.Abs(dataMin) * 0.5;
                dataMin -= delta;
                dataMax += delta;
            }

            var span = dataMax - dataMin;
            var padding = span * paddingRatio;
            var min = explicitMin ?? (dataMin - padding);
            var max = explicitMax ?? (dataMax + padding);

            if (min >= max)
            {
                max = min + 1.0;
            }

            return new AxisRange(min, max);
        }
    }
}
