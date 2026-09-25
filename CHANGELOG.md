# Changelog

## v0.5.1

- Fixed a crash after successful login by deferring initial page navigation until the shell loads and preventing reentrant selection changes.
- Moved the standard NavigationView toggle onto the Dashboard, set its expanded width to 240 DIP, and added EntranceNavigationTransitionInfo animations for section changes.

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
