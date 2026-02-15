# P3-2: WPF操作仕様の受け入れチェックリスト化

## Goal
- `docs/design/dep-visualizer-core.md` に WPF 操作仕様の受け入れチェックリストを追加し、Windows 実機での最終確認観点を明確化する。

## Task List
- [x] 現行のWPF操作仕様（解析入力/実行/キャンセル/ノード選択/コード表示）を棚卸しする。
- [x] 受け入れチェックリスト節を `docs/design/dep-visualizer-core.md` に追加する。
- [x] Non-WindowsバックログのP3-2を完了状態へ更新する。
- [x] 計画ファイルを完了更新してアーカイブする。

## Affected Files
- `docs/design/dep-visualizer-core.md`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- チェックリスト粒度が粗すぎると実機検証で判断がぶれる。
- 実装済み仕様との差分があると将来の受け入れで混乱する。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: 受け入れ項目を仕様として明文化するため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `dep-visualizer-core` に受け入れチェックリスト節（AC-WPF-01〜10）を追加。
- 2026-02-15: `20260215-nonwindows-backlog.md` のP3-2を完了化。
