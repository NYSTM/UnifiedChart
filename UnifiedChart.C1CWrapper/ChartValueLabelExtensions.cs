using System;
using System.Collections.Generic;

namespace UnifiedChart.C1CWrapper
{
    public static class ChartValueLabelExtensions
    {
        public static ChartValueLabel Add(this List<ChartValueLabel> labels, double value, string text)
        {
            ArgumentNullException.ThrowIfNull(labels);

            var label = new ChartValueLabel
            {
                Value = value,
                Text = text ?? string.Empty
            };
            labels.Add(label);
            return label;
        }

        public static ChartDataSeries AddNewSeries(this List<ChartDataSeries> seriesList)
        {
            ArgumentNullException.ThrowIfNull(seriesList);

            var series = new ChartDataSeries();
            seriesList.Add(series);
            return series;
        }
    }
}
