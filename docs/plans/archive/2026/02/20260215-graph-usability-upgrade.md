# グラフ可読性・操作性アップグレード

## Goal
- グラフ上でクラス名/メソッド名を把握しやすくする。
- 固定描画を解消し、ユーザー操作で見やすく調整できるようにする。

## Task List
- [x] ノードにクラス名ラベルを表示し、ノード情報にメソッド名を含める。
- [x] 自動回転を停止し、右ドラッグ回転・左ドラッグ平行移動・ホイールズームを有効化する。
- [x] ノード倍率/距離倍率を調整できるUIを追加する。
- [x] 関連テストを更新する。
- [x] 設計ドキュメントを更新し、回帰確認後に計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewNode.cs`
- `src/DepSphere.Analyzer/GraphViewBuilder.cs`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewBuilderTests.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- ラベル描画が多い場合にレンダリング負荷が上がる可能性がある。
- マウス操作変更で既存ユーザーの期待と異なる可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: グラフ操作仕様とUI操作導線が変わるため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `GraphViewBuilder` で型ラベル短縮・メソッド名抽出を実装。
- 2026-02-15: `GraphViewHtmlBuilder` で OrbitControls + スケールUI + ノード情報パネルを追加。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` を実行し、62件成功。
