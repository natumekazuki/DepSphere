# 依存可視化コア設計

## 1. 目的
- C#ソースコードを静的解析し、Type単位の依存関係とホットスポット重みを算出する。
- 解析結果を3Dグラフで可視化し、ノード選択からコード閲覧へ遷移できるようにする。

## 2. スコープ
- 対象:
  - `.sln` / `.csproj` 入力
  - 依存抽出（`reference` / `inherit` / `implement`）
  - 重み算出（静的メトリクス）
  - 3Dグラフ表示と選択操作
  - 内蔵ビューアへの遷移情報提供
- 対象外:
  - 実行時プロファイル
  - 動的ディスパッチの完全解決

## 3. 論理アーキテクチャ
```mermaid
flowchart LR
  A[UI Host/WPF] --> B[Application Service]
  B --> C[Roslyn Analyzer]
  B --> D[Weight Calculator]
  B --> E[Graph Store]
  E --> F[WebView2 + 3D Renderer]
  B --> G[Source View Adapter]
  G --> H[Embedded Code Viewer]
```

## 4. データモデル
```mermaid
classDiagram
  class GraphNode {
    string NodeId
    string Fqn
    string DisplayName
    string ProjectId
    string DocumentPath
    int StartLine
    int EndLine
    Metrics Metrics
  }

  class GraphEdge {
    string EdgeId
    string FromNodeId
    string ToNodeId
    EdgeKind Kind
    string Confidence
  }

  class Metrics {
    int MethodCount
    int StatementCount
    int BranchCount
    int CallSiteCount
    int FanOut
    int InDegree
    double WeightScore
  }

  class SymbolLocation {
    string DocumentPath
    int StartLine
    int StartColumn
    int EndLine
    int EndColumn
  }

  GraphNode --> Metrics
```

## 5. 依存抽出ルール
- 抽出単位: `NamedTypeSymbol`（class/struct/record/interface）
- エッジ種別:
  - `reference`: フィールド型、プロパティ型、引数型、戻り値型、`new`、静的メンバー参照
  - `inherit`: 基底クラス
  - `implement`: 実装インターフェース
- 除外:
  - `System.*` など外部BCLはノード生成しない（集約カウントのみ可）
  - 解析不能シンボルは `confidence = estimated`

## 6. 重み（ホットスポット）算出
### 6.1 指標
- `MethodCount`
- `StatementCount`
- `BranchCount`
- `CallSiteCount`
- `FanOut`
- `InDegree`

### 6.2 正規化
- 各指標 `x` を `log1p(x)` 化
- 対象集合の `P95` でクリップし `0..1` にスケール

### 6.3 合成スコア
```text
WeightScore =
  0.15*MethodCountN +
  0.30*StatementCountN +
  0.20*BranchCountN +
  0.20*CallSiteCountN +
  0.10*FanOutN +
  0.05*InDegreeN
```

### 6.4 判定
- 上位10%: `Hotspot`
- 上位3%: `Critical`

## 7. 3D表示仕様
- ノード:
  - サイズ: `WeightScore`
  - 色: 通常（青系）/ Hotspot（橙）/ Critical（赤）
- エッジ:
  - 色分け: `reference` / `inherit` / `implement`
  - 矢印で方向を表示
- 基本操作:
  - 回転、ズーム、パン
  - ノード選択、検索、フォーカス

## 8. ビューア連携仕様
- ノード選択時に `SymbolLocation` を解決
- 内蔵ビューアへ `DocumentPath + Range` を渡して表示
- 初期表示はReadOnly（編集機能は持たない）

## 9. 主要フロー
```mermaid
sequenceDiagram
  participant U as User
  participant UI as 3D UI
  participant APP as App Service
  participant AN as Analyzer
  participant GS as Graph Store
  participant ED as Embedded Viewer

  U->>UI: プロジェクト読み込み
  UI->>APP: AnalyzeCommand
  APP->>AN: 依存/メトリクス解析
  AN-->>APP: ノード/エッジ/位置情報
  APP->>GS: グラフ保存
  GS-->>UI: 描画データ

  U->>UI: ノードクリック
  UI->>APP: OpenNode(nodeId)
  APP->>ED: Open(DocumentPath, Range)
```

## 10. 非機能
- 初回解析目標: 中規模ソリューションで60秒以内
- ノード選択からビューア表示: 500ms以内（キャッシュ時）
- メモリ: 解析対象の構文木キャッシュを再利用し再解析時間を抑制

## 11. 受入対応
- A1, A2, A3 を本設計でカバーする。

## 12. テスト方針（TDD）
- 本機能はTDDで実装する。
- 最低限の先行テスト:
  - 依存抽出ルール（`reference`/`inherit`/`implement`）の単体テスト
  - 重み計算（正規化/合成スコア）の単体テスト
  - ノード選択からビューア遷移までの連携テスト

## 13. ホスト実装方針（WPF + WebView2）
- ホストは `src/DepSphere.App` に分離し、`DepSphere.Analyzer` を参照して描画HTML/コードHTMLを受け取る。
- UI構成:
  - 左ペイン: 操作・状態・選択ノード表示
  - 中央ペイン: 3Dグラフ（`GraphViewHtmlBuilder`）
  - 右ペイン: コードビュー（`SourceCodeViewerHtmlBuilder`）
- イベント連携:
  - 中央WebViewでノードクリック
  - `nodeSelected` メッセージをホストが受信
  - `GraphSelectionCoordinator` でコード取得
  - 右ペインへHTML再描画
- フォールバック:
  - SourceLocationが無い場合はノードのメトリクス情報を右ペインへ表示する。
- 解析入力導線:
  - 左ペインの `ProjectPathTextBox` + `参照` ボタンで `.sln/.csproj` を選択する。
  - `解析実行` ボタンで `DependencyAnalyzer.AnalyzePathAsync` を起動し、中央グラフと右ペイン初期表示を更新する。
  - `再解析` は直近の解析パスを優先し、未指定時はサンプル解析にフォールバックする。
  - 解析実行中は入力系ボタンを無効化し、多重実行を防止する。
  - `キャンセル` ボタンで `CancellationToken` を通知し、停止時は状態表示を `解析をキャンセルしました。` に更新する。
- ビルド方針:
  - `net8.0-windows10.0.19041.0` + `UseWPF=true`
  - 非Windows環境ビルドのため `EnableWindowsTargeting=true` を設定する。
