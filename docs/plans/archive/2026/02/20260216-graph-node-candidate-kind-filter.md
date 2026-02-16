# グラフ候補ノード表示とノード種別フィルタ対応

## Goal
- 大規模グラフでも全体構造を把握できるように、Project/Namespace/File/External の集約ノードを追加する。
- クラス配下に Method/Property/Field/Event ノードを追加し、対象ノード詳細を可視化する。
- ノード種別フィルタで表示対象を絞り、視認性を改善する。

## Task List
- [x] 計画を作成し、変更方針を定義する。
- [x] `GraphViewBuilder` を拡張し、Project/Namespace/File/Method/Property/Field/Event/External ノードを生成する。
- [x] `GraphViewHtmlBuilder` にノード種別フィルタUIを追加し、表示制御を実装する。
- [x] `GraphViewBuilderTests` / `GraphViewHtmlBuilderTests` と設計ドキュメントを更新する。
- [x] 関連テストを実行し、回帰がないことを確認する。

## Affected Files
- `docs/plans/20260216-graph-node-candidate-kind-filter.md`
- `src/DepSphere.Analyzer/GraphViewBuilder.cs`
- `src/DepSphere.Analyzer/GraphViewNode.cs`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewBuilderTests.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- フィルタ条件が複数重なることで、ノードが0件表示となり操作不能に見える可能性がある。
- 追加ノード数の増加により描画負荷が上がる可能性がある。
- 外部依存ノードの推定ロジックが誤分類を含む可能性がある。

## Design Check
- 3D表示操作仕様に新しいUI導線を追加するため、`docs/design/dep-visualizer-core.md` の更新が必要。

## Notes / Logs
- 2026-02-16: ユーザー要望（候補表示とノード種別フィルタ）対応として計画を作成。
- 2026-02-16: ユーザー指示により、追加候補 1〜5（Namespace/Project/File/FieldEvent/External）をすべて実装対象に拡張。
- 2026-02-16: `GraphViewBuilder` に構造ノード（Project/Namespace/File）・メンバーノード（Method/Property/Field/Event）・Externalノード生成を追加。
- 2026-02-16: `GraphViewHtmlBuilder` にノード種別フィルタUI（全選択/型中心）と表示制御ロジックを追加。
- 2026-02-16: `GraphViewNode` を拡張し `FieldNames` / `EventNames` を追加。
- 2026-02-16: `GraphViewBuilderTests` / `GraphViewHtmlBuilderTests` / `docs/design/dep-visualizer-core.md` を更新。
- 2026-02-16: `dotnet test ... --filter GraphViewBuilderTests` 実行（6件合格）。
- 2026-02-16: `dotnet test ... --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（68件合格）。
