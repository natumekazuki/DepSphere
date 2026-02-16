# 操作パネルの進捗更新間隔入力を廃止

## Goal
- WPF操作パネルから「進捗更新間隔（型件数）」入力欄を削除し、アプリ側は既定値固定で解析を実行する。

## Task List
- [x] `MainWindow.xaml` から進捗更新間隔のラベル/入力欄を削除する。
- [x] `MainWindow.xaml.cs` から進捗更新間隔入力の検証・引数受け渡し処理を削除し、既定値固定で `AnalysisOptions` を生成する。
- [x] 設計ドキュメントのUI仕様を「固定値運用」に更新する。
- [x] `dotnet build src/DepSphere.App/DepSphere.App.csproj` を実行してビルド確認する。

## Affected Files
- `docs/plans/20260216-remove-progress-interval-input.md`
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 更新頻度を動的調整できなくなるため、超大規模解析で進捗表示の粒度を変更したい要求に対応しづらくなる。

## Design Check
- 操作パネル仕様変更のため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-16: `MainWindow.xaml` から `進捗更新間隔（型件数）` ラベルと `ProgressIntervalTextBox` を削除。
- 2026-02-16: `MainWindow.xaml.cs` から進捗更新間隔入力バリデーション (`TryGetProgressInterval`) と関連分岐を削除。
- 2026-02-16: 解析実行は `new AnalysisOptions()` に統一し、既定値（25）で固定化。
- 2026-02-16: `docs/design/dep-visualizer-core.md` のWPF仕様と受け入れ条件を固定値運用に更新。
- 2026-02-16: `dotnet build src/DepSphere.App/DepSphere.App.csproj` 実行（成功）。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj` 実行（70件合格）。
