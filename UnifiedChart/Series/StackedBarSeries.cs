namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// カテゴリごとに複数の値を積み上げて表示する棒グラフの系列。
    /// </summary>
    public class StackedBarSeries : Series
    {
        /// <summary>
        /// カテゴリ名と、そのカテゴリに積み上げる値のリスト。
        /// </summary>
        public List<(string Category, List<double> Values)> Items { get; } = new();

        /// <summary>
        /// 積み上げ内訳(凡例)に表示する名称。指定がない場合は "系列1" などが自動割当される。
        /// </summary>
        public List<string> SeriesNames { get; } = new();
    }
}
