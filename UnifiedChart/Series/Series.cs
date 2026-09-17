namespace UnifiedChart
{
    /// <summary>
    /// すべての系列の基底クラス。
    /// </summary>
    public abstract class Series
    {
        public string Name { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public string TooltipText { get; set; } = string.Empty;

        /// <summary>
        /// データ点上に数値ラベルを表示するかどうか。
        /// </summary>
        public bool ShowDataLabels { get; set; } = false;

        /// <summary>
        /// データラベルの数値書式(未指定時は"0.##"を使用)。
        /// </summary>
        public string? DataLabelFormat { get; set; } = null;

        /// <summary>
        /// この系列をセカンダリ(第2)Y軸で描画するかどうか。
        /// PlotModel.Y2Axis が設定されていない場合は無視され、通常のY軸で描画される。
        /// </summary>
        public bool UseSecondaryYAxis { get; set; } = false;

        /// <summary>
        /// この系列をセカンダリ(第2)X軸で描画するかどうか。
        /// PlotModel.X2Axis が設定されていない場合は無視され、通常のX軸で描画される。
        /// </summary>
        public bool UseSecondaryXAxis { get; set; } = false;

        public double? MissingValueHole { get; set; }

        public bool ConnectAcrossMissingValues { get; set; } = true;
    }
}
