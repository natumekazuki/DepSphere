# WPFホスト: 進捗更新間隔の設定可能化

## Goal
- 解析進捗（metricsステージ）の更新間隔を固定値ではなく、UIから変更可能にする。

## Task List
- [x] 解析オプションモデルを `DepSphere.Analyzer` に追加する。
- [x] `DependencyAnalyzer.AnalyzePathAsync` に解析オプション受け渡しを追加する。
- [x] メトリクス進捗更新間隔をオプションで制御する。
- [x] `MainWindow` に進捗更新間隔の入力UIを追加し、解析時に反映する。
- [x] オプション値検証のテストを追加する。
- [x] 設計ドキュメント `docs/design/dep-visualizer-core.md` を更新する。
- [x] `dotnet build` と `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.Analyzer/AnalysisOptions.cs` (new)
- `src/DepSphere.Analyzer/DependencyAnalyzer.cs`
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `tests/DepSphere.Analyzer.Tests/DependencyAnalyzerTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 不正な間隔値（0以下）を許容すると進捗通知ロジックが破綻する可能性がある。
- UI入力値と実行値の同期不整合で意図しない間隔が使われる可能性がある。

## Design Check
- ホスト設定導線の仕様変更を含むため `docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `AnalysisOptions` を追加し、進捗更新間隔を `AnalyzePathAsync` オプションで注入可能化。
- 2026-02-15: `MainWindow` に進捗更新間隔入力を追加し、解析時のバリデーションを実装。
- 2026-02-15: `DependencyAnalyzerTests` に不正間隔の例外テストを追加。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` 成功（42件）。
