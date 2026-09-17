namespace UnifiedChart
{
    using System.Collections.Generic;

    /// <summary>
    /// グラフ全体のデータモデル。軸とグループ階層を保持する。
    /// </summary>
    public class PlotModel
    {
        public string Title { get; set; } = string.Empty;
        public string TitleCompass { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
        public string FooterCompass { get; set; } = string.Empty;
        public bool ShowFooter { get; set; } = true;

        public string FontFamilyName { get; set; } = "Meiryo UI";

        public float TitleFontSize { get; set; } = 12f;
        public float FooterFontSize { get; set; } = 9f;
        public float AxisLabelFontSize { get; set; } = 8f;
        public float AxisTitleFontSize { get; set; } = 9f;
        public float DataLabelFontSize { get; set; } = 8f;
        public float LegendFontSize { get; set; } = 8f;
        public float ValueLabelFontSize { get; set; } = 8f;
        public float AlarmZoneFontSize { get; set; } = 8f;

        public double PlotMarginLeft { get; set; } = PlotLayout.DefaultLeftMargin;
        public double PlotMarginTop { get; set; } = PlotLayout.DefaultTopMargin;
        public double PlotMarginRight { get; set; } = PlotLayout.DefaultRightMargin;
        public double PlotMarginBottom { get; set; } = PlotLayout.DefaultBottomMargin;

        public PlotAxes Axes { get; } = new();

        public Axis XAxis => Axes.XAxis;
        public Axis? X2Axis
        {
            get => Axes.X2Axis;
            set => Axes.X2Axis = value;
        }
        public Axis YAxis => Axes.YAxis;
        public Axis? Y2Axis
        {
            get => Axes.Y2Axis;
            set => Axes.Y2Axis = value;
        }

        /// <summary>
        /// グラフ全体の配色テーマ。既定は <see cref="PlotTheme.Light"/>。
        /// 系列側に明示的な色が設定されている場合はそちらが優先される。
        /// </summary>
        public PlotTheme Theme { get; set; } = PlotTheme.Light;

        public List<ChartGroup> ChartGroups { get; } = new() { new ChartGroup() };

        public List<ChartGroup> ChartGroup => ChartGroups;

        public IEnumerable<Series> GetAllSeries()
        {
            foreach (var group in ChartGroups)
            {
                foreach (var data in group.ChartData)
                {
                    foreach (var series in data.SeriesList)
                    {
                        yield return series;
                    }
                }
            }
        }

        /// <summary>
        /// グリッド線(目盛線)を描画するかどうか。既定は true。
        /// </summary>
        public bool ShowGridLines { get; set; } = true;

        /// <summary>
        /// 凡例の表示位置。既定は右上(TopRight)。
        /// </summary>
        public LegendPosition LegendPosition { get; set; } = LegendPosition.TopRight;

        /// <summary>
        /// 凡例を表示するかどうか。既定は true。
        /// C1C の <c>chart.Legend.Visible</c> に相当する。
        /// </summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// レーダーチャートの同心円グリッド数。既定は5。
        /// </summary>
        public int RadarGridLevels { get; set; } = 5;

        /// <summary>
        /// レーダーチャートの目盛りラベル。内側から外側の順に指定する。
        /// nullまたは要素数がレベル数と異なる場合は数値ラベルを表示する。
        /// </summary>
        public IReadOnlyList<string>? RadarGridLabels { get; set; }

        /// <summary>
        /// レーダーチャートの項目配置方向。既定はC1Chart互換の反時計回り。
        /// </summary>
        public RadarDirection RadarDirection { get; set; } = RadarDirection.CounterClockwise;

        /// <summary>
        /// プロット領域上に表示するテキスト注釈の一覧。
        /// </summary>
        public List<TextAnnotation> Annotations { get; } = new();

        public List<ChartLabel> ChartLabels { get; } = new();
    }
}
