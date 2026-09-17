# CoOne 使用箇所棚卸しテンプレート

このファイルは CoOne の使用箇所を収集するためのテンプレートです。移行作業の初期に、各項目を埋めてください。

- プロジェクト名:
- CoOne 製品名:C1.Win.C1Chart.2
- CoOne バージョン:2.0.20171.259
- 使用ファイル（ソース / Designer）:
  - ファイルパス1:
  - ファイルパス2:
- 呼び出しコードの例（重要なコンストラクタ・プロパティ・メソッド・イベント）:
  - (コードスニペット)
- C1C互換設定の使用有無:
  - `ChartArea` / `ChartGroups` / `ChartData` / `SeriesList`:
  - `Axis.Min` / `Axis.Max` / `Axis.Origin`:
  - 目盛方向 / TickLabels表示:
  - `AlarmZone` / `GridLine` / `Marker`:
  - スクロールバーボタン / 表示範囲 / `ZoomFactor`:
- `Chart2DPropBag` 使用有無と保存元:
  - PropBag文字列 / XMLファイル:
  - `Axes`、`ChartGroupsCollection`、`GridLines`、`Markers`、`AlarmZones` の使用要素:
- Designer への依存性（デザイナ生成コードがあるか）: Yes / No
- データバインディングの形式（DataTable / IList / バインド単一プロパティ / 仮想化等）:
- 表示しているグラフ種類（折れ線、散布図、棒グラフ、箱ひげ、ヒストグラム 等）:
- レーダー使用時の設定（`Chart2DType.Radar`、系列数、`CategoryLabels`、`RadarDirection`、`RadarGridLevels` / `RadarGridLabels`）:
- 系列マーカー設定（`SymbolStyle.Shape`、`SymbolStyle.Size`、`SymbolStyle.Color`、`SymbolStyle.OutlineColor`、表示有無）:
- レーダー塗りつぶし設定（`FillArea` / `FillStyle.Color1`）:
- 欠損値設定（`Hole` / `MissingValueHole`、`ConnectAcrossMissingValues`）:
- 操作機能（ズーム、パン、ポイント選択、領域選択、マーカー、ツールチップ 等）:
- 表示範囲の操作（スクロールボタン、マウスホイール、パン、リセット）:
- 画像出力 / 印刷 の使用有無:
- 画面例（スクリーンショットの保存先または参照先）:
- 優先度（高: すぐに移行必要 / 中: 次期で対応 / 低: 将来的に対応）:
- 補足・注意点:

---

記入例を複数集めることで、互換層で優先すべき API を特定します。可能であれば、Designer が生成する InitializeComponent 部分の該当コントロール生成コードも貼り付けてください。

## 記入・棚卸し推奨手順

1. **Grep検索による洗い出し**:
   - `C1.Win.C1Chart` や `C1C.2`、`.PropBag` をソリューション全体で検索し、利用しているフォーム・ユーザーコントロールをリストアップします。
2. **PropBag XML の採取**:
   - `*.resx` や設定ファイル、ソースコード内に埋め込まれた `<Chart2DPropBag>` XML を抽出し、使用されているタグ（`ValueLabels`, `AlarmZones`, `SB`, `Markers` 等）を確認します。
3. **データ投入ロジックの確認**:
   - `ChartData.SeriesList` への追加方法（`CopyDataIn`, `Add`, 配列直接代入等）および欠損値（`Hole`）の扱いを確認します。
4. **表示・操作テスト**:
   - `UnifiedChart.C1CWrapper.ChartControl` に置き換えた上で、軸の範囲（`Min`/`Max`/`Origin`）、目盛方向、スクロールバー、凡例、ツールチップの動作を実画面で比較確認します。
