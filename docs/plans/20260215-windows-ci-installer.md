# Windows CI: Inno Setupインストーラー生成追加

## Goal
- `windows-ci` workflow で `DepSphere.App` の Inno Setup インストーラー生成まで自動化する。
- 生成したセットアップEXEをArtifactsとして取得可能にする。

## Task List
- [ ] 現行 `windows-ci.yml` に Inno Setup 導入ステップを追加する。
- [ ] `build-installer.ps1` をCIから呼び出し、セットアップEXEを生成する。
- [ ] 生成インストーラーを `upload-artifact` で保存する。
- [ ] 関連ドキュメントにCI実行導線を追記する。

## Affected Files
- `.github/workflows/windows-ci.yml`
- `docs/design/windows-installer-inno-setup.md`

## Risks
- GitHub Hosted Runner の Inno Setup インストール失敗時にジョブ全体が失敗する。
- バージョン命名規則が曖昧だと成果物名が追跡しにくくなる。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/windows-installer-inno-setup.md`
- 理由: CIによる生成導線を仕様として明文化するため。

## Notes / Logs
- 2026-02-15: 初版作成。
