# Release運用: インストーラー自動公開

## Goal
- タグ作成時に Inno Setup でインストーラーを生成し、GitHub Release へ自動添付する。
- 手動配布オペレーションを最小化し、リリース再現性を高める。

## Task List
- [x] リリース専用 workflow（tag push / manual dispatch）を追加する。
- [x] workflow 内でバージョン解決・Inno Setup生成・artifact保存を実装する。
- [x] 生成したインストーラーを GitHub Release に自動添付する。
- [x] 設計ドキュメントに Release 連携手順を追記する。
- [x] ローカルで検証可能な範囲（テスト）を実施し、計画をアーカイブする。

## Affected Files
- `.github/workflows/release-installer.yml`
- `docs/design/windows-installer-inno-setup.md`

## Risks
- タグ命名規則の不一致でバージョン解決に失敗する可能性がある。
- Release 作成権限（`contents: write`）不足でアップロードに失敗する可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/windows-installer-inno-setup.md`
- 理由: 配布運用フロー（Release自動公開）の仕様追加を伴うため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `.github/workflows/release-installer.yml` を追加（tag push / workflow_dispatch）。
- 2026-02-15: `softprops/action-gh-release@v2` によりインストーラーを Release へ自動添付する処理を追加。
- 2026-02-15: `windows-installer-inno-setup.md` に Release 自動公開フローを追記。
- 2026-02-15: `dotnet test DepSphere.sln` を実行し 57 件成功（ローカル回帰確認）。
