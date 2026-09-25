# Fusion Ledger for Windows native-first design

## Scope

v0.4.0 is a native WinUI 3 shell and login-only prototype. The web application is used solely as the authentication surface in LoginWindow. MainWindow is not a browser wrapper and contains no WebView2.

## Window and title bar

MainWindow extends content into the native title bar and calls `SetTitleBar(AppTitleBar)` with the Windows App SDK `Microsoft.UI.Xaml.Controls.TitleBar` control. The control owns caption buttons, drag behavior, snap, and Alt+Space. It displays Fusion Ledger/Beta, a local page AutoSuggestBox, notifications, and a non-interactive PersonPicture account affordance. The TitleBar back button is only visible for nested native pages.

## Native navigation

NavigationView uses `PaneDisplayMode=Auto`, hides its built-in back/settings affordances, and exposes Dashboard, Projects and Commit history plus footer entries for server maintenance, app settings and version information. Maintenance and administration are available only for a parsed `admin` role. All pages are honest placeholders with a heading and a not-connected follow-up message; no fabricated metrics, user records or non-functional controls are allowed.

## Login boundary

LoginWindow owns the persistent WebView2 profile and navigates to the production origin. It observes only exact GET `/api/me` responses, with bounded body/avatar parsing and latest-response-wins invalidation. It denies permissions, disables DevTools, host objects and WebMessage, suppresses unsafe popups, opens external links in Windows, and never injects script/CSS or reads cookies/headers. Successful authentication creates an immutable `AuthenticatedUser` snapshot for MainWindow. Closing LoginWindow before authentication exits the app.

Sign out creates and activates a replacement LoginWindow first. That window uses the live WebView2 profile's `CookieManager.DeleteAllCookies()` and `Profile.ClearBrowsingDataAsync(AllSite)` APIs, then navigates to the production root only after both operations succeed. MainWindow closes only after the replacement login window is visible and the clear has completed, so there is no zero-window gap. A clear failure shows localized retry/close controls and cannot navigate or auto-login with stale data. It intentionally does not call a server logout endpoint; server-side revocation is a later phase.

## Visual system and accessibility

Use WinUI ThemeResources with neutral surfaces and the soft-red Fusion accent from App.xaml. Mica is the preferred backdrop with platform fallback. Use Segoe system typography and Segoe Fluent SymbolIcon glyphs only. Light, dark and high-contrast modes, keyboard navigation, localized automation names, tooltips and English/Japanese resource files are required. Responsive WinUI adaptive behavior must be preferred over manual title-bar inset arithmetic.

## Future phases

The next phase may connect native pages to the existing Cloudflare-backed APIs and add tray/background commit notifications. Those additions must preserve the login WebView security boundary and the server authorization rules.
