# Release運用: インストーラー自動公開

## Goal
- タグ作成時に Inno Setup でインストーラーを生成し、GitHub Release へ自動添付する。
- 手動配布オペレーションを最小化し、リリース再現性を高める。

## Task List
- [ ] リリース専用 workflow（tag push / manual dispatch）を追加する。
- [ ] workflow 内でバージョン解決・Inno Setup生成・artifact保存を実装する。
- [ ] 生成したインストーラーを GitHub Release に自動添付する。
- [ ] 設計ドキュメントに Release 連携手順を追記する。
- [ ] ローカルで検証可能な範囲（テスト）を実施し、計画をアーカイブする。

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
