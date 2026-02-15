# CLI出力導線: JSON/HTML保存の実装

## Goal
- WPFホストなしで解析結果を確認できるよう、`.sln/.csproj` 解析結果をCLIから `JSON/HTML` に保存可能にする。

## Task List
- [x] `src/DepSphere.Cli` コンソールプロジェクトを追加する。
- [x] CLI引数パーサを実装し、入力パス・出力先・進捗更新間隔を受け取れるようにする。
- [x] `DependencyAnalyzer` + `GraphViewBuilder` を呼び、`graph.json` と `graph.html` を保存する。
- [x] 実行ログ（ノード数/エッジ数/出力先）を標準出力に表示する。
- [x] `DepSphere.sln` にCLIプロジェクトを追加する。
- [x] 設計ドキュメントにCLI導線を追記する。
- [x] `dotnet build` / `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.Cli/DepSphere.Cli.csproj` (new)
- `src/DepSphere.Cli/Program.cs` (new)
- `DepSphere.sln`
- `docs/design/dep-visualizer-core.md`

## Risks
- 引数仕様が曖昧だと将来互換性を壊しやすい。
- 大規模解析でCLIの標準出力更新が過多になる可能性がある。

## Design Check
- ホスト以外の実行導線を追加するため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `DepSphere.Cli` を追加し、`--input/--out/--json/--html/--progress-interval` を実装。
- 2026-02-15: `DependencyAnalyzer` と `GraphView` 出力を接続し、JSON/HTML保存と進捗表示を実装。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` 成功（42件）。
- 2026-02-15: `dotnet run --project src/DepSphere.Cli -- --input tests/DepSphere.Analyzer.Tests/Fixtures/SampleLib/SampleLib.csproj --out artifacts/cli-smoke --progress-interval 10` で成果物出力を確認。
