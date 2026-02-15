# グラフ探索の戻る/進む履歴

## Goal
- グラフ探索状態を履歴化し、戻る/進むで移動できるようにする。

## Task List
- [ ] UIに戻る/進むボタンを追加する。
- [ ] 選択・フィルタ・カメラ状態を履歴スナップショットとして保持する。
- [ ] 戻る/進む適用時に状態復元（表示/選択/視点）を実装する。
- [ ] 既存操作（クリック/検索/Fit/解除）と履歴記録の接続を行う。
- [ ] テストを更新して回帰確認する。
- [ ] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 履歴適用時に再帰的に履歴が積まれると破綻する可能性がある。
- カメラ状態復元とフィルタ復元の順序で見え方が不安定になる可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: 操作仕様として戻る/進むが追加されるため。

## Notes / Logs
- 2026-02-15: 初版作成。
