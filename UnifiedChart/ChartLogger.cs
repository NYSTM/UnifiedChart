using System.Globalization;
using System.Text;

namespace UnifiedChart;

public static class ChartLogger
{
    private static readonly object SyncRoot = new();

    public static string LogFilePath => Path.Combine(AppContext.BaseDirectory, "UnifiedChart.log");

    public static void Write(string operation, PlotModel? model = null, string? details = null, Exception? exception = null)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {operation}");
            if (!string.IsNullOrWhiteSpace(details))
            {
                builder.AppendLine($"Details: {details}");
            }

            if (model is not null)
            {
                AppendModelSummary(builder, model);
            }

            if (exception is not null)
            {
                builder.AppendLine($"Exception: {exception.GetType().FullName}: {exception.Message}");
            }

            builder.AppendLine(new string('-', 80));
            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void WritePropBag(string xml, string summary, IReadOnlyList<string>? unsupportedItems = null)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] PropBag適用");
            builder.AppendLine("[解析結果]");
            builder.AppendLine(summary);
            builder.AppendLine("[未対応・部分対応]");
            if (unsupportedItems is null || unsupportedItems.Count == 0)
            {
                builder.AppendLine("未対応項目: なし");
            }
            else
            {
                foreach (var item in unsupportedItems)
                {
                    builder.AppendLine($"警告: {item}");
                }
            }
            builder.AppendLine("[受信XML]");
            builder.AppendLine(xml);
            builder.AppendLine(new string('-', 80));
            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void AppendModelSummary(StringBuilder builder, PlotModel model)
    {
        builder.AppendLine($"Model: Title=\"{model.Title}\", ChartGroups={model.ChartGroups.Count}, ShowLegend={model.ShowLegend}, LegendPosition={model.LegendPosition}");
        builder.AppendLine($"Axes: X={FormatAxis(model.XAxis)}, Y={FormatAxis(model.YAxis)}, Y2={(model.Y2Axis is null ? "なし" : FormatAxis(model.Y2Axis))}");
        var groupIndex = 0;
        foreach (var group in model.ChartGroups)
        {
            builder.AppendLine($"  ChartGroup[{groupIndex++}]: ChartData={group.ChartData.Count}");
            foreach (var data in group.ChartData)
            {
                for (var seriesIndex = 0; seriesIndex < data.SeriesList.Count; seriesIndex++)
                {
                    var series = data.SeriesList[seriesIndex];
                    builder.AppendLine($"    Series: Type={series.GetType().Name}, Name=\"{series.Name}\", Visible={series.Visible}, Summary={FormatSeries(series)}");
                    builder.AppendLine($"      Series[{seriesIndex}]: Label=\"{series.Name}\", Count={GetSeriesCount(series)}");
                }
            }
        }
    }

    private static int GetSeriesCount(Series series)
        => series switch
        {
            LineSeries line => line.Points.Count,
            ScatterSeries scatter => scatter.Points.Count,
            AreaSeries area => area.Points.Count,
            RangeSeries range => range.Points.Count,
            BarSeries bar => bar.Items.Count,
            PieSeries pie => pie.Items.Count,
            RadarSeries radar => radar.Points.Count,
            _ => 0
        };

    private static string FormatAxis(Axis axis)
        => $"Min={FormatValue(axis.Minimum)}, Max={FormatValue(axis.Maximum)}, Log={axis.IsLogarithmic}";

    private static string FormatValue(double? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? "自動";

    private static string FormatSeries(Series series)
        => series switch
        {
            LineSeries line => $"Points={line.Points.Count}, Color={line.Color}, Markers={line.ShowMarkers}",
            ScatterSeries scatter => $"Points={scatter.Points.Count}, Color={scatter.Color}",
            AreaSeries area => $"Points={area.Points.Count}, Color={area.Color}, Fill={area.FillColor}",
            RangeSeries range => $"Points={range.Points.Count}, Color={range.Color}",
            BarSeries bar => $"Items={bar.Items.Count}, Color={bar.Color}",
            PieSeries pie => $"Items={pie.Items.Count}",
            RadarSeries radar => $"Points={radar.Points.Count}, Color={radar.Color}, Fill={radar.FillColor}, Categories={radar.CategoryLabels.Count}, Hole={FormatValue(radar.MissingValueHole)}",
            _ => "概要なし"
        };
}
