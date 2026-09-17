namespace UnifiedChart
{
    using System.Collections.Generic;
    /// <summary>
    /// 数値軸を表す。自動範囲（Minimum/Maximum が null）と固定範囲の両方をサポートする。
    /// </summary>
    public class Axis
    {
        public string Title { get; set; } = string.Empty;
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }

        public double? MajorUnit { get; set; }
        public double? MinorUnit { get; set; }
        public bool AutoMajor { get; set; } = true;
        public bool AutoMinor { get; set; } = true;
        public bool AutoMinimum { get; set; } = true;
        public bool AutoMaximum { get; set; } = true;
        public bool GridMajorVisible { get; set; } = true;
        public double? GridMajorSpacing { get; set; }
        public double GridMajorThickness { get; set; } = 1.0;
        public bool GridMinorVisible { get; set; } = true;
        public double? GridMinorSpacing { get; set; }
        public double GridMinorThickness { get; set; } = 1.0;
        public double AxisLineThickness { get; set; } = 2.0;
        public string Compass { get; set; } = string.Empty;
        public bool ShowScrollButtons { get; set; }
        public AxisScrollSettings Scroll { get; } = new();
        public double? Origin { get; set; }
        public AxisTickDirection TickDirection { get; set; } = AxisTickDirection.Outward;
        public bool TickLabelsVisible { get; set; } = true;
        public AxisLabelMode LabelMode { get; set; } = AxisLabelMode.Standard;

        public bool ChartValueLabelsEnabled
        {
            get => LabelMode == AxisLabelMode.ValueLabels;
            set => LabelMode = value ? AxisLabelMode.ValueLabels : AxisLabelMode.Standard;
        }

        public List<AxisMarker> Markers { get; } = new();

        public List<AxisGridLine> GridLines { get; } = new();

        public List<AxisAlarmZone> AlarmZones { get; } = new();

        public List<AxisValueLabel> ValueLabels { get; } = new();

        /// <summary>
        /// この軸を対数(Log10)スケールで表示するかどうか。X軸・Y軸どちらでも使用できる。
        /// 有効な場合、0以下の値は描画から除外される。
        /// </summary>
        public bool IsLogarithmic { get; set; } = false;

        /// <summary>
        /// この軸を日時軸として扱うかどうか。X軸での使用を想定する。
        /// 有効な場合、データ値は <see cref="System.DateTime.ToOADate"/> による数値(OA日付値)として扱われる。
        /// </summary>
        public bool IsDateTime { get; set; } = false;

        /// <summary>
        /// 日時軸のラベル書式(<see cref="System.DateTime.ToString(string)"/> に渡す書式文字列)。既定は "yyyy/MM/dd"。
        /// </summary>
        public string DateTimeFormat { get; set; } = "yyyy/MM/dd";
    }
}
