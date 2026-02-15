# WPFホスト実装計画（モダンシェル）

## Goal
- Windows向けに `WPF + WebView2` のホストアプリを追加する。
- 左:操作、中央:3Dビュー、右:コードビューの3ペインUIをモダン寄せで実装する。
- 既存 `DepSphere.Analyzer` のHTML生成機能を使って最小表示・選択連携を成立させる。

## Scope
- 対象:
  - `src/DepSphere.App`（新規WPFアプリ）
  - WebView2でグラフHTML表示
  - WebView2でコードビューHTML表示
  - ノード選択メッセージを受けてコード表示更新
- 非対象（次段）:
  - 本格的なテーマ切替
  - 詳細設定UI
  - プロダクション品質の例外回復UI

## Design Doc要否チェック
- 判定: **更新必須**
- 更新対象:
  - `docs/design/dep-visualizer-core.md`（ホスト層構成を追記）

## Task List
- [ ] P1: WPFアプリプロジェクト作成とソリューション追加
- [ ] P2: モダン3ペインシェル（左/中央/右）を実装
- [ ] P3: WebView2ホスト接続（グラフ表示 + コード表示）
- [ ] P4: ノード選択イベント連携（クリック -> コード表示更新）
- [ ] P5: 最小動作確認（起動/表示/選択）
- [ ] P6: 設計書更新と計画更新

## Affected Files
- `docs/plans/20260215-wpf-host-modern-shell.md`
- `src/DepSphere.App/*`（新規）
- `DepSphere.sln`
- `docs/design/dep-visualizer-core.md`

## Risks
- Mac環境ではWPF実行確認ができないため、ビルド検証が限定される可能性がある。
- WebView2イベントのメッセージ形式差異で連携が崩れる可能性がある。
- 既存Analyzerとの参照境界（Windows依存コード分離）に注意が必要。

## Notes / Logs
- 2026-02-15: 初版作成。
