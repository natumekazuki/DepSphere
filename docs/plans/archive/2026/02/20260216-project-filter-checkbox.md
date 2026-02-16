# プロジェクトチェックボックスフィルタ対応

## Goal
- グラフ表示を `csproj` 単位で絞り込めるよう、プロジェクトチェックボックスフィルタを追加する。

## Task List
- [x] 計画を作成し、変更対象を明確化する。
- [x] `GraphViewHtmlBuilder` にプロジェクトフィルタUI（チェックボックス）を追加する。
- [x] プロジェクト選択状態に連動してノード・エッジの可視制御ロジックを実装する。
- [x] `GraphViewHtmlBuilderTests` と設計ドキュメントを更新する。
- [x] 関連テストを実行し、回帰がないことを確認する。

## Affected Files
- `docs/plans/20260216-project-filter-checkbox.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- プロジェクト帰属推定に失敗したノードの表示条件が直感とずれる可能性がある。
- フィルタ条件（種別・プロジェクト・表示限定）が重なると表示ゼロになりうる。

## Design Check
- 3D表示操作仕様のフィルタ導線を拡張するため、`docs/design/dep-visualizer-core.md` の更新が必要。

## Notes / Logs
- 2026-02-16: Issue #8 対応（プロジェクト単位フィルタ）として計画を作成。
- 2026-02-16: `GraphViewHtmlBuilder` にプロジェクトチェックボックス（全選択/全解除）UIを追加。
- 2026-02-16: `contains/member/external` エッジからプロジェクト帰属ノードを計算し、可視制御へ反映。
- 2026-02-16: 履歴状態にプロジェクトフィルタの選択状態を追加。
- 2026-02-16: `GraphViewHtmlBuilderTests` と `docs/design/dep-visualizer-core.md` を更新。
- 2026-02-16: `dotnet test ... --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test ... --filter GraphViewBuilderTests` 実行（6件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（68件合格）。
