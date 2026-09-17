namespace UnifiedChart.Samples.CSharp
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using UnifiedChart;
    using UnifiedChart.WinForms;

    /// <summary>
    /// UnifiedChart を C# から呼び出すサンプルフォーム。
    /// Line / Scatter / Bar の各系列を1つのプロットにまとめて表示する。
    /// </summary>
    public class MainForm : Form
    {
        public MainForm()
        {
            Text = "UnifiedChart サンプル (C#)";
            Width = 800;
            Height = 600;

            var tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(CreateTabPage("Line + Scatter", BuildLineScatterModel()));
            tabControl.TabPages.Add(CreateTabPage("Bar", BuildBarModel()));
            tabControl.TabPages.Add(CreateTabPage("Histogram", BuildHistogramModel()));
            tabControl.TabPages.Add(CreateTabPage("BoxPlot", BuildBoxPlotModel()));
            tabControl.TabPages.Add(CreateTabPage("Pie", BuildPieModel()));
            tabControl.TabPages.Add(CreateTabPage("Area", BuildAreaModel()));
            tabControl.TabPages.Add(CreateTabPage("StackedBar", BuildStackedBarModel()));
            tabControl.TabPages.Add(CreateTabPage("Range", BuildRangeModel()));
            Controls.Add(tabControl);
        }

        private static TabPage CreateTabPage(string title, PlotModel model)
        {
            var page = new TabPage(title);
            var plotView = new PlotView { Dock = DockStyle.Fill, Model = model };
            page.Controls.Add(plotView);
            return page;
        }

        private static PlotModel BuildLineScatterModel()
        {
            var model = new PlotModel { Title = "Line / Scatter サンプル" };

            var line = new LineSeries { Name = "Line", Color = "Blue", StrokeThickness = 2 };
            for (int i = 0; i <= 10; i++)
            {
                line.Points.Add((i, Math.Sin(i * 0.5) * 10));
            }
            model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

            var scatter = new ScatterSeries { Name = "Scatter", MarkerSize = 6 };
            var random = new Random(1);
            for (int i = 0; i <= 10; i++)
            {
                scatter.Points.Add((i, random.NextDouble() * 20 - 10));
            }
            model.ChartGroups[0].ChartData[0].SeriesList.Add(scatter);

            return model;
        }

        private static PlotModel BuildBarModel()
        {
            var model = new PlotModel { Title = "Bar サンプル" };

            var bar = new BarSeries { Name = "月別売上", ShowDataLabels = true };
            bar.Items.Add(("1月", 5));
            bar.Items.Add(("2月", 8));
            bar.Items.Add(("3月", 3));
            bar.Items.Add(("4月", 12));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(bar);

            return model;
        }

        private static PlotModel BuildHistogramModel()
        {
            var model = new PlotModel { Title = "Histogram サンプル" };

            var histogram = new HistogramSeries { Name = "分布", BinCount = 10 };
            var random = new Random(2);
            for (int i = 0; i < 200; i++)
            {
                histogram.Values.Add(random.NextDouble() * 10);
            }
            model.ChartGroups[0].ChartData[0].SeriesList.Add(histogram);

            return model;
        }

        private static PlotModel BuildBoxPlotModel()
        {
            var model = new PlotModel { Title = "BoxPlot サンプル" };

            var boxPlot = new BoxPlotSeries { Name = "グループ別分布" };
            boxPlot.Items.Add(("グループA", 2, 4, 6, 8, 10));
            boxPlot.Items.Add(("グループB", 1, 3, 5, 7, 9));
            boxPlot.Items.Add(("グループC", 3, 5, 6, 9, 12));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(boxPlot);

            return model;
        }

        private static PlotModel BuildPieModel()
        {
            var model = new PlotModel { Title = "Pie サンプル" };

            var pie = new PieSeries { Name = "シェア" };
            pie.Items.Add(("製品A", 40));
            pie.Items.Add(("製品B", 25));
            pie.Items.Add(("製品C", 20));
            pie.Items.Add(("製品D", 15));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(pie);

            return model;
        }

        private static PlotModel BuildAreaModel()
        {
            var model = new PlotModel { Title = "Area サンプル" };

            var area = new AreaSeries { Name = "気温推移", Color = "SteelBlue", FillColor = "LightSkyBlue" };
            for (int i = 0; i <= 10; i++)
            {
                area.Points.Add((i, Math.Sin(i * 0.4) * 8 + 2));
            }
            model.ChartGroups[0].ChartData[0].SeriesList.Add(area);

            return model;
        }

        private static PlotModel BuildStackedBarModel()
        {
            var model = new PlotModel { Title = "StackedBar サンプル" };

            var stackedBar = new StackedBarSeries { Name = "部門別売上" };
            stackedBar.SeriesNames.Add("製品A");
            stackedBar.SeriesNames.Add("製品B");
            stackedBar.SeriesNames.Add("製品C");
            stackedBar.Items.Add(("1月", new List<double> { 3, 5, 2 }));
            stackedBar.Items.Add(("2月", new List<double> { 4, 3, 6 }));
            stackedBar.Items.Add(("3月", new List<double> { 2, 7, 1 }));
            model.ChartGroups[0].ChartData[0].SeriesList.Add(stackedBar);

            return model;
        }

        private static PlotModel BuildRangeModel()
        {
            var model = new PlotModel { Title = "Range サンプル" };

            var range = new RangeSeries { Name = "予測範囲", Color = "SteelBlue", FillColor = "LightSkyBlue" };
            for (int i = 0; i <= 10; i++)
            {
                var center = Math.Sin(i * 0.4) * 8;
                range.Points.Add((i, center - 2, center + 2));
            }
            model.ChartGroups[0].ChartData[0].SeriesList.Add(range);

            return model;
        }
    }
}
