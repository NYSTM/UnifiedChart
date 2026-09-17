namespace UnifiedChart
{
    using System;

    /// <summary>
    /// PlotModel と IPlotView を仲介する MVP の Presenter。
    /// UI 側 (WinForms/WPF) は本クラスを介してモデルを設定・更新する。
    /// </summary>
    public class PlotPresenter
    {
        private readonly IPlotView view;
        private PlotModel model = new();

        public PlotPresenter(IPlotView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.view.Model = model;
        }

        public PlotModel Model
        {
            get => model;
            set
            {
                model = value ?? throw new ArgumentNullException(nameof(value));
                view.Model = model;
                Refresh();
            }
        }

        /// <summary>
        /// モデルが変更された後に呼び出し、View に再描画を要求する。
        /// </summary>
        public void Refresh() => view.InvalidatePlot();
    }
}
