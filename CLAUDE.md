# Fusion Ledger for Windows — Claude Code notes

Native-first WinUI 3 prototype (packaged x64, .NET `net10.0-windows10.0.19041.0`, Microsoft.WindowsAppSDK 2.5.1, Microsoft.Web.WebView2 1.0.3719.77). Read `README.md`, `DESIGN.md`, `VERSION.md` and `CHANGELOG.md` before changing behavior; `DESIGN.md` is the design contract.

## Layout

- `App.xaml.cs` — window lifecycle: `LoginWindow` first, `MainWindow` after authentication, sign-out sequencing.
- `LoginWindow.*` — the only WebView2 host. `NavigationPolicy.cs` pins the production origin; `ProfilePayloadParser.cs` and `ProfileResponseCoordinator.cs` parse and order `/api/me` responses.
- `MainWindow.*` — native shell (WinUI `TitleBar`, NavigationView, search, flyouts). Pages are built in code and assigned to the `ContentFrame` `ContentControl`. `NativePage.cs` lists pages and role gating.
- `SettingsPolicy.cs`, `ThemeService.cs`, `TitleBarLayoutPolicy.cs`, `RuntimeVersionInfo.cs` — settings, theme, search sizing, version info.
- `Strings/en-US` and `Strings/ja-JP` `Resources.resw` — all user-facing strings.
- `tests/FusionLedger.Windows.PolicyTests` — console assertion tests. They link the pure policy `.cs` files and also inspect source/XAML/docs text.

## Build and verify

```powershell
dotnet restore
dotnet run --project tests\FusionLedger.Windows.PolicyTests\FusionLedger.Windows.PolicyTests.csproj
dotnet build FusionLedger.Windows.csproj -p:Platform=x64 -p:Configuration=Debug
.\Install-Prototype.ps1 -WhatIf -PackageDirectory ".\AppPackages\FusionLedger.Windows_<version>_x64_Debug_Test"
```

- The build emits an unsigned MSIX under `AppPackages\` (git-ignored). Pass `-PackageDirectory` when more than one `*_x64_Debug_Test` directory exists; the script refuses to guess.
- Actual installation (`Install-Prototype.ps1` without `-WhatIf`) requires Developer Mode and changes the user's installed app — ask first.
- The policy tests are string-based: renaming the checked identifiers, XAML attributes or doc headings breaks them. Update the assertion together with an intentional design change, never to silence a regression.
- Report tests, build and real-device behavior (login → MainWindow, time-based stability) separately; a passing build is not proof of runtime behavior.

## Rules to preserve

- **MainWindow never hosts WebView2.** Only `LoginWindow` may. Do not use `Frame.Navigate` for section changes; v0.5.0/v0.5.1 crashed with an access violation inside it, fixed in v0.5.2 by setting `ContentFrame.Content`.
- **Login boundary:** allow only the HTTPS production origin in-app; open other http(s)/mailto externally; deny permissions; keep DevTools, host objects and WebMessage disabled; observe only exact GET `/api/me` with bounded parsing. Never inject script/CSS, read/log cookies or headers, or persist secrets.
- **Sign-out order:** create the replacement `LoginWindow`, clear cookies and site data through the WebView2 profile APIs, navigate only after the clear succeeds, then close `MainWindow`. Never delete the profile directory directly. Sign-out does not currently revoke the server session.
- **No fake data:** Dashboard, Projects, Commit history, Server maintenance, Profile settings and Administration are honest "not connected" placeholders. Do not add fabricated counts, records, notifications or non-functional controls.
- Administration/maintenance appear only for an approved `admin` user (`NativePageCatalog.CanOpenMaintenance`); hiding UI is not an authorization boundary.
- Settings persist only the fixed `ui.language` / `ui.theme` keys with validated fallbacks (`en-US`, `System`).
- UI: WinUI ThemeResources, Mica with fallback, Segoe typography and Segoe Fluent glyphs; support Light/Dark/High Contrast, keyboard access, localized automation names, English default plus Japanese. Put new strings in both `.resw` files. Let `TitleBar` own caption buttons and insets.

## Versioning

- `<Version>` in `FusionLedger.Windows.csproj` is the display-version source of truth; `Package.appxmanifest` carries the four-part `X.Y.Z.0`. Do not add another version constant.
- A release updates the csproj, manifest, `VERSION.md`, `CHANGELOG.md` and `README.md`, and the version assertions in the policy tests.
- Recent commit messages follow `vX.Y.Z, summary`.
