# C1C → UnifiedChart 移行マッピング表

`C1C.2.dll` を使用したWinFormsプロジェクトをUnifiedChartへ移行するためのAPI対応表です。本表はC1Chart製品全体の全APIではなく、本リポジトリで確認できるC1Chart利用箇所、`UnifiedChart.C1CWrapper`の互換API、共有Core、WinForms/WPF描画機能を網羅します。
`UnifiedChart.C1CWrapper` は `ChartControl`、`ChartArea`、`ChartAxis`、`ChartGroup`、`ChartDataSeries` 等を提供し、既存の階層APIを保った移行を支援します。`Chart2DPropBag` の設定読み込みにも対応しています。
また、現在は Xbar-R管理図、Cu-Sum管理図、ツインプロット図、時系列、TREND管理図、X管理図などの品質管理(QC)向けチャートで使用されているようです。これらは折れ線・散布図・注釈・複数系列グループ表示などの組み合わせで構成されるケースが多いため、移行時は各図表固有のロジック(管理限界線の描画、群分けなど)がどの C1C API に依存しているかを個別に確認する必要があります。
それ以外の項目は C1C の一般的な API 構成を前提とした標準的なマッピングとして作成しています。
実プロジェクトでの使用箇所がさらに確認できた際は、本ドキュメントの内容を実際の使用箇所に合わせて更新してください。

> 対象は 2D の `C1C` のみです。`C1C3D` は UnifiedChart が3Dグラフに対応していないため、本マッピングの対象外です。

## 対応状況の定義

| 表記 | 意味 |
|---|---|
| 対応 | C1Chartの基本動作をUnifiedChartで実行可能 |
| Wrapper対応 | `UnifiedChart.C1CWrapper`でC1Chartの階層/API形状を維持 |
| 部分対応 | 主要動作は対応するが、配置・装飾・値の種類などに制約あり |
| Core対応 | C1Chart互換名ではなく、共有Core APIとして利用可能 |
| 未対応 | 対応APIまたは描画機能が未実装 |

## 1. コントロール本体

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `C1C.C1C`(使用確認済み) | `UnifiedChart.WinForms.PlotView` または `UnifiedChart.C1CWrapper.ChartControl` | Wrapper対応。既存コードの書き換え量を最小限にする場合は`ChartControl`、完全移行時は`PlotView` |
| `chart.Header.Text` / `Header.Compass` | `ChartControl.Header.Text` / `ChartControl.Header.Compass` または `ChartControl.ChartArea.Header` / `PlotModel.Title` / `TitleCompass` | Wrapper対応。`ChartControl.Header`は`ChartArea.Header`への互換エイリアス。North/South/West/Eastのタイトル位置に対応 |
| `chart.ChartArea` | `ChartControl.ChartArea` / `PlotModel` + `PlotView.Model` | Wrapper対応。プロットエリア全体の設定 |
| `ChartArea.LocationDefault` / `SizeDefault` | `ChartControl.Location` / `Size`、`ChartArea.LocationDefault` / `SizeDefault` | Wrapper対応。PropBagから`x,y`および`width,height`を読み込み、WinFormsのコントロールレイアウトへ反映。未指定時は既存レイアウトを維持 |
| `chart.BeginInit()` / `EndInit()` | 不要 | 部分対応。WinForms標準初期化呼び出しはWrapperでは不要 |
| `chart.Size` / `Dock` / `Location` | `ChartControl.Size` / `Dock` / `Location`、`PlotView` | 対応。WinForms Controlの標準プロパティ |

## 2. データバインディング・系列

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.ChartGroups[0].ChartData.SeriesList` | `ChartControl.ChartGroups[0].ChartData.SeriesList` または `ChartArea.ChartGroups[0].ChartData.SeriesList` | C1C互換Wrapperでは`ChartDataCollection.SeriesList`が先頭のChartDataの系列一覧を公開。既存の`ChartData[0].SeriesList`も使用可能 |
| `chart.ChartGroups` | `ChartControl.ChartGroups` または `ChartControl.ChartArea.ChartGroups` | Wrapperでは既存のChartArea.ChartGroupsを転送公開し、C1C置換時の参照階層を維持 |
| `C1C.ChartGroup(i).ChartData.SeriesList.AddNewSeries()` | `ChartControl.ChartGroups[i].ChartData.AddNewSeries()` または `ChartControl.ChartGroups[i].ChartData[0].AddNewSeries()` | WrapperのファクトリAPIで系列を追加。`ChartData`は先頭データの`SeriesList`を公開し、既存のインデクサー形式も使用可能 |
| `ChartDataSeries.X` / `.Y` | `ChartDataSeries.X` / `.Y` またはCoreの`Series.Points` (`List<(double X, double Y)>`) | Wrapper対応。CoreではタプルのリストとしてXY座標を保持 |
| `ChartDataSeries.PointData.Length` | `ChartDataSeries.PointData.Length` / `ChartDataSeries.PointDataLength` / `ChartDataSeries.Count` | X/Yの両方が存在する対応点数。Wrapperでは`PointData.Length = count`および`PointDataLength = count`による書き込みにも対応し、実データ数を上限としてクランプ |
| `ChartDataSeries.Label` | `Series.Name` | 凡例に表示される系列名 |
| `ChartDataSeries.DataType` / `DataTypes` | `ChartDataSeries.DataType` / `DataTypes` | Wrapper対応。PropBagの単一形・複数形を読み込み、Single/Float指定は数値変換へ反映 |
| `SeriesStyle.SymbolStyle.Shape` | `ChartDataSeries.SymbolStyle.Shape` / `ChartDataSeries.MarkerShape` / `LineSeries.MarkerShape` / `ScatterSeries.MarkerShape` | Wrapperでは`SymbolShapeEnum`と`UnifiedChart.MarkerShape`の両方を受け付ける。マーカー形状は共有系列へ変換される |
| `SeriesStyle.SymbolStyle.Color` | `ChartDataSeries.SymbolStyle.Color` | `System.Drawing.Color`または文字列を指定可能。未指定時は系列色を使用 |
| `SeriesStyle.FillStyle.Color1` | `ChartDataSeries.FillStyle.Color1` | `System.Drawing.Color`を指定可能。Areaの塗りつぶし色、Barの色などへ反映 |
| `SeriesStyle.FillStyle.Color2` | `ChartDataSeries.FillStyle.Color2` | 互換保持用。現在の共有描画モデルではグラデーションとしては使用せず、値を保持 |
| `SeriesStyle.FillStyle.OutlineColor` | `ChartDataSeries.FillStyle.OutlineColor` | Area/Lineの輪郭色へ反映 |
| `TooltipText` / `PointToolTip` | `ChartDataSeries.TooltipText` / `Series.TooltipText` | `{0}`=X、`{1}`=Y、`{2}`=系列名。空の場合は既定の系列名・座標表示 |
| `chart.ChartGroups[0].ChartType = Chart2DTypeEnum.LineSymbols` | `LineSeries` | 折れ線グラフ |
| `Chart2DTypeEnum.XYScatter` | `ScatterSeries` | 散布図。`ScatterSeries.MarkerSize` でマーカーサイズ指定 |
| `Chart2DTypeEnum.Bar` / `.Column` | `BarSeries` | 棒グラフ。`BarSeries.Items` にカテゴリ+値を保持 |
| `Chart2DTypeEnum.Bar` (積み上げ, `IsStacked100 = false`) | `StackedBarSeries` | 積み上げ棒グラフ |
| `Chart2DTypeEnum.Area` | `AreaSeries` | 面グラフ。`AreaSeries.FillColor` / `BaselineValue` に相当 |
| `Chart2DTypeEnum.Pie` / `.Doughnut` | `Chart2DType.Pie` / `.Doughnut` → `PieSeries` | Wrapper enumとPropBagの型変換に対応。Pieは円グラフとして描画。Doughnutは系列状態を保持するが、WinForms描画での穴表現は要確認 |
| `Chart2DTypeEnum.Radar` | `Chart2DType.Radar` → `RadarSeries` | Core/Wrapper対応。WinForms/WPFで同心円グリッド、カテゴリラベル、系列凡例を描画 |
| `Chart2DTypeEnum.HiLoOpenClose` に近い範囲塗り | `RangeSeries` | Core対応。上限/下限の帯グラフ |
| ヒストグラム相当(手動集計 + Bar) | `HistogramSeries` | Core対応。`Values`と`BinCount`でビン計算 |
| 箱ひげ図(サードパーティ拡張 or 手動描画) | `BoxPlotSeries` | Core対応。C1Chart固有の入力形式は変換が必要 |

### 2.1 系列プロパティの網羅表

| C1Chart / C1C API | UnifiedChart API | 対応状況・制約 |
|---|---|---|
| `ChartGroup.ChartType` | `ChartGroup.ChartType` / `Chart2DType` | Wrapper対応。`Line`、`LineSymbols`、`XYPlot`、`XYScatter`、`Bar`、`Column`、`Area`、`Pie`、`Doughnut`に対応。未知の非空ChartTypeはLineへフォールバックせずFormatException |
| `ChartGroup.Name` | `ChartGroup.Name` / `Series.Name` | Wrapper/Core対応 |
| `ChartGroup.Hole` / `DataSerializer.Hole` | `ChartGroup.MissingValueHole` / `Series.MissingValueHole` | Wrapper対応。欠損値は入力データから削除せず描画・範囲計算で扱う |
| `ChartGroup.UseSecondaryYAxis` 相当 | `ChartGroup.UseSecondaryYAxis` / `Series.UseSecondaryYAxis` | Wrapper/Core対応。既定では後続ChartGroupも主Y軸へ描画し、`AutoAssignSecondaryYAxis = true` を明示した場合のみ暗黙Y2割り当てを有効化 |
| `ChartData.SeriesList` | `ChartData.SeriesList` | Wrapper対応。`AddNewSeries()`、`Clear()`、インデクサー、`FindByLabel()`を提供 |
| `ChartDataSeries.PointData` | `ChartDataPointData` | Wrapper対応。`Length`は読み書き可能で、`PointDataLength`へ転送 |
| `ChartDataSeries.PointData.Length` | `ChartDataPointData.Length` | Wrapper対応。設定値は0以上へ補正し、X/Y対応点数を上限に制限 |
| `ChartDataSeries.X` / `Y` / `CopyDataIn` / `Add` / `Clear` | 同名Wrapper API | Wrapper対応。X/Yの点数が異なる場合は対応する最小点数を使用 |
| `ChartDataSeries.Label` | `ChartDataSeries.Label` / Core `Series.Name` | Wrapper/Core対応 |
| `ChartDataSeries.Display` / `SeriesDisplayEnum` | `ChartDataSeries.Display` / Core `Series.Visible` | Wrapper対応。`Show`/`Hide`。Hide系列は描画・自動範囲計算から除外 |
| `SeriesStyle.Line.Thickness` | `ChartDataSeries.StrokeThickness` / `LineSeries.StrokeThickness` | Wrapper/Core対応 |
| `SeriesStyle.Line.Style.DashStyle` | Core `Series.DashStyle` | Core対応。`Solid`、`Dash`、`Dot`、`DashDot`。Wrapperの系列設定面は要確認 |
| `SeriesStyle.Color` / `Border.Color` | `ChartDataSeries.Color` / `LineSeries.Color` / `BarSeries.Color` / `AreaSeries.Color` | Wrapper/Core対応。色名文字列を使用 |
| `SeriesStyle.SymbolStyle.Size` | `ChartDataSeries.SymbolStyle.Size` / `ChartDataSeries.MarkerSize` / `LineSeries.MarkerSize` / `ScatterSeries.MarkerSize` / `RadarSeries.MarkerSize` | Wrapper/Core対応。`MarkerSize`は`SymbolStyle.Size`のエイリアス |
| `SeriesStyle.SymbolStyle.Shape` | `SymbolShapeEnum` / `MarkerShape` | Wrapper/Core対応。Dot、Box、InvertedTri、DiagCrossを含む |
| `SeriesStyle.SymbolStyle.Color` / `OutlineColor` | `ChartDataSeries.SymbolStyle.Color` / `MarkerOutlineColor` | Wrapper対応。文字列または`System.Drawing.Color`。描画層ではマーカー輪郭色へ反映 |
| `SeriesStyle.FillStyle.Color1` / `Color2` / `OutlineColor` | `ChartDataSeries.FillStyle` | Wrapper対応。Color1とOutlineColorを描画へ反映、Color2は保持のみ |
| `PointToolTip` / `TooltipText` | `ChartDataSeries.TooltipText` / Core `Series.TooltipText` | Wrapper/Core対応。書式置換は`{0}` X、`{1}` Y、`{2}`系列名 |
| `SeriesStyle.ShowDataLabels` / `ValueLabel` | Core `Series.ShowDataLabels` / `DataLabelFormat` | CoreのLine/Scatter/Barで対応。WrapperのChartDataSeries直接プロパティは未提供 |

### 2.2 レーダーチャート

| C1Chart / C1C API | UnifiedChart API | 対応状況・制約 |
|---|---|---|
| レーダー系列 | `RadarSeries` / `Chart2DType.Radar` | Core/Wrapper対応。複数系列、系列別マーカー、系列名凡例に対応。塗りつぶしは`FillArea=true`で有効化し、既定値は無効 |
| レーダーの目盛り段階 | `PlotModel.RadarGridLevels` | Core対応。既定値は5。1以上の値を指定 |
| レーダーの目盛りラベル | `PlotModel.RadarGridLabels` | Core対応。内側から外側の順に指定。`RadarGridLabels`の要素数が`RadarGridLevels`と一致する場合に表示し、不一致または未設定時は数値ラベルを表示 |
| レーダーの外周カテゴリ名 | `ChartDataSeries.CategoryLabels` → `RadarSeries.CategoryLabels` | Wrapper/Core対応。文字列項目名をデータ点順に指定し、点数と一致する場合にWinForms/WPFの外周へ表示。不一致時はX値へフォールバック |
| レーダー項目の配置方向 | `PlotModel.RadarDirection` | Core対応。既定はC1Chart互換の`CounterClockwise`。`Clockwise`へ変更可能 |
| レーダーの塗りつぶし | `RadarSeries.FillArea` / `ChartDataSeries.FillArea` | Core/Wrapper対応。既定は`false`。`true`かつ欠損値がない3点以上の場合のみ半透明塗りつぶし |
| レーダー系列の凡例色・マーカー | `RadarSeries.Color` / `MarkerShape` / `MarkerOutlineColor` | Core/WinForms対応。系列ごとの色・マーカーを凡例へ反映 |
| レーダー凡例クリック | `Series.Visible` | WinForms対応。凡例項目クリックで系列の表示/非表示を切り替え。全系列非表示時もレーダーの軸・グリッドを維持 |

## 3. 軸(Axis)

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.ChartArea.AxisX` | `PlotModel.XAxis` | X軸設定 |
| `chart.ChartArea.AxisY` | `PlotModel.YAxis` | 主Y軸設定 |
| `chart.ChartArea.AxisY` を2軸目として複製設定 | `PlotModel.Y2Axis`(nullable) + `Series.UseSecondaryYAxis` | セカンダリY軸。系列ごとにフラグで割り当てる |
| セカンダリX軸 | `PlotModel.X2Axis`(nullable) + `Series.UseSecondaryXAxis` | 対応。X2系列のデータから自動範囲を計算し、WinFormsでは上端へ目盛り・ラベル・グリッドを描画 |
| `Axis.Min` / `Axis.Max` / `AutoMin` / `AutoMax` | `ChartAxis.Min` / `Max` / `AutoMin` / `AutoMax`、Coreの`Axis` | Wrapper/Core対応。自動範囲はデータから計算 |
| `Axis.AnnoFormat` (対数表示など) | `Axis.IsLogarithmic` | Log10 スケール表示 |
| `Axis.Text` | `Axis.Title` (`string`) | 対応済み。`PlotModel.XAxis.Title` / `YAxis.Title` に文字列を設定するとプロット領域の軸外側にタイトルが描画される(WinForms/WPF両対応) |
| `Axis.ValType = ValueType.DateTime` | `Axis.IsDateTime` (`bool`) | 対応済み。X値を `DateTime.ToOADate()` で数値化して使用し、目盛りは秒/分/時/日/月/年単位で自動選択される。ラベル書式は `Axis.DateTimeFormat`(既定 `"yyyy/MM/dd"`)で変更可能。日時ラベルは情報保持のため間引かず、WinForms/WPFとも45度回転して全て描画する。PropBagの`IsDateTime`と`DateTimeFormat`にも対応 |
| `Axis.Origin` / 原点設定 | `ChartAxis.Origin` / `Axis.Origin` / `OriginValue` | 対応。X軸はY値、Y軸はX値として交差位置を計算し、プロット領域内へクランプ |
| 目盛方向設定 | `ChartAxis.TickDirection` / `Axis.TickDirection` | `Outward`、`Inward`、`Cross` に対応 |
| TickLabels表示設定 | `ChartAxis.TickLabelsVisible` / `Axis.TickLabelsVisible` | 目盛線・目盛を残したまま軸ラベルだけを非表示にできる。サムネイル表示等に利用 |
| `Axis.ShowScrollButtons` / ScrollBar | `ChartAxis.ShowScrollButtons` / `Axis.ShowScrollButtons` / `Scroll` | 対応。Visible、Buttons、Unit、Min、Max、Enabled、Y2同期を保持 |
| `ChartArea.Axes(index).ScrollBar.Scale` | `ChartArea.Axes[index].ScrollBar.Scale` | Wrapper対応。C1Cのスクロール倍率を保持し、`ChartAxis.ScrollBar`から設定可能 |
| 表示範囲のスクロール | `ChartControl.DisplayXRange` / `DisplayYRange`、`SetDisplayRange`、`ScrollX`、`ScrollY` | Min/Maxまたは自動全体範囲を境界として表示幅を維持して移動 |
| `ChartXBar.AxisScroll` | `ChartControl.AxisScroll` | Wrapper対応。`ScrollX()`/`ScrollY()`および軸スクロールボタンによる表示範囲変更後に`EventHandler`として発火 |
| `ChartXBar.GetImage()` / `GetImage(Size)` / `GetImage(ImageFormat)` / `GetImage(ImageFormat, Size)` | `ChartControl.GetImage()` / 同じ4種類のオーバーロード | Wrapper対応。チャートを`Bitmap`として取得し、サイズ指定時は指定サイズへ描画。`ImageFormat`はC1C互換の引数として受け付ける |
| `ChartXBar.SaveImage(...)` | `ChartControl.SaveImage(...)` | Wrapper対応。ファイル、`Stream`、`byte[]`へのPNG/JPEG等の画像保存、およびサイズ指定に対応 |
| 表示フォントサイズ | `ChartArea.TitleFontSize` / `FooterFontSize` / `AxisLabelFontSize` / `AxisTitleFontSize` / `DataLabelFontSize` / `LegendFontSize` / `ValueLabelFontSize` / `AlarmZoneFontSize` | Wrapper/Core対応。単位はポイント。未設定時は従来値(12/9/8/9/8/8/8/8)を使用 |
| プロット領域の余白 | `ChartArea.PlotMarginLeft` / `PlotMarginTop` / `PlotMarginRight` / `PlotMarginBottom` | Wrapper/Core対応。単位はピクセル相当。既定値は左48、上32、右16、下32 |

### 3.1 軸プロパティの網羅表

| C1Chart / C1C API | UnifiedChart API | 対応状況・制約 |
|---|---|---|
| `Axis.Min` / `Max` | `ChartAxis.Min` / `Max`、Core `Axis.Min` / `Max` | 対応。null/自動時はデータ範囲から算出 |
| `Axis.AutoMin` / `AutoMax` | `ChartAxis.AutoMin` / `AutoMax`、Core `Axis.AutoMin` / `AutoMax` | 対応 |
| `Axis.UnitMajor` / `UnitMinor` | `ChartAxis.UnitMajor` / `UnitMinor`、Core `Axis.UnitMajor` / `UnitMinor` | 対応。明示値を優先し、未指定時は範囲から自動算出 |
| `Axis.AnnoFormat` | `Axis.IsLogarithmic` / `Axis.NumberFormat`相当 | 部分対応。対数軸は対応するが、C1Cの全書式指定は同一ではない |
| `Axis.Text` | `ChartAxis.Text` / Core `Axis.Title` | 対応 |
| `Axis.ValType` | `ChartAxis.IsDateTime` / Core `Axis.IsDateTime` | 対応。DateTimeはOADate値と自動目盛表示を使用 |
| `Axis.DateTimeFormat`相当 | `Axis.DateTimeFormat` | 対応。PropBagの`DateTimeFormat`からも設定可能 |
| `Axis.Origin` / `OriginValue` | `ChartAxis.Origin` / Core `Axis.Origin` | 対応 |
| `Axis.TickMark` / 目盛方向 | `ChartAxis.TickDirection` / Core `Axis.TickDirection` | 対応。Inward/Outward/Cross |
| `Axis.TickLabels.Visible` | `ChartAxis.TickLabelsVisible` / Core `Axis.TickLabelsVisible` | 対応 |
| `Axis.GridMajor` / `GridMinor` | `ChartAxis.GridMajorVisible` / `GridMinorVisible`、Spacing、Thickness | 対応 |
| 軸個別のGridLineコレクション | `ChartAxis.GridLines` / Core `Axis.GridLines` | 対応。値、色、太さ、破線、Visible |
| `Axis.ValueLabels` | `ChartAxis.ValueLabels` / Core `Axis.ValueLabels` | 対応。Value、Text、Color、Visible |
| `Axis.Markers` | `ChartAxis.Markers` / Core `Axis.Markers` | 対応。値、方向、矢印、接続線、ラベル |
| `Axis.AlarmZones` | `ChartAxis.AlarmZones` / Core `Axis.AlarmZones` | 対応。Explicit/Inside/Outside |
| `Axis.ShowScrollButtons` | `ChartAxis.ShowScrollButtons` / Core `Axis.ShowScrollButtons` | 対応 |
| `Axis.ScrollBar.Unit` / `Min` / `Max` / `Enabled` | `ChartAxis.ScrollUnit` / `ScrollMin` / `ScrollMax` / `ScrollEnabled` | 対応 |
| Y2軸 | `ChartArea.Y2Axis` / `ChartAxes.Y2` / Core `PlotModel.Y2Axis` | 対応。系列単位のY2割り当てと自動範囲に対応 |
| X2軸 | `ChartArea.AxisX2` / `ChartArea.EnableSecondaryXAxis(ChartGroup)` / `ChartAxes.X2` / Core `PlotModel.X2Axis` | 対応。`ChartGroup.UseSecondaryXAxis`または`Series.UseSecondaryXAxis`相当で系列を割り当てる |

## 4. グリッド線・見た目

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.ChartArea.AxisX/Y.GridMajor/GridMinor` | `ChartAxis.GridMajorVisible` / `GridMinorVisible` / `GridMajorSpacing` / `GridMinorSpacing` / `GridMajorThickness` / `GridMinorThickness`、CoreのGridLines | Wrapper/Core対応。メジャー/マイナーの表示、間隔、太さに対応 |
| `AlarmZone` / `AlarmZoneCollection` | `ChartAxis.AlarmZones` / `Axis.AlarmZones` | X/Y/Y2軸に追随する背景帯。塗りつぶし、透明度、枠線、破線、ラベル、表示状態に対応 |
| 軸専用グリッド線 | `ChartAxis.GridLines` / `Axis.GridLines` | 値、色、太さ、破線、表示状態を指定 |
| 軸マーカー | `ChartAxis.Markers` / `Axis.Markers` | 値、ラベル、矢印、接続線を指定 |
| `ChartDataSeries.SeriesStyle.Border.Color` | `ChartDataSeries.Color` / `Series.Color` | Wrapper/Core対応。文字列またはWrapperのColor互換プロパティ |
| `SeriesStyle.SymbolStyle.Fill.Style.BackgroundColor` | `AreaSeries.FillColor` / `RangeSeries.FillColor` | 塗りつぶし色 |
| `SymbolShapeEnum` / `SymbolsShapeEnum` | `UnifiedChart.C1CWrapper.SymbolShapeEnum` / `UnifiedChart.MarkerShape` | Wrapper対応。`Circle`/`Dot`/`Square`/`Box`/`Diamond`/`Triangle`/`InvertedTri`/`Cross`/`DiagCross`/`Plus`/`Star`を対応。矢印・画像形状は未対応 |
| `SeriesStyle.Line.Style.DashStyle` / `LinePatternEnum`(使用確認済み) | `Series.DashStyle` (`LineDashStyle`: Solid/Dash/Dot/DashDot) | 破線スタイル。C1C の `LinePatternEnum` はより多くの種類(DashDotDot等)を持つ可能性があるため、対応関係の詳細確認が必要 |
| `ChartDataSeries.SeriesStyle.SeriesDisplay` / `SeriesDisplayEnum`(使用確認済み) | `ChartDataSeries.Display = SeriesDisplayEnum.Show/Hide` / `Series.Visible` (`bool`) | WrapperではC1C互換の`SeriesDisplayEnum.Show`と`Hide`を公開。`Hide`の系列は描画および自動範囲計算の対象外 |
| `chart.ChartArea.Palette` / 手動カラーパレット | `PlotModel.Theme.SeriesPalette` | 複数系列色や円グラフのパレット。`PlotTheme.Light`/`Dark`/`Colorful` から選択、または独自 `PlotTheme` を作成 |
| 背景色・文字色を個別プロパティで設定 | `PlotTheme.BackgroundColor` / `TitleColor` / `AxisLabelColor` / `GridLineColor` / `LegendTextColor` | Phase 4 で追加されたテーマ機構に集約 |

## 5. 凡例(Legend)

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.Legend.Visible` | `PlotModel.ShowLegend` (`bool`) | 対応済み。凡例全体の表示/非表示切替。既定は `true` |
| `chart.Legend.Compass` (配置位置) | `PlotModel.LegendPosition` (`TopRight`/`TopLeft`/`BottomRight`/`BottomLeft`) | 4隅からの選択のみ対応(C1C ほど自由な配置指定はできない) |
| 凡例クリックで系列表示切替 | 標準対応(`PlotView` クリックで `Series.Visible` を切替) | 追加実装不要 |
| レーダー凡例の系列別色・マーカー | `RadarSeries.Color` / `MarkerShape` / `MarkerOutlineColor` | 対応。通常系列と同じ共通凡例で描画し、クリックによる表示切替に対応 |

### 5.1 凡例・テーマ・背景

| C1Chart / C1C API | UnifiedChart API | 対応状況・制約 |
|---|---|---|
| `Legend.Visible` | `ChartLegend.Visible` / `PlotModel.ShowLegend` | Wrapper/Core対応 |
| `Legend.Compass` | `ChartLegend.Position` / `PlotModel.LegendPosition` | 部分対応。4隅配置 |
| `Legend.Text` / 系列Label | `Series.Name` | 対応 |
| `ChartArea.BackColor` | `PlotTheme.BackgroundColor` | Core対応 |
| 軸・タイトル・凡例の文字色 | `PlotTheme.TitleColor` / `AxisLabelColor` / `LegendTextColor` | Core対応 |
| C1Cのパレット | `PlotTheme.SeriesPalette` | Core対応。C1Cの全パレット名は個別確認が必要 |

## 6. 操作機能(ズーム・パン・ツールチップ)

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.View.AxisX.ZoomFactor` / マウスドラッグズーム | マウスホイールズーム(`PlotView`) | 操作方法が異なるため、UI仕様を確認して利用者への案内が必要 |
| ドラッグパン | 左ドラッグパン | 対応 |
| ダブルクリックでリセット | ダブルクリックでリセット(`PlotView.ResetZoom`) | 対応 |
| スクロールバーボタン | WinForms/WPF `PlotView` の軸スクロールボタン | 対応。X/Y方向のボタン操作は表示範囲をMin/Max境界内で移動 |
| `ChartControl.RefreshChart()` | `ChartControl.RefreshChart()` | Wrapper設定を共有PlotModelへ再変換して再描画 |
| `PointToolTip` / カスタムツールチップ | マウス直下の最近傍データ点のツールチップ表示 | 標準対応 |
| ピンチズーム(タッチ操作、`C1C` は非対応) | WinForms `WM_GESTURE` / WPF `ManipulationDelta` によるピンチズーム・パン | Phase 3 で追加された UnifiedChart 独自の拡張機能 |
| アニメーション遷移(`C1C` は非対応) | ズーム時のイージング補間描画(ease-out cubic, 150ms) | Phase 3 で追加された UnifiedChart 独自の拡張機能 |

### 6.1 コントロール・操作API

| C1Chart / C1C API | UnifiedChart API | 対応状況・制約 |
|---|---|---|
| `Refresh` / 再描画 | `ChartControl.RefreshChart()` / `PlotView.InvalidatePlot()` | Wrapper/Core対応 |
| `Zoom` / `ResetZoom` | `ChartControl.ResetZoom()` / `PlotView.ResetZoom()` | 対応 |
| 現在の表示範囲 | `ChartControl.DisplayXRange` / `DisplayYRange` | 対応 |
| 表示範囲の直接設定 | `ChartControl.SetDisplayRange()` | 対応 |
| 軸スクロール | `ChartControl.ScrollX()` / `ScrollY()` | 対応 |
| `ChartXBar.AxisScroll`イベント | `ChartControl.AxisScroll`イベント | 対応。`chartXBar.AxisScroll += Handler`の形式で購読可能。イベント引数は標準の`EventArgs` |
| マウスホイールズーム | WinForms/WPF `PlotView` | 対応。C1Chartと操作細部は異なる |
| マウスドラッグパン | WinForms/WPF `PlotView` | 対応 |
| 最近傍点検索 | `PlotRenderHelper.FindNearestPoint()`相当 | Core対応 |
| `ChartGroups(0).CoordToDataIndex(e.X, e.Y, CoordianteFocusEnum.XandYCoord, seriesIndex, pointIndex, distance)` | `ChartControl.ChartGroups[0].CoordToDataIndex(e.X, e.Y, CoordianteFocusEnum.XandYCoord, out seriesIndex, out pointIndex, out distance)` | Wrapper対応。`seriesIndex`はChartGroup内の系列番号、`pointIndex`は系列内の点番号、`distance`は画面上の距離(px)。`CoordinateFocusEnum`の正しい綴りも利用可能 |
| `CoordToDataIndex(x, y)`相当 | `ChartControl.CoordToDataIndex(x, y)` / `PlotView.CoordToDataIndex(Point)` | 最近傍のデータ点のインデックスを返し、該当点がない場合は`-1` |
| カスタム描画イベント | `PlotView.CustomRender` / `CustomRenderEventArgs` | WinForms/WPF対応 |

## 7. 注釈・カスタム描画

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `C1C.Annotation` 系クラス | `PlotModel.Annotations` (`TextAnnotation`) | データ座標を指定したテキスト注釈 |
| `AnnotationMethodEnum`(使用確認済み。軸ラベル・ValueLabelsの注釈整形方式) | `ChartAxis.AnnoMethod` / `ChartAxis.AnnotationRotation` | Wrapper保持・部分対応。`None`、`Rotate`、`Stagger`、`Wrap`を保持するが、C1Chart固有の自動回転・段違い配置・折り返し描画は未実装。`TextAnnotation`のデータ座標指定とは別概念 |
| `chart.Draw` イベントによるカスタム描画 | `PlotView.CustomRender` イベント | 通常描画完了後に任意の `Graphics`/`DrawingContext` へ描画できる |

### 7.4 Core系列の対応表

| C1Chartの系列・用途 | UnifiedChart Core | 対応状況・制約 |
|---|---|---|
| Line / XYPlot | `LineSeries` | 対応。Points、Color、DashStyle、Marker、Tooltip、Y2 |
| LineSymbols | `LineSeries` | 対応。ShowMarkers、MarkerShape、MarkerSize |
| XYScatter | `ScatterSeries` | 対応。Points、Color、MarkerShape、MarkerSize |
| Bar / Column | `BarSeries` | 対応。カテゴリ、値、XValues、Color |
| Stacked Bar | `StackedBarSeries` | Core対応。C1C WrapperのChart2DTypeからの自動変換は要確認 |
| Area | `AreaSeries` | 対応。Points、Color、FillColor、BaselineValue |
| Pie / Doughnut | `PieSeries` | Pieは対応。Doughnutは`PieSeries.IsDoughnut`へ変換し、データ形式を維持する。穴の描画・Doughnut固有ラベルは未対応 |
| HiLo / 範囲帯 | `RangeSeries` | Core対応。Points、Low/High、FillColor |
| Histogram | `HistogramSeries` | Core対応。Values、BinCount |
| BoxPlot | `BoxPlotSeries` | Core対応。カテゴリ別Min/Q1/Median/Q3/Max |
| C1Chart独自金融系列・3D系列 | なし | 未対応 |

## 7.1 軸ValueLabelsと表示方式

| C1C | UnifiedChart | 移行時の確認事項 |
|---|---|---|
| `Axis.ValueLabels` / `ValueLabelCollection` | `Axis.ValueLabels`、Wrapperでは`ChartAxis.ValueLabels` | 軸上の任意の数値位置に任意文字列を表示する。`MEAN`、`+1SD`、`R`、`RS`、日付、TwinPlotの実値ラベルなどに利用できる |
| `ValueLabel.Value` / `Text` | `AxisValueLabel.Value` / `Text` | 軸目盛の代替ではなく、標準目盛とは別の明示ラベル。データ系列の`ChartLabel`/`ShowDataLabels`と混同しない |
| `AnnotationMethodEnum.ValueLabels` | `ChartAxis.AnnoMethod = AnnotationMethodEnum.ValueLabels` または `Axis.LabelMode = AxisLabelMode.ValueLabels` | Wrapperでは`AnnoMethod`への設定を共有軸へ反映する。PropBagでは`LabelMode`、`AnnotationMethod`、`ChartValueLabelsEnabled`の別名を読み込む |
| 標準目盛表示 | `Axis.LabelMode = AxisLabelMode.Standard` | `TickLabelsVisible`は目盛文字の可視性であり、ValueLabels方式の選択とは別設定 |
| `LabelCompassEnum.Auto` | 自動配置と衝突回避 | データラベルの重なりを避ける。厳密なC1Cのピクセル配置が必要な場合は画面比較で調整する |

### 7.1.1 `AnnotationMethodEnum` 対応表

`AnnotationMethodEnum` は、軸ラベルやValueLabelsなどの注釈文字列をプロット領域内へどのように配置・整形するかを指定するC1Chart側の列挙型です。`TextAnnotation` のデータ座標指定方式を表すものではありません。

| C1Chart `AnnotationMethodEnum` | UnifiedChart 対応API | 対応状況 | 備考 |
|---|---|---|---|
| `None` | `ChartAxis.AnnoMethod = AnnotationMethodEnum.None` / `Axis.LabelMode = AxisLabelMode.Standard` | Wrapper保持・部分対応 | C1Chartの自動注釈整形を行わず、標準目盛ラベルを使用する。`ChartAxis.AnnoMethod`で値は保持されるが、独立した描画効果はない |
| `Rotate` | `ChartAxis.AnnoMethod = AnnotationMethodEnum.Rotate` / `ChartAxis.AnnotationRotation` | 部分対応 | 回転方式の指定をWrapperで保持する。UnifiedChartの共有Core/描画層では、C1Chartと同じ自動回転角度の適用は未実装。必要な場合は`AnnotationRotation`またはカスタム描画で補う |
| `Stagger` | `ChartAxis.AnnoMethod = AnnotationMethodEnum.Stagger` | 部分対応 | 交互配置方式の指定をWrapperで保持する。標準軸ラベルに対するC1Chart固有の段違い配置は未実装 |
| `Wrap` | `ChartAxis.AnnoMethod = AnnotationMethodEnum.Wrap` | 部分対応 | 折り返し方式の指定をWrapperで保持する。標準軸ラベルの自動折り返しは未実装。長いラベルはカスタム描画または表示領域の調整が必要 |

#### ValueLabelsとの関係

`AnnotationMethodEnum` とValueLabelsの表示方式は別概念です。C1ChartのPropBagで`AnnotationMethod="ValueLabels"`が指定される既存形式は、UnifiedChartでは次のように`AxisLabelMode.ValueLabels`へ変換されます。

| C1Chart / PropBag | UnifiedChart | 備考 |
|---|---|---|
| `AnnotationMethod="ValueLabels"` | `ChartAxis.LabelMode = AxisLabelMode.ValueLabels` | 互換入力として読み込み可能。`AnnotationMethodEnum`の列挙値(`None`/`Rotate`/`Stagger`/`Wrap`)とは別の旧形式・別名扱い |
| `AnnotationMethodEnum.None` + 標準目盛 | `ChartAxis.LabelMode = AxisLabelMode.Standard` | `TickLabelsVisible`はラベル方式ではなく、目盛文字の表示/非表示 |
| `ValueLabels`コレクション | `ChartAxis.ValueLabels` | 任意の値位置に任意文字列を表示する。`Rotate`/`Stagger`/`Wrap`による自動配置とは独立 |

## 7.2 AlarmZone、Marker、GridLine

| C1C | UnifiedChart | 移行時の確認事項 |
|---|---|---|
| `AlarmZone`の明示範囲 | `Axis.AlarmZones` / `ChartAxis.AlarmZones`、`Mode = Explicit` | `From`/`To`をそのまま帯として描画する。帯は現在の軸範囲に追随する背景帯として表示される |
| AlarmZoneの内側/外側 | `AlarmZoneMode.Inside` / `Outside` | `LowerLimit`/`UpperLimit`を基準に、内側または外側の帯を現在の軸範囲まで拡張する。ズーム・パン後も軸範囲に追随し、単なる`From`/`To`指定とは意味が異なる |
| `C1ChartMarker`の方向 | `AxisMarker.ArrowDirection` | 値・ラベルだけでなく矢印の向きも描画へ反映される |
| Y2軸のMarker/GridLine | `PlotModel.Y2Axis.Markers` / `GridLines` | 主Y軸と同じコレクションへ混在させず、Y2軸に所属させる。系列の`UseSecondaryYAxis`とは独立した軸装飾設定 |

## 7.3 欠損値(Hole)契約

| C1C | UnifiedChart | 移行時の確認事項 |
|---|---|---|
| `ChartGroup.Hole = 100` | `ChartGroup.MissingValueHole = 100` | 入力された点は保持するが、描画と自動範囲計算では欠損として扱う。元データを削除・置換しない |
| Holeを含むLine/Area/Range | 欠損位置で線分・面を分割 | 欠損値を0として接続しない。移行後の線の切れ方を確認する |
| 欠損値をまたぐ線の接続 | `ChartDataSeries.ConnectAcrossMissingValues` / `Series.ConnectAcrossMissingValues` | 既定値は`true`。C1ChartのHole表示と同様に欠損箇所で線を切る場合は`false`を指定 |

## 7.5. データラベル(系列ValueLabel / ChartLabels)

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `C1C.ValueLabel` | `Series.ShowDataLabels` (`bool`) | データ点上に数値ラベルを表示するかどうかのフラグ。C1C は `ValueLabel` オブジェクトを系列に割り当てるが、UnifiedChart は `Series` 側の真偽値プロパティで on/off する |
| `ValueLabel.Text` / カスタム書式文字列 | `Series.DataLabelFormat` (`string`) | 表示する数値の書式指定。標準の数値書式文字列(例: `"0.0"`, `"0.##"`)を指定する |
| `ValueLabel.Position` (Above/Below/Center 等) | 自動配置(衝突回避) | `LabelCompassEnum.Auto`相当の自動配置には対応。個別のAbove/Below/Center指定やC1Cと同一のピクセル配置は未対応 |
| `ValueLabel.Border` / `ValueLabel.Fill` (吹き出し風装飾) | (未対応: ラベル背景/枠線) | UnifiedChart は文字列描画のみで、背景色・枠線の装飾には未対応 |
| `ChartArea.ChartLabels` | `ChartArea.ChartLabels` / `PlotModel.ChartLabels` | 系列名(`SeriesName`)とデータ点番号(`DataIndex`)に紐付く任意テキスト注記。接続線(`ShowConnectionLine`)や自動配置(`Compass = Auto`)、オフセット指定に対応 |
| 対応系列 | `LineSeries` / `ScatterSeries` / `BarSeries` | いずれも `ShowDataLabels` / `DataLabelFormat` を持つ。他系列(Area/Range/StackedBar/Histogram/BoxPlot)は現状データラベル未対応のため、必要なら追加実装を検討 |

## 7.6. スクロールと表示範囲

| C1C | UnifiedChart | 移行時の確認事項 |
|---|---|---|
| `Axis.ShowScrollButtons` / `SB` | `Axis.ShowScrollButtons` / `ChartAxis.ShowScrollButtons` | WinForms/WPFで軸端のスクロール操作を表示する |
| スクロール幅 | `Axis.ScrollUnit` | 明示した数値を1回の移動幅として使用する。未指定時は後方互換のため現在表示幅の20% |
| スクロール境界 | `Axis.ScrollMin` / `ScrollMax` | データ全体の自動範囲とは別に移動可能範囲を制限できる |
| Y2連動スクロール | `Axis.SynchronizeWithY2` | 主Y軸とY2軸の表示操作を同期する。Y2軸未設定時は同期対象なし |
| `ZoomFactor`等の表示範囲 | `ChartControl.DisplayXRange` / `DisplayYRange`、`SetDisplayRange`、`ScrollX`、`ScrollY` | 表示範囲を直接検証できる。`Min`/`Max`は境界、DisplayRangeは現在表示中の範囲として扱う |

## 7.7. PropBag XML 要素対応表

`ChartControl.PropBag` へ既存 XML 文字列を設定することで、以下の属性・タグを自動解釈して反映します。

| XML要素 / パス | 読み込み対象属性・タグ | UnifiedChart対応プロパティ | 備考 |
|---|---|---|---|
| `<Header>` | `<Text>`, `Compass`, `Visible` | `ChartArea.Header.Text`, `Compass`, `Visible` → `PlotModel.Title` / `TitleCompass` | タイトル文字列・配置・表示状態 |
| `<Footer>` | `<Text>`, `Compass`, `Visible` | `ChartArea.Footer.Text`, `Compass`, `Visible` → `PlotModel.Footer` / `FooterCompass` / `ShowFooter` | フッター文字列・配置・表示状態をWinForms描画へ反映 |
| `<Legend>` | `Visible`, `Compass` | `ChartArea.Legend.Visible`, `Position` | 凡例の可視性と配置(North/South/West/East) |
| `<ChartArea>` | `LocationDefault`, `SizeDefault` | `ChartArea.LocationDefault`, `SizeDefault` → `ChartControl.Location`, `Size` | `x,y`および`width,height`形式を読み込み、WinFormsレイアウトへ反映 |
| `<Axes><Axis>` (1番目) | `Min`, `Max`, `AutoMin`, `AutoMax`, `UnitMajor`, `UnitMinor` | `ChartArea.AxisX` (範囲・目盛単位) | 自動フラグがTrueの場合はnull(自動計算)として扱う |
| `<Axes><Axis>` (2番目) | 上記同様 | `ChartArea.AxisY` | 主Y軸設定 |
| `<Axes><Axis>` (3番目) | 上記同様 | `ChartArea.Y2Axis` (または `AxisY2`) | セカンダリY軸が存在する場合に自動生成 |
| `<Axis>` | `Origin`, `OriginValue` | `ChartAxis.Origin` | 軸の交差位置 |
| `<Axis>` | `TickDirection`, `TickMarkDirection` | `ChartAxis.TickDirection` | `Inward`, `Outward`, `Cross` |
| `<Axis><TickLabels>` | `Visible` | `ChartAxis.TickLabelsVisible` | 軸の数値目盛ラベル表示/非表示 |
| `<Axis>` | `LabelMode`, `AnnotationMethod`, `ChartValueLabelsEnabled` | `ChartAxis.LabelMode` | `Standard` または `ValueLabels` |
| `<Axis><ValueLabels>` | `<ValueLabel Value="..." Text="..." Color="...">` | `ChartAxis.ValueLabels` | 任意位置への文字目盛(MEAN, +1SD, UCL等) |
| `<Axis><ValueLabelCollection>` | `<ValueLabel ...>` | `ChartAxis.ValueLabels` | `<ValueLabels>`の別名。Parser処理・未対応警告判定ともに対応 |
| `<Axis><AlarmZones>` | `<AlarmZone Mode="..." Min="..." Max="..." LowerLimit="..." UpperLimit="..." Color="..." Opacity="..." Visible="...">` | `ChartAxis.AlarmZones` | 明示範囲(`Explicit`)および上下限内外(`Inside`/`Outside`)、軸範囲に追随する背景帯 |
| `<Axis><AlarmZoneCollection>` | `<AlarmZone ...>` | `ChartAxis.AlarmZones` | `<AlarmZones>`の別名。Parser処理・未対応警告判定ともに対応 |
| `<Axis><Markers>` | `<Marker Value="..." Color="..." ArrowDirection="..." ShowArrow="..." ShowConnectionLine="...">` | `ChartAxis.Markers` | 任意値のマーカー線・矢印・ラベル |
| `<Axis><MarkerCollection>` | `<Marker ...>` | `ChartAxis.Markers` | `<Markers>`の別名。Parser処理・未対応警告判定ともに対応 |
| `<Axis><GridLines>` | `<GridLine Value="..." Color="..." DashStyle="..." Thickness="..." Visible="...">` | `ChartAxis.GridLines` | 任意値の専用グリッド線 |
| `<Axis><GridLineCollection>` | `<GridLine ...>` | `ChartAxis.GridLines` | `<GridLines>`の別名。Parser処理・未対応警告判定ともに対応 |
| `<Axis><SB>` / `<ScrollBar>` | `Visible`, `Buttons`, `Unit`, `Min`, `Max`, `Enabled`, `SynchronizeWithY2` | `ChartAxis.ShowScrollButtons`, `ScrollUnit`, `ScrollMin`, `ScrollMax`, `ScrollEnabled`, `SynchronizeWithY2` | スクロールボタンおよびスクロール境界・連動 |
| `<ChartGroupsCollection>` | `<ChartGroup Name="...">` | `ChartArea.ChartGroups` | 系列グループ定義 |
| `<ChartGroup><DataSerializer>` | `Hole`, `DefaultSet` | `ChartGroup.MissingValueHole` | `Hole=100` 等の欠損値契約 |

### 7.7.1 PropBag対応範囲

| C1Chart PropBag要素 | UnifiedChart.C1CWrapper | 対応状況 |
|---|---|---|
| `Header` | `ChartHeader.Text` / `Compass` | 対応 |
| `Legend` | `ChartLegend.Visible` / `Position` | 対応 |
| `Axes` / `Axis` | `ChartAxis` / `Y2Axis` | 対応 |
| `GridMajor` / `GridMinor` | 軸のGrid設定 | 対応 |
| `GridLines` | `ChartAxis.GridLines` | 対応 |
| `Markers` | `ChartAxis.Markers` | 対応 |
| `ValueLabels` | `ChartAxis.ValueLabels` | 対応 |
| `AlarmZones` | `ChartAxis.AlarmZones` | 対応 |
| `ScrollButtons` / `ScrollBar` | `ChartAxis`のScroll設定 | 対応 |
| `ChartGroups` / `ChartGroupCollection` | `ChartArea.ChartGroups` | 対応。旧単数形式も読み込み |
| DataSerializer / Hole | `ChartGroup.MissingValueHole` | 対応 |
| PropBag内の実データ点 | なし | 仕様上保持しない。実行時にSeriesListへ追加 |

> **重要**: PropBag は見た目・軸設定・系列定義・欠損設定を保持するための設定データであり、実データ点は保持しません。移行時は `PropBag` プロパティ設定後に `ChartGroup` → `ChartData` → `SeriesList` へ実データを追加し、`RefreshChart()` を呼び出してください。

## 7.8. C1C互換APIの使用例

### 系列の列挙と表示切り替え

`ChartData.SeriesList` は、C1Cの `ChartGroups(0).ChartData.SeriesList` と同じ階層でアクセスできます。

```csharp
foreach (var series in chartControl.ChartGroups[0].ChartData.SeriesList)
{
	series.Display = SeriesDisplayEnum.Show;
}

chartControl.ChartGroups[0].ChartData.SeriesList[e.RowIndex].Display = SeriesDisplayEnum.Hide;
chartControl.RefreshChart();
```

VBでは次のように記述できます。

```vb
Dim ds As ChartDataSeries
For Each ds In ChartControl.ChartGroups(0).ChartData.SeriesList
	ds.Display = SeriesDisplayEnum.Show
Next

ChartControl.ChartGroups(0).ChartData.SeriesList(e.RowIndex).Display = SeriesDisplayEnum.Hide
ChartControl.RefreshChart()
```

`Display = Hide` の系列は描画だけでなく、自動軸範囲計算の対象からも除外されます。

### 点数の設定

`PointData.Length` と `PointDataLength` は、C1C移行コードの書き換えを減らすため、どちらも設定可能です。

```csharp
var series = chartControl.ChartGroups[0].ChartData.SeriesList[0];
series.PointData.Length = count;
// または
series.PointDataLength = count;
```

指定値がX/Yの実データ数を超える場合は、実際に存在する対応点数へ制限されます。

### 色とシンボル形状の設定

`System.Drawing.Color` とC1互換enumを使用できます。

```csharp
series.SymbolStyle.Color = Color.Red;
series.SymbolStyle.Shape = SymbolShapeEnum.Diamond;
series.FillStyle.Color1 = Color.LightBlue;
series.FillStyle.OutlineColor = Color.DarkBlue;
```

既存の文字列色指定および `UnifiedChart.MarkerShape` も引き続き使用できます。

```csharp
series.SymbolStyle.Color = "Red";
series.SymbolStyle.Shape = UnifiedChart.MarkerShape.Circle;
```

`FillStyle.Color1` は面の塗りつぶし色や棒系列の色に、`FillStyle.OutlineColor` は面・線系列の輪郭色に反映されます。`Color2` は互換情報として保持されますが、現在の描画エンジンではグラデーションには使用されません。

### SymbolShapeEnum の互換対応

`SymbolShapeEnum` はC1Cのシンボル形状指定を、`UnifiedChart.C1CWrapper` で継続利用するための公開enumです。`ChartDataSeries.SymbolStyle.Shape` は、C1C互換の `SymbolShapeEnum` と共有コアの `UnifiedChart.MarkerShape` の両方を受け付けます。

| C1Chart `SymbolShapeEnum` | UnifiedChart `MarkerShape` | `ChartDataSeries.SymbolStyle.Shape`での指定 | 対応状況 |
|---|---|---|---|
| `Circle` | `Circle` | `SymbolShapeEnum.Circle` または `MarkerShape.Circle` | 対応 |
| `Dot` | `Dot` | `SymbolShapeEnum.Dot` または `MarkerShape.Dot` | 対応。小さい塗りつぶし円 |
| `Square` | `Square` | `SymbolShapeEnum.Square` または `MarkerShape.Square` | 対応 |
| `Box` | `Box` | `SymbolShapeEnum.Box` または `MarkerShape.Box` | 対応。枠付き正方形 |
| `Diamond` | `Diamond` | `SymbolShapeEnum.Diamond` または `MarkerShape.Diamond` | 対応 |
| `Triangle` | `Triangle` | `SymbolShapeEnum.Triangle` または `MarkerShape.Triangle` | 対応 |
| `InvertedTri` | `InvertedTri` | `SymbolShapeEnum.InvertedTri` または `MarkerShape.InvertedTri` | 対応。逆三角形 |
| `Cross` | `Cross` | `SymbolShapeEnum.Cross` または `MarkerShape.Cross` | 対応。X字の十字 |
| `DiagCross` | `DiagCross` | `SymbolShapeEnum.DiagCross` または `MarkerShape.DiagCross` | 対応。斜め十字 |
| `Plus` | `Plus` | `SymbolShapeEnum.Plus` または `MarkerShape.Plus` | 対応。+字の十字 |
| `Star` | `Star` | `SymbolShapeEnum.Star` または `MarkerShape.Star` | UnifiedChart側で対応。C1Chartのバージョンによっては未提供 |

#### 対応関係

- C1Chartから移行する場合は、C1C互換Wrapperの `SymbolShapeEnum` をそのまま使用できます。
- Wrapperは設定値を内部でUnifiedChartの `MarkerShape` へ変換します。
- `LineSeries` と `ScatterSeries` の両方で同じ対応表を使用します。
- C1Chart固有の矢印形状、画像形状、ユーザー定義Bitmapなどは、UnifiedChartの標準 `MarkerShape` には対応していません。

C#では次のように指定します。

```csharp
using UnifiedChart.C1CWrapper;

ChartDataSeries series = chartControl.ChartGroups[0].ChartData.SeriesList[0];
series.SymbolStyle.Shape = SymbolShapeEnum.Diamond;
```

VBでは次のように指定します。

```vb
Dim series As ChartDataSeries = ChartControl.ChartGroups(0).ChartData.SeriesList(0)
series.SymbolStyle.Shape = SymbolShapeEnum.Diamond
```

系列変換時に `SymbolShapeEnum` は内部で `UnifiedChart.MarkerShape` へ正規化され、Line系列とScatter系列のマーカー描画へ反映されます。矢印系、画像、ユーザー定義Bitmapなどのカスタムシンボルは、現在の互換範囲外です。

## 7.9. Designer 生成コード（InitializeComponent）の置き換え例

フォームデザイナーが生成した `InitializeComponent` 内のコードは、最小限の手順で書き換えられます。

### C# での置換例

**置換前 (C1.Win.C1Chart.2):**
```csharp
// フィールド定義
private C1.Win.C1Chart.C1Chart c1Chart1;

// InitializeComponent 内
this.c1Chart1 = new C1.Win.C1Chart.C1Chart();
((System.ComponentModel.ISupportInitialize)(this.c1Chart1)).BeginInit();
this.c1Chart1.Dock = System.Windows.Forms.DockStyle.Fill;
this.c1Chart1.Location = new System.Drawing.Point(0, 0);
this.c1Chart1.Name = "c1Chart1";
this.c1Chart1.PropBag = resources.GetString("c1Chart1.PropBag");
this.c1Chart1.Size = new System.Drawing.Size(800, 600);
this.Controls.Add(this.c1Chart1);
((System.ComponentModel.ISupportInitialize)(this.c1Chart1)).EndInit();
```

**置換後 (UnifiedChart.C1CWrapper):**
```csharp
// フィールド定義
private UnifiedChart.C1CWrapper.ChartControl c1Chart1;

// InitializeComponent 内
this.c1Chart1 = new UnifiedChart.C1CWrapper.ChartControl();
this.c1Chart1.Dock = System.Windows.Forms.DockStyle.Fill;
this.c1Chart1.Location = new System.Drawing.Point(0, 0);
this.c1Chart1.Name = "c1Chart1";
this.c1Chart1.PropBag = resources.GetString("c1Chart1.PropBag");
this.c1Chart1.Size = new System.Drawing.Size(800, 600);
this.Controls.Add(this.c1Chart1);
```
*(※ISupportInitialize の呼び出しは不要になるため削除またはコメントアウトします)*

## 7.10. 品質管理(QC)向けチャートの移行ポイント

社内システムで多用される各種QCチャートにおける、C1C API と UnifiedChart の対応付けです。

- **Xbar-R管理図 / X管理図**:
  - 管理線（中心線 CL、上方管理限界線 UCL、下方管理限界線 LCL）: `Axis.Markers`（または `Axis.GridLines`）および `Axis.ValueLabels` で「CL」「UCL」「LCL」ラベルを描画。
  - 規格外警戒ゾーン: `Axis.AlarmZones` を使用。`Mode = AlarmZoneMode.Outside`（または `Inside`）を指定することで、UCL/LCL を境界とする背景帯がズーム・パン時も軸範囲に追随。
- **Cu-Sum管理図**:
  - 累積和折れ線: `ChartType = Chart2DType.LineSymbols`。折れ線とシンボルマーカー（Circle/Square等）を表示。
  - Vマスク等の判定領域: `PlotView.CustomRender` イベントを活用して動的な任意多角形・注釈線を追加描画可能。
- **ツインプロット図 (TwinPlot)**:
  - 左右2軸（主Y軸 + セカンダリY2軸）の表示: `ChartArea.Y2Axis` を設定し、2番目の `ChartGroup` または系列で `UseSecondaryYAxis = true` を指定。
  - 実値ラベル: `Axis.ValueLabels` または `ChartArea.ChartLabels` を利用し、データ点番号に紐付いた任意テキスト（MEAN, +1SD, R, RS 等）を自動配置（`Auto`）表示。
- **時系列 / TREND管理図**:
  - 日時X軸: `Axis.IsDateTime = true`、`DateTimeFormat = "yyyy/MM/dd HH:mm"`。
  - 欠損値: 機器停止や通信エラー時の無効データは `MissingValueHole = 100`（または業務上の欠損コード）で指定し、データ配列を壊さずに線を自動分割。
  - 履歴スクロール: `Axis.ShowScrollButtons = true`、`Axis.ScrollUnit`、`Axis.ScrollMin`/`ScrollMax` による一定期間単位のスクロール。

## 8. 出力

| C1C | UnifiedChart | 備考 |
|---|---|---|
| `chart.SaveImage(...)` | `PlotView.SaveAsPng(string filePath)` | PNG画像として保存 |
| 印刷(`PrintDocument` 連携) | アスペクト比を保持したスケーリング・中央配置での印刷 | 標準対応 |

## 9. 未対応・要検討項目(Phase 5 互換性ギャップ候補)

- 4隅以外への凡例自由配置
- `DataTable` / `IList` からの直接データバインディング(現状は `Series.Points` への手動追加のみ)
- `SymbolsShapeEnum` の矢印系・カスタム画像等、C1固有の拡張形状(対応済み11形状以外)
- `LinePatternEnum` と UnifiedChart `LineDashStyle` の値の対応関係詳細(種類数の差異)
- `AnnotationMethodEnum` が持つ注釈座標指定方式のバリエーション対応
- `ValueLabel.Position` (Above/Below/Center 等) による個別装飾の完全な互換性(自動配置は対応)
- `ValueLabel.Border` / `ValueLabel.Fill` (吹き出し風装飾) によるラベル背景・枠線の詳細装飾
- 対応系列(Area/Range/StackedBar/Histogram/BoxPlot)へのデータラベル(`ShowDataLabels`)追加
- 複数の `ChartGroup` に相当する、系列グループ単位での軸共有/独立制御の柔軟性

`Origin`、目盛方向、TickLabels非表示、ValueLabels、AlarmZoneの内外、Marker方向、Y2専用装飾、`Hole=100`、スクロールバーボタン、表示範囲スクロールは実装済みです。未対応項目は実際の移行対象プロジェクトで使用有無を確認し、画面比較で優先度を付けてください。
