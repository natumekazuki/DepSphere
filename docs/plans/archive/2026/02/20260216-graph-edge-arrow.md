# グラフ参照方向矢印の追加

## Goal
- Issue #5 の要件に合わせ、グラフエッジに参照方向（from -> to）を示す矢印を表示する。

## Task List
- [x] `GraphViewHtmlBuilder` のエッジ描画に矢印ヘッド（3Dオブジェクト）を追加する。
- [x] ノードスケール・表示フィルタと連動して矢印の位置/可視状態が正しく更新されるようにする。
- [x] `GraphViewHtmlBuilderTests` に矢印描画スクリプトの存在検証を追加する。
- [x] `docs/design/dep-visualizer-core.md` に矢印仕様（終点側表示）を追記する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行する。

## Affected Files
- `docs/plans/20260216-graph-edge-arrow.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 短いエッジで矢印がノードと重なり、見づらくなる可能性がある。
- エッジ本数が多いケースで描画負荷が増える可能性がある。

## Design Check
- 視覚仕様の明確化のため、`docs/design/dep-visualizer-core.md` の操作仕様に矢印ヘッド表示ルールを追記する。

## Notes / Logs
- 2026-02-16: `GraphViewHtmlBuilder` に `ConeGeometry` ベースの矢印ヘッド描画を追加。
- 2026-02-16: `refreshEdges()` でノードサイズ・表示状態に応じた矢印位置/向き/可視性更新を実装。
- 2026-02-16: `GraphViewHtmlBuilderTests` に矢印関連スクリプトの存在アサートを追加。
- 2026-02-16: `docs/design/dep-visualizer-core.md` のエッジ仕様を「終点ノード手前の矢印ヘッド表示」に更新。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（68件合格）。
