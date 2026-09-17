namespace UnifiedChart
{
    /// <summary>
    /// グラフ全体の配色を定義するテーマ。背景色・文字色・目盛線色・系列パレットなどを保持する。
    /// <see cref="PlotModel.Theme"/> に設定することで WinForms/WPF 双方のレンダラーに反映される。
    /// 系列側(<see cref="Series.Color"/> 等)に明示的な色が設定されている場合はそちらが優先される。
    /// </summary>
    public class PlotTheme
    {
        /// <summary>プロット領域全体の背景色。</summary>
        public string BackgroundColor { get; set; } = "White";

        /// <summary>タイトル文字色。</summary>
        public string TitleColor { get; set; } = "Black";

        /// <summary>軸目盛ラベルの文字色。</summary>
        public string AxisLabelColor { get; set; } = "Black";

        /// <summary>グリッド線(目盛線)の色。</summary>
        public string GridLineColor { get; set; } = "LightGray";

        /// <summary>凡例文字色。</summary>
        public string LegendTextColor { get; set; } = "Black";

        /// <summary>
        /// 系列既定色が未指定の場合に使用する配色パレット。
        /// 円グラフ・積み上げ棒グラフ・複数カテゴリの色分けに使用する。
        /// </summary>
        public string[] SeriesPalette { get; set; } = new[]
        {
            "SteelBlue", "SeaGreen", "IndianRed", "Goldenrod",
            "MediumPurple", "Teal", "Chocolate", "SlateGray"
        };

        /// <summary>棒グラフ・ヒストグラム・散布図などの既定系列色。</summary>
        public string DefaultSeriesColor { get; set; } = "SteelBlue";

        /// <summary>明るい背景を基調とした既定テーマ。</summary>
        public static PlotTheme Light { get; } = new PlotTheme
        {
            BackgroundColor = "White",
            TitleColor = "Black",
            AxisLabelColor = "Black",
            GridLineColor = "LightGray",
            LegendTextColor = "Black",
            SeriesPalette = new[]
            {
                "SteelBlue", "SeaGreen", "IndianRed", "Goldenrod",
                "MediumPurple", "Teal", "Chocolate", "SlateGray"
            },
            DefaultSeriesColor = "SteelBlue"
        };

        /// <summary>暗い背景を基調としたダークテーマ。</summary>
        public static PlotTheme Dark { get; } = new PlotTheme
        {
            BackgroundColor = "#FF1E1E1E",
            TitleColor = "White",
            AxisLabelColor = "#FFDDDDDD",
            GridLineColor = "#FF444444",
            LegendTextColor = "White",
            SeriesPalette = new[]
            {
                "#FF4FC3F7", "#FF81C784", "#FFE57373", "#FFFFD54F",
                "#FFBA68C8", "#FF4DB6AC", "#FFFF8A65", "#FF90A4AE"
            },
            DefaultSeriesColor = "#FF4FC3F7"
        };

        /// <summary>鮮やかな配色を基調としたカラフルテーマ。</summary>
        public static PlotTheme Colorful { get; } = new PlotTheme
        {
            BackgroundColor = "White",
            TitleColor = "#FF2C2C2C",
            AxisLabelColor = "#FF2C2C2C",
            GridLineColor = "#FFE0E0E0",
            LegendTextColor = "#FF2C2C2C",
            SeriesPalette = new[]
            {
                "#FFE91E63", "#FF3F51B5", "#FF4CAF50", "#FFFF9800",
                "#FF9C27B0", "#FF00BCD4", "#FFFFC107", "#FF795548"
            },
            DefaultSeriesColor = "#FFE91E63"
        };
    }
}
