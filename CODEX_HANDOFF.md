# 新しい Codex チャットへの引継ぎ

以下を新しい Codex チャットへ貼り付けてください。

```text
Fusion Ledger Windows の引継ぎです。このチャットでの Codex の役割は、Claude Code に渡すプロンプトの作成だけです。今後のリポジトリ調査、実装、テスト、ビルド、修正、配布準備は Claude Code が担当します。私が明示的に依頼するまで、Codex はこれらを実行しないでください。次の具体的な作業は、私の指示を待ってください。

対象の Windows リポジトリは C:\Users\Azunel\Documents\Codex\Fusion Legder\FusionLedger.Windows です。v0.5.2 は WinUI 3 のネイティブ shell と WebView2 ログインを備えていますが、Dashboard、Projects、Commit history、Administration は未接続のプレースホルダーです。v0.5.1 でログイン後にメインへ移行できるようになった一方、空白のメインウィンドウが後でクラッシュする問題が報告されました。v0.5.2 で Frame.Navigate を使わない構成に修正しましたが、実機での解消確認は未完了です。

Web リポジトリは C:\Users\Azunel\Documents\mfa edit site です。現行 Web v0.18.0 の表示名は MolHub です。v0.15.0 で Windows 向け /api/v1、OpenAPI 契約、同一オリジンの固定コマンド WebView2 bridge が導入され、現行リポジトリにも残っています。本番配備は未確認です。Windows 側の WebMessage は無効のままで bridge host は未実装です。Web はダークテーマのみですが、Windows v0.5.2 は独立した Fusion Ledger for Windows という製品名と System／Light／Dark 設定を持ちます。両者のブランドとテーマを統一するかは未決です。詳細は Windows 側 CLAUDE_CODE_HANDOFF.md と Web 側 AGENTS.md、docs/WINDOWS_API.md、docs/openapi.yaml を参照してください。

次に相談したいのは、既存 Web の重要な操作を Dashboard、Projects、Commit history、Administration のネイティブ UI にどう落とし込むかです。情報の優先順位、WinUI に適した画面構成、安全な bridge 接続、段階的な導入を議論し、私が依頼した範囲に合わせて Claude Code 用プロンプトを作ってください。議論中に Claude Code へ直ちに実装を命じないでください。
```
