# Inno Setup配布: ビルド手順書とスクリプト整備

## Goal
- DepSphere.App を Windows向けに配布するため、ビルドからインストーラーEXE生成までを再現可能にする。
- 手動操作を減らすため、PowerShellスクリプトとInno Setupスクリプトを整備する。

## Task List
- [x] 配布用 `dotnet publish` を実行する PowerShell スクリプトを追加する。
- [x] Inno Setup の `.iss` スクリプトを追加する。
- [x] publish と Inno Setup コンパイルを連結する PowerShell スクリプトを追加する。
- [x] Windows実行手順（前提ツール・コマンド・出力先）のドキュメントを作成する。
- [x] 非Windows環境で可能な静的検証を実施する。

## Affected Files
- `scripts/windows/build-app.ps1` (new)
- `scripts/windows/build-installer.ps1` (new)
- `installer/DepSphere.iss` (new)
- `docs/design/windows-installer-inno-setup.md` (new)

## Risks
- Inno Setup本体（`ISCC.exe`）のインストール先が環境差分で変わる。
- WebView2 Runtime が未導入の端末では初回起動に影響が出る可能性がある。
- macOS/Linux上ではインストーラー生成を実実行できないため、Windowsでの最終確認が必要。

## Design Check
- 配布運用フローを追加するため、新規設計ドキュメントを作成する。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `build-app.ps1` / `build-installer.ps1` / `DepSphere.iss` を追加。
- 2026-02-15: `docs/design/windows-installer-inno-setup.md` を追加し、手順とトラブルシュートを整理。
- 2026-02-15: `dotnet build DepSphere.sln` 成功、PowerShell構文パース確認（2スクリプト）成功。
- 2026-02-15: `pwsh -File scripts/windows/build-app.ps1 -Version 0.1.0-ci -Configuration Release -Runtime win-x64 -Clean` 実行成功（publish出力生成）。
- 2026-02-15: 非Windows環境のため `ISCC.exe` 実実行によるインストーラー生成は未検証（Windowsで要確認）。
