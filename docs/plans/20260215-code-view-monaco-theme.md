# コードビューのVSライク化（Monaco導入）

## Goal
- コードビューを Visual Studio に近い見た目・操作感へ改善する。
- 既存のシンボルダブルクリックジャンプ導線を維持する。

## Task List
- [ ] `SourceCodeViewerHtmlBuilder` を Monaco Editor ベースへ置換する。
- [ ] テーマ・行番号・読み取り専用・初期ハイライトを設定する。
- [ ] 既存のシンボルダブルクリック通知を Monaco 選択イベントへ接続する。
- [ ] Monaco 初期化失敗時のフォールバック表示を用意する。
- [ ] 関連テストを更新して回帰確認する。
- [ ] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/SourceCodeViewerHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/SourceCodeViewerHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- CDN読込に失敗した環境で Monaco が起動しない可能性がある。
- WebView2でWorkerパス解決が崩れるとシンタックス強調が機能しない可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: コードビュー実装方式とUI仕様が変わるため。

## Notes / Logs
- 2026-02-15: 初版作成。
