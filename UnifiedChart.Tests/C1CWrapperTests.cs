using System.Drawing;
using System.IO;
using UnifiedChart.C1CWrapper;

namespace UnifiedChart.Tests;

public class C1CWrapperTests
{
    [Fact]
    public void PropBagの対応済み別名コンテナーを未対応警告にしない()
    {
        var logPath = UnifiedChart.ChartLogger.LogFilePath;
        File.Delete(logPath);

        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis>
                      <ValueLabelCollection><ValueLabel Value="1" Text="V" /></ValueLabelCollection>
                      <AlarmZoneCollection><AlarmZone From="0" To="1" /></AlarmZoneCollection>
                      <GridLineCollection><GridLine Value="1" /></GridLineCollection>
                      <MarkerCollection><Marker Value="1" /></MarkerCollection>
                    </Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        var log = File.ReadAllText(logPath);
        Assert.DoesNotContain("未対応要素: <ValueLabelCollection>", log);
        Assert.DoesNotContain("未対応要素: <AlarmZoneCollection>", log);
        Assert.DoesNotContain("未対応要素: <GridLineCollection>", log);
        Assert.DoesNotContain("未対応要素: <MarkerCollection>", log);
        Assert.Single(control.ChartArea.AxisX.ValueLabels);
        Assert.Single(control.ChartArea.AxisX.AlarmZones);
        Assert.Single(control.ChartArea.AxisX.GridLines);
        Assert.Single(control.ChartArea.AxisX.Markers);
    }

    [Fact]
    public void PropBagの日時X軸設定を共有モデルへ反映する()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis IsDateTime="True" DateTimeFormat="yyyy-MM-dd" />
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.True(control.PlotView.Model!.XAxis.IsDateTime);
        Assert.Equal("yyyy-MM-dd", control.PlotView.Model.XAxis.DateTimeFormat);
    }

    [Fact]
    public void PropBagの未知ChartTypeはLineへフォールバックしない()
    {
        var exception = Assert.Throws<FormatException>(() => new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup ChartType="UnknownChartType" />
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        });

        Assert.Contains("UnknownChartType", exception.Message);
    }

    [Fact]
    public void Chart2DPropBag形式のネストしたDataSerializerを読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup Name="Group1" ChartType="Radar">
                      <DataSerializer DefaultSet="True">
                        <DataSerializer>
                          <DataSeriesCollection>
                            <DataSeries>
                              <StyleData Color="LightPink" />
                              <SymbolStyle Color="LightPink" OutlineColor="DeepPink" Shape="Dot" />
                              <SeriesLabel>series 0</SeriesLabel>
                              <X>1 2 3 4 5</X>
                              <Y>26 22 19 24 25</Y>
                            </DataSeries>
                            <DataSeriesSerializer>
                              <LineStyle Color="LightBlue" />
                              <SymbolStyle Color="LightBlue" OutlineColor="DarkBlue" Shape="Dot" />
                              <SeriesLabel>series 1</SeriesLabel>
                              <X>1 2 3 4 5</X>
                              <Y>8 12 16 12 15</Y>
                            </DataSeriesSerializer>
                          </DataSeriesCollection>
                        </DataSerializer>
                      </DataSerializer>
                    </ChartGroup>
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        };

        var group = Assert.Single(control.ChartArea.ChartGroups);
        Assert.Equal(Chart2DType.Radar, group.ChartType);
        Assert.Equal(2, group.ChartData[0].SeriesList.Count);
        Assert.Equal(5, group.ChartData[0][0].Count);
        Assert.Equal(25, group.ChartData[0][0].Y[4]);
        Assert.Equal("LightBlue", group.ChartData[0][1].Color);

        control.RefreshChart();

        var radarSeries = control.PlotView.Model!.GetAllSeries().Cast<UnifiedChart.RadarSeries>().ToArray();
        Assert.Equal(2, radarSeries.Length);
        Assert.All(radarSeries, series => Assert.True(series.ShowMarkers));
        Assert.All(radarSeries, series => Assert.Equal(5, series.Points.Count));
        Assert.Equal("LightPink", radarSeries[0].Color);
        Assert.Equal("LightBlue", radarSeries[1].Color);
        Assert.Equal("DeepPink", radarSeries[0].MarkerOutlineColor);
        Assert.Equal("DarkBlue", radarSeries[1].MarkerOutlineColor);
        Assert.Equal(UnifiedChart.MarkerShape.Dot, radarSeries[0].MarkerShape);
    }

    [Fact]
    public void C1CWrapperのRadarカテゴリラベルを共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Radar;
        var series = group.ChartData[0].AddNewSeries();
        series.Add(1, 8);
        series.Add(2, 6);
        series.Add(3, 9);
        series.CategoryLabels.AddRange(["売上", "品質", "納期"]);

        control.RefreshChart();

        var radar = Assert.IsType<UnifiedChart.RadarSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(["売上", "品質", "納期"], radar.CategoryLabels);
    }

    [Fact]
    public void C1CWrapperのRadar色はLineと同じ優先順位で共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Radar;
        var series = group.ChartData[0].AddNewSeries();
        series.Add(0, 8);
        series.Add(1, 6);
        series.Add(2, 9);
        series.Color = "Black";
        series.FillStyle.OutlineColor = Color.Red;
        series.FillStyle.Color1 = Color.Blue;
        series.SymbolStyle.Color = Color.Green;

        control.RefreshChart();

        var radar = Assert.IsType<UnifiedChart.RadarSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal("Red", radar.Color);
        Assert.Equal("Red", radar.FillColor);
        Assert.Equal("Red", radar.MarkerOutlineColor);
    }

    [Fact]
    public void C1CWrapperのRadar塗りつぶし設定を共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Radar;
        var series = group.ChartData[0].AddNewSeries();
        series.FillArea = true;
        series.Add(0, 8);
        series.Add(1, 6);
        series.Add(2, 9);

        control.RefreshChart();

        var radar = Assert.IsType<UnifiedChart.RadarSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.True(radar.FillArea);
    }

    [Fact]
    public void C1CWrapperのSymbolStyleサイズをRadarへ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Radar;
        var series = group.ChartData[0].AddNewSeries();
        series.SymbolStyle.Size = 6;
        series.Add(0, 8);
        series.Add(1, 6);
        series.Add(2, 9);

        control.RefreshChart();

        var radar = Assert.IsType<UnifiedChart.RadarSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(6, radar.MarkerSize);
        Assert.Equal(6, series.MarkerSize);
    }

    [Fact]
    public void PropBag_実データ系列とスタイルを読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup Name="Group1">
                      <DataSerializer DefaultSet="True">
                        <DataSeriesCollection>
                          <DataSeriesSerializer>
                            <LineStyle Color="LightBlue" />
                            <SymbolStyle Color="LightBlue" OutlineColor="DarkBlue" Shape="Dot" />
                            <SeriesLabel>series 1</SeriesLabel>
                            <X>1;2;3;4;5</X>
                            <Y>6;12;10;12;15</Y>
                            <DataTypes>Single;Single;Double;Double;Double</DataTypes>
                            <FillStyle />
                            <Histogram />
                          </DataSeriesSerializer>
                        </DataSeriesCollection>
                      </DataSerializer>
                    </ChartGroup>
                    <ChartGroup Name="Group2">
                      <DataSerializer><Highlight /></DataSerializer>
                    </ChartGroup>
                  </ChartGroupsCollection>
                  <Header Compass="North" Visible="False"><Text>Header text</Text></Header>
                  <Footer Compass="South" Visible="False"><Text>Footer text</Text></Footer>
                </Chart2DPropBag>
                """
        };

        var group = control.ChartArea.ChartGroups[0];
        var series = Assert.Single(group.ChartData[0].SeriesList);
        Assert.Equal("series 1", series.Label);
        Assert.Equal(5, series.Count);
        Assert.Equal("LightBlue", series.Color);
        Assert.Equal(UnifiedChart.MarkerShape.Dot, series.MarkerShape);
        Assert.Equal("Footer text", control.ChartArea.Footer.Text);
        Assert.False(control.ChartArea.Footer.Visible);
        Assert.Equal(2, control.ChartArea.ChartGroups.Count);
    }

    [Fact]
    public void PropBagのDataTypeとDataTypesを系列へ適用する()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup>
                      <DataSerializer>
                        <DataSeriesCollection>
                          <DataSeriesSerializer DataType="Single">
                            <X>1.23456789;2.3456789</X>
                            <Y>3.456789;4.567891</Y>
                          </DataSeriesSerializer>
                          <DataSeriesSerializer>
                            <X>5;6</X>
                            <Y>7;8</Y>
                            <DataTypes>Int32;Single</DataTypes>
                          </DataSeriesSerializer>
                        </DataSeriesCollection>
                      </DataSerializer>
                    </ChartGroup>
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        };

        var series = control.ChartArea.ChartGroups[0].ChartData[0].SeriesList;
        Assert.Equal(2, series.Count);
        Assert.Equal("Single", series[0].DataType);
        Assert.Equal((float)1.23456789, (float)series[0].X[0]);
        Assert.Equal((float)4.567891, (float)series[0].Y[1]);
        Assert.Equal(new[] { "Int32", "Single" }, series[1].DataTypes);
    }

    [Fact]
    public void PropBagのHeaderVisibleFalseはタイトルを非表示にする()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Header Visible="False" Compass="North"><Text>非表示タイトル</Text></Header>
                </Chart2DPropBag>
                """
        };

        Assert.False(control.ChartArea.Header.Visible);
        Assert.Equal("非表示タイトル", control.ChartArea.Header.Text);

        control.RefreshChart();

        Assert.Empty(control.PlotView.Model!.Title);
    }

    [Fact]
    public void SampleForm相当のPropBag値を共有モデルへ反映する()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Header Compass="North"><Text>ヘッダー</Text></Header>
                  <Footer Compass="South"><Text>フッター</Text></Footer>
                  <Legend Compass="East" Visible="True" />
                  <Axes>
                    <Axis AutoMin="False" Min="0" AutoMax="False" Max="10" UnitMajor="1">
                      <GridMajor Visible="True" Spacing="1" />
                      <Markers><Marker Value="4" Text="注記" /></Markers>
                    </Axis>
                  </Axes>
                  <ChartGroupsCollection>
                    <ChartGroup Name="SampleGroup" ChartType="LineSymbols">
                      <DataSerializer Hole="100" DefaultSet="True" />
                    </ChartGroup>
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        };

        var model = control.PlotView.Model!;
        Assert.Equal("ヘッダー", model.Title);
        Assert.Equal("フッター", model.Footer);
        Assert.True(model.ShowFooter);
        Assert.Equal(UnifiedChart.LegendPosition.TopRight, model.LegendPosition);
        Assert.Equal(1, model.XAxis.MajorUnit);
        Assert.Equal(100, control.ChartArea.ChartGroups[0].MissingValueHole);
    }

    [Fact]
    public void PropBagのChartAreaレイアウトをChartControlへ反映する()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartArea LocationDefault="12, 24" SizeDefault="640, 480" />
                </Chart2DPropBag>
                """
        };

        Assert.Equal(new Point(12, 24), control.Location);
        Assert.Equal(new Size(640, 480), control.Size);
        Assert.Equal(new Point(12, 24), control.ChartArea.LocationDefault);
        Assert.Equal(new Size(640, 480), control.ChartArea.SizeDefault);

        control.Location = new Point(80, 90);
        control.Size = new Size(320, 240);
        control.RefreshChart();

        Assert.Equal(new Point(80, 90), control.Location);
        Assert.Equal(new Size(320, 240), control.Size);
    }

    [Fact]
    public void AnnotationMethodEnum_ValueLabelsは共有軸のValueLabels表示へ変換される()
    {
        using var control = new ChartControl();

        control.ChartArea.AxisX.AnnoMethod = AnnotationMethodEnum.ValueLabels;

        Assert.Equal(AnnotationMethodEnum.ValueLabels, control.ChartArea.AxisX.AnnoMethod);
        Assert.Equal(UnifiedChart.AxisLabelMode.ValueLabels, control.ChartArea.AxisX.LabelMode);
        Assert.True(control.ChartArea.AxisX.ChartValueLabelsEnabled);
    }

    [Fact]
    public void AnnotationMethodEnum_Noneは共有軸の標準表示へ戻る()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.AnnoMethod = AnnotationMethodEnum.ValueLabels;

        control.ChartArea.AxisX.AnnoMethod = AnnotationMethodEnum.None;

        Assert.Equal(UnifiedChart.AxisLabelMode.Standard, control.ChartArea.AxisX.LabelMode);
        Assert.False(control.ChartArea.AxisX.ChartValueLabelsEnabled);
    }

    [Fact]
    public void ChartDataSeries_PointDataLengthは対応するXとYの点数を返す()
    {
        var series = new ChartDataSeries();

        Assert.Equal(0, series.PointData.Length);

        series.X.AddRange([1, 2, 3]);
        series.Y.AddRange([10, 20]);
        Assert.Equal(2, series.PointData.Length);
        Assert.Equal(series.Count, series.PointData.Length);

        series.Add(4, 40);
        Assert.Equal(3, series.PointData.Length);

        series.Clear();
        Assert.Equal(0, series.PointData.Length);
    }

    [Fact]
    public void ChartDataSeries_PointDataLengthを設定すると変換する点数を制限する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Line;
        var series = group.ChartData[0].AddNewSeries();
        series.Add(0, 10);
        series.Add(1, 20);
        series.Add(2, 30);
        series.PointDataLength = 2;

        Assert.Equal(2, series.PointData.Length);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(2, line.Points.Count);
        Assert.Equal((0d, 10d), line.Points[0]);
        Assert.Equal((1d, 20d), line.Points[1]);

        series.PointDataLength = 99;
        Assert.Equal(3, series.PointData.Length);
    }

    [Fact]
    public void ChartDataSeries_PointDataのLengthへ設定すると点数を制限する()
    {
        var series = new ChartDataSeries();
        series.Add(0, 10);
        series.Add(1, 20);
        series.Add(2, 30);

        series.PointData.Length = 1;

        Assert.Equal(1, series.PointData.Length);
        Assert.Equal(1, series.Count);
    }

    [Fact]
    public void ChartDataSeries_SymbolStyleとTooltipTextを共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroup[0];
        group.ChartType = Chart2DType.LineSymbols;
        var dataSeries = group.ChartData[0].AddNewSeries();
        dataSeries.Label = "系列1";
        dataSeries.Color = "Black";
        dataSeries.SymbolStyle.Shape = UnifiedChart.MarkerShape.Diamond;
        dataSeries.SymbolStyle.Color = "Red";
        dataSeries.TooltipText = "X={0:0.0},Y={1:0.0},S={2}";
        dataSeries.Add(1, 2);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(UnifiedChart.MarkerShape.Diamond, line.MarkerShape);
        Assert.Equal("Red", line.MarkerOutlineColor);
        Assert.Equal("X={0:0.0},Y={1:0.0},S={2}", line.TooltipText);
        Assert.Equal("X=1.0,Y=2.0,S=系列1", UnifiedChart.PlotRenderHelper.FormatTooltip(line, 1, 2));
    }

    [Fact]
    public void ChartDataSeries_SymbolShapeEnumを共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.LineSymbols;
        var series = group.ChartData[0].AddNewSeries();
        series.SymbolStyle.Shape = SymbolShapeEnum.Diamond;
        series.Add(0, 1);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(UnifiedChart.MarkerShape.Diamond, line.MarkerShape);
    }

    [Theory]
    [InlineData(SymbolShapeEnum.Dot, UnifiedChart.MarkerShape.Dot)]
    [InlineData(SymbolShapeEnum.Box, UnifiedChart.MarkerShape.Box)]
    [InlineData(SymbolShapeEnum.InvertedTri, UnifiedChart.MarkerShape.InvertedTri)]
    [InlineData(SymbolShapeEnum.DiagCross, UnifiedChart.MarkerShape.DiagCross)]
    public void ChartDataSeries_追加SymbolShapeEnumを共有系列へ変換する(
        SymbolShapeEnum shape,
        UnifiedChart.MarkerShape expectedShape)
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.LineSymbols;
        var series = group.ChartData[0].AddNewSeries();
        series.SymbolStyle.Shape = shape;
        series.Add(0, 1);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(expectedShape, line.MarkerShape);
    }

    [Fact]
    public void ChartDataSeries_SymbolStyleのColor型を共有系列へ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.LineSymbols;
        var series = group.ChartData[0].AddNewSeries();
        series.SymbolStyle.Color = Color.Red;
        series.Add(0, 1);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal("Red", line.MarkerOutlineColor);
    }

    [Fact]
    public void ChartDataSeries_Displayを共有系列の表示状態へ変換する()
    {
        using var control = new ChartControl();
        var series = control.ChartGroups[0].ChartData[0].AddNewSeries();
        series.Add(0, 1);
        series.Display = SeriesDisplayEnum.Hide;

        control.RefreshChart();

        var hidden = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.False(hidden.Visible);

        series.Display = SeriesDisplayEnum.Show;
        control.RefreshChart();

        var visible = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.True(visible.Visible);
    }

    [Fact]
    public void ChartGroup_ChartDataのSeriesListは先頭ChartDataの系列一覧を公開する()
    {
        using var control = new ChartControl();
        var chartData = control.ChartGroups[0].ChartData[0];
        var series = chartData.AddNewSeries();

        Assert.Same(chartData.SeriesList, control.ChartGroups[0].ChartData.SeriesList);
        Assert.Same(series, control.ChartGroups[0].ChartData.SeriesList[0]);
    }

    [Fact]
    public void ChartDataSeries_FillStyleをAreaSeriesへ変換する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Area;
        var series = group.ChartData[0].AddNewSeries();
        series.FillStyle.Color1 = Color.LightBlue;
        series.FillStyle.OutlineColor = Color.DarkBlue;
        series.Add(0, 1);
        series.Add(1, 2);

        control.RefreshChart();

        var area = Assert.IsType<UnifiedChart.AreaSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal("LightBlue", area.FillColor);
        Assert.Equal("DarkBlue", area.Color);
    }

    [Fact]
    public void ChartControl_RefreshChart_ConvertsLineSeries()
    {
        using var control = new ChartControl();
        var area = control.ChartArea;
        area.Header.Text = "テスト";
        area.AxisX.Text = "X";
        area.AxisY.Text = "Y";
        area.ChartGroup[0].ChartType = Chart2DType.LineSymbols;

        var dataSeries = area.ChartGroup[0].ChartData[0].AddNewSeries();
        dataSeries.Label = "系列1";
        dataSeries.X.Add(1);
        dataSeries.Y.Add(2);

        Assert.Same(dataSeries, area.ChartGroup[0].ChartData[0][0]);

        control.RefreshChart();

        var model = control.PlotView.Model;
        Assert.NotNull(model);
        Assert.Equal("テスト", model!.Title);
        Assert.Equal("X", model.XAxis.Title);
        Assert.Equal("Y", model.YAxis.Title);
        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(model.GetAllSeries()));
        Assert.Equal("系列1", line.Name);
        Assert.Equal((1d, 2d), Assert.Single(line.Points));
        Assert.True(line.ShowMarkers);
    }

    [Fact]
    public void ChartControl_ChartGroupsはChartAreaと同じコレクションを公開する()
    {
        using var control = new ChartControl();

        Assert.Same(control.ChartArea.ChartGroups, control.ChartGroups);
        Assert.Same(control.ChartArea.ChartGroups[0], control.ChartGroups[0]);

        var group = control.ChartArea.AddNewChartGroup();

        Assert.Same(group, control.ChartArea.ChartGroups[1]);
    }

    [Fact]
    public void ChartControl_RefreshChart_Lineはマーカーを強制しない()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroups[0];
        group.ChartType = Chart2DType.Line;
        var series = group.ChartData[0].AddNewSeries();
        series.Add(0, 1);
        series.Add(1, 2);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.False(line.ShowMarkers);
    }

    [Fact]
    public void 線幅未指定時は既定値2で描画モデルへ反映する()
    {
        using var control = new ChartControl();
        var series = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
        series.X.Add(0);
        series.Y.Add(1);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(control.PlotView.Model!.GetAllSeries().Single());
        Assert.Equal(2.0, series.StrokeThickness);
        Assert.Equal(2.0, line.StrokeThickness);
    }

    [Fact]
    public void 欠損値をまたぐ接続設定を共有系列へ反映する()
    {
        using var control = new ChartControl();
        var series = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
        series.ConnectAcrossMissingValues = false;
        series.Add(0, 1);
        series.Add(1, 100);
        control.ChartArea.ChartGroups[0].MissingValueHole = 100;

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(control.PlotView.Model!.GetAllSeries().Single());
        Assert.False(line.ConnectAcrossMissingValues);
    }

    [Fact]
    public void 欠損値をまたぐ接続設定の既定値は有効()
    {
        using var control = new ChartControl();
        var series = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
        series.Add(0, 1);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(control.PlotView.Model!.GetAllSeries().Single());
        Assert.True(series.ConnectAcrossMissingValues);
        Assert.True(line.ConnectAcrossMissingValues);
    }

    [Fact]
    public void スクロール契約をWrapperから共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.ScrollUnit = 5;
        control.ChartArea.AxisX.ScrollMin = 0;
        control.ChartArea.AxisX.ScrollMax = 100;
        control.ChartArea.AxisX.SynchronizeWithY2 = true;

        control.RefreshChart();

        Assert.Equal(5, control.PlotView.Model!.XAxis.Scroll.Unit);
        Assert.Equal(0, control.PlotView.Model.XAxis.Scroll.Minimum);
        Assert.Equal(100, control.PlotView.Model.XAxis.Scroll.Maximum);
        Assert.True(control.PlotView.Model.XAxis.Scroll.SynchronizeWithY2);
    }

    [Fact]
    public void 表示設定をWrapperから共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.TitleFontSize = 18;
        control.ChartArea.FooterFontSize = 11;
        control.ChartArea.AxisLabelFontSize = 10;
        control.ChartArea.AxisTitleFontSize = 12;
        control.ChartArea.DataLabelFontSize = 9;
        control.ChartArea.LegendFontSize = 13;
        control.ChartArea.ValueLabelFontSize = 10;
        control.ChartArea.AlarmZoneFontSize = 9;
        control.ChartArea.PlotMarginLeft = 60;
        control.ChartArea.PlotMarginTop = 40;
        control.ChartArea.PlotMarginRight = 30;
        control.ChartArea.PlotMarginBottom = 45;

        control.RefreshChart();

        var model = control.PlotView.Model!;
        Assert.Equal(18, model.TitleFontSize);
        Assert.Equal(11, model.FooterFontSize);
        Assert.Equal(10, model.AxisLabelFontSize);
        Assert.Equal(12, model.AxisTitleFontSize);
        Assert.Equal(9, model.DataLabelFontSize);
        Assert.Equal(13, model.LegendFontSize);
        Assert.Equal(10, model.ValueLabelFontSize);
        Assert.Equal(9, model.AlarmZoneFontSize);
        Assert.Equal(60, model.PlotMarginLeft);
        Assert.Equal(40, model.PlotMarginTop);
        Assert.Equal(30, model.PlotMarginRight);
        Assert.Equal(45, model.PlotMarginBottom);
    }

    [Fact]
    public void Hole値はNaNへ変換せず欠損契約として系列へ保持する()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.ChartGroup[0];
        group.MissingValueHole = 100;
        var series = group.ChartData[0].AddNewSeries();
        series.Add(0, 1);
        series.Add(1, 100);
        series.Add(2, 3);

        control.RefreshChart();

        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(100, line.Points[1].Y);
        Assert.Equal(100, line.MissingValueHole);
        Assert.False(double.IsNaN(line.Points[1].Y));
    }

    [Fact]
    public void AxisMarkerの方向と接続線設定を共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisY.Markers.Add(new ChartAxisMarker
        {
            Value = 5,
            ArrowDirection = "East",
            ShowArrow = false,
            ShowConnectionLine = true,
            Text = "R"
        });

        control.RefreshChart();

        var marker = Assert.Single(control.PlotView.Model!.YAxis.Markers);
        Assert.Equal("East", marker.ArrowDirection);
        Assert.False(marker.ShowArrow);
        Assert.True(marker.ShowConnectionLine);
    }

    [Fact]
    public void Y2軸の専用GridLineとMarkerを共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.Y2Axis = new ChartAxis();
        control.ChartArea.Y2Axis.GridLines.Add(new ChartGridLine
        {
            Value = 25,
            Color = "Blue"
        });
        control.ChartArea.Y2Axis.Markers.Add(new ChartAxisMarker
        {
            Value = 75,
            ArrowDirection = "West",
            Text = "Y2"
        });

        control.RefreshChart();

        var y2 = control.PlotView.Model!.Y2Axis;
        Assert.NotNull(y2);
        Assert.Equal(25, Assert.Single(y2!.GridLines).Value);
        Assert.Equal("Blue", Assert.Single(y2.GridLines).Color);
        Assert.Equal("West", Assert.Single(y2.Markers).ArrowDirection);
        Assert.Equal("Y2", Assert.Single(y2.Markers).Text);
    }

    [Fact]
    public void LabelCompassAutoは共有モデルへ変換される()
    {
        using var control = new ChartControl();
        var label = control.ChartArea.ChartLabels.Add("系列A", 0, "自動");
        label.Compass = LabelCompassEnum.Auto;

        control.RefreshChart();

        Assert.True(Assert.Single(control.PlotView.Model!.ChartLabels).Auto);
    }

    [Fact]
    public void ChartValueLabelsEnabledは標準目盛表示と独立して方式を切り替える()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.ChartValueLabelsEnabled = true;
        control.ChartArea.AxisX.TickLabelsVisible = false;

        control.RefreshChart();

        Assert.Equal(UnifiedChart.AxisLabelMode.ValueLabels, control.PlotView.Model!.XAxis.LabelMode);
        Assert.True(control.PlotView.Model.XAxis.ChartValueLabelsEnabled);
        Assert.False(control.PlotView.Model.XAxis.TickLabelsVisible);
    }

    [Fact]
    public void AxisValueLabelsはChartLabelと独立して共有モデルへ変換される()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.ValueLabels.Add(new ChartValueLabel
        {
            Value = 10,
            Text = "MEAN"
        });
        control.ChartArea.AxisY.ValueLabels.Add(new ChartValueLabel
        {
            Value = 20,
            Text = "+1SD",
            Color = "Red"
        });

        control.RefreshChart();

        var xLabel = Assert.Single(control.PlotView.Model!.XAxis.ValueLabels);
        Assert.Equal(10, xLabel.Value);
        Assert.Equal("MEAN", xLabel.Text);
        var yLabel = Assert.Single(control.PlotView.Model.YAxis.ValueLabels);
        Assert.Equal("+1SD", yLabel.Text);
        Assert.Equal("Red", yLabel.Color);
    }

    [Fact]
    public void PropBagから標準値とValueLabelsの表示方式を読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis LabelMode="ValueLabels" TickLabelsVisible="False" />
                    <Axis AnnotationMethod="Standard" />
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.Equal(UnifiedChart.AxisLabelMode.ValueLabels, control.ChartArea.AxisX.LabelMode);
        Assert.False(control.ChartArea.AxisX.TickLabelsVisible);
        Assert.Equal(UnifiedChart.AxisLabelMode.Standard, control.ChartArea.AxisY.LabelMode);
    }

    [Fact]
    public void PropBag適用時にXMLと解析結果をログへ出力する()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "UnifiedChart.log");
        File.Delete(logPath);

        try
        {
            using var control = new ChartControl
            {
                PropBag = """
                    <Chart2DPropBag>
                      <ChartGroupsCollection>
                        <ChartGroup Name="実データ" ChartType="LineSymbols" />
                      </ChartGroupsCollection>
                    </Chart2DPropBag>
                    """
            };
            var series = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
            series.Label = "確認系列";
            series.Add(1, 2);
            control.RefreshChart();

            var log = File.ReadAllText(logPath);
            Assert.Contains("<Chart2DPropBag>", log);
            Assert.Contains("ChartGroup[0]: Name=\"実データ\"", log);
            Assert.Contains("Series[0]: Label=\"確認系列\"", log);
            Assert.Contains("Count=1", log);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void PropBagから軸ValueLabelsを読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis>
                      <ValueLabels>
                        <ValueLabel Value="1" Text="R" />
                        <ValueLabel Position="2" Caption="RS" Color="Blue" />
                      </ValueLabels>
                    </Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.Equal(2, control.ChartArea.AxisX.ValueLabels.Count);
        Assert.Equal("R", control.ChartArea.AxisX.ValueLabels[0].Text);
        Assert.Equal("RS", control.ChartArea.AxisX.ValueLabels[1].Text);
        Assert.Equal(2, control.ChartArea.AxisX.ValueLabels[1].Value);
    }

    [Fact]
    public void Originと目盛方向とTickLabels設定を共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.Origin = 5;
        control.ChartArea.AxisX.TickDirection = UnifiedChart.AxisTickDirection.Cross;
        control.ChartArea.AxisX.TickLabelsVisible = false;
        control.ChartArea.AxisY.Origin = 2;
        control.ChartArea.AxisY.TickDirection = UnifiedChart.AxisTickDirection.Inward;

        control.RefreshChart();

        Assert.Equal(5, control.PlotView.Model!.XAxis.Origin);
        Assert.Equal(UnifiedChart.AxisTickDirection.Cross, control.PlotView.Model.XAxis.TickDirection);
        Assert.False(control.PlotView.Model.XAxis.TickLabelsVisible);
        Assert.Equal(2, control.PlotView.Model.YAxis.Origin);
        Assert.Equal(UnifiedChart.AxisTickDirection.Inward, control.PlotView.Model.YAxis.TickDirection);
    }

    [Fact]
    public void PropBagからOrigin目盛方向TickLabels表示を読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis Origin="5" TickDirection="Cross" TickLabelsVisible="False" />
                    <Axis OriginValue="2"><TickLabels Visible="False" /></Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.Equal(5, control.ChartArea.AxisX.Origin);
        Assert.Equal(UnifiedChart.AxisTickDirection.Cross, control.ChartArea.AxisX.TickDirection);
        Assert.False(control.ChartArea.AxisX.TickLabelsVisible);
        Assert.Equal(2, control.ChartArea.AxisY.Origin);
        Assert.False(control.ChartArea.AxisY.TickLabelsVisible);
    }

    [Fact]
    public void WrapperのMinMaxと表示範囲スクロールを利用できる()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.Min = 0;
        control.ChartArea.AxisX.Max = 10;
        control.ChartArea.AxisX.ShowScrollButtons = true;
        control.ChartArea.ChartGroup[0].ChartData[0].AddNewSeries().X.AddRange(new[] { 0d, 10d });
        control.ChartArea.ChartGroup[0].ChartData[0][0].Y.AddRange(new[] { 0d, 10d });

        control.RefreshChart();
        control.SetDisplayRange(new UnifiedChart.AxisRange(2, 6), new UnifiedChart.AxisRange(2, 6));
        control.ScrollX(1);

        Assert.Equal(2.8, control.DisplayXRange!.Value.Min, 10);
        Assert.Equal(6.8, control.DisplayXRange!.Value.Max, 10);
        Assert.Equal(0d, control.PlotView.Model!.XAxis.Minimum);
        Assert.Equal(10d, control.PlotView.Model.XAxis.Maximum);
    }

    [Fact]
    public void PropBag_設定とChartGroupを読み込むが系列データは生成しない()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <?xml version="1.0"?>
                <Chart2DPropBag Version="2.0.20082.18078">
                  <ChartGroupsCollection>
                    <ChartGroup Name="Group1" User3D="False">
                      <DataSerializer Hole="100" DefaultSet="True"><Highlight /></DataSerializer>
                    </ChartGroup>
                    <ChartGroup Name="Group2">
                      <DataSerializer><Highlight /></DataSerializer>
                    </ChartGroup>
                  </ChartGroupsCollection>
                  <Header Compass="West"><Text>R</Text></Header>
                  <Legend Compass="East" Visible="False" />
                  <Axes>
                    <Axis Max="5" Min="1" UnitMajor="1" UnitMinor="0.5" AutoMajor="True" AutoMinor="True" AutoMax="True" AutoMin="True" Compass="South">
                      <Text>Axis X</Text><GridMajor Visible="True" Spacing="1" />
                    </Axis>
                    <Axis Max="26" Min="18" UnitMajor="2" UnitMinor="1" AutoMajor="True" AutoMinor="True" AutoMax="True" AutoMin="True" Compass="West">
                      <Text>Axis Y</Text><GridMajor Visible="True" Spacing="2" />
                    </Axis>
                    <Axis Max="0" Min="0" AutoMajor="True" AutoMinor="True" AutoMax="True" AutoMin="True" Compass="East">
                      <SB Buttons="ScrollButtons" />
                    </Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.Equal("R", control.ChartArea.Header.Text);
        Assert.False(control.ChartArea.Legend.Visible);
        Assert.Null(control.ChartArea.AxisX.Min);
        Assert.Null(control.ChartArea.AxisX.Max);
        Assert.Equal(1d, control.ChartArea.AxisX.UnitMajor);
        Assert.Equal(0.5d, control.ChartArea.AxisX.UnitMinor);
        Assert.Equal("Axis Y", control.ChartArea.AxisY.Text);
        Assert.Equal(2d, control.ChartArea.AxisY.UnitMajor);
        Assert.Equal(2, control.ChartArea.ChartGroups.Count);
        Assert.Equal("Group1", control.ChartArea.ChartGroups[0].Name);
        Assert.Equal(100d, control.ChartArea.ChartGroups[0].MissingValueHole);
        Assert.True(control.ChartArea.ChartGroups[0].DefaultDataSerializer);
        Assert.False(control.ChartArea.ChartGroups[1].UseSecondaryYAxis);
        Assert.True(control.ChartArea.Y2Axis!.ShowScrollButtons);
        Assert.Empty(control.ChartArea.ChartGroups[0].ChartData[0].SeriesList);
        Assert.Empty(control.PlotView.Model!.GetAllSeries());
    }

    [Fact]
    public void PropBagの既定系列スタイルとWestHeaderはC1互換になる()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Header Compass="West"><Text>非表示Header</Text></Header>
                  <ChartGroupsCollection>
                    <ChartGroup ChartType="LineSymbols" />
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        };
        var series = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
        series.Add(0, 1);
        control.RefreshChart();

        var model = control.PlotView.Model!;
        Assert.Equal("非表示Header", control.ChartArea.Header.Text);
        Assert.Empty(model.Title);
        var line = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(model.GetAllSeries()));
        Assert.Equal("SteelBlue", line.Color);
        Assert.Equal(UnifiedChart.MarkerShape.Square, line.MarkerShape);
    }

    [Fact]
    public void MinorUnit_副目盛りを生成する()
    {
        var axis = new UnifiedChart.Axis { MajorUnit = 1, MinorUnit = 0.5 };

        var ticks = UnifiedChart.AxisTickHelper.GetMinorTicks(new UnifiedChart.AxisRange(0, 2), axis);

        Assert.Equal(new[] { 0.5, 1.5 }, ticks);
    }

    [Fact]
    public void PropBag_旧形式の単数ChartGroupCollectionも読み込める()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupCollection>
                    <ChartGroup Name="Legacy" />
                  </ChartGroupCollection>
                </Chart2DPropBag>
                """
        };

        var group = Assert.Single(control.ChartArea.ChartGroups);
        Assert.Equal("Legacy", group.Name);
    }

    [Fact]
    public void PropBag_空のChartGroupsCollectionでも既定ChartGroupを維持する()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection />
                </Chart2DPropBag>
                """
        };

        var group = Assert.Single(control.ChartArea.ChartGroups);
        Assert.Empty(group.ChartData[0].SeriesList);
        Assert.Empty(control.PlotView.Model!.GetAllSeries());
    }

    [Fact]
    public void PropBagからChartGroupの種類を読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup Name="Area" ChartType="Area" />
                    <ChartGroup Name="Scatter" Type="XYScatter" />
                  </ChartGroupsCollection>
                </Chart2DPropBag>
                """
        };

        Assert.Equal(UnifiedChart.C1CWrapper.Chart2DType.Area, control.ChartArea[0].ChartType);
        Assert.Equal(UnifiedChart.C1CWrapper.Chart2DType.XYScatter, control.ChartArea[1].ChartType);
    }

    [Fact]
    public void PropBag設定が共有モデルへ反映される()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Header Compass="East"><Text>右寄せ</Text></Header>
                  <Axes>
                    <Axis><GridMajor Visible="False" Spacing="2" /></Axis>
                    <Axis ChartValueLabelsEnabled="True" />
                  </Axes>
                </Chart2DPropBag>
                """
        };

        var model = control.PlotView.Model!;
        Assert.Equal("右寄せ", model.Title);
        Assert.Equal("East", model.TitleCompass);
        Assert.False(model.XAxis.GridMajorVisible);
        Assert.Equal(2, model.XAxis.GridMajorSpacing);
        Assert.Equal(UnifiedChart.AxisLabelMode.ValueLabels, model.YAxis.LabelMode);
    }

    [Fact]
    public void 空のChartGroupと後続の実データグループは第2Y軸へ割り当てられる()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <ChartGroupsCollection>
                    <ChartGroup Name="Empty" />
                    <ChartGroup Name="Primary" />
                    <ChartGroup Name="Secondary" />
                  </ChartGroupsCollection>
                  <Axes>
                    <Axis />
                    <Axis />
                    <Axis />
                  </Axes>
                </Chart2DPropBag>
                """
        };

        var primary = control.ChartArea[1].ChartData[0].AddNewSeries();
        primary.Add(1, 10);
        var secondary = control.ChartArea[2].ChartData[0].AddNewSeries();
        secondary.Add(1, 100);
        control.ChartArea.AutoAssignSecondaryYAxis = true;

        control.RefreshChart();

        var series = control.PlotView.Model!.GetAllSeries().ToArray();
        Assert.False(series[0].UseSecondaryYAxis);
        Assert.True(series[1].UseSecondaryYAxis);
        Assert.NotNull(control.PlotView.Model!.Y2Axis);
    }

    [Fact]
    public void Y2軸未設定の複数ChartGroupは後続系列も主Y軸へ描画する()
    {
        using var control = new ChartControl();
        var primary = control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries();
        primary.Label = "主系列";
        primary.Add(0, 12);
        primary.Add(1, 8);

        var secondaryGroup = control.ChartArea.AddNewChartGroup();
        secondaryGroup.ChartType = Chart2DType.Line;
        var secondary = secondaryGroup.ChartData[0].AddNewSeries();
        secondary.Label = "第2系列";
        secondary.Add(0, 5);
        secondary.Add(1, 4);

        control.RefreshChart();

        var series = control.PlotView.Model!.GetAllSeries().ToArray();
        Assert.Equal(2, series.Length);
        Assert.False(series[0].UseSecondaryYAxis);
        Assert.False(series[1].UseSecondaryYAxis);
        Assert.Null(control.PlotView.Model!.Y2Axis);
    }

    [Fact]
    public void 明示Y2軸がない場合は第二系列を自動第2Y軸範囲で描画する()
    {
        using var control = new ChartControl();
        control.ChartArea.AutoAssignSecondaryYAxis = true;
        control.ChartArea.AxisY.Min = -12;
        control.ChartArea.AxisY.Max = 12;

        control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries().Add(0, 12);
        var secondaryGroup = control.ChartArea.AddNewChartGroup();
        var secondarySeries = secondaryGroup.ChartData[0].AddNewSeries();
        secondarySeries.Add(0, 5);
        secondarySeries.Add(1, -4.95);

        control.RefreshChart();

        var model = control.PlotView.Model;
        Assert.NotNull(model);
        var transform = PlotRenderHelper.CreateTransform(model, new PlotRect(0, 0, 100, 100));
        Assert.True(transform.HasSecondaryYAxis);
        Assert.Equal(-12, transform.YRange.Min);
        Assert.Equal(12, transform.YRange.Max);
        Assert.Equal(-5, transform.Y2Range.Min);
        Assert.Equal(5, transform.Y2Range.Max);
        var y2Axis = Assert.IsType<UnifiedChart.Axis>(model.Y2Axis);
        Assert.Null(y2Axis.MajorUnit);
        Assert.Null(y2Axis.GridMajorSpacing);

        var ticks = AxisTickHelper.GetTicks(transform.Y2Range, y2Axis, 10);
        Assert.Equal(11, ticks.Count);
        Assert.Equal(-5, ticks[0]);
        Assert.Equal(5, ticks[^1]);
        Assert.All(ticks.Zip(ticks.Skip(1)), pair => Assert.Equal(1, pair.Second - pair.First));
    }

    [Fact]
    public void AutoAssignSecondaryYAxisを無効にすると従来どおり主Y軸へ描画する()
    {
        using var control = new ChartControl();
        control.ChartArea.AutoAssignSecondaryYAxis = false;
        control.ChartArea.AxisY.Min = -12;
        control.ChartArea.AxisY.Max = 12;

        control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries().Add(0, 12);
        var secondaryGroup = control.ChartArea.AddNewChartGroup();
        secondaryGroup.ChartData[0].AddNewSeries().Add(0, 5);

        control.RefreshChart();

        var series = control.PlotView.Model!.GetAllSeries().ToArray();
        Assert.False(series[1].UseSecondaryYAxis);
        Assert.Null(control.PlotView.Model.Y2Axis);
    }

    [Fact]
    public void EnableSecondaryYAxisで指定したChartGroupを第2Y軸へ割り当てる()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisY.Min = -12;
        control.ChartArea.AxisY.Max = 12;

        control.ChartArea.ChartGroups[0].ChartData[0].AddNewSeries().Add(0, 12);
        var secondaryGroup = control.ChartArea.AddNewChartGroup();
        secondaryGroup.ChartData[0].AddNewSeries().Add(0, 5);
        var secondaryAxis = control.ChartArea.EnableSecondaryYAxis(secondaryGroup);
        secondaryAxis.Min = -5;
        secondaryAxis.Max = 5;
        secondaryAxis.UnitMajor = 1;

        control.RefreshChart();

        var series = control.PlotView.Model!.GetAllSeries().ToArray();
        Assert.False(series[0].UseSecondaryYAxis);
        Assert.True(series[1].UseSecondaryYAxis);
        Assert.NotNull(control.PlotView.Model.Y2Axis);
        Assert.Equal(-5, control.PlotView.Model.Y2Axis!.Minimum);
        Assert.Equal(5, control.PlotView.Model.Y2Axis.Maximum);

        var transform = PlotRenderHelper.CreateTransform(control.PlotView.Model, new PlotRect(0, 0, 100, 100));
        Assert.True(transform.HasSecondaryYAxis);
        Assert.Equal(5, transform.ToDataY2(transform.ToScreenY2(5)));
        Assert.Equal(12, transform.YRange.Max);
    }

    [Fact]
    public void ChartLabelsはSeriesNameとDataIndexでデータ点に結合される()
    {
        using var control = new ChartControl();
        var series = control.ChartArea[0].ChartData[0].AddNewSeries();
        series.Label = "系列A";
        series.Add(10, 100);
        series.Add(20, 200);

        var label = control.ChartArea.ChartLabels.Add("系列A", 1, "注記");
        label.Auto = false;
        label.ShowConnectionLine = false;
        label.OffsetX = 12;
        label.OffsetY = -16;

        control.RefreshChart();

        var modelLabel = Assert.Single(control.PlotView.Model!.ChartLabels);
        Assert.True(modelLabel.IsResolved);
        Assert.Equal(20, modelLabel.X);
        Assert.Equal(200, modelLabel.Y);
        Assert.False(modelLabel.Auto);
        Assert.False(modelLabel.ShowConnectionLine);
        Assert.Equal(12, modelLabel.OffsetX);
        Assert.Equal(-16, modelLabel.OffsetY);
    }

    [Fact]
    public void ChartLabelsは範囲外のDataIndexを未解決として扱う()
    {
        using var control = new ChartControl();
        var series = control.ChartArea[0].ChartData[0].AddNewSeries();
        series.Label = "系列A";
        series.Add(10, 100);

        control.ChartArea.ChartLabels.Add("系列A", 1, "未解決");
        control.RefreshChart();

        Assert.False(Assert.Single(control.PlotView.Model!.ChartLabels).IsResolved);
    }

    [Fact]
    public void 軸マーカーと専用グリッド線を共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisX.Markers.Add(new ChartAxisMarker
        {
            Value = 2,
            Text = "X注記",
            Color = "Red",
            ShowConnectionLine = false
        });
        control.ChartArea.AxisY.GridLines.Add(new ChartGridLine
        {
            Value = 5,
            Color = "Blue",
            DashStyle = "Dot",
            Thickness = 2,
            Visible = true
        });

        control.RefreshChart();

        var model = control.PlotView.Model!;
        var marker = Assert.Single(model.XAxis.Markers);
        Assert.Equal(2, marker.Value);
        Assert.Equal("X注記", marker.Text);
        Assert.False(marker.ShowConnectionLine);
        var line = Assert.Single(model.YAxis.GridLines);
        Assert.Equal(5, line.Value);
        Assert.Equal("Blue", line.Color);
        Assert.Equal("Dot", line.DashStyle);
        Assert.Equal(2, line.Thickness);
    }

    [Fact]
    public void PropBagから軸マーカーと専用グリッド線を読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis>
                      <GridLines><GridLine Value="3" Color="Green" DashStyle="Dot" Thickness="2" /></GridLines>
                      <Markers><Marker Value="4" Color="Red" ShowArrow="True" ShowConnectionLine="False"><Text>Marker</Text></Marker></Markers>
                    </Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        Assert.Equal(3, control.ChartArea.AxisX.GridLines.Single().Value);
        Assert.Equal("Green", control.ChartArea.AxisX.GridLines.Single().Color);
        Assert.Equal("Marker", control.ChartArea.AxisX.Markers.Single().Text);
        Assert.False(control.ChartArea.AxisX.Markers.Single().ShowConnectionLine);
    }

    [Fact]
    public void AlarmZoneを共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisY.AlarmZones.Add(new ChartAlarmZone
        {
            From = 10,
            To = 20,
            FillColor = "Orange",
            Opacity = 96,
            BorderColor = "Red",
            BorderThickness = 2,
            BorderDashStyle = "Dash",
            Text = "警戒"
        });

        control.RefreshChart();

        var zone = Assert.Single(control.PlotView.Model!.YAxis.AlarmZones);
        Assert.Equal(10, zone.From);
        Assert.Equal(20, zone.To);
        Assert.Equal("Orange", zone.FillColor);
        Assert.Equal((byte)96, zone.Opacity);
        Assert.Equal("Red", zone.BorderColor);
        Assert.Equal(2, zone.BorderThickness);
        Assert.Equal("Dash", zone.BorderDashStyle);
        Assert.Equal("警戒", zone.Text);
    }

    [Fact]
    public void PropBagからAlarmZoneを読み込む()
    {
        using var control = new ChartControl
        {
            PropBag = """
                <Chart2DPropBag>
                  <Axes>
                    <Axis>
                      <AlarmZones>
                        <AlarmZone Min="1" Max="5" Color="Gold" Opacity="80" LineColor="Red" LineWidth="2" LineStyle="Dot" Visible="True"><Text>注意</Text></AlarmZone>
                      </AlarmZones>
                    </Axis>
                  </Axes>
                </Chart2DPropBag>
                """
        };

        var zone = Assert.Single(control.ChartArea.AxisX.AlarmZones);
        Assert.Equal(1, zone.From);
        Assert.Equal(5, zone.To);
        Assert.Equal("Gold", zone.FillColor);
        Assert.Equal((byte)80, zone.Opacity);
        Assert.Equal("Red", zone.BorderColor);
        Assert.Equal("注意", zone.Text);
    }

    [Fact]
    public void AlarmZoneの内側外側モードを共有モデルへ変換する()
    {
        using var control = new ChartControl();
        control.ChartArea.AxisY.AlarmZones.Add(new ChartAlarmZone
        {
            Mode = UnifiedChart.AlarmZoneMode.Outside,
            LowerLimit = 10,
            UpperLimit = 90
        });

        control.RefreshChart();

        var zone = Assert.Single(control.PlotView.Model!.YAxis.AlarmZones);
        Assert.Equal(UnifiedChart.AlarmZoneMode.Outside, zone.Mode);
        Assert.Equal(10, zone.LowerLimit);
        Assert.Equal(90, zone.UpperLimit);
    }

    [Fact]
    public void AlarmZoneResolverは外側を上下2帯へ展開する()
    {
        var zone = new UnifiedChart.AxisAlarmZone
        {
            Mode = UnifiedChart.AlarmZoneMode.Outside,
            LowerLimit = 10,
            UpperLimit = 90
        };

        var ranges = UnifiedChart.AxisAlarmZoneResolver.Resolve(zone, 0, 100).ToArray();

        Assert.Equal(new[] { (0d, 10d), (90d, 100d) }, ranges);
    }

    [Fact]
    public void C1Chart風の階層APIで系列データを追加できる()
    {
        using var control = new ChartControl();
        var group = control.ChartArea.AddNewChartGroup();
        group.Name = "移行グループ";
        group.ChartType = Chart2DType.Line;
        var data = group.AddNewChartData();
        var series = data.AddNewSeries();

        series.Label = "系列A";
        series.Add(1, 10);
        series.Add(2, 20);

        Assert.Same(group, control.ChartArea[1]);
        Assert.Same(data, group[1]);
        Assert.Same(series, data.FindByLabel("系列A"));
        Assert.Equal(2, series.Count);

        control.RefreshChart();

        var unifiedSeries = Assert.IsType<UnifiedChart.LineSeries>(Assert.Single(control.PlotView.Model!.GetAllSeries()));
        Assert.Equal(new[] { (1d, 10d), (2d, 20d) }, unifiedSeries.Points);
    }
}
