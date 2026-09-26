# Fusion Ledger Windows: Codex から Claude Code への引継ぎ

## 役割と作業範囲

今後、Codex は Claude Code に渡すプロンプトの作成だけを担当する。リポジトリの調査、実装、テスト、ビルド、修正、配布準備は Claude Code が担当する。ただし、この文書は直ちに実装を命じるものではない。次の具体的なユーザー指示を待ち、その範囲を守ること。コンセプトや UI の相談だけが依頼された場合はコードを変更しない。

- Windows リポジトリ: `C:\Users\Azunel\Documents\Codex\Fusion Legder\FusionLedger.Windows`
- Web リポジトリ: `C:\Users\Azunel\Documents\mfa edit site`
- 引継ぎ時点の基準: Windows `2b0183b` (v0.5.2)、Web `8839dc7` (v0.18.0)。作業開始時に最新状態を再確認すること。

各リポジトリで存在する `AGENTS.md` と作業ツリーを最初に確認し、ユーザーの既存変更を保護する。引継ぎ時点で Web 側の `docs/HANDOFF_CLAUDE_CODE.md` と `docs/HANDOFF_CODEX.md` は既存の追跡ファイルで、Web の HEAD `8839dc7` は clean です。この引継ぎ更新では触れていません。

## 製品と経緯

Web アプリの現行表示名は MolHub（v0.16.0 で Fusion Ledger から変更）である。Windows アプリの現行製品名は Fusion Ledger for Windows のままである。Web は Clickteam Fusion の MFA 本体を保存せず、各版の外部共有 URL と変更履歴を管理する。ファイル自体のアクセス権は外部ストレージ側で管理する。Web アプリは Cloudflare Workers、D1、静的 HTML/CSS/JavaScript で構成される。DB 名、API パス、`window.fusionLedgerBridge` などの内部識別子はブランド変更に合わせて機械的に改名しない。

Windows v0.5.1 でログイン後にメインウィンドウへ移行しない問題は解消した。しかし、メインウィンドウが空白のまま一定時間後にクラッシュした。v0.5.2 では当時の例外ログで画面表示時の `Frame.Navigate` に関連するアクセス違反を確認し、その経路を廃止して `ContentFrame.Content` にネイティブ画面を設定する構成へ修正した。テスト、x64 Debug ビルド、インストーラの `-WhatIf` は通過したとの報告がある。修正版をインストールしてログインする実機確認は未完了であり、クラッシュ解消を断定しないこと。

## Windows v0.5.2 の現状

WinUI 3 の packaged x64 アプリで、Microsoft.WindowsAppSDK 2.5.1 を使用する。専用の `LoginWindow` だけが WebView2 を作り、production origin の正確な HTTPS GET `/api/me` 応答を検証して、認証済みユーザーのスナップショットを `MainWindow` に渡す。`MainWindow` は Web ページのラッパーではなく、ネイティブの TitleBar、検索欄、NavigationView、アカウントと通知のフライアウトを備える。アプリ設定とバージョン情報は機能する。Dashboard、Projects、Commit history、Administration、Profile settings、Server maintenance は未接続のプレースホルダーで、架空の件数や操作を表示しない。

現行 `LoginWindow` は WebMessage、ホストオブジェクト、DevTools を無効化し、アプリ内で許すオリジン、外部リンク、ポップアップ、権限を制限する。Cookie、ヘッダー、秘密情報を読み出したりログへ記録したりしない。現在のサインアウトは WebView2 の Cookie とサイトデータを消去するが、サーバー側セッションを失効させる API 呼び出しは行っていない。Windows の現行設定は英語・日本語、テーマは System／Light／Dark に対応し、高コントラストとアクセシビリティにも配慮する。Web の dark-only 方針を Windows に適用するかは未決の設計判断であり、現行 Windows の設定を事実として残す。

## Web v0.18.0 の現状と v0.15.0 導入の API・bridge

Web v0.15.0 で Windows ネイティブ画面向けの `/api/v1`、`docs/openapi.yaml` による契約、同一オリジンの `/webview-bridge.html` と固定コマンドの `public/webview-bridge.js` が追加され、現行リポジトリにも残っている。従来の `/api/*` は Web UI 向けに存続する。文書上の対象は、セッション・メンテナンス、Dashboard、プロジェクト一覧・詳細・コミット、本人のコミット履歴、作業予約・解除、版の投稿、プロフィール・パスワード・ログアウト、プロジェクト管理者操作、サイト管理者操作、監査・申請・保守である。Discord 関連の状態と操作も既存の Web 側ビジネスロジックを利用する。`docs/openapi.yaml` の API 名には旧 Fusion Ledger 表記が残り、server URL は `example.invalid` の置換用値である。実装内容と `/api/v1` の本番配備状態は、着手時にコードと実環境で再確認すること。

Web v0.16.0 で表示名を MolHub に変更し、v0.17.0 でダークテーマのみとなり、左上の MolHub wordmark だけに Archivo Black を採用した。v0.18.0 では hash-based routing と Dashboard／Project の UI 調整が行われた。Web の現行 `AGENTS.md` に従い、通常のラテン UI 文字にはセルフホストの Mona Sans VF、日本語にはフォールバック、UI アイコンには Heroicons 24px outline を使用する。Archivo Black は wordmark 専用である。ニュートラルな暗い面と柔らかい赤のアクセントを使用し、青いアクセントやグラデーション、ライトテーマやテーマ切替を再導入しない。英語を既定として日本語訳、動きを減らす設定、モバイル下部ナビゲーション、承認フロー、外部ストレージの注意を維持する。権限外のデータや架空の件数・操作を UI 都合で追加しない。これらは Web 側の規約であり、Windows の製品名・テーマとの統一は今後の設計判断とする。

bridge は WebView2 で既存の Web ログインを終えた後、同じ production origin の bridge ページを読み込んで使用する。Windows host は HttpOnly `fusion` Cookie を読み出し、コピー、保存、手動送信しない。独自の `Authorization` ヘッダーを足さない。WebMessage の要求は `{ command, requestId, payload }` で、公開された固定コマンドに限定する。任意の URL や HTTP メソッドは受け付けない。応答を requestId に対応付け、パスワード、Cookie、セッショントークンなどを host に渡さない。現行 Windows 側では WebMessage が無効で bridge 未接続のため、導入時には安全な有効化範囲、origin とナビゲーションの検証、ログイン画面との分離を設計する必要がある。

API の認可境界を維持する。本人のコミット履歴は、その本人が現在アクセス可能なプロジェクトへ投稿したものだけ。プロジェクト情報は所属・オーナー・サイト管理者の権限内に限定される。投稿と予約では現在の予約所有者や最新版との競合を扱う。`401`、`403`、`409`、`429`、`503` は通常の UI 状態として表現し、タイムアウト後の投稿や予約を盲目的に再送しない。状態を再取得し、利用者に判断を求める。メンテナンス状態やセッション失効も画面遷移へ反映する。

## ネイティブ UI の構想

現行 Web の `docs/DESIGN.md` に沿い、Dashboard は参加プロジェクト、閲覧可能な最近の動き、自分の作業予約、承認待ちを優先する。Projects は一覧から詳細へ進み、詳細に「概要」「コミット」「作業予約」「メンバー」「設定（権限がある場合）」を置く。版の取得、作業予約・解除、投稿、プロジェクト固有の履歴を扱う。Commit history は本人投稿のみを日付順に表示し、プロジェクト、期間、文字列、最新版で絞り込んでページ送りする。Administration は権限に応じて承認、担当任命、所属、予約解除、Discord、削除・復元申請、監査や保守を扱う。Web v0.18.0 は画面をハッシュ URL に反映し、戻る／進むや再読込から画面を復元するため、Windows 側でも対応するナビゲーション体験を検討する。

既存の NavigationView を軸に Dashboard、Projects、Commit history をトップレベルとし、管理は権限付きのネスト画面として検討する。Web の３列レイアウトを機械的に複製せず、WinUI の画面幅、戻る操作、リスト、タブ、絞り込み、確認ダイアログ、読込中・空・失敗・競合の状態を設計する。Web の承認フローと外部ストレージの注意書きを維持し、実データ以外の件数や通知を作らない。英語を既定とし、日本語、Windows 現行のテーマ設定、高コントラスト、キーボード操作、アクセシビリティを保つ。Windows と Web の表示名・テーマを揃えるかは別途判断する。

検討上の導入順は、(1) セッションと bridge 接続、(2) 読み取り専用の Dashboard・Projects・本人履歴、(3) 作業予約と投稿、(4) プロフィールと管理操作。これは実装指示ではなく、次のユーザー依頼と最新コードに合わせて調整する案である。通知やトレイ常駐は v0.5.2 では未実装で、データ・配信・背景動作の契約が確認できるまで架空の通知を追加しない。

## 検証と未確認事項

Windows 側の `README.md` に記された policy tests、x64 Debug ビルド、`Install-Prototype.ps1 -WhatIf` を基本確認とする。変更内容に応じて、認証、権限、画面状態、セッション失効、競合、メンテナンスを検証する。ビルド成功とログイン後の実機動作は区別して報告する。開発用 MSIX は未署名であり、実配布には署名と配布手順の検討が必要。API 契約の記載と Web 側の本番配備も同一視せず確認する。

主要資料: Windows `README.md`、`DESIGN.md`、`MainWindow.xaml`、`MainWindow.xaml.cs`、`LoginWindow.xaml.cs`。Web `docs/WINDOWS_API.md`、`docs/openapi.yaml`、`docs/DESIGN.md`、`src/api-v1.js`、`public/webview-bridge.js`。このメモは引継ぎ時点の情報であり、具体的な bridge コマンド、DTO、権限、配備状態は最新の資料と実装で確認すること。
