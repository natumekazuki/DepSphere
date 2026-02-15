# グラフ描画退行修正

## Goal
- グラフが表示されない退行を解消し、操作要件（右回転/左パン/ズーム）を維持する。

## Task List
- [x] 外部 `OrbitControls` 依存を除去し、内蔵カメラ操作実装へ置換する。
- [x] 既存のノード選択・フォーカス・スケール調整が動作するように接続し直す。
- [x] 回帰テストを更新して描画スクリプト要件を固定する。
- [x] テスト実行で回帰がないことを確認する。
- [x] 完了後に計画をアーカイブする。

## Affected Files
- `src/DepSphere.Analyzer/GraphViewHtmlBuilder.cs`
- `tests/DepSphere.Analyzer.Tests/GraphViewHtmlBuilderTests.cs`

## Risks
- カメラ操作実装の細部（パン速度/ズーム下限）によって体感が変わる可能性がある。
- ポインタイベント競合でクリック選択が不安定になる可能性がある。

## Design Check
- 判定: 軽微変更のため更新不要
- 理由: 仕様は維持し実装方式のみ切替。

## Notes / Logs
- 2026-02-15: 初版作成。
- 2026-02-15: `OrbitControls` 依存を除去し、`createCameraController` に置換。
- 2026-02-15: `dotnet test tests/DepSphere.Analyzer.Tests/DepSphere.Analyzer.Tests.csproj -v minimal` 実行、62件成功。
