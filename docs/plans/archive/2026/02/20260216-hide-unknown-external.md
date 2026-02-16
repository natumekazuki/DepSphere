# 判定不能外部ノードの非表示化

## Goal
- `ext:external` のような判定不能な外部依存ノードを生成しないようにする。

## Task List
- [x] `GraphViewBuilder` の外部ルート抽出で、判定不能ケースを `null` 扱いに変更する。
- [x] `GraphViewBuilderTests` に判定不能外部型を表示しないことのテストを追加する。
- [x] 必要最小限の設計ドキュメント記述を更新する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewBuilderTests` を実行する。

## Affected Files
- `docs/plans/20260216-hide-unknown-external.md`
- `src/DepSphere.Analyzer/GraphViewBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 以前は見えていた曖昧外部ノードが見えなくなるため、依存把握漏れと感じる可能性がある。

## Design Check
- 外部依存可視化の挙動変更のため、`docs/design/dep-visualizer-core.md` に補足を追記する。

## Notes / Logs
- 2026-02-16: `GraphViewBuilder.ExtractExternalRoot` のフォールバック値 `External` を廃止し、判定不能時は `null` を返すように変更。
- 2026-02-16: `GraphViewBuilderTests` に「判定不能な外部型は external ノードを生成しない」テストを追加。
- 2026-02-16: `docs/design/dep-visualizer-core.md` に判定不能外部型を生成しない旨を追記。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewBuilderTests` 実行（7件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（70件合格）。
