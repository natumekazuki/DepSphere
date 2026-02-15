# P2-3: 依存エッジ統計の出力追加

## Goal
- 依存エッジ種別（reference/inherit/implement）ごとの件数と密度を算出・出力し、ホットスポット判断の補助情報を可視化する。

## Task List
- [ ] `DependencyGraph` からエッジ統計を算出するビルダを実装する。
- [ ] エッジ統計の単体テストを追加する。
- [ ] CLI出力へエッジ統計（標準出力 + JSONファイル）を追加する。
- [ ] CLI利用仕様を設計ドキュメントへ反映する。
- [ ] `dotnet test` で回帰確認し、Non-Windowsバックログを更新する。

## Affected Files
- `src/DepSphere.Analyzer/*`
- `src/DepSphere.Cli/Program.cs`
- `tests/DepSphere.Analyzer.Tests/*`
- `docs/design/dep-visualizer-core.md`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- 密度の定義（有向グラフ前提）を曖昧にすると、集計値の解釈がぶれる。
- 出力ファイル追加でCLI利用者が既存フォーマット固定前提の場合に影響が出る可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: CLI出力成果物と利用フローに統計出力を追加するため。

## Notes / Logs
- 2026-02-15: 初版作成。
