# Fusion Ledger for Windows — design規約

この文書は、Fusion Ledger for Windows の WinUI 3 ネイティブUIを実装・レビューするための規約です。Windows 11 の Fluent Design guidance と WinUI 3 のネイティブコントロールを基準にします。

## 基準となる資料

次の3資料を本プロジェクトの基準資料とします。

1. https://learn.microsoft.com/ja-jp/windows/apps/design/iconography/segoe-fluent-icons-font
2. https://learn.microsoft.com/ja-jp/windows/apps/design/signature-experiences/typography
3. https://developer.microsoft.com/en-us/fluentui#/

実装時は、章ごとに記載するMicrosoft Learnの一次資料も確認してください。Microsoft LearnのWindowsアプリ設計ガイダンスは、Fluent Designの原則をWindows、入力方式、画面サイズに適用するための資料です。[Fluent UI](https://developer.microsoft.com/en-us/fluentui#/)はWeb/Reactライブラリを含む参照ハブであり、WinUI 3アプリへパッケージ導入するという意味ではありません。Fusion Ledger for Windowsのnative側では、WinUI 3/Windows App SDKのbuilt-in Fluent stylesとXAMLコントロールを使用します。

## Scope

- UIはWinUI 3のXAMLネイティブUIで構築する。Web版と同じCloudflare API・認証を利用するが、Windows専用のナビゲーション、タイトルバー、通知、状態表示はネイティブ層に置く。
- WebView2はWeb版を表示する境界として扱う。WebView2のページへCSS、JavaScript、DOM操作、テーマ用の注入スクリプトを追加しない。
- WebView2へネイティブUIを埋め込まない。ネイティブUIとWebコンテンツの通信を追加する場合は、目的・データ・方向を限定したレビュー対象のAPIにする。
- データベースへ直接接続しない。権限、認証、データ取得は既存のCloudflareサーバー/APIの責務とする。
- 既存Web版の英語・日本語、ライト・ダークテーマ、承認フロー、外部ストレージのプライバシー表示を壊さない。

## プラットフォームとコンポーネント

使用するのはWinUI 3 / Windows App SDKのネイティブFluentコントロールです。

- 画面構造は`NavigationView`、`Grid`、`StackPanel`、`ScrollViewer`など、WinUI 3のXAMLコントロールで組み立てる。
- 操作は`Button`、`ToggleButton`、`ToggleSwitch`、`CheckBox`、`RadioButton`、`ComboBox`、`TextBox`、`NumberBox`、`DatePicker`など、意味に合う標準コントロールを優先する。
- 状態・結果は`InfoBar`、`ProgressBar`、`ProgressRing`、`TeachingTip`、`ContentDialog`を使い分ける。独自のモーダル、トースト、入力部品を作る前に標準部品を検討する。
- `ContentDialog`はユーザーが判断を必要とする確認に限定し、単純な成功・失敗は`InfoBar`またはインラインフィードバックで示す。
- Fluent UI React、Fluent UI Web Components、HTML/CSSのコントロールライブラリをWinUI層へ追加しない。Web版の見た目をXAMLへコピーするのではなく、Windowsの操作モデルへ適応させる。

参照：[WinUI controls](https://learn.microsoft.com/en-us/windows/apps/design/controls/)、[NavigationView](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview)、[Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)。

## Typography

### フォント

- WindowsネイティブUIの既定フォントは`Segoe UI Variable`とする。XAMLの共通コントロールが提供する既定値を尊重し、必要な場合だけ明示する。
- 英語・欧州言語・ギリシャ語・ロシア語は`Segoe UI Variable`を基本とする。
- 日本語は`Yu Gothic UI`などWindowsの言語別system fallbackを使用する。アプリ独自の日本語フォントを無断で同梱しない。
- フォントファミリーを明示する場合も、言語別フォールバックを失わせない。フォントが特定文字を持たない場合に記号や日本語が豆腐にならないことを確認する。
- 既存プロトタイプのMona SansはWindowsネイティブUIの標準フォントではないため、移行時に原則として`Segoe UI Variable`へ戻す。ブランド上の理由で残す場合は、対象範囲・フォールバック・ライセンスを個別にレビューする。

参照：[Typography in Windows](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography)、[Adjusting layout and fonts to support globalization](https://learn.microsoft.com/en-us/windows/apps/design/globalizing/adjusting-layout-and-fonts-to-support-globalization)。

### Windows 11 type ramp

値はWindowsのeffective pixels（epx）で、`font-size / line-height`として扱います。本文はRegular、強調はSemiboldを基本とし、太字・斜体を新しい階層として増やさないでください。

| 用途 | Weight | Size / line height |
| --- | --- | --- |
| Caption | Regular | 12 / 16 |
| Body | Regular | 14 / 20 |
| Body strong | Semibold | 14 / 20 |
| Body large | Regular | 18 / 24 |
| Body large strong | Semibold | 18 / 24 |
| Subtitle | Semibold | 20 / 28 |
| Title | Semibold | 28 / 36 |
| Title large | Semibold | 40 / 52 |
| Display | Semibold | 68 / 92 |

`11px`の本文、バッジ、補助ラベルを新規に作らない。12px Regular未満、または14px未満の強調文字は、翻訳・高DPI・読みやすさの確認なしに使わない。文章はsentence caseを基本とし、TextBlockは必要に応じてwrapまたはellipsisを設定する。

## Icons

- UIアイコンはSegoe Fluent IconsまたはWinUI標準の`SymbolIcon`を使う。意味が標準Symbolにある場合は`SymbolIcon`を第一候補とする。
- Symbolにないアイコンを追加する場合は、`FontIcon`に`SymbolThemeFontFamily`を設定する方法を第二候補とする。
- `E0`–`E5`のlegacy/deprecated範囲を新規用途に流用せず、Microsoftが文書化したコードポイントだけを使用する。コードポイントの推測、手書きの別パス、絵文字、別アイコンライブラリを使わない。
- 既存のプロダクトロゴ、ユーザーがアップロードしたアバター、ブランド提供の画像は例外とする。ただしUI操作の意味を画像や絵文字で代替しない。
- アイコンのcrisp sizeは16、20、24、32、40、48、64 epxから選ぶ。24 epxを標準、16/20 epxを密度の高い補助UI、32 epx以上をナビゲーションや空状態などの主要視覚要素に使う。
- アイコンだけで操作を表す場合は`AutomationProperties.Name`またはAccessible nameを設定し、ツールチップは補助として使う。

参照：[Segoe Fluent Icons font](https://learn.microsoft.com/ja-jp/windows/apps/design/iconography/segoe-fluent-icons-font)、[Iconography](https://fluent2.microsoft.design/iconography)。

## Color、ThemeResource、material

- 色は`ThemeResource`とWinUIのテーマリソースを優先する。ライト・ダークの両方でコントラストと意味が保たれることを確認する。
- Fusion Ledgerのアクセントは既存のsoft redを使う。青いアクセント、彩度だけで状態を伝える色、背景のグラデーションは追加しない。
- 固定色をXAMLへ直書きするのは、ブランド色、状態色、またはテーマリソースにできない境界色に限定する。通常の背景・本文・境界・無効状態はThemeResourceを使う。
- Micaは、アプリの背後の壁紙を控えめに反映するWindows 11向けのベースレイヤーとして、ウィンドウまたはタイトルバーの主要背景に採用できる。コンテンツの可読性を優先し、Micaの上に不透明なカード面を置く。
- Acrylicは透過・ぼかしを伴うため、ポップアップ、メニュー、コンテキストフライアウトなど一時的なsurfaceに限定する。常時表示の主要コンテンツへAcrylicを使わない。
- MicaやAcrylicが利用できないWindows 10、高コントラスト、バッテリー節約時には不透明なテーマ面へフォールバックする。materialの有無で情報の意味を変えない。
- elevationは影の強さを競うために使わない。surfaceの階層は、ThemeResourceの面色、境界、余白、必要最小限のshadowで表す。

参照：[Color](https://learn.microsoft.com/en-us/windows/apps/design/style/color)、[Mica material](https://learn.microsoft.com/en-us/windows/apps/design/style/mica)、[Acrylic material](https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic)、[Theme resources](https://learn.microsoft.com/en-us/windows/apps/design/style/xaml-theme-resources)。

## Layout、spacing、corners、elevation

以下はFusion Ledgerの実装トークンです。Windowsの既定コントロールのサイズ・状態を上書きするための独自デザインシステムではありません。

- 基本spacingは4 epx単位とし、4、8、12、16、20、24、32、40、48を使用する。画面内の同じ関係には同じ値を繰り返す。
- ページの主要paddingは16または24、カード・セクション間は16または24、ラベルと入力の間は8、関連するボタン群の間は8を基本とする。
- 余白で階層を作り、罫線を増やしすぎない。Web版のカードをそのまま移植せず、WindowsのNavigationView・CommandBar・InfoBarと重複しないようにする。
- 角丸は標準コントロールの既定CornerRadiusを優先する。独自surfaceは4（小型入力・ボタン）、8（カード・パネル）、12（大きなコンテナ）を上限の目安とする。
- 強いドロップシャドウ、二重の境界線、過剰な透明面を同じ要素へ重ねない。
- 画面を狭めても操作部品の最小サイズとキーボード操作を犠牲にしない。テキストは切り捨てる前にwrap、レイアウトはstackまたはnavigation paneのcompact化で対応する。

参照：[Content layout and spacing](https://learn.microsoft.com/en-us/windows/apps/design/basics/content-basics)、[Geometry in Windows 11](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/geometry)、[CornerRadius API](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.cornerradius)。Fluent 2の数値は設計上の参考とし、WinUIコントロールの既定値を必要なく再定義しない。

## Windows shell patterns

### Title bar

- Windowsのcaption controls（最小化・最大化/復元・閉じる）はシステムに任せる。閉じる、最大化、ドラッグ領域を自作ボタンで置き換えない。
- 完全カスタムタイトルバーでは、`Window.ExtendsContentIntoTitleBar`と`Window.SetTitleBar`を使い、明示的にドラッグ可能な空き領域を確保する。
- タイトルバー内の操作部品は、システムボタンと衝突しない位置に置き、`InputNonClientPointerSource`やAppWindowのinsetを考慮する。
- タイトルバーの表示名はmanifestと一致する意味のある一行テキストとし、バージョンや状態を過密に並べない。
- Micaをタイトルバーへ拡張する場合も、caption controlsの可読性とlight/dark/high contrastを確認する。

参照：[Title bar customization](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar)、[Title bar design](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/title-bar)。

### Navigation

- 主要領域は`NavigationView`を第一候補とし、Dashboard、Projects、My commitsなどの情報構造を左ナビゲーションへ置く。
- 画面幅が狭いときはNavigationViewのcompact/overlay動作を使い、ナビゲーションが本文を恒久的に圧迫しないようにする。
- 選択状態、戻る操作、深い階層のbreadcrumbを一貫させる。WebView2内のWebナビゲーションとネイティブナビゲーションを混同させない。
- 主要な行動はページごとに一つのprimary commandへ絞り、ユーザーの権限外の操作を表示だけで有効にしない。

### Forms、dialogs、feedback

- 入力はlabel、説明、validation、error recoveryを同じ視線の流れに置く。placeholderをlabelの代わりにしない。
- 保存・予約・承認などの書き込み操作は、成功・失敗・保留を`InfoBar`またはインライン状態で明示し、二重送信を防ぐ。
- 破壊的操作は`ContentDialog`で対象と結果を説明し、primary/secondaryの選択肢を明確にする。取り消せる操作はUndoを優先する。
- 長い処理は`ProgressRing`または`ProgressBar`で進行中を示し、UIを無反応に見せない。処理が終わった後に結果を読み上げ可能な状態で通知する。
- 通知はユーザーの設定・権限・状態を尊重し、同じコミットや同じエラーを重複表示しない。

参照：[InfoBar](https://learn.microsoft.com/en-us/windows/apps/design/controls/infobar)、[ContentDialog](https://learn.microsoft.com/en-us/windows/apps/design/controls/dialogs)、[Progress controls](https://learn.microsoft.com/en-us/windows/apps/design/controls/progress-controls)。

## Accessibility、globalization、input

- ライト、ダーク、高コントラストの3状態で、文字、アイコン、境界、hover、pressed、disabled、focusを確認する。色だけで成功・失敗・選択を伝えない。
- すべての操作可能要素に意味のあるAccessible nameを付け、読み上げ順が画面の情報順と一致するようにする。`IsTabStop`を無効にする場合は、装飾要素か、別の操作経路があることを確認する。
- キーボードだけで、ナビゲーション、検索、入力、確定、キャンセル、ダイアログ閉鎖を完了できるようにする。focus visualを消さない。
- Narratorで、ページ名、見出し、入力label、validation、InfoBarの結果が理解できることを確認する。
- XAMLのeffective pixelを使い、DPIや画面倍率ごとに固定座標へ依存しない。100%、125%、150%、200%と、Windowsのtext scaleを上げた状態でクリップ・重なり・最小操作領域を確認する。
- 英語と日本語で同じ画面を確認し、翻訳で文字列が伸びてもボタン、タイトルバー、ナビゲーション、dialogが壊れないようにする。日本語のために英語のフォントサイズを下げない。
- `prefers-reduced-motion`ではなく、Windowsのユーザー設定とWinUIのcomposition/animation方針に従う。重要情報をアニメーションだけで伝えず、不要な連続アニメーション・parallax・自動再生を追加しない。
- タッチ、マウス、ペン、キーボードのいずれでも主要操作が完了できるようにする。hoverだけに依存しない。

参照：[Accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview)、[High contrast themes](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes)、[Keyboard interactions](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-interactions)、[Screen sizes and breakpoints](https://learn.microsoft.com/en-us/windows/apps/design/layout/screen-sizes-and-breakpoints-for-responsive-design)、[Motion](https://learn.microsoft.com/en-us/windows/apps/design/motion/)。

## Prohibited practices

- WebView2へCSS、JavaScript、DOM、script injectionで見た目や操作を注入する。
- WinUI 3アプリへFluent UI React、Fluent UI Web Components、別のWebコンポーネントライブラリを持ち込む。
- Web版のHTML/CSSをXAMLへ機械的にコピーし、Webのピクセル値・カード階層・レスポンシブブレークポイントをそのまま再現する。
- 11px本文、絵文字をUIアイコンにすること、手書きSVG path、未定義のSegoe Fluent Iconsコードポイント、アイコンの意味を説明しないtooltipに頼ること。
- 青いリンク色・グラデーション・固定された赤のRGB値を、新しい画面の既定スタイルとして増やす。
- `Thread.Sleep`、UIスレッドでの同期ネットワーク処理、進行状態のない長時間操作、失敗を無視するcatch、ユーザー権限境界をUI都合で緩めること。
- high contrast、DPI、text scale、キーボード、Narratorを確認せずに完了とすること。

## Current prototype migration checklist

現行プロトタイプからWindowsネイティブUIへ移行するときは、次を確認する。

- [ ] `Mona Sans`の明示指定を、原則`Segoe UI Variable`とWindowsの言語別system fallbackへ置換する。Mona Sansを残す場合は、ブランド上の理由、フォールバック、サイズ、OFLライセンスを記録する。
- [ ] Heroiconsの`Path`や`PathIcon`を、対応するWinUI `SymbolIcon`へ置換する。Symbolにない場合だけ`FontIcon` + `SymbolThemeFontFamily`を検討し、コードポイントを推測しない。
- [ ] 11pxのBETA・補助ラベルを、type rampの12/16 Captionまたは14/20 Bodyへ再設計する。
- [ ] `#C64F57`などのhard-coded redを、ライト・ダーク・high contrastを考慮したThemeResourceまたは意味名のsoft-red tokenへ置換する。
- [ ] `Window.SetTitleBar`、caption-button inset、ドラッグ領域、最大化・復元、DPI変更時のレイアウトをWindows 11のtitle bar guidanceで再確認する。
- [ ] WebView2は本番Web版の表示境界として維持し、CSS/JS注入やDOM依存のネイティブUI連携を追加しない。
- [ ] `/api/me`、logout、password、通知などの非同期結果がlatest-winsで適用され、ログアウト後に古いprofileや通知が復元されないことをテストする。
- [ ] 100/125/150/200% DPI、text scale拡大、英語・日本語、light/dark/high contrast、キーボード、Narratorでスモークテストする。

## Review checklist

### Visual and interaction

- [ ] WinUI 3 native controlを使用し、Web向けFluent UIを追加していない。
- [ ] Segoe UI Variable、言語別fallback、12/16以上の最小サイズ、type rampのline-heightを確認した。
- [ ] アイコンはSymbolIcon優先、Segoe Fluent Iconsの既知の割り当て、16/20/24/32/40/48/64 epxのいずれかである。
- [ ] ThemeResource、ライト/ダーク/high contrast、Mica/Acrylicのfallbackを確認した。
- [ ] spacing、corner、surface、elevationが既存tokenと整合し、不要なshadow・gradient・固定色がない。
- [ ] title bar、NavigationView、forms、dialogs、feedbackのWindows patternと一致している。

### Accessibility and resilience

- [ ] キーボード、focus visual、Narrator、DPI、text scale、タッチ/マウス/ペンを確認した。
- [ ] 英語・日本語で文字列の伸長、fallback、clip/wrap、RTLを想定した。
- [ ] reduced-motion設定で不要なアニメーションを抑制し、情報を静的にも理解できる。
- [ ] WebView2のnavigation policy、認証cookie、Cloudflare APIの権限境界を変更していない。
- [ ] 非同期レスポンスの世代・キャンセル・重複通知をテストし、古い結果が新しい状態を上書きしない。
- [ ] ビルド、policy test、MSIX内資産確認、実機またはVMでの起動確認を記録した。
