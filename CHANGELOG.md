# Changelog

## v0.7.1

- The data bridge retries hidden controller creation (up to three attempts with backoff), because the sign-in WebView may still be shutting down the shared browser process right after sign-in.
- Version info shows a diagnostic next to an unavailable bridge: the failing stage with an HRESULT, WebView2 error status or session HTTP status. It never contains page data.

## v0.7.0

- Added the MolHub web data bridge host (`WebBridgeClient`). A hidden WebView2 controller in its own invisible window loads only `https://<origin>/webview-bridge` (the canonical path; `/webview-bridge.html` redirects there), enables WebMessage only there, accepts messages only from that exact document, sends only allow-listed commands and correlates results by `requestId`. Host timeouts (20 s reads, 40 s writes) back up the page's own 15 s / 30 s aborts; writes that time out or lose the bridge are reported with outcome Unknown and are never resent.
- The sign-in window and the bridge share one WebView2 environment (`WebViewProfile`), so the browser engine shares the HttpOnly session cookie; the host never reads it. The sign-in WebView keeps WebMessage disabled.
- Sign out now calls the bridge `logout` operation to revoke the server session before clearing local WebView2 data (local clearing still happens if the bridge is unavailable).
- Version info shows the web data connection state (connecting, connected, server session ended, unavailable). Data pages remain placeholders.

## v0.6.4

- Title-bar flyouts now use thin desktop acrylic so the screen behind visibly shows through. The windowed flyout popup never becomes the active window, so its backdrop is configured as active (following the app theme and High Contrast) instead of drawing the inactive solid fallback.
- Title-bar flyouts open above their button when the monitor work area below is too short, so they no longer slide under the taskbar.

## v0.6.2

- Restyled the account and notification flyouts after the Windows account menu: 320 px account flyout (48 px avatar, name, status and role, compact sign-out link, separator, 36 px icon rows) and a slightly wider 360 px notifications flyout with the same header/separator layout. Both use a transparent presenter over a desktop acrylic backdrop, 8 px corners, may extend beyond the main window near screen edges and scroll instead of clipping.
- The main window now opens at 1464 x 934.

## v0.6.1

- Made the sign-in window a compact dialog: a 481 x 683 DIP client area (32 DIP title bar with only a close button, no title text or icon) scaled for the monitor's DPI, clamped to its work area and centered. The window can no longer be resized, maximized or minimized.
- The web sign-in surface now fills the window. A centered progress ring with "Please wait…" covers it until the first page load completes; sign-out and recovery states use the same overlay.

## v0.6.0

- Renamed the user-facing product from Fusion Ledger for Windows to MolHub for Windows in the package display names, window and title-bar text, accessibility names and installer messages. The package identity, namespace and WebView2 profile folder are unchanged so upgrades keep sign-in data and settings.
- Replaced the app icon with the MolHub mole artwork: scaled Start/taskbar/tile/store logos, `targetsize` unplated variants, dark-line `lightunplated` variants for light taskbars, and theme-aware title-bar and window icons.

## v0.5.2

- Fixed the post-login access violation by replacing `Frame.Navigate` with `ContentControl` content updates and `EntranceThemeTransition` animations.

## v0.5.1

- Moved the standard NavigationView toggle onto the Dashboard and set the expanded pane width to 240 DIP.
- Added `EntranceNavigationTransitionInfo` animations for section changes.
- Deferred initial page navigation until the shell loaded and guarded against reentrant selection updates; the post-login access violation remained unresolved.

## v0.5.0

- Added native App settings with persisted English/Japanese language and System/Light/Dark theme choices.
- Added runtime Version info and configured the supported AppWindow title bar for the Tall system caption-button layout.

## v0.4.1

- Refined the native TitleBar alignment and responsive search sizing.
- Kept page search visible at narrow widths with staged 96/180/280px states and moved Ctrl+K ownership to the search box.
- Reduced the notification glyph and changed only its hover hit surface to a rounded rectangle while retaining the circular profile action.
- Replaced adaptive search VisualStates with client-size-driven 96/200/320/420/540px sizing and hid root accelerator placement hints without removing shortcuts.
- Added transparent circular notification/account actions with standard focus and hover behavior.
- Restructured the account flyout into an identity block, separator and accessible menu rows.
- Localized account-menu automation names and retained role-based administration visibility.

## v0.4.0

- Replaced the browser-wrapper MainWindow with a native WinUI 3 shell.
- Added the Windows App SDK TitleBar control, native NavigationView, local page search, account and notification flyouts, and honest disconnected placeholders.
- Added LoginWindow as the only WebView2 host with persistent profile authentication and strict `/api/me` parsing.
- Added latest-response-wins authentication coordination and local-only sign-out behavior.
- Sign-out now keeps a replacement LoginWindow alive, clears the active WebView2 profile through its supported APIs, and blocks navigation on clear failure.
- Added English/Japanese resources and updated build, security and unsigned-package documentation.

Known limitation: sign out clears local WebView2 data when possible but does not revoke the server-side session; commit notifications and tray/background operation remain future work.
