# P1-1: 重み係数とHotspot閾値の設定化

## Goal
- 重みスコア計算の係数と、Hotspot/Critical判定閾値を `AnalysisOptions` で設定可能にする。
- CLIから設定値を渡して比較実験しやすくする。

## Task List
- [x] `AnalysisOptions` に重み係数と閾値設定を追加し、バリデーションを実装する。
- [x] `DependencyAnalyzer` の重みスコア計算を `AnalysisOptions` 参照に変更する。
- [x] `GraphViewBuilder` のHotspot/Critical判定を `AnalysisOptions` 参照に変更する。
- [x] `DepSphere.Cli` に係数/閾値指定オプションを追加する。
- [x] 係数/閾値の単体テストを追加・更新する。
- [x] 設計ドキュメントとNon-Windowsバックログを更新する。
- [x] `dotnet build` / `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.Analyzer/AnalysisOptions.cs`
- `src/DepSphere.Analyzer/DependencyAnalyzer.cs`
- `src/DepSphere.Analyzer/GraphViewBuilder.cs`
- `src/DepSphere.Cli/Program.cs`
- `tests/DepSphere.Analyzer.Tests/DependencyAnalyzerTests.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- 設定値の組み合わせ次第で極端なスコア分布になり、比較が難しくなる可能性がある。
- 既存テストの期待値が固定閾値前提のため更新漏れが起きる可能性がある。

## Design Check
- Analyzer/可視化判定仕様の変更を含むため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `AnalysisOptions` に重み係数/閾値を追加し、Analyzerのスコア計算とGraph判定へ反映。
- 2026-02-15: CLIに `--weight-*` と `--hotspot-top/--critical-top` を追加。
- 2026-02-15: 係数変更・閾値変更・不正設定の単体テストを追加。
- 2026-02-15: Fixture配下 `obj/*.cs` の巻き込み回避のため、`DepSphere.Analyzer.Tests.csproj` に `Fixtures/**/obj|bin` の `Compile Remove` を追加。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` 成功（47件）。
- 2026-02-15: CLIスモーク実行（重み・閾値指定）で `graph.json` / `graph.html` 出力を確認。
