namespace UnifiedChart
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// AxisRange から見やすい間隔の目盛り(Tick)値を計算するヘルパー。UI 非依存。
    /// </summary>
    public static class AxisTickHelper
    {
        /// <summary>
        /// 指定した範囲に対して、概ね desiredTickCount 個程度になるよう
        /// 「きれいな」間隔(1, 2, 5 の10のべき乗倍)で目盛り値を計算する。
        /// </summary>
        public static IReadOnlyList<double> GetTicks(AxisRange range, int desiredTickCount = 5)
        {
            var result = new List<double>();
            double min = range.Min;
            double max = range.Max;

            if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max) || min >= max || desiredTickCount <= 0)
            {
                result.Add(min);
                return result;
            }

            double step = CalculateNiceStep(max - min, desiredTickCount);
            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
            {
                result.Add(min);
                return result;
            }

            double firstTick = Math.Ceiling(min / step) * step;
            const double epsilon = 1e-9;
            for (double tick = firstTick; tick <= max + step * epsilon; tick += step)
            {
                // 浮動小数点誤差で -0 や微小な誤差が出ないよう丸める
                double rounded = Math.Round(tick / step) * step;
                if (rounded < min - step * epsilon) continue;
                if (rounded > max + step * epsilon) break;
                result.Add(rounded);
            }

            return result;
        }

        public static IReadOnlyList<double> GetTicks(AxisRange range, Axis axis, int desiredTickCount = 5)
            => axis.GridMajorSpacing is double gridSpacing && gridSpacing > 0
                ? GetFixedTicks(range, gridSpacing)
                : axis.MajorUnit is double majorUnit && majorUnit > 0
                    ? GetFixedTicks(range, majorUnit)
                    : GetTicks(range, desiredTickCount);

        private static IReadOnlyList<double> GetFixedTicks(AxisRange range, double step)
        {
            var result = new List<double>();
            if (double.IsNaN(step) || double.IsInfinity(step) || step <= 0 || range.Min >= range.Max)
            {
                result.Add(range.Min);
                return result;
            }

            var firstTick = Math.Ceiling(range.Min / step) * step;
            for (var tick = firstTick; tick <= range.Max + step * 1e-9; tick += step)
            {
                result.Add(Math.Round(tick / step) * step);
            }

            return result;
        }

        public static IReadOnlyList<double> GetMinorTicks(AxisRange range, Axis axis)
        {
            var minorUnit = axis.GridMinorSpacing ?? axis.MinorUnit;
            if (minorUnit is not double unit || unit <= 0 || !axis.AutoMinor)
            {
                return Array.Empty<double>();
            }

            var result = new List<double>();
            var majorUnit = axis.MajorUnit;
            var firstTick = Math.Ceiling(range.Min / unit) * unit;
            for (var tick = firstTick; tick <= range.Max + unit * 1e-9; tick += unit)
            {
                var rounded = Math.Round(tick / unit) * unit;
                if (majorUnit is double major && major > 0 && Math.Abs(rounded / major - Math.Round(rounded / major)) < 1e-9)
                {
                    continue;
                }

                if (rounded >= range.Min && rounded <= range.Max)
                {
                    result.Add(rounded);
                }
            }

            return result;
        }

        /// <summary>
        /// 1, 2, 5 の10のべき乗倍から、指定した目盛り数に最も近くなる「きれいな」間隔を求める。
        /// </summary>
        private static double CalculateNiceStep(double span, int desiredTickCount)
        {
            double roughStep = span / desiredTickCount;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            double normalized = roughStep / magnitude;

            double niceNormalized;
            if (normalized <= 1.0) niceNormalized = 1.0;
            else if (normalized <= 2.0) niceNormalized = 2.0;
            else if (normalized <= 5.0) niceNormalized = 5.0;
            else niceNormalized = 10.0;

            return niceNormalized * magnitude;
        }

        /// <summary>
        /// 対数(Log10)空間の範囲(logRange.Min/Max は Log10 済みの値)から、
        /// 10 のべき乗を基準とした実データ値の目盛(1, 10, 100, ... および必要に応じて 2,5 倍数)を生成する。
        /// </summary>
        public static IReadOnlyList<double> GetLogTicks(AxisRange logRange)
        {
            var result = new List<double>();
            double min = logRange.Min;
            double max = logRange.Max;

            if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max) || min >= max)
            {
                result.Add(Math.Pow(10, min));
                return result;
            }

            int startExponent = (int)Math.Floor(min);
            int endExponent = (int)Math.Ceiling(max);
            const double epsilon = 1e-9;

            for (int exponent = startExponent; exponent <= endExponent; exponent++)
            {
                foreach (var multiplier in new[] { 1.0, 2.0, 5.0 })
                {
                    double value = multiplier * Math.Pow(10, exponent);
                    double logValue = Math.Log10(value);
                    if (logValue < min - epsilon || logValue > max + epsilon) continue;
                    result.Add(value);
                }
            }

            result.Sort();
            return result;
        }

        /// <summary>
        /// OA日付値(<see cref="DateTime.ToOADate"/>)で表された範囲に対して、
        /// 秒・分・時・日・月・年のうち「きれいな」単位を自動選択して目盛り(OA日付値)を計算する。
        /// </summary>
        public static IReadOnlyList<double> GetDateTimeTicks(AxisRange range, int desiredTickCount = 5)
        {
            var result = new List<double>();
            double min = range.Min;
            double max = range.Max;

            if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max) || min >= max || desiredTickCount <= 0)
            {
                result.Add(min);
                return result;
            }

            DateTime minDate = DateTime.FromOADate(min);
            DateTime maxDate = DateTime.FromOADate(max);
            TimeSpan span = maxDate - minDate;

            var unit = CalculateNiceDateTimeUnit(span, desiredTickCount);
            DateTime tick = AlignToUnit(minDate, unit);
            if (tick < minDate)
            {
                tick = AddUnit(tick, unit);
            }

            const int safetyLimit = 1000;
            int count = 0;
            while (tick <= maxDate && count < safetyLimit)
            {
                double oaValue = tick.ToOADate();
                if (oaValue >= min - 1e-9 && oaValue <= max + 1e-9)
                {
                    result.Add(oaValue);
                }
                tick = AddUnit(tick, unit);
                count++;
            }

            if (result.Count == 0)
            {
                result.Add(min);
            }

            return result;
        }

        private enum DateTimeUnit
        {
            Second,
            Minute,
            Hour,
            Day,
            Month,
            Year,
        }

        /// <summary>
        /// 期間と目標目盛り数から、秒/分/時/日/月/年のうち最も適した「きれいな」単位を決定する。
        /// </summary>
        private static DateTimeUnit CalculateNiceDateTimeUnit(TimeSpan span, int desiredTickCount)
        {
            double totalSeconds = span.TotalSeconds / desiredTickCount;

            if (totalSeconds < 60) return DateTimeUnit.Second;
            if (totalSeconds < 3600) return DateTimeUnit.Minute;
            if (totalSeconds < 86400) return DateTimeUnit.Hour;
            if (span.TotalDays / desiredTickCount < 31) return DateTimeUnit.Day;
            if (span.TotalDays / desiredTickCount < 365) return DateTimeUnit.Month;
            return DateTimeUnit.Year;
        }

        /// <summary>
        /// 指定した日時を、単位の切りの良い境界(分の0秒、時の0分、月初、年初など)に切り上げる。
        /// </summary>
        private static DateTime AlignToUnit(DateTime value, DateTimeUnit unit)
        {
            return unit switch
            {
                DateTimeUnit.Second => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second),
                DateTimeUnit.Minute => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0),
                DateTimeUnit.Hour => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0),
                DateTimeUnit.Day => new DateTime(value.Year, value.Month, value.Day),
                DateTimeUnit.Month => new DateTime(value.Year, value.Month, 1),
                DateTimeUnit.Year => new DateTime(value.Year, 1, 1),
                _ => value,
            };
        }

        private static DateTime AddUnit(DateTime value, DateTimeUnit unit)
        {
            return unit switch
            {
                DateTimeUnit.Second => value.AddSeconds(1),
                DateTimeUnit.Minute => value.AddMinutes(1),
                DateTimeUnit.Hour => value.AddHours(1),
                DateTimeUnit.Day => value.AddDays(1),
                DateTimeUnit.Month => value.AddMonths(1),
                DateTimeUnit.Year => value.AddYears(1),
                _ => value,
            };
        }
    }
}
