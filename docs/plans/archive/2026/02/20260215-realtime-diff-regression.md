# P2-2: Realtime差分適用の回帰テスト拡張

## Goal
- Realtime更新で「変更ファイルの差分だけがパッチ化される」ことをテストで固定する。
- 過剰再解析の兆候（無関係ノードの大量Upsert/Remove）を検出できる状態にする。

## Task List
- [x] 既存 `RealtimeUpdateTests` のカバレッジギャップを洗い出す。
- [x] 変更ファイル限定の差分適用テスト（DocumentChanged）を追加する。
- [x] 削除イベントの差分適用テスト（DocumentRemoved）を追加する。
- [x] 追加テストを `dotnet test` で実行し回帰確認する。
- [x] Non-Windowsバックログへ進捗を反映する。

## Affected Files
- `tests/DepSphere.Analyzer.Tests/RealtimeUpdateTests.cs`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- 重みスコア再計算仕様により、実装上は無関係ノードにもスコア変動が波及する可能性がある。
- Fixture変更が他テストと衝突するとフレーク化する可能性がある。

## Design Check
- 判定: 不要
- 理由: 今回は既存Realtime仕様に対する回帰テスト追加のみで、仕様変更を伴わない。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `RealtimeUpdateTests` に回帰テストを3件追加。
- 2026-02-15: `DocumentChanged` の無差分更新で、無関係ノードにパッチが波及しないことを検証。
- 2026-02-15: `DocumentRemoved` と単一ファイル変更時の差分適用（対象ノードの追加/削除）を検証。
- 2026-02-15: `dotnet test DepSphere.sln` を実行し成功（55件）。
