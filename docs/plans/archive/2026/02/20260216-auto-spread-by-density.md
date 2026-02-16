# ノード密度連動の自動拡散ボタン追加

## Goal
- ノード数が多い/密度が高いグラフで、固定上限を超える距離倍率を自動適用し、可読性を改善する。

## Task List
- [x] `GraphViewHtmlBuilder` の操作UIに「自動拡散」ボタンを追加する。
- [x] 現在表示中ノード数とエッジ密度から推奨距離倍率を算出するロジックを追加する。
- [x] 距離倍率スライダーの既定上限（2.6）を必要に応じて動的拡張し、推奨値を適用する。
- [x] `GraphViewHtmlBuilderTests` に自動拡散UI/スクリプト存在アサートを追加する。
- [x] `docs/design/dep-visualizer-core.md` に自動拡散操作仕様を追記する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行する。

## Affected Files
- `docs/plans/20260216-auto-spread-by-density.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 極端に大きい距離倍率を許可すると、カメラ初期位置から見切れる可能性がある（適用後にFitを併用して緩和）。
- 密度推定ロジックが状況により過剰/不足拡散を起こす可能性がある。

## Design Check
- 操作仕様追加のため `docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-16: 操作UIへ `自動拡散` ボタン（`#auto-spread`）を追加。
- 2026-02-16: 表示中ノード数・エッジ密度・ノードあたりエッジ数から推奨距離倍率を算出する `computeRecommendedSpreadScale` を実装。
- 2026-02-16: 推奨倍率が既定上限を超える場合に `spread-scale` の `max` を動的拡張する `ensureSpreadScaleMax` を実装。
- 2026-02-16: `applyAutoSpread` で推奨倍率適用 → 再描画 → Fit を一連で実行するよう実装。
- 2026-02-16: `GraphViewHtmlBuilderTests` に自動拡散関連の存在アサートを追加。
- 2026-02-16: `docs/design/dep-visualizer-core.md` に自動拡散操作仕様を追記。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（70件合格）。
