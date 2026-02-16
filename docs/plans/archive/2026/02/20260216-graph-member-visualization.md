# グラフのメソッド/プロパティ描画対応

## Goal
- クラスノードに加え、メソッドノード・プロパティノードをグラフ上に描画できるようにする。
- メンバーノードをクリックした際も既存のコード表示導線が破綻しないようにする。

## Task List
- [x] 実装計画を作成する。
- [x] 設計ドキュメントへメンバーノード描画仕様を追記する。
- [x] `GraphViewBuilder` を拡張し、メンバーノードと member エッジを生成する。
- [x] `GraphViewNode` / `GraphViewHtmlBuilder` を拡張し、メンバー識別と表示・遷移挙動を実装する。
- [x] テストを更新し、関連テストを実行して回帰がないことを確認する。

## Affected Files
- `docs/plans/20260216-graph-member-visualization.md`
- `docs/design/dep-visualizer-core.md`
- `src/DepSphere.Analyzer/GraphViewNode.cs`
- `src/DepSphere.Analyzer/GraphViewBuilder.cs`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewBuilderTests.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`

## Risks
- メンバーノード増加により初期描画が重くなる可能性がある。
- ノードID仕様の拡張がコード表示遷移に影響し、ダブルクリック時にコードが開けなくなる可能性がある。

## Design Check
- 新機能（描画単位の追加）に該当するため、`docs/design/dep-visualizer-core.md` の更新を実施する。

## Notes / Logs
- 2026-02-16: Issue #2 対応として計画を作成。
- 2026-02-16: `docs/design/dep-visualizer-core.md` にメンバーノード（メソッド/プロパティ）描画仕様と `Properties` 表示を追記。
- 2026-02-16: `GraphViewBuilder` でクラスノード配下にメソッド/プロパティノードを生成し、`member` エッジを追加。
- 2026-02-16: `GraphViewNode` に `NodeKind` / `OwnerNodeId` / `PropertyNames` を追加。
- 2026-02-16: `GraphViewHtmlBuilder` でメンバーノード情報表示と、メンバー選択時は親クラスIDへコード遷移する処理を追加。
- 2026-02-16: `GraphViewBuilderTests` / `GraphViewHtmlBuilderTests` を更新。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewBuilderTests` 実行（6件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（68件合格）。
