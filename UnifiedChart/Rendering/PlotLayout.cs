namespace UnifiedChart
{
    /// <summary>
    /// コントロール全体のサイズから、タイトル・軸ラベル・凡例分の余白を差し引いた
    /// プロット領域（データ描画領域）を計算する。初期版は固定余白を用いる簡易実装。
    /// </summary>
    public static class PlotLayout
    {
        public const double DefaultLeftMargin = 48;
        public const double DefaultTopMargin = 32;
        public const double DefaultRightMargin = 16;
        public const double DefaultBottomMargin = 32;

        public static PlotRect CalculatePlotArea(double totalWidth, double totalHeight,
            double leftMargin = DefaultLeftMargin, double topMargin = DefaultTopMargin,
            double rightMargin = DefaultRightMargin, double bottomMargin = DefaultBottomMargin)
        {
            leftMargin = Math.Max(0, leftMargin);
            topMargin = Math.Max(0, topMargin);
            rightMargin = Math.Max(0, rightMargin);
            bottomMargin = Math.Max(0, bottomMargin);
            var width = totalWidth - leftMargin - rightMargin;
            var height = totalHeight - topMargin - bottomMargin;
            if (width < 0) width = 0;
            if (height < 0) height = 0;
            return new PlotRect(leftMargin, topMargin, width, height);
        }
    }
}
