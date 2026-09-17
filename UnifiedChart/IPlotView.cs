namespace UnifiedChart
{
    /// <summary>
    /// グラフ表示コントロールが実装する View 契約。UI 側 (WinForms/WPF) が実装する。
    /// </summary>
    public interface IPlotView
    {
        PlotModel? Model { get; set; }
        void InvalidatePlot();
    }
}
