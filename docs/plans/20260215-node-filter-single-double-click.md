# ノード絞り込み操作追加

## Goal
- シングルクリックで選択ノードに直接紐づくノードのみを表示する。
- ダブルクリックで既存のコード表示（nodeSelected通知）を実行する。
- 表示限定解除ボタンで全ノード表示へ戻せるようにする。

## Task List
- [ ] グラフUIに「表示限定解除」ボタンを追加する。
- [ ] シングルクリック時に隣接ノードのみ表示するフィルタ機能を実装する。
- [ ] ダブルクリック時のみホストへ nodeSelected を送るよう変更する。
- [ ] フォーカス操作とフィルタ状態の整合性を調整する。
- [ ] テストを更新し、回帰確認する。
- [ ] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- クリック判定とドラッグ判定が競合すると意図しない絞り込みが発生する可能性がある。
- ダブルクリック時に先行するシングルクリック処理との体感差が出る可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: ノード操作仕様（クリック/ダブルクリック）を変更するため。

## Notes / Logs
- 2026-02-15: 初版作成。
