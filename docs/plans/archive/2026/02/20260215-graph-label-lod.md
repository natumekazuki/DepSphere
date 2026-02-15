# グラフラベルLOD最適化

## Goal
- 遠景でのラベル重なりと描画負荷を抑えつつ、重要ノードの可読性を維持する。

## Task List
- [x] ラベルLOD制御を実装し、遠景では重要ノード中心に表示する。
- [x] 近景/選択/ホバー/フィルタ時はラベル可読性を優先する。
- [x] カメラ移動時にラベル表示状態が追随するよう更新処理を追加する。
- [x] テストを更新して回帰確認する。
- [x] 設計ドキュメントを更新し、計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`
- `docs/design/dep-visualizer-core.md`

## Risks
- LOD閾値が厳しすぎると必要なラベルが消える可能性がある。
- フレームごとの表示判定でノード数が多い場合に負荷が増える可能性がある。

## Design Check
- 判定: 更新必須
- 更新対象: `docs/design/dep-visualizer-core.md`
- 理由: ラベル表示仕様を更新するため。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: ラベルLOD（距離 + 重要度判定）を追加。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行、67件成功。
