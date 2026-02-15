# WPFホスト: プロジェクト選択解析導線の追加

## Goal
- `src/DepSphere.App` で `.sln/.csproj` を選択し、静的解析を実行して3Dグラフ表示まで到達できるようにする。
- 既存のサンプル読込導線は維持し、再解析時は選択済みパスを優先する。

## Task List
- [x] 左ペインにプロジェクトパス入力と参照/解析実行ボタンを追加する。
- [x] `MainWindow.xaml.cs` にファイル選択と解析実行ハンドラを実装する。
- [x] 再解析ボタンの挙動を「選択済みパス優先、未選択時はサンプル」に変更する。
- [x] ノード未選択時のコードビュー初期表示と状態表示を統一する。
- [x] 設計ドキュメント `docs/design/dep-visualizer-core.md` にホストの入力導線を追記する。
- [x] `dotnet build` と `dotnet test` で回帰確認する。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- UIスレッド上での解析実行時間が長い場合、体感的に固まって見える可能性がある。
- 無効なパスや未復元ソリューションを指定した場合に例外表示が増える可能性がある。

## Design Check
- 本作業は新規機能（ホスト入力導線）を含むため、`docs/design/dep-visualizer-core.md` の更新を実施する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `MainWindow` に解析対象入力導線を追加し、`DependencyAnalyzer.AnalyzePathAsync` 連携を実装。
- 2026-02-15: `dotnet build src/DepSphere.App/DepSphere.App.csproj` と `dotnet test DepSphere.sln` 成功。
