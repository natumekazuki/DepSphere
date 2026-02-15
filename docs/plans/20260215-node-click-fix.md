# ノードクリック無反応の修正

## Goal
- 3Dグラフでノードクリック時に右ペインのコード表示が確実に更新されるようにする。

## Task List
- [ ] WebViewメッセージ送信形式（JS）と受信パーサ（C#）の不一致を解消する。
- [ ] 既存形式との互換性（JSON文字列化されたメッセージ）を維持する。
- [ ] 関連テストを追加・更新する。
- [ ] ビルド/テストで回帰確認し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `src/DepSphere.Analyzer/GraphHostMessageParser.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`

## Risks
- 受信パーサの変更で既存メッセージ形式の互換性を壊す可能性がある。

## Design Check
- 判定: 不要
- 理由: メッセージ連携の実装修正であり、外部仕様変更を伴わないため。

## Notes / Logs
- 2026-02-15: 初版作成。
