# MolHub for Windows

A native-first Windows client for MolHub (formerly Fusion Ledger), built with WinUI 3. Current version: **v0.6.4** (beta prototype).

The app opens a sign-in window that hosts the existing MolHub web login in WebView2. Once the production `/api/me` response confirms the signed-in user, the sign-in window closes and a native main window opens. The main window is a WinUI shell, not a wrapper around the web application.

## Status

| Area | State |
| --- | --- |
| Sign-in (WebView2) | Working. A saved session signs in automatically on the next launch. |
| Native shell | Title bar, page search, navigation pane, account and notification flyouts. |
| App settings | Working. Language (English / Japanese) and theme (System / Light / Dark). |
| Version info | Working. Reports the app, Windows App SDK, WebView2 runtime and package details. |
| Dashboard, Projects, Commit history | Placeholders. They are not connected to data yet. |
| Server maintenance, Administration, Profile settings | Placeholders. Maintenance and administration appear only for approved admins. |
| Notifications | Not connected. The flyout says so; no notifications are generated. |

Placeholder pages show a heading and a "not connected" message only. They never show sample counts, records or controls that do nothing.

## Requirements

- Windows 10 version 2004 (build 19041) or later, x64
- .NET 10 SDK
- Microsoft Edge WebView2 Runtime
- Developer Mode, to install the unsigned development package

## Build and test

Run these from the repository root:

```powershell
dotnet restore
dotnet run --project tests\FusionLedger.Windows.PolicyTests\FusionLedger.Windows.PolicyTests.csproj
dotnet build FusionLedger.Windows.csproj -p:Platform=x64 -p:Configuration=Debug
```

- The policy tests check navigation and sign-in rules, settings validation, `/api/me` parsing, and required structure in the source, XAML, version files and resources. A passing run prints a single summary line.
- The build creates an unsigned MSIX in `AppPackages\FusionLedger.Windows_<version>_x64_Debug_Test\`.

## Install a development build

Use Windows PowerShell 5.1 or later. First enable **Settings > System > For developers > Developer Mode**. Then check and install the package you built:

```powershell
.\Install-Prototype.ps1 -WhatIf -PackageDirectory ".\AppPackages\FusionLedger.Windows_0.6.4.0_x64_Debug_Test"
.\Install-Prototype.ps1 -PackageDirectory ".\AppPackages\FusionLedger.Windows_0.6.4.0_x64_Debug_Test"
```

- `-WhatIf` only checks that the package and its x64 dependency packages are present; nothing is installed.
- `-PackageDirectory` can be omitted only when `AppPackages` contains exactly one `*_x64_Debug_Test` directory.
- The script installs for the current user with `Add-AppxPackage -AllowUnsigned`, including the packages in `Dependencies\x64`. It does not need administrator rights or a certificate.
- If the execution policy blocks the script, allow it for the current PowerShell session only: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`.

This package is unsigned and meant for development only. Windows SmartScreen may warn about it. Signed distribution is not set up yet.

## How it works

- **Packaging:** packaged WinUI 3 desktop app for x64, using Microsoft.WindowsAppSDK 2.5.1 and Microsoft.Web.WebView2.
- **Sign-in window:** a compact, fixed-size dialog with a 481 × 683 DIP client area (32 DIP title bar with only a close button). It scales with the monitor's DPI, stays inside the work area and opens centered; a progress ring covers the web view until the first page load.
- **Windows:** `LoginWindow` is the only window that creates WebView2. `MainWindow` is fully native and changes pages by replacing the content of `ContentFrame`.
- **Sign-in data:** the WebView2 profile is stored at `ApplicationData.Current.LocalFolder\FusionLedger.WebView2`. For an installed package this is normally `%LOCALAPPDATA%\Packages\FusionLedger.Windows_*\LocalState\FusionLedger.WebView2`.
- **User details:** the app reads only an exact HTTPS `GET /api/me` response. It accepts bounded JSON with a username, a known status (`pending`, `approved`, `rejected`, `suspended`) and role (`member`, `admin`), plus an optional small PNG avatar.
- **Settings:** only the `ui.language` and `ui.theme` keys are stored in `LocalSettings`. Invalid values fall back to English and System. Theme changes apply at once; a language change applies after restarting the app.
- **Name and icon:** the user-facing name is MolHub for Windows. Internal identifiers (the `FusionLedger.Windows` package identity, namespace, WebView2 profile folder and production host name) are unchanged so upgrades keep the sign-in profile and settings. The icon is a white line drawing; light surfaces (light taskbar and Start, the Light app theme) use a dark-line variant generated from the same artwork.
- **Accessibility:** English and Japanese resources, Light, Dark and High Contrast themes, keyboard shortcuts (Ctrl+K search, Ctrl+, settings, Alt+N notifications, Alt+Left back) and localized accessibility names.

## Security boundary

- The sign-in view allows only `https://fusion-ledger.desase0175.workers.dev` inside the app. Other HTTP(S) and `mailto:` links open in the default Windows app.
- Pop-ups are controlled and permission requests are denied. DevTools, host objects and WebMessage are disabled. Certificate errors are never bypassed.
- The app injects no scripts, CSS or message bridge. It never reads or logs cookies, request headers or other secrets.

## Known limitations

- Sign out clears the WebView2 cookies and site data before showing the sign-in page, but it does not end the session on the server. No logout API is called yet.
- The data pages are not connected, and there are no notifications, tray icon or background activity.

## Versioning

The `<Version>` element in `FusionLedger.Windows.csproj` defines the app version. `Package.appxmanifest` uses the matching four-part package version (for example `0.6.4.0`). See [VERSION.md](VERSION.md) for the policy and [CHANGELOG.md](CHANGELOG.md) for release notes.
