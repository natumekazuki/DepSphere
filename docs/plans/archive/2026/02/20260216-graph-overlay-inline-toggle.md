# グラフオーバーレイ内トグルボタン対応

## Goal
- グラフ上オーバーレイの表示切替を、外側固定ボタンではなく各オーバーレイ内部の `▽` トグルボタンで行えるようにする。

## Task List
- [x] 計画を作成し変更対象を明確化する。
- [x] `GraphViewHtmlBuilder` のオーバーレイ構造をヘッダー内トグル方式へ変更する。
- [x] `GraphViewHtmlBuilderTests` を新UI仕様に合わせて更新する。
- [x] 設計ドキュメントの3D表示仕様を更新する。
- [x] 関連テストを実行して回帰がないことを確認する。

## Affected Files
- `docs/plans/20260216-graph-overlay-inline-toggle.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 折りたたみ時の再展開導線が分かりづらいと操作性が落ちる可能性がある。
- オーバーレイ構造変更で既存スタイルやテストが壊れる可能性がある。

## Design Check
- 3D表示UI仕様の操作導線を変更するため、`docs/design/dep-visualizer-core.md` の更新が必要。

## Notes / Logs
- 2026-02-16: ユーザー要望により、オーバーレイ内 `▽` アイコンによる表示切替へ変更対応を開始。
- 2026-02-16: 外側固定トグルボタンを廃止し、各オーバーレイのヘッダー内トグル（`▽/△`）で折りたたみ/展開する構成へ変更。
- 2026-02-16: `GraphViewHtmlBuilderTests` を新仕様に合わせて更新（`overlay-body` / `node-info-content` / `▽` を検証）。
- 2026-02-16: `docs/design/dep-visualizer-core.md` にオーバーレイ内 `▽/△` トグル仕様を反映。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行し13件合格。
