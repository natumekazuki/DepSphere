# WebView2初期化失敗（E_ACCESSDENIED）対応とエラーコピー導線

## Goal
- `0x80070005 (E_ACCESSDENIED)` による WebView2 初期化失敗を回避する。
- 画面上のエラー詳細をユーザーがコピーできるようにする。

## Task List
- [ ] WebView2 の `UserDataFolder` を書き込み可能な `LocalAppData` 配下に固定する。
- [ ] 初期化失敗時の詳細（例外メッセージ/StackTrace）を保持・表示する。
- [ ] エラー詳細をクリップボードへコピーするUI（ボタン）を追加する。
- [ ] 設計ドキュメントへ反映する。
- [ ] ビルド/テストで回帰確認し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml.cs`
- `src/DepSphere.App/MainWindow.xaml`
- `docs/design/dep-visualizer-core.md`

## Risks
- ユーザーデータフォルダの競合や破損時に別種の初期化失敗が発生する可能性がある。
- エラー詳細表示が長文の場合、UIレイアウトが崩れる可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: エラー表示/コピー導線のUI仕様追加を伴うため。

## Notes / Logs
- 2026-02-15: 初版作成。
