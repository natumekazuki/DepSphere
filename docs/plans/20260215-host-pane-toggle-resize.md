# ホストUIのペイン操作改善

## Goal
- 左の操作ペインを表示/非表示で切り替え可能にする。
- グラフペインとコードペインの幅をドラッグで調整可能にする。

## Task List
- [ ] 左操作ペインの表示切替ボタンをヘッダーに追加する。
- [ ] 左ペイン表示状態に応じて列幅/区切り表示を切り替える。
- [ ] 左ペイン-中央ペイン、中央ペイン-右ペインに `GridSplitter` を追加する。
- [ ] 設計ドキュメントに操作仕様を追記する。
- [ ] ビルド確認後に計画をアーカイブする。

## Affected Files
- `src/DepSphere.App/MainWindow.xaml`
- `src/DepSphere.App/MainWindow.xaml.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 列幅の保持処理が不適切だと再表示時に幅が意図せず変わる可能性がある。
- Splitter追加により最小幅制約が不足すると表示崩れが発生する可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: ホスト操作仕様に新機能を追加するため。

## Notes / Logs
- 2026-02-15: 初版作成。
