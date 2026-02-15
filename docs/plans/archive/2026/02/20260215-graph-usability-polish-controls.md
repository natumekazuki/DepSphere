# グラフ操作性改善（Fit/状態バー/ショートカット）

## Goal
- グラフ探索の操作性を改善し、迷子/誤操作を減らす。
- 対象: Fit to View、フィルタ状態表示、シングル/ダブルクリック誤爆対策、主要ショートカット。

## Task List
- [x] `Fit to View` ボタンを追加し、表示中ノード全体を画面内に収める。
- [x] フィルタ状態バー（表示中ノード数/全体ノード数/起点）を追加する。
- [x] シングルクリック処理を遅延確定し、ダブルクリック時の誤絞り込みを防止する。
- [x] キーボードショートカット（`F`/`Esc`/`Ctrl+F`）を追加する。
- [x] テストを更新して回帰確認する。
- [x] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- クリック遅延時間が長すぎると操作レスポンスが悪化する。
- ショートカットがブラウザ既定操作と競合する可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: 操作導線とUI要素が追加されるため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `Fit to View` / 検索ボックス / フィルタ状態バーをオーバーレイに追加。
- 2026-02-15: シングルクリック遅延確定（220ms）とダブルクリック時キャンセルを実装。
- 2026-02-15: `F` / `Esc` / `Ctrl+F` ショートカットを追加。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行、64件成功。
