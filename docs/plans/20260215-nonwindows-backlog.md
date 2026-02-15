# Non-Windows期間の開発バックログ計画

## Goal
- Windows実行環境がない期間でも、解析品質・開発効率・将来のWindows検証準備を前進させる。

## Scope
- 本計画は「実装優先度の整理」と「着手順の確定」が目的。
- WPF実行確認が必須な作業は後段へ回し、Analyzer中心に進める。

## Task List (Priority Order)
- [x] P1: Analyzerの重みスコアを設定化（係数/Hotspot閾値）し、比較実験しやすくする。
- [x] P1: Analyzerの出力をCLIで保存できる導線（JSON/HTML）を追加し、ホストなしでも確認可能にする。
- [x] P1: 解析性能ベンチ（中規模Fixture）を追加し、回帰基準（時間/ノード数）を定義する。
- [x] P2: クラス移動（namespace/file/project）の回帰テストケースを拡張し、壊れやすいパターンを網羅する。
- [x] P2: Realtime更新ロジックの差分適用テストを追加し、過剰再解析を検出できるようにする。
- [x] P2: 依存エッジ種別ごとの統計（件数・密度）を出力し、ホットスポット判断の補助情報を追加する。
- [ ] P3: CIにWindowsジョブを追加し、WPFビルド/最小起動確認を自動化する（ローカルWindows不要化）。
- [ ] P3: WPF操作仕様の最終受け入れ項目を `docs/design/dep-visualizer-core.md` にチェックリスト化する。

## Deliverables
- `src/DepSphere.Analyzer` の設定拡張とCLI利用導線
- `tests/DepSphere.Analyzer.Tests` の性能/回帰テスト拡充
- `docs/design/dep-visualizer-core.md` の受け入れチェックリスト
- （任意）`.github/workflows/` のWindows検証ジョブ

## Affected Files (Expected)
- `src/DepSphere.Analyzer/*`
- `tests/DepSphere.Analyzer.Tests/*`
- `docs/design/dep-visualizer-core.md`
- `.github/workflows/*` (CI追加時)

## Risks
- ベンチ用Fixtureが小さすぎると性能回帰を検知しにくい。
- CLI導線とWPF導線の責務が混ざると保守性が落ちる。
- Windows CIの初回構築で依存解決に時間がかかる可能性がある。

## Design Check
- P1でAnalyzerの入出力仕様が変わるため、`docs/design/dep-visualizer-core.md` の更新を伴う。

## Notes / Logs
- 2026-02-15: Windows非依存で進める優先タスクの初版を作成。
- 2026-02-15: P1-2完了。`src/DepSphere.Cli` を追加し、解析結果の JSON/HTML 保存導線を実装。
- 2026-02-15: P1-1完了。`AnalysisOptions` に重み係数/判定閾値を追加し、Analyzer/CLIから設定可能化。
- 2026-02-15: P1-3完了。`MediumBenchmark` Fixtureと性能ベンチテストを追加し、回帰基準（82ノード/320エッジ/20秒以内）を定義。
- 2026-02-15: P2-1完了。クラス移動の同名衝突/既存ファイル衝突/partial class 回帰テストを追加し、`ClassMover` を複数宣言移動に対応。
- 2026-02-15: P2-2完了。Realtime差分適用テスト（無関係ノード非波及、DocumentRemoved、単一ファイル変更）を追加し、過剰更新の検出観点を拡張。
- 2026-02-15: P2-3完了。依存エッジ種別ごとの件数・密度を `edge-stats.json` とCLI標準出力で確認可能化。
