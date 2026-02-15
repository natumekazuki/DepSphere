# GitHub Actions: Inno Setup失敗の修正

## Goal
- `windows-ci` の `Build Installer (Inno Setup)` 失敗を再発しない形で修正する。

## Task List
- [x] 失敗原因をログに基づいて特定し、修正方針を確定する。
- [x] `scripts/windows/build-installer.ps1` の `PublishDir` 取得処理を修正する。
- [x] ローカルで実行可能な範囲の回帰確認を行う。
- [x] 計画ファイルを完了更新してアーカイブする。

## Affected Files
- `scripts/windows/build-installer.ps1`

## Risks
- PowerShellの出力チャネルの扱い次第で、別環境で再発する可能性がある。

## Design Check
- 判定: 不要
- 理由: 既存配布仕様の不具合修正であり、設計変更を伴わない。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: 失敗原因を特定。`build-app.ps1` の標準出力（restoreログ等）を `PublishDir` 変数に混入させ、`Resolve-Path` が無効文字列を解釈して失敗していた。
- 2026-02-15: `build-installer.ps1` で `build-app.ps1` 実行結果を `Select-Object -Last 1` で絞り込み、`PublishDir` の空文字ガードを追加。
- 2026-02-15: `dotnet test DepSphere.sln` を実行し 57 件成功。GitHub Actions 上の再実行検証は push 後に実施。
