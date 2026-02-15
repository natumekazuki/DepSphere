# グラフホバー強調の追加

## Goal
- ノードにマウスホバーした時に視覚強調し、クリック対象の視認性を上げる。

## Task List
- [x] ホバー中ノードの強調表示（発光・スケール・ラベル強調）を実装する。
- [x] ポインタ移動/離脱時のホバー状態更新を実装する。
- [x] 既存の選択/フィルタ/ダブルクリック導線と競合しないよう調整する。
- [x] テストを更新して回帰確認する。
- [x] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- ポインタ移動イベントで `applyVisualSettings` が頻発すると描画負荷が増える可能性がある。
- ホバー強調と選択強調の優先順位が曖昧だと視認性が低下する可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: 操作仕様（ホバー時の視覚挙動）を追加するため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: ホバー時の発光・ラベル強調とポインタ離脱時リセットを実装。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行、65件成功。
