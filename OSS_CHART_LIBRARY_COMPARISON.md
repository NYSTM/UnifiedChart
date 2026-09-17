# UnifiedChart vs 主要OSSチャートライブラリ 比較評価

本ドキュメントは、UnifiedChart(自社開発コアライブラリ)と主要な.NET向けOSSチャートライブラリとの比較を行い、
**「なぜUnifiedChartを自社開発するのか」** の妥当性を裏付けるための社内資料です。

評価観点は以下の2点を重視します。

1. **機能網羅性**: 系列種類・軸機能・注釈・凡例・テーマ等のカバー範囲
2. **C1C(CoOne)資産移行のしやすさ**: 既存 `C1C` ベースのWinForms資産をどれだけ低コストで移行できるか

> 本資料は公開されている公式ドキュメント・リポジトリ情報に基づく定性評価です。比較対象のバージョンや機能は更新されるため、採用判断時は最新情報で再確認してください。
> 実際の採用検討時は各ライブラリの最新バージョンでの詳細な検証(PoC)を別途実施してください。

## 1. 比較対象ライブラリ概要

| ライブラリ | ライセンス | 対応UI基盤 | 開発体制 | 備考 |
|---|---|---|---|---|
| **UnifiedChart**(本プロジェクト) | 自社(社内利用) | WinForms / WPF(コアはUI非依存) | 社内 | C1C移行専用に設計 |
| **OxyPlot** | MIT | WPF / WinForms / Avalonia / Xamarin 等 | OSSコミュニティ(更新頻度は緩やか) | 老舗の.NET向け汎用プロットライブラリ |
| **ScottPlot** | MIT | WPF / WinForms / Avalonia / MAUI / Blazor(WebAssembly) 等 | OSSコミュニティ(活発) | 高速描画・大量データ・リアルタイム更新に強み |
| **LiveCharts2** | MIT(一部商用サポートあり) | WPF / WinForms / Avalonia / MAUI / Uno / Blazor | OSSコミュニティ(活発、旧LiveChartsから刷新) | アニメーション豊富なMVVM指向 |
| **Chart.js**(WebView2経由) | MIT | JavaScript(WebView2/Blazor Hybrid等でホスト) | OSSコミュニティ(非常に活発、Web業界標準) | .NETネイティブではなくWeb技術+ブリッジが必要 |

## 2. 機能網羅性比較

### 2.1 系列(グラフ種類)

| 系列種類 | UnifiedChart | OxyPlot | ScottPlot | LiveCharts2 | Chart.js |
|---|:---:|:---:|:---:|:---:|:---:|
| 折れ線(Line) | ○ | ○ | ○ | ○ | ○ |
| 散布図(Scatter) | ○ | ○ | ○ | ○ | ○ |
| 棒グラフ(Bar) | ○ | ○ | ○ | ○ | ○ |
| 積み上げ棒(StackedBar) | ○ | ○ | ○ | ○ | ○ |
| 面(Area) | ○ | ○ | ○ | ○ | ○ |
| 円(Pie) | ○ | ○ | ○ | ○ | ○ |
| ヒストグラム | ○ | ○(要組み合わせ) | ○ | △(要カスタム) | △(要プラグイン) |
| 箱ひげ図(BoxPlot) | ○ | ○ | ○ | △(要カスタム/拡張) | △(要プラグイン: chartjs-chart-boxplot) |
| 範囲/帯(Range) | ○ | ○ | ○ | ○ | △(要プラグイン) |
| ローソク足/株価 | ✕ | ○(CandleStickSeries) | ○ | ○ | ○(要プラグイン: chartjs-chart-financial) |
| レーダー/極座標 | ○(`RadarSeries`) | ○ | ○ | ○ | ○ |
| ヒートマップ | ✕ | △(要拡張) | ○ | △(要拡張) | ○(要プラグイン) |
| 3D | ✕ | ✕ | △(限定的) | ✕ | △(要プラグイン) |

### 2.2 軸・座標系

| 機能 | UnifiedChart | OxyPlot | ScottPlot | LiveCharts2 | Chart.js |
|---|:---:|:---:|:---:|:---:|:---:|
| 対数軸(Log10) | ○ | ○ | ○ | ○ | ○ |
| セカンダリY軸 | ○ | ○ | ○ | ○ | ○ |
| セカンダリX軸 | ○(`PlotModel.X2Axis` / `Series.UseSecondaryXAxis`) | ○ | ○ | ○ | ○ |
| カテゴリ軸 | △(Bar/StackedBar専用ラベル) | ○ | ○ | ○ | ○ |
| 日時軸 | ○(`Axis.IsDateTime`、秒/分/時/日/月/年の自動目盛り) | ○ | ○ | ○ | ○ |
| 軸範囲自動計算 | ○(Min/Max、表示範囲スクロール境界、ScrollBar.Scale) | ○ | ○ | ○ | ○ |

### 2.3 装飾・付帯機能

| 機能 | UnifiedChart | OxyPlot | ScottPlot | LiveCharts2 | Chart.js |
|---|:---:|:---:|:---:|:---:|:---:|
| 凡例表示位置切替 | ○(4隅) | ○(詳細指定可) | ○(詳細指定可) | ○(詳細指定可) | ○(詳細指定可) |
| データラベル表示 | ○(Line/Scatter/Bar) | △(要カスタムアノテーション) | ○ | ○ | ○(要プラグイン: datalabels) |
| テキスト注釈 | ○ | ○(豊富: Arrow/Rectangle/Ellipse等) | ○(豊富) | ○ | △(要プラグイン: annotation) |
| マーカー形状 | ○(11種、系列別サイズ・形状、軸Markerの矢印方向にも対応) | ○(豊富) | ○(豊富) | ○(豊富) | ○(豊富) |
| ズーム/パン/リセット | ○ | ○ | ○(高機能、GPU支援あり) | ○ | ○(要プラグイン: zoom) |
| アニメーション | ○(ズーム/パンのみ) | ✕ | △(限定的) | ◎(標準で豊富) | ◎(標準で豊富) |
| カスタム描画フック | ○(CustomRenderイベント) | ○(Annotation拡張) | ○(Plottable拡張) | ○(VisualElement拡張) | △(Canvas APIで可能だがC#から直接操作不可) |
| テーマ/カラーパレット | ○(Light/Dark/Colorful) | ○ | ○ | ○(豊富なテーマ) | ○(CSSベース) |
| PNG保存 | ○ | ○ | ○ | ○ | ○(Canvas.toDataURL) |
| 印刷対応 | ○ | △(要自前実装) | △(要自前実装) | △(要自前実装) | ✕(Web印刷経由のみ) |
| C1C互換軸表示(Origin/目盛方向/TickLabels/ValueLabels) | ○(WinForms/WPF、Wrapper/PropBag対応) | △(個別再実装) | △(個別再実装) | △(個別再実装) | △(設定再設計) |
| AlarmZone/軸背景帯 | ○(明示/内側/外側、Y2、Wrapper/PropBag対応) | △(Annotation等で再実装) | △(Span等で再実装) | △(VisualElement等で再実装) | ○(プラグイン/カスタム実装) |
| C1C欠損・スクロール互換 | ○(`Hole=100`、単位/倍率/境界/Y2連動、AxisScroll、Wrapper対応) | △(個別実装) | △(個別実装) | △(個別実装) | △(プラグイン/ブリッジ実装) |

### 2.4 総評(機能網羅性)

- **OxyPlot・ScottPlot・LiveCharts2** はいずれもUnifiedChartより広範な系列種類(ローソク足・ヒートマップ等)を標準で持つ。UnifiedChartもレーダーとセカンダリX軸を実装している。日時軸はUnifiedChartも`Axis.IsDateTime`で対応済みだが、カテゴリ軸などその他の高度な軸機能は他OSSの方が充実している。
- **Chart.js** はエコシステム(プラグイン)が非常に広く機能面では最も網羅的だが、.NETネイティブではないため WebView2/Blazor Hybrid 経由のブリッジ実装(JS⇔C#相互運用)が別途必要。
- UnifiedChartは「C1C(2D)からの移行に必要な機能」に的を絞って実装しているため、汎用チャートライブラリと比べると機能種類は少ないが、**C1C互換Wrapper、ChartGroup階層、PropBag読み込み、ValueLabels、AlarmZoneの内外、Y2専用装飾、`Hole=100`、ScrollBar.Scale、AxisScroll、CoordToDataIndex、GetImage、スクロール表示範囲まで移行向け機能を優先している**。

## 3. C1C資産移行のしやすさ比較

| 観点 | UnifiedChart | OxyPlot | ScottPlot | LiveCharts2 | Chart.js |
|---|---|---|---|---|---|
| API設計思想の近さ | ◎ `UnifiedChart.C1CWrapper`で`ChartControl`/`ChartArea`/`ChartAxis`/`ChartGroup`階層を提供 | △ 独自のモデル(`PlotModel`/`LineSeries`等)だが概念は類似 | △ 独自のモデル(`Plot.Add.Scatter`等の宣言的API) | △ MVVM/バインディング前提の独自モデル(`ISeries`) | ✕ JSONベースのオプションオブジェクト。C#プロパティとの対応が薄い |
| WinForms資産の再利用性 | ◎ `C1CWrapper.ChartControl`で既存のChartArea/ChartAxis/ChartGroup構造を維持し、`PlotView`へ委譲 | ○ `PlotView`コントロール提供、差し替えは可能だが書き直し量は中程度 | ○ 同上 | ○ 同上(ただしAvalonia/MAUI志向が強くWinForms実装は後発) | ✕ WinFormsへの組み込みは`WebView2`コントロール経由となり、ホスト側とJS側の二重実装が必要 |
| VB.NET対応 | ◎ サンプル・ドキュメントともにVB.NET対応済み | △ 利用は可能だが公式サンプルはC#中心 | △ 同上 | △ 同上 | ✕ JS側のロジックはVB.NETと無関係になり恩恵が薄い |
| 移行工数(既存C1Cコード1本あたりの書き換え量、定性評価) | 小(プロパティ名・型がほぼ対応) | 中(概念の再マッピングが必要) | 中(宣言的APIへの書き直しが必要) | 中〜大(MVVM前提のためコードビハインド中心のC1C実装からの移行コストが高い) | 大(言語・実行基盤が異なるため事実上作り直し) |
| 移行マッピング資料の有無 | ◎ 本リポジトリに `C1C_MIGRATION_MAPPING.md` として整備済み | ✕ 存在しない(自作が必要) | ✕ 存在しない(自作が必要) | ✕ 存在しない(自作が必要) | ✕ 存在しない(自作が必要) |
| 学習コスト(既存C1C経験者にとって) | 低(Wrapperの命名・階層が近い。未対応機能はマッピング表で明示) | 中 | 中 | 中〜高(リアクティブ・バインディング概念の習得が必要) | 高(JS/Web技術の習得が必要) |

## 4. パフォーマンス傾向(参考、定性評価)

大量データ・リアルタイム更新を主眼とした場合の傾向(公開情報・一般的評価に基づく参考情報。数値ベンチマークではない):

| ライブラリ | 大量点描画 | リアルタイム更新 | 備考 |
|---|---|---|---|
| UnifiedChart | 中(数千点規模までは実用範囲、GDI+/WPF標準描画) | 中(タイマーベースのイージング補間あり) | 大規模データ最適化は未実施 |
| OxyPlot | 中 | 中 | 標準的なGDI+/WPF描画、大量データでは工夫が必要 |
| ScottPlot | 高 | 高 | 大量データ・高頻度更新に特化した設計(間引き描画等) |
| LiveCharts2 | 中〜高 | 高(アニメーション前提) | SkiaSharpベースでGPU寄りの描画性能 |
| Chart.js | 中 | 中〜高 | Canvas描画、WebView2のオーバーヘッドが追加でかかる |

> 本項目は将来的にProfiler Agentによる実測ベンチマーク(大量点描画・更新頻度別のフレームレート等)で裏付けることを推奨します。

## 5. ライセンス・保守性

| ライブラリ | ライセンスリスク | 保守継続性リスク | 社内カスタマイズ自由度 |
|---|---|---|---|
| UnifiedChart | なし(自社コード) | 自社次第(要員確保が前提) | 最大(ソース完全所有) |
| OxyPlot | MIT、商用利用に問題なし | 更新頻度が緩やかで将来的な停滞リスクあり | 中(フォークすれば対応可能) |
| ScottPlot | MIT、商用利用に問題なし | 活発、リスク低 | 中 |
| LiveCharts2 | MIT(コア)、一部エンタープライズ機能は商用ライセンスの可能性あり(要最新規約確認) | 活発、リスク低 | 中 |
| Chart.js | MIT、商用利用に問題なし | 非常に活発、リスク最低 | 高(Web標準のためエコシステムが広い)だが.NET統合部分は自社実装が必要 |

## 6. 結論

- **機能網羅性のみで見れば**、OxyPlot・ScottPlot・LiveCharts2はいずれもUnifiedChartより多機能であり、Chart.jsはエコシステムの広さで圧倒的に優位。
- しかし**C1C資産移行のしやすさ**という観点では、これらのOSSはいずれも「概念は近いが命名・構造は独自」であり、`C1C` からの移行には相応の設計変換コストがかかる。特にLiveCharts2はMVVM/バインディング前提でありコードビハインド中心の既存C1C実装からの移行負荷が大きく、Chart.jsはランタイム自体が異なるため事実上の作り直しとなる。
- UnifiedChartは機能を意図的にC1C 2D相当にスコープした上で、Wrapperのプロパティ名・型・階層をC1C由来のものに近づけている。軸Origin、目盛方向、TickLabels非表示、ValueLabels、AlarmZone、Y2専用Marker/GridLine、`Hole=100`、スクロールボタンと表示範囲連動、PropBag読み込みも実装済みであり、**移行対象コード量が多い場合の総移行工数を抑えられる**という点で独自開発の投資対効果は妥当と判断できる。
- 一方で、カテゴリ軸・ローソク足チャート・ヒートマップ等、UnifiedChart未対応の機能を必要とするプロジェクトについては、該当箇所のみOSSライブラリ(特にScottPlotまたはOxyPlot)を併用する、または該当機能をUnifiedChartに追加実装する方針を個別に検討する(日時軸とレーダーは対応済み)。

## 7. 今後の検証候補

- ScottPlot / OxyPlot を用いたPoCベンチマーク(同一データセットでの描画時間・メモリ使用量計測)
- LiveCharts2のWinForms実装成熟度の追加調査
- Chart.js + WebView2ブリッジのプロトタイプ作成によるオーバーヘッド実測
