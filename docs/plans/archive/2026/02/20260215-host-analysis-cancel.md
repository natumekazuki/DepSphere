# WPFホスト: 解析実行の非ブロック化とキャンセル対応

## Goal
- `.sln/.csproj` 解析中にUI操作状態を明確化し、キャンセル操作を提供する。
- 解析処理の多重実行を防止し、状態表示を一貫させる。

## Task List
- [x] `MainWindow.xaml` にキャンセルボタンを追加し、主要ボタンへ `x:Name` を付与する。
- [x] `MainWindow.xaml.cs` に解析実行中フラグと `CancellationTokenSource` 管理を追加する。
- [x] `解析実行` / `再解析` 中のボタン有効状態を制御し、多重実行を防止する。
- [x] `キャンセル` 操作で `AnalyzePathAsync` の `CancellationToken` を停止し、状態表示を更新する。
- [x] 設計ドキュメント `docs/design/dep-visualizer-core.md` にキャンセル運用を追記する。
- [x] `dotnet build` と `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- キャンセル要求後にMSBuildWorkspace側の停止反映までタイムラグが発生する可能性がある。
- 状態遷移が不整合だとボタンが押せないまま残る可能性がある。

## Design Check
- ホストの操作仕様変更を含むため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: 解析中のUI状態制御（ボタン無効化）とキャンセル要求送信を `MainWindow` に実装。
- 2026-02-15: `dotnet build src/DepSphere.App/DepSphere.App.csproj` と `dotnet test DepSphere.sln` 成功。
