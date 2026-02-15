# P3-1: Windows CIジョブ追加（WPFビルド/最小起動確認）

## Goal
- GitHub Actions で Windows ジョブを実行し、`DepSphere.App` のビルドと最小起動確認を自動化する。

## Task List
- [ ] `.github/workflows` に Windows CI workflow を追加する。
- [ ] AnalyzerテストをWindows上で実行し、既存回帰を確認する。
- [ ] `DepSphere.App` をWindows上でビルドする。
- [ ] ビルド成果物の最小起動スモーク（起動して短時間で終了）を追加する。
- [ ] Non-Windowsバックログへ進捗を反映する。

## Affected Files
- `.github/workflows/windows-ci.yml`
- `docs/plans/20260215-nonwindows-backlog.md`

## Risks
- GitHub Hosted Runner 上でGUIアプリの起動検証が不安定になる可能性がある。
- WebView2ランタイムや環境差分でスモーク起動が失敗する可能性がある。

## Design Check
- 判定: 不要
- 理由: プロダクト仕様の変更ではなく、CI運用追加のため。

## Notes / Logs
- 2026-02-15: 初版作成。
