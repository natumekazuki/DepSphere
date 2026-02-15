# グラフ操作性改善（Fit/状態バー/ショートカット）

## Goal
- グラフ探索の操作性を改善し、迷子/誤操作を減らす。
- 対象: Fit to View、フィルタ状態表示、シングル/ダブルクリック誤爆対策、主要ショートカット。

## Task List
- [ ] `Fit to View` ボタンを追加し、表示中ノード全体を画面内に収める。
- [ ] フィルタ状態バー（表示中ノード数/全体ノード数/起点）を追加する。
- [ ] シングルクリック処理を遅延確定し、ダブルクリック時の誤絞り込みを防止する。
- [ ] キーボードショートカット（`F`/`Esc`/`Ctrl+F`）を追加する。
- [ ] テストを更新して回帰確認する。
- [ ] 設計ドキュメントを更新し、計画をアーカイブする。

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
