# Fusion Ledger for Windows v0.5.0

This is a native-first WinUI 3 prototype. The app starts with a dedicated `LoginWindow` containing the WebView2 sign-in surface. After a verified production `/api/me` response, that window closes and the authenticated native `MainWindow` opens. The MainWindow is a native shell with functional settings/version pages; it does not embed the web application.

## Architecture

- WinUI 3 packaged x64 desktop app, Microsoft.WindowsAppSDK 2.5.1.
- MainWindow uses the actual `Microsoft.UI.Xaml.Controls.TitleBar`, Mica, NavigationView, native placeholders, account and notification flyouts.
- App settings persist only the fixed `ui.language` and `ui.theme` keys in `ApplicationData.Current.LocalSettings`. Invalid stored values fall back to English/System. Theme changes apply immediately; language changes require a restart for the full resource tree to refresh.
- Version info is assembled at runtime from assembly/package identity and reports process architecture, the Windows App SDK assembly version, the installed WebView2 runtime version when available, and the sign-in-only role. Unavailable runtime values are shown as unavailable.
- LoginWindow is the only window that creates WebView2. Its persistent profile is `ApplicationData.Current.LocalFolder.Path\FusionLedger.WebView2` (normally `%LOCALAPPDATA%\Packages\FusionLedger.Windows_*\LocalState\FusionLedger.WebView2` for a packaged install).
- Only an exact HTTPS GET to the production `/api/me` is observed. Request headers, cookies, response headers and secrets are never read or logged.
- The parser requires bounded JSON, a username, known status values (`pending`, `approved`, `rejected`, `suspended`), role values (`member`, `admin`) and optionally a bounded PNG data URI.

## Security boundary

The login WebView2 allows only `https://fusion-ledger.desase0175.workers.dev` as an in-app origin. External HTTP(S) and `mailto:` links are opened by Windows; popups are controlled, permissions are denied, DevTools/host objects/WebMessage are disabled, and certificate errors are not bypassed. No script, CSS, DOM or WebMessage bridge is injected or exposed.

## Build and tests

```powershell
dotnet restore
dotnet run --project tests\FusionLedger.Windows.PolicyTests\FusionLedger.Windows.PolicyTests.csproj
dotnet build FusionLedger.Windows.csproj -p:Platform=x64 -p:Configuration=Debug
.
Install-Prototype.ps1 -WhatIf
```

The build creates an unsigned MSIX under `AppPackages`. Do not install it in production. `Install-Prototype.ps1` is a developer-mode, `-AllowUnsigned` flow and must be run from PowerShell; double-click installation is not supported. A real distribution requires a trusted signing certificate and an appropriately signed package. Unsigned packages can trigger Windows SmartScreen warnings.

## Current limitations

Dashboard, projects, commit history, server maintenance, profile settings and administration are intentionally placeholders with no fake counts, cards, users or actions. App settings persist language/theme choices, and Version info reports observed runtime details. Notifications show an honest not-connected preview; commit notifications are not implemented yet. Sign out opens the replacement LoginWindow first, clears cookies and site data through the active WebView2 profile, and only then closes MainWindow and navigates to login. A clear failure stays in a localized retry/close state and cannot auto-login with stale data. Sign out does not revoke the server-side session because no logout API call is made in this prototype. System tray/background notifications are also not part of v0.5.0.

See [VERSION.md](VERSION.md), [CHANGELOG.md](CHANGELOG.md), and [DESIGN.md](DESIGN.md) for the design contract.
