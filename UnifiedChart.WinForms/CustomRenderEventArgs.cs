namespace UnifiedChart.WinForms
{
    using System;
    using System.Drawing;
    using UnifiedChart;

    /// <summary>
    /// 通常描画完了後にユーザーコードから追加描画を行うためのイベント引数。
    /// </summary>
    public class CustomRenderEventArgs : EventArgs
    {
        public CustomRenderEventArgs(Graphics graphics, PlotRect plotArea, CoordinateTransform transform, PlotModel model)
        {
            Graphics = graphics;
            PlotArea = plotArea;
            Transform = transform;
            Model = model;
        }

        /// <summary>描画に使用する GDI+ の Graphics。</summary>
        public Graphics Graphics { get; }

        /// <summary>プロット領域(データ描画に使われる矩形)。</summary>
        public PlotRect PlotArea { get; }

        /// <summary>データ座標と画面座標の変換。</summary>
        public CoordinateTransform Transform { get; }

        /// <summary>描画中の PlotModel。</summary>
        public PlotModel Model { get; }
    }
}
