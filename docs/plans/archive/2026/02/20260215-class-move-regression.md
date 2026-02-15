# P2-1: クラス移動の回帰テスト拡張

## Goal
- クラス移動の壊れやすいケース（同名衝突、partial class）を回帰テストで固定する。
- テスト成立に必要な最小実装修正を `ClassMover` に適用する。

## Task List
- [x] `ClassMover` に移動先ファイル衝突検出を追加する。
- [x] `MoveNamespace` / `MoveProject` に同名型衝突検出を追加する。
- [x] `ClassMover` を partial class の全パーツ移動に対応させる。
- [x] 同名衝突ケースと partial class ケースのテストを追加する。
- [x] 設計ドキュメントとNon-Windowsバックログを更新する。
- [x] `dotnet build` / `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.Analyzer/ClassMover.cs`
- `tests/DepSphere.Analyzer.Tests/ClassMoverTests.cs`
- `docs/design/refactor-move-class-spec.md`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- partial class 検出の実装が単純すぎると誤検出や移動漏れが発生する可能性がある。
- 衝突検出の判定条件が厳しすぎると正当な移動まで拒否する可能性がある。

## Design Check
- クラス移動の仕様と実装挙動に差分が出るため `docs/design/refactor-move-class-spec.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `ClassMover` にファイル衝突/同名型衝突検出を追加。
- 2026-02-15: 同一 `TypeFqn` の複数宣言（partial class）を全パーツ移動する実装に更新。
- 2026-02-15: `ClassMoverTests` に衝突失敗ケースと partial class 回帰ケースを追加（10件）。
- 2026-02-15: `docs/design/refactor-move-class-spec.md` と `docs/plans/20260215-nonwindows-backlog.md` を更新。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` を実行し成功。
