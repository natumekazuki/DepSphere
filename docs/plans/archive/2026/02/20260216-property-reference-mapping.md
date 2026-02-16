# プロパティ参照マッピング修正

## Goal
- Issue #3 の要件に合わせ、クラス内で利用したプロパティ参照が依存エッジとして正しくマッピングされるようにする。

## Task List
- [x] `DependencyAnalyzer` の `MemberAccessExpression` 解析で、参照メンバー自体のシンボル（`IPropertySymbol` など）を基準に依存先型を抽出する。
- [x] プロパティ参照時に、必要な型（プロパティ型/宣言型）への参照エッジが欠落しないよう補完する。
- [x] 回帰防止として `DependencyAnalyzerTests` にプロパティ参照マッピングのテストを追加する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter DependencyAnalyzerTests` を実行して確認する。

## Affected Files
- `docs/plans/20260216-property-reference-mapping.md`
- `src/DepSphere.Analyzer/DependencyAnalyzer.cs`
- `tests/DepSphere.Analyzer.Tests/DependencyAnalyzerTests.cs`

## Risks
- 参照抽出範囲を広げることで、これまで未計上だったエッジが増え、既存のグラフ密度が変化する可能性がある。
- メソッド/プロパティの戻り型と宣言型を同時採用した場合、意図より広い依存検出になる可能性がある。

## Design Check
- 依存抽出ロジックの不具合修正であり、UI/操作仕様変更ではないため `docs/design/` の更新は不要。

## Notes / Logs
- 2026-02-16: `MemberAccessExpression` 解析を `memberAccess.Expression` ベースから `memberAccess` シンボルベースへ変更。
- 2026-02-16: `IPropertySymbol` / `IFieldSymbol` / `IMethodSymbol` / `IEventSymbol` ごとに、宣言型とメンバー型の参照エッジ補完を追加。
- 2026-02-16: `DependencyAnalyzerTests` にインスタンスプロパティ参照 (`provider.Value`) の回帰テストを追加。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter DependencyAnalyzerTests` 実行（13件合格）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（69件合格）。
