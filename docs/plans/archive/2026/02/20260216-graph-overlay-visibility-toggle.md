# グラフオーバーレイ表示切替対応

## Goal
- グラフWebView上の2つのオーバーレイ（操作オーバーレイ、ノード情報オーバーレイ）を個別に表示/非表示できるようにする。

## Task List
- [x] オーバーレイ表示切替の実装計画を作成する。
- [x] `GraphViewHtmlBuilder` のHTML/CSS/JSに個別トグルUIを追加する。
- [x] `GraphViewHtmlBuilderTests` に表示切替UIの存在を検証するテストを追加する。
- [x] 設計ドキュメントにオーバーレイ表示切替仕様を追記する。
- [x] テスト実行で回帰がないことを確認する。

## Affected Files
- `docs/plans/20260216-graph-overlay-visibility-toggle.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- トグルUI配置で既存グラフ操作（ドラッグ/クリック）を邪魔する可能性がある。
- オーバーレイ非表示時に再表示導線が消えると操作不能になる可能性がある。

## Design Check
- 3D表示仕様のUI操作が増えるため、`docs/design/dep-visualizer-core.md` の更新が必要。

## Notes / Logs
- 2026-02-16: Issue #1（グラフ内オーバーレイの表示切替）対応として計画を作成。
- 2026-02-16: `GraphViewHtmlBuilder` に操作オーバーレイ/ノード情報オーバーレイの個別トグルボタンを追加。
- 2026-02-16: `GraphViewHtmlBuilderTests` にトグルUI要素の検証を追加。
- 2026-02-16: `docs/design/dep-visualizer-core.md` の3D表示仕様へオーバーレイ表示切替を追記。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行し13件合格。
