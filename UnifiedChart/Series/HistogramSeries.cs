namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// ヒストグラムの系列。生データを BinCount 個の区間に分割し、区間ごとの度数を棒状に描画する。
    /// </summary>
    public class HistogramSeries : Series
    {
        public List<double> Values { get; } = new();
        public int BinCount { get; set; } = 10;
    }
}
