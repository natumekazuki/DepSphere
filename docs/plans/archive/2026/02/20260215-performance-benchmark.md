# P1-3: 解析性能ベンチ（中規模Fixture）

## Goal
- 中規模Fixtureを追加し、解析時間とグラフ規模（ノード数/エッジ数）の回帰基準をテストで固定する。

## Task List
- [x] 中規模Fixtureプロジェクトを `tests/Fixtures` に追加する。
- [x] 解析性能ベンチテストを追加し、ノード数/エッジ数の基準を固定する。
- [x] 解析時間の上限（回帰検知用）をテストで定義する。
- [x] 設計ドキュメントとNon-Windowsバックログを更新する。
- [x] `dotnet build` / `dotnet test` で回帰確認する。

## Affected Files
- `tests/DepSphere.Analyzer.Tests/Fixtures/MediumBenchmark/MediumBenchmark.csproj` (new)
- `tests/DepSphere.Analyzer.Tests/Fixtures/MediumBenchmark/GeneratedTypes.cs` (new)
- `tests/DepSphere.Analyzer.Tests/DependencyAnalyzerTests.cs`
- `docs/design/dep-visualizer-core.md`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- 時間上限を厳しくしすぎると環境差分でフレークしやすい。
- Fixture規模が小さすぎると回帰検知力が不足する。

## Design Check
- 非機能基準（解析性能）を明文化するため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `MediumBenchmark` Fixture（80クラス + `NodeBase` + `INode`）を追加。
- 2026-02-15: ベンチ基準テストを追加（82ノード / 320エッジ / 20秒以内）。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` 成功（48件）。
