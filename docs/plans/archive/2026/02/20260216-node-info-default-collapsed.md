# ノード情報オーバーレイ初期折りたたみ化

## Goal
- Issue #6 の要件に合わせ、ノード情報オーバーレイをデフォルトで縮小（折りたたみ）表示にする。

## Task List
- [x] `GraphViewHtmlBuilder` のノード情報パネル初期状態を折りたたみに変更する。
- [x] 初期ARIA属性とトグルアイコンの整合を取り、アクセシビリティ上の意味を維持する。
- [x] 関連テストと設計ドキュメントの記述を更新する。
- [x] `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` を実行して確認する。

## Affected Files
- `docs/plans/20260216-node-info-default-collapsed.md`
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- 既存利用者が初期表示の情報量減少を使いづらく感じる可能性がある。
- ARIAラベルや初期アイコンが実状態と一致しないと操作性が落ちる可能性がある。

## Design Check
- オーバーレイの初期表示仕様変更のため、`docs/design/dep-visualizer-core.md` を更新する。

## Notes / Logs
- 2026-02-16: `node-info-toggle` の初期 `aria-expanded` を `false`、ラベルを `ノード情報を展開`、アイコンを `△` に変更。
- 2026-02-16: `isNodeInfoExpanded` の初期値を `false` に変更し、ノード情報オーバーレイをデフォルト折りたたみに変更。
- 2026-02-16: `GraphViewHtmlBuilderTests` の期待値を初期折りたたみ仕様へ更新。
- 2026-02-16: `docs/design/dep-visualizer-core.md` に「デフォルトは折りたたみ」を追記。
- 2026-02-16: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj --filter GraphViewHtmlBuilderTests` 実行（13件合格）。
