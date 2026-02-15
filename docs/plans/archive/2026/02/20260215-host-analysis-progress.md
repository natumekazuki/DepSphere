# WPFホスト: 解析進捗表示の追加

## Goal
- `.sln/.csproj` 解析時に進捗ステップを `StatusText` へ表示し、長時間解析時の待機状態を把握しやすくする。

## Task List
- [x] 解析進捗モデル（ステージ/メッセージ/件数）を `DepSphere.Analyzer` に追加する。
- [x] `DependencyAnalyzer.AnalyzePathAsync` に進捗通知オーバーロードを追加する。
- [x] プロジェクト読込・コンパイル生成・メトリクス算出の進捗通知を実装する。
- [x] `MainWindow` で進捗通知を受け取り `StatusText` に反映する。
- [x] 進捗通知の単体テストを追加する。
- [x] 設計ドキュメント `docs/design/dep-visualizer-core.md` を更新する。
- [x] `dotnet build` と `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.Analyzer/DependencyAnalyzer.cs`
- `src/DepSphere.Analyzer/AnalysisProgress.cs` (new)
- `src/DepSphere.App/MainWindow.xaml.cs`
- `tests/DepSphere.Analyzer.Tests/DependencyAnalyzerTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 進捗イベントの発火頻度が高すぎるとUI更新コストが増える可能性がある。
- 既存API互換を崩すと他コンポーネントに波及する可能性がある。

## Design Check
- ホスト表示仕様に変更があるため `docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `AnalysisProgress` 追加、`AnalyzePathAsync` に進捗通知オーバーロード実装。
- 2026-02-15: `MainWindow` の `StatusText` を進捗イベントで段階更新する処理を追加。
- 2026-02-15: `DependencyAnalyzerTests` に進捗通知テストを追加し、`dotnet build/test` 成功（41件）。
