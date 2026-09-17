# UnifiedChart

CoOne（C1C.2）からの移行を目的とした、.NET 8 向けのグラフ描画ライブラリです。
UI に依存しないコアライブラリ、WinForms / WPF 向けのプロットコントロール、C1C互換Wrapperで構成されています。
VB.NET / C# のどちらからも利用できます。

## 構成

| プロジェクト | 内容 |
|---|---|
| `UnifiedChart.csproj` | UI 非依存のコアライブラリ(データモデル・座標変換・軸範囲計算など) |
| `UnifiedChart.WinForms` | WinForms 向け `PlotView` コントロール |
| `UnifiedChart.Wpf` | WPF 向け `PlotView` コントロール |
| `UnifiedChart.C1CWrapper` | `ChartControl`、`ChartArea`、`ChartAxis` 等のC1C互換APIとPropBag読み込み |
| `UnifiedChart.Samples.CSharp` | C# からの呼び出しサンプル(WinForms) |
| `UnifiedChart.Samples.VisualBasic` | VB.NET からの呼び出しサンプル(WinForms) |
| `UnifiedChart.Tests` | xUnit によるコアロジックの単体テスト |

## 対応グラフ種類

- 折れ線グラフ (`LineSeries`)
- 散布図 (`ScatterSeries`)
- 棒グラフ (`BarSeries`)
- ヒストグラム (`HistogramSeries`)
- 箱ひげ図 (`BoxPlotSeries`)
- 円グラフ (`PieSeries`)
- 面グラフ (`AreaSeries`)
- 積み上げ棒グラフ (`StackedBarSeries`)
- 範囲/帯グラフ (`RangeSeries`)
- レーダーチャート (`RadarSeries`)

> 円グラフ (`PieSeries`) は X/Y 軸を使わない特殊な描画のため、他の系列とは混在させず単独の `PlotModel` で使用してください。

## 主な機能

- ズーム(マウスホイール) / パン(左ドラッグ) / リセット(ダブルクリック)
- マウス直下の最近傍データ点のツールチップ表示
- X/Y 軸の数値目盛りと Bar 系列のカテゴリラベル表示
- 凡例(系列名と色)の表示、クリックによる系列の表示/非表示切り替え
- データラベル表示(`Series.ShowDataLabels` / `Series.DataLabelFormat`)による Line/Scatter/Bar 系列の数値表示
- PNG 画像への保存
- 印刷(アスペクト比を保持したスケーリング・中央配置)
- Y 軸の対数(Log10)スケール表示(`Axis.IsLogarithmic`)。0以下の値は描画から除外されます
- X 軸の対数(Log10)スケール表示(`Axis.IsLogarithmic`)。0以下の値は描画から除外されます
- グリッド線(目盛線)の表示/非表示切り替え(`PlotModel.ShowGridLines`)
- 線・面グラフの破線スタイル指定(`Series.DashStyle`: Solid/Dash/Dot/DashDot)
- 凡例の表示位置指定(`PlotModel.LegendPosition`: TopRight/TopLeft/BottomRight/BottomLeft)
- プロット領域内の任意データ座標へのテキスト注釈表示(`PlotModel.Annotations`)
- セカンダリ(第2)Y軸対応(`PlotModel.Y2Axis` / `Series.UseSecondaryYAxis`)
- セカンダリ(第2)X軸対応(`PlotModel.X2Axis` / `Series.UseSecondaryXAxis`)。X2系列の自動範囲計算、対数軸、上端目盛り・ラベル・グリッド表示に対応
- カスタム描画フック(`PlotView.CustomRender` イベントによる任意図形の追加描画)
- アニメーション付きズーム・パン遷移、タッチ操作(ピンチズーム等)対応
- テーマ/配色プリセットの切り替え(`PlotModel.Theme`: `PlotTheme.Light` / `PlotTheme.Dark` / `PlotTheme.Colorful`)
- 日時軸対応(`Axis.IsDateTime`)。X値を `DateTime.ToOADate()` で数値化して使用し、目盛りは秒/分/時/日/月/年単位で自動選択され、ラベル書式は `Axis.DateTimeFormat`(既定 `"yyyy/MM/dd"`)で変更可能。日時X軸のラベルは間引かず、WinForms/WPFとも45度回転して描画します
- グラフ全体のフォントファミリー指定(`PlotModel.FontFamilyName`)。既定値は `"Meiryo UI"` で、WinForms/WPFのタイトル・凡例・軸ラベル・データラベル・注釈に適用されます
- 軸のOrigin設定(`Axis.Origin`)、目盛方向(`Axis.TickDirection`: Outward/Inward/Cross)、TickLabels表示制御(`Axis.TickLabelsVisible`)
- 軸ValueLabels(`Axis.ValueLabels`)と標準目盛の表示方式切り替え(`Axis.LabelMode`、`ChartValueLabelsEnabled`)
- 軸マーカー(`Axis.Markers`: 矢印方向を含む)、専用グリッド線、AlarmZone(軸範囲に追随する背景帯)。AlarmZoneは明示範囲のほか、上下限の内側/外側を指定可能
- セカンダリY軸専用のMarker/GridLineと、主軸・Y2軸間の表示範囲同期
- `Hole=100`等の欠損値契約。入力点を保持したまま、描画・範囲計算では欠損として扱い、`ConnectAcrossMissingValues = false`の場合は欠損位置で線を分割
- 欠損値をまたぐ線の接続制御(`Series.ConnectAcrossMissingValues` / `ChartDataSeries.ConnectAcrossMissingValues`)
- スクロールバーボタン(`Axis.ShowScrollButtons`)とMin/Max境界に連動した表示範囲スクロール。単位未指定時は従来互換の表示幅20%を使用
- C1C互換Wrapperの表示範囲API(`ChartControl.DisplayXRange` / `DisplayYRange`、`SetDisplayRange`、`ScrollX`、`ScrollY`、`AxisScroll`イベント)
- C1C互換のヘッダーAPI(`ChartControl.Header.Text` / `Compass` / `Visible`)
- C1C互換の軸スクロール設定(`ChartAxis.ScrollBar.Scale`、`Unit`、`Min`、`Max`、`Visible`)
- C1C互換の座標検索(`ChartGroup.CoordToDataIndex`、`ChartControl.CoordToDataIndex`)
- C1C互換の画像取得(`ChartControl.GetImage`の4種類のオーバーロード)
- C1C互換の画像保存(`ChartControl.SaveImage`のファイル、`Stream`、`byte[]`、サイズ指定オーバーロード)
- 描画サイズ設定(`PlotModel` / `ChartArea`の`TitleFontSize`、`FooterFontSize`、`AxisLabelFontSize`、`AxisTitleFontSize`、`DataLabelFontSize`、`LegendFontSize`、`ValueLabelFontSize`、`AlarmZoneFontSize`)
- プロット余白設定(`PlotMarginLeft`、`PlotMarginTop`、`PlotMarginRight`、`PlotMarginBottom`)。未設定時は従来の左48px、上32px、右16px、下32px
- `Chart2DPropBag` XMLの読み込み(Header、Legend、Axes、ChartGroups、GridLines、Markers、AlarmZones、スクロール設定等)
- レーダーチャートの同心円グリッドと目盛りラベル設定(`PlotModel.RadarGridLevels` / `PlotModel.RadarGridLabels`)
- レーダーチャートの項目配置方向(`PlotModel.RadarDirection`)。既定はC1Chart互換の反時計回り
- レーダー系列のカテゴリラベル、複数系列、系列別マーカー(`RadarSeries.CategoryLabels` / `MarkerShape` / `MarkerSize`)
- レーダー系列の凡例表示、系列別色・マーカー、凡例クリックによる表示/非表示切り替え
- レーダー系列の塗りつぶし(`RadarSeries.FillArea`)。既定は無効で、重なりによる色の混合を防止

## 使い方 (C# - コア直接利用)

```csharp
using UnifiedChart;
using UnifiedChart.WinForms;

var plotView = new PlotView { Dock = DockStyle.Fill };

var model = new PlotModel { Title = "サンプル" };
var line = new LineSeries { Name = "Line", Color = "Blue" };
line.Points.Add((0, 0));
line.Points.Add((10, 10));
model.ChartGroups[0].ChartData[0].SeriesList.Add(line);

plotView.Model = model;
```

### レーダーチャートの目盛りラベル

レーダーチャートでは、目盛りの段階数とラベルを外部から指定できます。ラベルは内側から外側の順に指定します。

```csharp
var model = new PlotModel { Title = "評価結果" };
model.RadarDirection = RadarDirection.CounterClockwise;
model.RadarGridLevels = 3;
model.RadarGridLabels = ["最小", "中間", "最大"];

var radar = new RadarSeries
{
	Name = "実績",
	Color = "SteelBlue",
	MarkerShape = MarkerShape.Diamond,
	MarkerSize = 6,
	ShowMarkers = true,
	FillArea = false
};
radar.CategoryLabels.AddRange(["売上", "品質", "納期"]);
radar.Points.Add((0, 80));
radar.Points.Add((1, 60));
radar.Points.Add((2, 90));
model.ChartGroups[0].ChartData[0].SeriesList.Add(radar);

plotView.Model = model;
```

`RadarGridLabels` が未設定の場合は数値ラベルを表示します。ラベル数が `RadarGridLevels` と異なる場合も、安全のため数値ラベルに戻ります。

C1C互換Wrapperでは、`ChartDataSeries.CategoryLabels`に外周の項目名を設定できます。

```csharp
var group = chart.ChartArea.ChartGroups[0];
group.ChartType = Chart2DType.Radar;
var series = group.ChartData[0].AddNewSeries();
series.CategoryLabels.AddRange(["売上", "品質", "納期"]);
series.Add(0, 80);
series.Add(1, 60);
series.Add(2, 90);
chart.RefreshChart();
```

カテゴリラベルの数がデータ点数と一致する場合に外周へ表示し、一致しない場合は従来のX値表示へ戻ります。

`FillArea`を`true`にすると、欠損値がない3点以上のレーダー系列を半透明で塗りつぶします。既定値は`false`です。欠損値を含む場合は塗りつぶさず、連続する点だけを線で接続します。

系列別のマーカーは、Wrapperでは次のように設定できます。

```csharp
series.ShowMarkers = true;
series.SymbolStyle.Shape = SymbolShapeEnum.Diamond;
series.SymbolStyle.Size = 6;
series.ConnectAcrossMissingValues = false;
```

## 使い方 (VB.NET - コア直接利用)

```vb
Imports UnifiedChart
Imports UnifiedChart.WinForms

Dim plotView As New PlotView With {.Dock = DockStyle.Fill}

Dim model As New PlotModel With {.Title = "サンプル"}
Dim line As New LineSeries With {.Name = "Line", .Color = "Blue"}
line.Points.Add((0, 0))
line.Points.Add((10, 10))
model.Series.Add(line)

plotView.Model = model
```

詳細な実装例は `UnifiedChart.Samples.CSharp` および `UnifiedChart.Samples.VisualBasic` を参照してください。

## 使い方 (C1C互換Wrapperでの既存コード移行例)

既存の C1C（`C1.Win.C1Chart.2`）のコードを最小限の変更で動かす場合、`UnifiedChart.C1CWrapper.ChartControl` を使用します。

### C# での C1CWrapper 移行例

```csharp
using UnifiedChart.C1CWrapper;

// コントロールの初期化（デザイナーで配置されていた C1Chart の代替）
var chart = new ChartControl { Dock = DockStyle.Fill };

// PropBag XML から見た目・軸・凡例・グリッド・アラーム帯設定を一括復元
chart.PropBag = xmlString; // Chart2DPropBag の XML 文字列

// 実データの投入（C1C と同様の階層構造）
var group = chart.ChartArea.ChartGroups[0];
group.ChartType = Chart2DType.LineSymbols;
group.MissingValueHole = 100; // Hole 欠損値契約

var series = group.ChartData[0].AddNewSeries();
series.Label = "実績値";
series.Add(1, 10.5);
series.Add(2, 100);  // 欠損点（線分が分割され、範囲計算からも除外）
series.Add(3, 14.2);

// 軸マーカーや ValueLabels の追加（QC管理図の限界線や特定ラベル）
chart.ChartArea.AxisY.ValueLabels.Add(new ChartValueLabel { Value = 12.0, Text = "CL" });
chart.ChartArea.AxisY.Markers.Add(new ChartAxisMarker { Value = 15.0, Text = "UCL", ArrowDirection = "East" });

// 反映
chart.RefreshChart();
```

### VB.NET での C1CWrapper 移行例

```vb
Imports UnifiedChart.C1CWrapper

Dim chart As New ChartControl With {.Dock = DockStyle.Fill}
chart.PropBag = xmlString

Dim group = chart.ChartArea.ChartGroups(0)
group.ChartType = Chart2DType.LineSymbols
group.MissingValueHole = 100

Dim series = group.ChartData(0).AddNewSeries()
series.Label = "実績値"
series.Add(1, 10.5)
series.Add(2, 100)
series.Add(3, 14.2)

chart.ChartArea.AxisY.ValueLabels.Add(New ChartValueLabel With {.Value = 12.0, .Text = "CL"})
chart.RefreshChart()
```

## ビルド・テスト

```powershell
dotnet build UnifiedChart.slnx
dotnet test UnifiedChart.Tests\UnifiedChart.Tests.csproj
```

## CoOne / C1C からの移行

移行は「使用箇所の棚卸し → Wrapperで構造を維持した置換 → 画面比較 → 必要箇所だけコアAPIへ最適化」の順で進めると安全です。

1. [COONE_INVENTORY.md](COONE_INVENTORY.md) に製品バージョン、Designerコード、PropBag、軸・系列・操作・出力を記録する。
2. **Designerコード（InitializeComponent）の置換**:
   `C1.Win.C1Chart.C1Chart` を `UnifiedChart.C1CWrapper.ChartControl` に型置換。`ChartArea`、`ChartGroups`、`ChartData`、`SeriesList` へのアクセスコードはそのまま維持できます。
3. **PropBag の読み込み**:
   リソースや DB に保存されている `Chart2DPropBag` XML 文字列を `ChartControl.PropBag` プロパティへ渡すことで、Header、Legend、Axes、ChartGroups、Markers、GridLines、AlarmZones、ValueLabels、ScrollBar などの見た目設定が自動復元されます。
   *(※PropBagは実データを保持しないため、データ投入はコード側で行います)*
4. **軸仕様の確認と差異検証**:
   - `Min`/`Max`、`Origin`（交差原点）、`TickDirection`（目盛方向: Inward/Outward/Cross）、`TickLabelsVisible`（数値ラベル表示/非表示）
   - `LabelMode` / `ChartValueLabelsEnabled`: 標準目盛か任意のValueLabels指定（MEAN, +1SD, UCL/LCL等）か
   - `AlarmZone`: 明示範囲指定（`Explicit`）および上下限基準の内外帯指定（`Inside`/`Outside`、軸範囲に追随する背景帯）
5. **特殊データ契約の確認**:
   - `Hole=100` などの欠損値: データを破損・削除せず系列内に保持したまま、描画・範囲計算のみ除外して線を分割します。
	  - セカンダリY軸（Y2）: 既定では2番目以降の実データグループも主Y軸へ描画します。`UseSecondaryYAxis` または `EnableSecondaryYAxis` を明示するか、`AutoAssignSecondaryYAxis = true` を設定した場合にY2へ割り当て、Y2専用の Marker / GridLine も独立描画します。
	   - スクロール: スクロールボタン（`ShowScrollButtons`）、スクロール倍率（`ScrollBar.Scale`）、1回あたりのスクロール単位（`ScrollUnit`、未指定時は20%幅）、および移動境界（`ScrollMin`/`ScrollMax`）、主Y軸・Y2軸の連動（`SynchronizeWithY2`）。スクロール後は`ChartControl.AxisScroll`で通知を受け取れます。
	- 座標検索: `ChartGroups[index].CoordToDataIndex(x, y, focus, out seriesIndex, out pointIndex, out distance)`で最近傍系列・データ点を取得できます。
	- 画像出力: `ChartControl.GetImage()`、`GetImage(Size)`、`GetImage(ImageFormat)`、`GetImage(ImageFormat, Size)`でチャートを`Image`として取得できます。
	- 描画サイズ: `ChartArea.TitleFontSize`、`FooterFontSize`、`AxisLabelFontSize`、`AxisTitleFontSize`、`DataLabelFontSize`、`LegendFontSize`、`ValueLabelFontSize`、`AlarmZoneFontSize`で各文字サイズを指定できます。未設定時は従来の固定値を使用します。
	- プロット余白: `ChartArea.PlotMarginLeft`/`Top`/`Right`/`Bottom`でプロット領域の余白を指定できます。
6. **段階的リファクタリング**:
   まずは Wrapper 経由で既存画面を完全動作させた後、新規機能やパフォーマンスが求められる画面から、共有コア `UnifiedChart.PlotModel` の直接利用へ徐々に切り替えることが可能です。

詳細なAPI対応、XMLの別名、互換上の制約は [C1C_MIGRATION_MAPPING.md](C1C_MIGRATION_MAPPING.md) を参照してください。対象は`C1C`(2D)であり、`C1C3D`は対象外です。

## 他OSSチャートライブラリとの比較評価

UnifiedChart 独自開発の妥当性を裏付ける社内資料として、OxyPlot / ScottPlot / LiveCharts2 / Chart.js(WebView2経由)との
機能網羅性・C1C資産移行のしやすさの比較評価を [OSS_CHART_LIBRARY_COMPARISON.md](OSS_CHART_LIBRARY_COMPARISON.md) にまとめています。

## 今後の計画(未実装機能)

現時点で確認できている未対応機能を、移行対象の実使用箇所に応じて優先順位付けします。

### Phase 1(最優先) — 完了

- [x] セカンダリ(第2)Y軸対応
  - `PlotModel` に `Y2Axis`(nullable)を追加し、系列側に `UseSecondaryYAxis` フラグを持たせる方向で設計した
  - `CoordinateTransform` / `PlotRenderHelper.CreateTransform(...)` を第2軸の変換にも対応させた
  - WinForms/WPF 双方の目盛描画(`DrawAxisTicks`)を左右2軸表示に対応させた
  - 既存の単一Y軸APIとの後方互換性を維持した(第2軸未設定時は現行動作のまま)

### Phase 2 — 完了

- [x] カスタム描画フック(ユーザーコールバックによる任意図形描画の拡張ポイント)
  - `PlotView` に `CustomRender` イベントを追加し、通常描画後に任意の `Graphics`/`DrawingContext` へアクセスできるようにした

### Phase 3 — 完了

- [x] アニメーション付き描画・トランジション
  - マウスホイールズームおよびタッチのピンチズームの軸範囲変化を、WinForms は `System.Windows.Forms.Timer`、WPF は `DispatcherTimer` によるイージング補間(ease-out cubic, 150ms)で滑らかに描画するようにした
- [x] タッチ操作(ピンチズーム等)への対応
  - WinForms は `WM_GESTURE`(`GID_ZOOM`/`GID_PAN`)を `WndProc` でハンドリングし、ピンチズームとタッチパンに対応した
  - WPF は `IsManipulationEnabled` と `ManipulationDelta` イベントでピンチズーム・パンに対応した

### Phase 4 — 完了

- [x] テーマ/配色プリセットの切り替え機能
  - 共有コアに `PlotTheme` クラスを追加し、背景色・タイトル色・軸ラベル色・グリッド線色・凡例文字色・系列パレット・既定系列色を一括管理できるようにした
  - `PlotTheme.Light`(既定)/`PlotTheme.Dark`/`PlotTheme.Colorful` の3種類のプリセットを用意した
  - `PlotModel.Theme` プロパティ(既定 `PlotTheme.Light`)を追加し、WinForms/WPF 両レンダラーの背景・目盛線・ラベル・タイトル・凡例・円グラフ/積み上げ棒グラフのパレット・Bar/Histogram/Scatterの既定色をテーマから取得するようにした
  - 系列側(`Series.Color` 等)に明示的な色が設定されている場合はそちらを優先し、既存APIとの後方互換性を維持した

### Phase 5(現在進行中) — CoOne 互換の推進

- [x] C1C(2D) → UnifiedChart の一般的な API マッピング表作成
  - [C1C_MIGRATION_MAPPING.md](C1C_MIGRATION_MAPPING.md) にコントロール・系列・軸・見た目・凡例・操作機能・注釈・出力の対応表を整理した(`C1C3D` は対象外)

- [ ] CoOne 使用箇所の棚卸し
	- [COONE_INVENTORY.md](COONE_INVENTORY.md) のテンプレートを用いて、移行対象プロジェクトごとの CoOne 製品名・バージョン・使用ファイル・呼び出しコードを収集する
- [ ] 実プロジェクトでのマッピング検証・更新
  - 実際の使用箇所が判明次第、`C1C_MIGRATION_MAPPING.md` の内容を実コードに合わせて更新する
- [ ] 互換性ギャップの解消(必要に応じて機能追加)
	- マッピング表の「未対応・要検討項目」(カテゴリ軸、ローソク足、ヒートマップ、ValueLabelの位置・装飾、データバインディング等)を実使用箇所に基づいて実装する
- [ ] Designer 生成コード(InitializeComponent)の置き換えガイド作成
	 - CoOne コントロールを `PlotView` に置き換える際の典型的なコード変換例をドキュメント化する
- [ ] 移行後の画面比較検証
  - 移行前後のスクリーンショットや表示内容を比較し、差異があれば対応する

### 実装済み互換機能

- [x] C1C互換Wrapper (`UnifiedChart.C1CWrapper`) と `Chart2DPropBag` 読み込み
- [x] ChartGroup → ChartData → SeriesList の階層API
- [x] 軸のMin/Max、自動範囲、スクロールバーボタン、表示範囲スクロール
- [x] 軸Origin、目盛方向、TickLabels表示制御
- [x] 軸マーカー、専用グリッド線、AlarmZone
- [x] ChartValueLabels、標準目盛/ValueLabels切り替え、LabelCompassEnum.Auto相当の自動配置
- [x] AlarmZoneの明示範囲/内側/外側、Y2軸専用Marker/GridLine
- [x] ChartLabels、Y2軸、`Hole=100`欠損契約、系列表示切り替え
- [x] スクロール単位・境界・有効状態・Y2連動、および表示範囲API
