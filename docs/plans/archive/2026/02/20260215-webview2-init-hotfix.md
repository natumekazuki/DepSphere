# WebView2初期化競合のホットフィックス

## Goal
- WebView2 初期化完了前の操作で発生する `CoreWebView2` 未初期化例外を防止する。

## Task List
- [x] `MainWindow` に WebView2 初期化ゲートを追加する。
- [x] 初期化完了前の操作系UIを無効化する。
- [x] 初期化失敗時の再試行可能な状態を確保する。
- [x] ローカル回帰確認（ビルド/テスト）を実施する。
- [x] 設計ドキュメントへ反映し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 初期化失敗時のハンドリングが不十分だと、操作不能状態に陥る可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: ホスト操作仕様（初期化完了前のUI状態）を変更するため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `EnsureWebView2InitializedAsync` を追加し、初期化完了前に `NavigateToString/CoreWebView2` へ到達しないよう制御。
- 2026-02-15: 初期化失敗時に `_webViewInitializationTask` をリセットし、再試行可能に修正。
- 2026-02-15: `dotnet build DepSphere.sln` / `dotnet test DepSphere.sln` を実行し成功。
