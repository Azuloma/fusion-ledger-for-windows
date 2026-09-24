# Fusion Ledger for Windows (WinUI 3 prototype)

Fusion Ledger for Windows v0.3.0 is a packaged WinUI 3 application shell. It displays the production Fusion Ledger site in WebView2 and keeps authentication cookies in a persistent WebView2 user-data profile. The web application remains responsible for authentication, data and Cloudflare services.

The app uses the standard WinUI 3 packaged desktop/full-trust entry point. The XAML compiler generates the `[STAThread]` `Main` method, and the source manifest uses the Visual Studio template tokens; MSBuild resolves them to the packaged full-trust entry point.

The 48px custom title bar uses `Window.ExtendsContentIntoTitleBar` and `Window.SetTitleBar`, leaving the Windows-provided caption buttons intact. The installed Microsoft.WindowsAppSDK 2.5.1 package resolves WinUI 2.3.9, which predates the newer `Microsoft.UI.Xaml.Controls.TitleBar` control, so the equivalent native title-bar root is used. It contains a 20px Segoe Fluent `GlobalNavigationButton`, app icon, product name, soft-red BETA badge, a stretch drag area, and a non-interactive profile `PersonPicture` immediately before the caption buttons. The menu contains version information only; its version is read from the assembly version. Alt+I opens the menu; F1 is not used.

The browser command/status row is a fixed 44px Grid surface containing standard WinUI `AppBarButton` controls. This preserves exact 40px targets, a 16px status icon and a responsive status region that a standard `CommandBar` could not guarantee without moving the status content into overflow. Alt+Left/Right, Ctrl+R and F5 are supported. A reserved 2px progress row keeps the WebView layout stable. Failures use a standard `InfoBar` with a manual Retry action that can navigate only to the configured production URL.

## Build requirements

- .NET SDK 10.0.401
- Visual Studio 2026 MSBuild 18.10
- Windows SDK 10.0.28000.0
- Microsoft.WindowsAppSDK 2.5.1
- WebView2 1.0.3719.77 runtime

## Build and test

```powershell
dotnet restore .\FusionLedger.Windows.csproj --locked-mode
dotnet build .\FusionLedger.Windows.csproj -c Debug -p:Platform=x64
dotnet run --project .\tests\FusionLedger.Windows.PolicyTests\FusionLedger.Windows.PolicyTests.csproj
```

The packaged prototype uses the standard Windows caption buttons. The MSIX output is intentionally unsigned for local development; no private key or certificate is stored in this repository. Its manifest publisher uses the Windows Developer Mode unsigned-package namespace (`CN=Fusion Ledger, OID.2.25.311729368913984317654407730594956997722=1`) so `Add-AppxPackage -AllowUnsigned` can deploy it locally. This publisher value is for the prototype only; a production signed package must remove the OID namespace suffix and use a publisher subject that exactly matches the trusted signing certificate.

## Installing the unsigned prototype

Enable Windows Developer Mode first: **Settings > System > For developers > Developer Mode**. Then run PowerShell from this repository root:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\Install-Prototype.ps1
```

The script accepts only a generated direct child matching `AppPackages\FusionLedger.Windows_*_x64_Debug_Test`, resolves it and its x64 dependency packages under the repository, and calls `Add-AppxPackage -AllowUnsigned`. It performs no cleanup or removal. `-WhatIf` validates the package and dependency paths without installing:

```powershell
.\Install-Prototype.ps1 -WhatIf
```

The generated `Add-AppDevPackage.ps1` is not the installation path for this prototype: it assumes a developer certificate/signing flow and is not suitable for this intentionally unsigned package. Double-click installation is not supported. This workflow is limited to Developer Mode on a development machine. Production distribution requires a trusted code-signing certificate and a signed MSIX; an unsigned package should not be distributed to end users.

## WebView2 security boundary

- Only `https://fusion-ledger.desase0175.workers.dev` is allowed as a top-level origin.
- Other `http`, `https`, and `mailto` links are handed to the Windows default app.
- `NewWindowRequested` is handled explicitly; no popup window is created.
- WebView2 permission requests are denied by default.
- Certificate errors are not overridden.
- DevTools, host objects and WebMessage communication are disabled. No arbitrary host object or bridge is exposed.
- Cookie state is stored under the packaged app's local application data in `FusionLedger.WebView2`.

The native profile indicator listens only to WebView2's response event for an exact `GET https://fusion-ledger.desase0175.workers.dev/api/me` request. It never reads request headers or cookies, never logs response bodies, and caps the response before JSON parsing. Only `user.username` and a `data:image/png;base64,` avatar decoded to at most 32 KB are accepted. External image URLs and SVG are rejected. A `401`, `{ "user": null }`, or successful `/api/logout` or `/api/password` response clears the indicator. `ProfileResponseCoordinator` assigns a monotonically increasing generation at the start of every accepted `/api/me` response and applies data only when that generation is still current, so overlapping profile requests and session invalidation are latest-wins. No WebMessage, host object, or script bridge is exposed.

Native UI uses the WinUI default Segoe UI Variable/system fallback. English and Japanese native strings are packaged in `Strings/en-US/Resources.resw` and `Strings/ja-JP/Resources.resw`; the WebView2 content keeps its own existing language and theme behavior. The window uses `MicaBackdrop` with the standard platform fallback, while the WebView surface remains opaque.

## Scope

Tray integration and commit notifications are intentionally not implemented in v0.3.0. The shell is structured so those capabilities can be added as separate services later without changing the navigation policy or exposing a renderer bridge.

The provided Fusion Ledger icon is used by the package manifest. The WebView2 page is not modified with injected CSS or JavaScript.
