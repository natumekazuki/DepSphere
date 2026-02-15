# Windows配布: Inno Setup手順

## 1. 目的
- `DepSphere.App` の配布を、Windows環境で再現可能な手順に統一する。
- `dotnet publish` と Inno Setup (`ISCC.exe`) をスクリプト化し、手動ミスを減らす。

## 2. 前提
- OS: Windows 10/11 x64
- インストール済みツール:
  - .NET SDK 8.x
  - Inno Setup 6 (`ISCC.exe` が利用可能)
  - PowerShell 7 以上（`pwsh` 推奨）
- ソース取得済み: `DepSphere` リポジトリ

## 3. 追加ファイル
- `scripts/windows/build-app.ps1`
- `scripts/windows/build-installer.ps1`
- `installer/DepSphere.iss`

## 4. 最短手順（推奨）
リポジトリルートで実行:

```powershell
pwsh -File .\scripts\windows\build-installer.ps1 -Version 0.1.0
```

成功時の生成物:
- publish出力: `artifacts\publish\DepSphere.App\Release\win-x64\0.1.0\`
- インストーラー: `artifacts\installer\0.1.0\DepSphere-setup-0.1.0.exe`

## 5. オプション例
### 5.1 ISCCのパスを明示する

```powershell
pwsh -File .\scripts\windows\build-installer.ps1 `
  -Version 0.1.0 `
  -IsccPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

### 5.2 publishのみ実行する

```powershell
pwsh -File .\scripts\windows\build-app.ps1 -Version 0.1.0 -Clean
```

### 5.3 既存publishを使ってインストーラーのみ作成する

```powershell
pwsh -File .\scripts\windows\build-installer.ps1 `
  -Version 0.1.0 `
  -PublishDir ".\artifacts\publish\DepSphere.App\Release\win-x64\0.1.0"
```

## 6. CLI引数（build-installer.ps1）
- `-Version`: アプリ/インストーラーのバージョン（既定: `0.1.0`）
- `-Configuration`: `Release`/`Debug`（既定: `Release`）
- `-Runtime`: `win-x64` など（既定: `win-x64`）
- `-Publisher`: Inno Setupの `AppPublisher`
- `-PublishDir`: 既存publishディレクトリ（未指定なら内部でpublish実行）
- `-OutputRoot`: インストーラー出力ルート（既定: `artifacts\installer`）
- `-InnoScriptPath`: `.iss` のパス（既定: `installer\DepSphere.iss`）
- `-IsccPath`: `ISCC.exe` のフルパス
- `-SelfContained`: self-contained publishの有無（既定: `true`）
- `-Clean`: 出力先を削除して再生成

## 7. 既知の注意点
- WebView2 Runtime が未導入端末ではアプリ起動時にランタイムが必要になる。
- macOS/Linuxでは `ISCC.exe` を実行できないため、最終的なインストーラー生成確認はWindowsで行う。

## 8. トラブルシュート
- `ISCC.exe not found`:
  - Inno Setup 6 をインストールする。
  - もしくは `-IsccPath` を明示する。
- `Project not found`:
  - リポジトリルート配下で実行しているか確認する。
- 生成されたインストーラーが見つからない:
  - `artifacts\installer\<Version>\` を確認し、`ISCC` のログ出力を確認する。

## 9. GitHub Actions連携
- Workflow: `.github/workflows/windows-ci.yml`
- 実行内容:
  - `dotnet restore/build/test`（Windows）
  - Inno Setup を導入（`ISCC.exe` 未導入時は `choco install innosetup`）
  - `scripts/windows/build-installer.ps1` でセットアップEXEを生成
  - `actions/upload-artifact` でインストーラーを保存
- バージョン規則:
  - タグビルド（`v1.2.3` / `1.2.3`）: タグ値を使用
  - それ以外（push/PR）: `0.1.<run_number>`
