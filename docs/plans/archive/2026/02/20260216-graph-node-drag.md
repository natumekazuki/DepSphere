# グラフノードドラッグ移動対応

## Goal
- Issue #4 の要件に合わせ、3D グラフ上でノードをドラッグして位置調整できるようにする。

## Task List
- [x] `GraphViewHtmlBuilder` にノードドラッグ状態管理と平面投影ベースの移動処理を追加する。
- [x] ノードドラッグ中はカメラのパン操作と競合しないようイベント伝播を制御する。
- [x] ドラッグ後のノード位置がエッジ表示とフィルタ更新に反映されるようにする。
- [x] `GraphViewHtmlBuilderTests` にノードドラッグ関連スクリプトの存在検証を追加する。
- [x] `docs/design/dep-visualizer-core.md` にノードドラッグ操作仕様を追記する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行する。

## Affected Files
- `docs/plans/20260216-graph-node-drag.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- ノードドラッグと既存の単/ダブルクリック挙動が競合する可能性がある。
- ドラッグ平面の定義次第でカメラ角度によって操作感が変わる可能性がある。

## Design Check
- 操作仕様の追加のため `docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-16: ノードドラッグ用に `dragPlane` / `projectPointerToPlane` を追加し、左クリックでノードを掴んで移動できるようにした。
- 2026-02-16: ノードドラッグ開始時に `event.stopImmediatePropagation()` を適用し、既存カメラパンとの競合を抑止した。
- 2026-02-16: ドラッグ中に `basePositions` を更新し、`refreshEdges()` / `updateLabelLod()` で表示同期するようにした。
- 2026-02-16: `GraphViewHtmlBuilderTests` にノードドラッグ関連スクリプトの存在アサートを追加した。
- 2026-02-16: `docs/design/dep-visualizer-core.md` に「ノード上左ドラッグ: ノード移動」「背景左ドラッグ: パン」を追記した。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（68件合格）。
