namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 箱ひげ図の系列。カテゴリごとに最小値・第1四分位数・中央値・第3四分位数・最大値を保持する。
    /// </summary>
    public class BoxPlotSeries : Series
    {
        public List<(string Category, double Min, double Q1, double Median, double Q3, double Max)> Items { get; } = new();
    }
}
