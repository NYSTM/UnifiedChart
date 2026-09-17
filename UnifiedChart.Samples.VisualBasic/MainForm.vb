Imports System.Collections.Generic
Imports System.Windows.Forms
Imports UnifiedChart
Imports UnifiedChart.WinForms

''' <summary>
''' UnifiedChart を VB.NET から呼び出すサンプルフォーム。
''' Line / Scatter / Bar の各系列をタブに分けて表示する。
''' </summary>
Public Class MainForm
    Inherits Form

    Public Sub New()
        Text = "UnifiedChart サンプル (VB.NET)"
        Width = 800
        Height = 600

        Dim tabControl As New TabControl With {.Dock = DockStyle.Fill}
        tabControl.TabPages.Add(CreateTabPage("Line + Scatter", BuildLineScatterModel()))
        tabControl.TabPages.Add(CreateTabPage("Bar", BuildBarModel()))
        tabControl.TabPages.Add(CreateTabPage("Histogram", BuildHistogramModel()))
        tabControl.TabPages.Add(CreateTabPage("BoxPlot", BuildBoxPlotModel()))
        tabControl.TabPages.Add(CreateTabPage("Pie", BuildPieModel()))
        tabControl.TabPages.Add(CreateTabPage("Area", BuildAreaModel()))
        tabControl.TabPages.Add(CreateTabPage("StackedBar", BuildStackedBarModel()))
        tabControl.TabPages.Add(CreateTabPage("Range", BuildRangeModel()))
        Controls.Add(tabControl)
    End Sub

    Private Shared Function CreateTabPage(title As String, model As PlotModel) As TabPage
        Dim page As New TabPage(title)
        Dim plotView As New PlotView With {.Dock = DockStyle.Fill, .Model = model}
        page.Controls.Add(plotView)
        Return page
    End Function

    Private Shared Function BuildLineScatterModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Line / Scatter サンプル"}

        Dim line As New LineSeries With {.Name = "Line", .Color = "Blue", .StrokeThickness = 2}
        For i As Integer = 0 To 10
            line.Points.Add((CDbl(i), Math.Sin(i * 0.5) * 10))
        Next
        model.ChartGroups(0).ChartData(0).SeriesList.Add(line)

        Dim scatter As New ScatterSeries With {.Name = "Scatter", .MarkerSize = 6}
        Dim random As New Random(1)
        For i As Integer = 0 To 10
            scatter.Points.Add((CDbl(i), random.NextDouble() * 20 - 10))
        Next
        model.ChartGroups(0).ChartData(0).SeriesList.Add(scatter)

        Return model
    End Function

    Private Shared Function BuildBarModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Bar サンプル"}

        Dim bar As New BarSeries With {.Name = "月別売上", .ShowDataLabels = True}
        bar.Items.Add(("1月", 5))
        bar.Items.Add(("2月", 8))
        bar.Items.Add(("3月", 3))
        bar.Items.Add(("4月", 12))
        model.ChartGroups(0).ChartData(0).SeriesList.Add(bar)

        Return model
    End Function

    Private Shared Function BuildHistogramModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Histogram サンプル"}

        Dim histogram As New HistogramSeries With {.Name = "分布", .BinCount = 10}
        Dim random As New Random(2)
        For i As Integer = 0 To 199
            histogram.Values.Add(random.NextDouble() * 10)
        Next
        model.ChartGroups(0).ChartData(0).SeriesList.Add(histogram)

        Return model
    End Function

    Private Shared Function BuildBoxPlotModel() As PlotModel
        Dim model As New PlotModel With {.Title = "BoxPlot サンプル"}

        Dim boxPlot As New BoxPlotSeries With {.Name = "グループ別分布"}
        boxPlot.Items.Add(("グループA", 2, 4, 6, 8, 10))
        boxPlot.Items.Add(("グループB", 1, 3, 5, 7, 9))
        boxPlot.Items.Add(("グループC", 3, 5, 6, 9, 12))
        model.ChartGroups(0).ChartData(0).SeriesList.Add(boxPlot)

        Return model
    End Function

    Private Shared Function BuildPieModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Pie サンプル"}

        Dim pie As New PieSeries With {.Name = "シェア"}
        pie.Items.Add(("製品A", 40))
        pie.Items.Add(("製品B", 25))
        pie.Items.Add(("製品C", 20))
        pie.Items.Add(("製品D", 15))
        model.ChartGroups(0).ChartData(0).SeriesList.Add(pie)

        Return model
    End Function

    Private Shared Function BuildAreaModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Area サンプル"}

        Dim area As New AreaSeries With {.Name = "気温推移", .Color = "SteelBlue", .FillColor = "LightSkyBlue"}
        For i As Integer = 0 To 10
            area.Points.Add((CDbl(i), Math.Sin(i * 0.4) * 8 + 2))
        Next
        model.ChartGroups(0).ChartData(0).SeriesList.Add(area)

        Return model
    End Function

    Private Shared Function BuildStackedBarModel() As PlotModel
        Dim model As New PlotModel With {.Title = "StackedBar サンプル"}

        Dim stackedBar As New StackedBarSeries With {.Name = "部門別売上"}
        stackedBar.SeriesNames.Add("製品A")
        stackedBar.SeriesNames.Add("製品B")
        stackedBar.SeriesNames.Add("製品C")
        stackedBar.Items.Add(("1月", New List(Of Double) From {3, 5, 2}))
        stackedBar.Items.Add(("2月", New List(Of Double) From {4, 3, 6}))
        stackedBar.Items.Add(("3月", New List(Of Double) From {2, 7, 1}))
        model.ChartGroups(0).ChartData(0).SeriesList.Add(stackedBar)

        Return model
    End Function

    Private Shared Function BuildRangeModel() As PlotModel
        Dim model As New PlotModel With {.Title = "Range サンプル"}

        Dim range As New RangeSeries With {.Name = "予測範囲", .Color = "SteelBlue", .FillColor = "LightSkyBlue"}
        For i As Integer = 0 To 10
            Dim center = Math.Sin(i * 0.4) * 8
            range.Points.Add((CDbl(i), center - 2, center + 2))
        Next
        model.ChartGroups(0).ChartData(0).SeriesList.Add(range)

        Return model
    End Function
End Class
