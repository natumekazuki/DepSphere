# アプリアイコン作成（球体＋ノード線）

## Goal
- Issue #7 向けに、DepSphere の実行ファイルへ「球体＋ノード線」モチーフのアプリアイコンを設定する。

## Task List
- [x] 設計ドキュメント（アイコン仕様）を追加し、デザイン意図とアセット方針を明文化する。
- [x] `src/DepSphere.App/Assets/` にアイコンアセット（`.ico`）を追加する。
- [x] `src/DepSphere.App/DepSphere.App.csproj` に `ApplicationIcon` 設定を追加する。
- [x] `dotnet build src/DepSphere.App/DepSphere.App.csproj` でビルド確認する。

## Affected Files
- `docs/plans/20260216-app-icon-graph-sphere.md`
- `docs/design/app-icon-spec.md`（新規）
- `src/DepSphere.App/Assets/app-icon.ico`（新規）
- `src/DepSphere.App/DepSphere.App.csproj`

## Risks
- 環境差分により `.ico` の表示色や輪郭が小サイズで潰れる可能性がある。
- 高DPI環境での見え方を十分に検証できない可能性がある。

## Design Check
- 新規機能（アプリアイコン導入）のため、`docs/design/app-icon-spec.md` を追加して仕様を管理する。

## Notes / Logs
- 2026-02-16: `docs/design/app-icon-spec.md` を新規追加。
- 2026-02-16: PowerShell で球体＋ノード線モチーフの `src/DepSphere.App/Assets/app-icon.ico` を生成。
- 2026-02-16: `src/DepSphere.App/DepSphere.App.csproj` に `ApplicationIcon` を追加。
- 2026-02-16: `dotnet build src/DepSphere.App/DepSphere.App.csproj` 実行（成功、警告/エラー 0）。
