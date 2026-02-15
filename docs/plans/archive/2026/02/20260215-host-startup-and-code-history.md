# ホスト起動挙動とコード履歴ナビ改善

## Goal
- 起動時のサンプル自動表示を廃止し、明示的な解析実行起点へ統一する。
- ボタンの無効時コントラストを改善し、キャンセルボタンを含む同系統配色ボタンの可読性を担保する。
- コードシンボルリンク遷移に対して、戻る/進む操作をホストUIから行えるようにする。

## Task List
- [x] 起動時フローからサンプル自動読込を除去し、初期プレースホルダー表示へ置換する。
- [x] `Reload` のフォールバック挙動を見直し、未解析時にサンプルへ戻さない。
- [x] 共通 `Button` スタイルを更新し、無効時の前景/背景コントラストを調整する。
- [x] キャンセルボタンのスタイルを危険操作向け配色に変更する。
- [x] コードナビ履歴（戻る/進む）の状態管理を `MainWindow` に実装する。
- [x] コードナビ履歴ボタンを操作パネルへ追加し、選択同期と有効/無効制御を連携する。
- [x] 関連ドキュメント (`docs/design/dep-visualizer-core.md`) を更新する。
- [x] 既存テストを実行して回帰を確認する。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `src/DepSphere.App/App.xaml`
- `docs/design/dep-visualizer-core.md`

## Risks
- 履歴適用時に再帰的に履歴追加されると戻る/進むが破綻する。
- WebView再描画時の選択同期がずれると、グラフフォーカスとコード表示に乖離が出る。
- WPFテーマ変更で既存ボタンの視認性は改善しても、強調度が不足する可能性がある。

## Design Doc Check
- 本変更はホスト操作仕様とビューア連携仕様に影響するため、`docs/design/dep-visualizer-core.md` の更新を必須とする。

## Notes / Logs
- `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行: 成功（68 passed）。
- `dotnet build src/DepSphere.App/DepSphere.App.csproj -v minimal` 実行: 成功。
