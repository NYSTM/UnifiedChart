namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// 円グラフの系列。ラベルと値のペアを保持し、値の割合に応じたスライスとして描画される。
    /// X/Y 軸を使わない特殊な系列のため、他の系列とは混在させず単独の PlotModel で使用することを想定する。
    /// </summary>
    public class PieSeries : Series
    {
        public bool IsDoughnut { get; set; }

        public List<(string Label, double Value)> Items { get; } = new();
    }
}
