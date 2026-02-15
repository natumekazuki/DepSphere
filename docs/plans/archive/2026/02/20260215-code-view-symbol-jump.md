# コードビューからグラフジャンプ導線

## Goal
- コード表示内でシンボルをダブルクリックした際に、対応ノードへグラフ/コードを同期ジャンプできるようにする。

## Task List
- [x] コードビューHTMLにシンボルリンクマップを埋め込み、ダブルクリック通知を追加する。
- [x] ホストでコードビューからのWebMessageを受信し、ノード選択処理へ接続する。
- [x] グラフ側へフォーカススクリプトを実行して同期表示する。
- [x] 関連テストを更新して回帰確認する。
- [x] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/SourceCodeViewerHtmlBuilder.cs`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `tests/DepSphere.Analyzer.Tests/SourceCodeViewerHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 同名型が複数ある場合、リンク先解決が曖昧になる可能性がある。
- コードWebViewの再描画直後に連続ジャンプすると操作感が不安定になる可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: コードビューからグラフへの双方向導線を追加するため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: コードビュー内ダブルクリックで `nodeSelected` 通知するスクリプトを追加。
- 2026-02-15: ホストで `CodeWebView` メッセージ受信を追加し、グラフフォーカス同期を実装。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行、68件成功。
- 2026-02-15: `dotnet build src/DepSphere.App/DepSphere.App.csproj -v minimal` 実行、成功。
