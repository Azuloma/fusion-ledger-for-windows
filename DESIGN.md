# Fusion Ledger for Windows native-first design

## Scope

v0.5.0 is a native WinUI 3 shell and login-only prototype. The web application is used solely as the authentication surface in LoginWindow. MainWindow is not a browser wrapper and contains no WebView2.

## Window and title bar

MainWindow extends content into the native title bar and calls `SetTitleBar(AppTitleBar)` with the Windows App SDK `Microsoft.UI.Xaml.Controls.TitleBar` control. The control owns caption buttons, drag behavior, snap, and Alt+Space. It displays Fusion Ledger/Beta, a local page AutoSuggestBox, notifications, and a non-interactive PersonPicture account affordance. The TitleBar back button is only visible for nested native pages.

The TitleBar is 48px high with centered content. The local search box is 32px high and remains visible at every width; its width is applied from the actual client width on load and resize: 96px below 600px, 200px at 600–899px, 320px at 900–1199px, 420px at 1200–1599px, and 540px at 1600px or wider. The right header uses centered 32px transparent circular buttons with standard WinUI focus/hover states; caption-button spacing and insets remain owned by the TitleBar template. Root-level shortcut behavior remains available while placement hints are hidden; Ctrl+K belongs to the search control.

The account flyout is a 320px-wide, 440px-maximum-height standard Flyout. Its identity block contains only the authenticated username, localized status/role and validated avatar, followed by a separator and full-width 44px-minimum rows for profile settings, role-gated administration and sign out. Icons are Segoe Fluent controls and decorative icon access is hidden from automation.

App settings is a single-column, scrollable native page. It persists the fixed `ui.language` and `ui.theme` values in LocalSettings, validates values on read and falls back to `en-US`/`System`. Theme changes update both native roots immediately; System resolves from `UISettings.GetColorValue(UIColorType.Background)` while preserving High Contrast resources and follows `ColorValuesChanged` when available. Language changes show a localized restart notice. Version info reads assembly/package identity and reports the Windows App SDK assembly and installed WebView2 runtime versions when available; unavailable values are not fabricated.

The supported AppWindow title bar is configured with `PreferredHeightOption=Tall` when customization is available. XAML retains its 48 DIP TitleBar height; physical AppWindow pixels are never assigned directly to XAML. Windows caption buttons remain system-owned; no manual caption-button reimplementation or inset arithmetic is used.

## Native navigation

NavigationView uses `PaneDisplayMode=Auto`, hides its built-in back/settings affordances, and exposes Dashboard, Projects and Commit history plus footer entries for server maintenance, app settings and version information. Maintenance and administration are available only for a parsed `admin` role. Dashboard, Projects, Commit history, maintenance, profile and administration are honest placeholders with a heading and a not-connected follow-up message; settings and version information are functional native pages. No fabricated metrics, user records or non-functional controls are allowed.

## Login boundary

LoginWindow owns the persistent WebView2 profile and navigates to the production origin. It observes only exact GET `/api/me` responses, with bounded body/avatar parsing and latest-response-wins invalidation. It denies permissions, disables DevTools, host objects and WebMessage, suppresses unsafe popups, opens external links in Windows, and never injects script/CSS or reads cookies/headers. Successful authentication creates an immutable `AuthenticatedUser` snapshot for MainWindow. Closing LoginWindow before authentication exits the app.

Sign out creates and activates a replacement LoginWindow first. That window uses the live WebView2 profile's `CookieManager.DeleteAllCookies()` and `Profile.ClearBrowsingDataAsync(AllSite)` APIs, then navigates to the production root only after both operations succeed. MainWindow closes only after the replacement login window is visible and the clear has completed, so there is no zero-window gap. A clear failure shows localized retry/close controls and cannot navigate or auto-login with stale data. It intentionally does not call a server logout endpoint; server-side revocation is a later phase.

## Visual system and accessibility

Use WinUI ThemeResources with neutral surfaces and the soft-red Fusion accent from App.xaml. Mica is the preferred backdrop with platform fallback. Use Segoe system typography and Segoe Fluent SymbolIcon glyphs only. Light, dark and high-contrast modes, keyboard navigation, localized automation names, tooltips and English/Japanese resource files are required. Responsive WinUI adaptive behavior must be preferred over manual title-bar inset arithmetic.

## Future phases

The next phase may connect native pages to the existing Cloudflare-backed APIs and add tray/background commit notifications. Those additions must preserve the login WebView security boundary and the server authorization rules.
