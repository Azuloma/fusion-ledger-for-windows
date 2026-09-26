using System.Text;
using FusionLedger.Windows;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

Assert(NavigationPolicy.IsAllowed(new Uri(NavigationPolicy.AppUrl)), "Production URL must be allowed.");
Assert(NavigationPolicy.IsAllowed(new Uri("https://fusion-ledger.desase0175.workers.dev/projects")), "Production paths must be allowed.");
Assert(!NavigationPolicy.IsAllowed(new Uri("http://fusion-ledger.desase0175.workers.dev/")), "HTTP must be rejected.");
Assert(!NavigationPolicy.IsAllowed(new Uri("https://example.com/")), "Other origins must be rejected.");
Assert(NavigationPolicy.IsExternalLaunchable(new Uri("https://example.com/")), "External HTTPS must be launchable.");
Assert(NavigationPolicy.IsExternalLaunchable(new Uri("mailto:help@example.com")), "Mail links must be launchable.");
Assert(!NavigationPolicy.IsExternalLaunchable(new Uri(NavigationPolicy.AppUrl)), "Internal URLs must not launch externally.");
Assert(SettingsPolicy.LanguageKey == "ui.language" && SettingsPolicy.ThemeKey == "ui.theme", "Settings schema keys must remain fixed.");
Assert(SettingsPolicy.NormalizeLanguage("ja-JP") == "ja-JP" && SettingsPolicy.NormalizeLanguage("fr-FR") == "en-US", "Language values must be validated with an English fallback.");
Assert(SettingsPolicy.NormalizeTheme("Dark") == "Dark" && SettingsPolicy.NormalizeTheme("invalid") == "System", "Theme values must be validated with a System fallback.");
Assert(ThemePolicy.IsLightColor(255, 255, 255) && !ThemePolicy.IsLightColor(0, 0, 0), "System theme color classification must be deterministic.");
Assert(ThemePolicy.ResolveIsLight("System", true) && !ThemePolicy.ResolveIsLight("System", false)
    && ThemePolicy.ResolveIsLight("Light", false) && !ThemePolicy.ResolveIsLight("Dark", true), "System/Light/Dark theme resolution must respect OS lightness and explicit choices.");

Assert(ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me"), "GET"), "Exact /api/me GET must be accepted.");
Assert(!ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me?x=1"), "GET"), "Query /api/me must be rejected.");
Assert(!ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me"), "POST"), "Only GET /api/me is observed.");

var signedPng = "iVBORw0KGgo=";
var json = Encoding.UTF8.GetBytes($"{{\"user\":{{\"username\":\"  azunel  \",\"status\":\"approved\",\"role\":\"admin\",\"project_manager\":true,\"avatar\":\"data:image/png;base64,{signedPng}\"}}}}");
var parsed = ProfilePayloadParser.ParseAuthenticatedUser(200, json);
Assert(parsed?.Username == "azunel" && parsed.Status == "approved" && parsed.Role == "admin" && parsed.ProjectManager, "Strict user fields must parse.");
Assert(parsed?.AvatarPng?.Length == 8, "Only bounded PNG bytes must be retained.");
Assert(ProfilePayloadParser.ParseAuthenticatedUser(401, Encoding.UTF8.GetBytes("not-json")) is null, "401 must not parse a body.");
Assert(ProfilePayloadParser.ParseAuthenticatedUser(200, Encoding.UTF8.GetBytes("{\"user\":null}")) is null, "Null user cannot authenticate.");
Assert(ProfilePayloadParser.ParseAuthenticatedUser(200, Encoding.UTF8.GetBytes("{\"user\":{\"username\":\"x\",\"status\":\"approved\",\"role\":\"owner\"}}")) is null, "Unknown roles must be rejected.");
Assert(ProfilePayloadParser.ParseAuthenticatedUser(200, Encoding.UTF8.GetBytes("{\"user\":{\"username\":\"x\",\"status\":\"approved\",\"role\":\"member\",\"avatar\":\"https://example.com/a.png\"}}"))?.AvatarPng is null, "External avatars must be rejected.");
Assert(ProfilePayloadParser.ParseAuthenticatedUser(200, new byte[ProfilePayloadParser.MaxResponseBytes + 1]) is null, "Oversized bodies must be rejected.");

var coordinator = new ProfileResponseCoordinator();
var first = coordinator.BeginResponse();
var second = coordinator.BeginResponse();
Assert(!coordinator.IsCurrent(first) && coordinator.IsCurrent(second), "Latest /api/me response must win.");
coordinator.Invalidate();
Assert(!coordinator.IsCurrent(second), "Logout invalidation must stale an in-flight response.");

var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root is not null && !File.Exists(Path.Combine(root.FullName, "FusionLedger.Windows.csproj"))) root = root.Parent;
Assert(root is not null, "Source root must be discoverable.");
var sourceRoot = root!.FullName;
var mainXaml = File.ReadAllText(Path.Combine(sourceRoot, "MainWindow.xaml"));
var mainCode = File.ReadAllText(Path.Combine(sourceRoot, "MainWindow.xaml.cs"));
var loginXaml = File.ReadAllText(Path.Combine(sourceRoot, "LoginWindow.xaml"));
var loginCode = File.ReadAllText(Path.Combine(sourceRoot, "LoginWindow.xaml.cs"));
var appCode = File.ReadAllText(Path.Combine(sourceRoot, "App.xaml.cs"));
var versionCode = File.ReadAllText(Path.Combine(sourceRoot, "RuntimeVersionInfo.cs"));
var themeCode = File.ReadAllText(Path.Combine(sourceRoot, "ThemeService.cs"));
Assert(mainXaml.Contains("controls:TitleBar", StringComparison.Ordinal), "MainWindow must use the WinUI TitleBar control.");
Assert(mainXaml.Contains("BackRequested", StringComparison.Ordinal), "TitleBar back event must remain wired.");
Assert(mainXaml.Contains("NavigationView", StringComparison.Ordinal) && mainXaml.Contains("FooterMenuItems", StringComparison.Ordinal), "Native navigation must expose footer items.");
Assert(mainXaml.Contains("IsPaneToggleButtonVisible=\"True\"", StringComparison.Ordinal)
    && mainXaml.Contains("PaneDisplayMode=\"Auto\"", StringComparison.Ordinal), "NavigationView must show its standard pane toggle in the automatic pane layout.");
Assert(mainXaml.Contains("OpenPaneLength=\"240\"", StringComparison.Ordinal), "Expanded NavigationView pane must be narrower than the 320 DIP default.");
var titleBarMarkup = mainXaml[..mainXaml.IndexOf("</controls:TitleBar>", StringComparison.Ordinal)];
Assert(!mainXaml.Contains("PaneToggleRequested", StringComparison.Ordinal)
    && !titleBarMarkup.Contains("IsPaneToggleButtonVisible", StringComparison.Ordinal), "TitleBar must not host a duplicate pane toggle.");
Assert(mainXaml.Contains("<ContentControl x:Name=\"ContentFrame\" HorizontalContentAlignment=\"Stretch\" VerticalContentAlignment=\"Stretch\">", StringComparison.Ordinal)
    && mainXaml.Contains("<EntranceThemeTransition FromHorizontalOffset=\"40\"", StringComparison.Ordinal)
    && mainCode.Contains("ContentFrame.Content = CreatePlaceholder(page)", StringComparison.Ordinal)
    && !mainCode.Contains("ContentFrame.Navigate", StringComparison.Ordinal), "Section content must animate with the native theme transition without Frame navigation.");
Assert(mainCode.Contains("private void RootGrid_Loaded", StringComparison.Ordinal)
    && mainCode.Contains("_initialNavigationCompleted = true", StringComparison.Ordinal)
    && mainCode.Contains("NavigateTo(NativePage.Dashboard, false)", StringComparison.Ordinal)
    && !mainCode.Contains("Navigation.SelectedItem = Navigation.MenuItems[0]", StringComparison.Ordinal), "Initial frame navigation must wait until the shell has loaded instead of reentering NavigationView selection.");
Assert(mainCode.Contains("_syncingNavigationSelection", StringComparison.Ordinal), "Programmatic selected-item synchronization must not recursively navigate the frame.");
Assert(mainXaml.Contains("Height=\"48\"", StringComparison.Ordinal) && mainXaml.Contains("VerticalContentAlignment=\"Center\"", StringComparison.Ordinal), "TitleBar must use centered 48px alignment.");
Assert(mainXaml.Contains("Height=\"32\"", StringComparison.Ordinal) && mainXaml.Contains("MinWidth=\"96\"", StringComparison.Ordinal) && mainXaml.Contains("MaxWidth=\"540\"", StringComparison.Ordinal), "Page search must use the bounded runtime size.");
Assert(mainXaml.Contains("SizeChanged=\"RootGrid_SizeChanged\"", StringComparison.Ordinal)
    && mainXaml.Contains("Loaded=\"RootGrid_Loaded\"", StringComparison.Ordinal)
    && mainCode.Contains("TitleBarLayoutPolicy.SearchWidthForClient", StringComparison.Ordinal)
    && !mainXaml.Contains("WindowWidthStates", StringComparison.Ordinal), "Page search width must follow actual client-size events, not VisualStates.");
Assert(TitleBarLayoutPolicy.SearchWidthForClient(1906) == 540
    && TitleBarLayoutPolicy.SearchWidthForClient(1600) == 540
    && TitleBarLayoutPolicy.SearchWidthForClient(1200) == 420
    && TitleBarLayoutPolicy.SearchWidthForClient(900) == 320
    && TitleBarLayoutPolicy.SearchWidthForClient(600) == 200
    && TitleBarLayoutPolicy.SearchWidthForClient(599) == 96, "Runtime search width policy must match all target ranges.");
Assert(mainXaml.Contains("Spacing=\"12\" VerticalAlignment=\"Center\"", StringComparison.Ordinal), "TitleBar right header must be centered with 12px spacing.");
Assert(mainXaml.Contains("KeyboardAcceleratorPlacementMode=\"Hidden\"", StringComparison.Ordinal), "Root shortcut placement hints must be hidden.");
Assert(mainXaml.Contains("Background=\"Transparent\"", StringComparison.Ordinal) && mainXaml.Contains("CornerRadius=\"16\"", StringComparison.Ordinal), "TitleBar action buttons must be transparent circular chrome.");
Assert(mainXaml.Contains("Glyph=\"&#xEA8F;\"", StringComparison.Ordinal), "Notifications must retain the Segoe Fluent Ringer glyph.");
Assert(mainXaml.Contains("Glyph=\"&#xEA8F;\" FontSize=\"18\" AutomationProperties.AccessibilityView=\"Raw\"", StringComparison.Ordinal)
    && mainXaml.Contains("x:Name=\"ProfilePicture\"", StringComparison.Ordinal)
    && mainXaml.Contains("DisplayName=\"\" AutomationProperties.AccessibilityView=\"Raw\"", StringComparison.Ordinal), "Decorative title-bar icon and avatar must be hidden from the accessibility tree.");
Assert(mainXaml.Contains("Width=\"320\" MaxHeight=\"440\"", StringComparison.Ordinal) && mainXaml.Contains("AccountSeparator", StringComparison.Ordinal), "Account flyout must have bounded identity/separator structure.");
Assert(mainXaml.Contains("x:Name=\"PageSearchBox\"", StringComparison.Ordinal)
    && mainXaml.Contains("<KeyboardAccelerator Key=\"K\" Modifiers=\"Control\"", StringComparison.Ordinal)
    && mainXaml.Contains("PageSearchKeyboardAccelerator_Invoked", StringComparison.Ordinal)
    && !mainCode.Contains("AddAccelerator(global::Windows.System.VirtualKey.K", StringComparison.Ordinal), "Ctrl+K must belong to the search box, not the root grid.");
Assert(mainCode.Contains("VirtualKey.N", StringComparison.Ordinal)
    && mainCode.Contains("VirtualKey.Left", StringComparison.Ordinal)
    && mainCode.Contains("VK_OEM_COMMA", StringComparison.Ordinal), "Non-search root shortcuts must remain defined.");
Assert(mainXaml.Contains("x:Name=\"NotificationsButton\" Width=\"32\" Height=\"32\"", StringComparison.Ordinal)
    && mainXaml.Contains("CornerRadius=\"6\"", StringComparison.Ordinal)
    && mainXaml.Contains("x:Name=\"ProfileButton\" Width=\"32\" Height=\"32\"", StringComparison.Ordinal)
    && mainXaml.Contains("CornerRadius=\"16\"", StringComparison.Ordinal), "Notification/profile button corner radii must follow their visual roles.");
Assert(mainXaml.IndexOf("x:Name=\"SignOutButton\"", StringComparison.Ordinal) < mainXaml.IndexOf("AccountSeparator", StringComparison.Ordinal)
    && mainXaml.IndexOf("AccountSeparator", StringComparison.Ordinal) < mainXaml.IndexOf("x:Name=\"ProfileSettingsButton\"", StringComparison.Ordinal)
    && mainXaml.IndexOf("x:Name=\"ProfileSettingsButton\"", StringComparison.Ordinal) < mainXaml.IndexOf("x:Name=\"AdministrationButton\"", StringComparison.Ordinal), "Account flyout must keep sign-out in the identity block, then profile/admin rows.");
Assert(mainXaml.Split("<local:ThinAcrylicBackdrop />").Length == 3
    && mainXaml.Split("ShouldConstrainToRootBounds=\"False\"").Length == 3
    && mainXaml.Contains("<Setter Property=\"Background\" Value=\"Transparent\" />", StringComparison.Ordinal)
    && mainXaml.Contains("<Setter Property=\"Padding\" Value=\"0\" />", StringComparison.Ordinal), "Account and notification flyouts must use a transparent presenter over an acrylic backdrop and may extend past the window.");
Assert(mainXaml.Contains("<Grid Width=\"360\" MaxHeight=\"440\">", StringComparison.Ordinal)
    && mainXaml.Contains("<ScrollViewer Width=\"320\" MaxHeight=\"440\"", StringComparison.Ordinal), "Notifications must be slightly wider than the 320px account flyout, and both must scroll instead of clipping.");
Assert(!mainXaml.Contains("WebView2", StringComparison.Ordinal) && !mainCode.Contains("WebView2", StringComparison.Ordinal), "MainWindow must not host WebView2.");
Assert(mainCode.Contains("CreateSettingsPage", StringComparison.Ordinal) && mainCode.Contains("CreateVersionInfoPage", StringComparison.Ordinal), "Settings and Version info must be functional native pages.");
Assert(mainCode.Contains("ScrollViewer", StringComparison.Ordinal) && mainCode.Contains("RadioButtons", StringComparison.Ordinal)
    && mainCode.Contains("LocalSettings", StringComparison.Ordinal) && themeCode.Contains("RequestedTheme", StringComparison.Ordinal), "Settings must be scrollable, native, persisted and theme-aware.");
Assert(mainCode.Contains("AppWindowTitleBar.IsCustomizationSupported", StringComparison.Ordinal)
    && mainCode.Contains("PreferredHeightOption = TitleBarHeightOption.Tall", StringComparison.Ordinal)
    && !mainCode.Contains("appWindow.TitleBar.Height", StringComparison.Ordinal), "Title bar must request Tall without assigning physical AppWindow pixels to XAML DIP.");
Assert(mainCode.Contains("SettingsThemeSavedMessage", StringComparison.Ordinal) && mainCode.Contains("SettingsRestartMessage", StringComparison.Ordinal), "Theme and language save feedback must be distinct.");
Assert(versionCode.Contains("RuntimeInformation.ProcessArchitecture", StringComparison.Ordinal)
    && versionCode.Contains("Package.Current", StringComparison.Ordinal)
    && versionCode.Contains("Dependencies", StringComparison.Ordinal)
    && versionCode.Contains("Microsoft.WindowsAppRuntime", StringComparison.Ordinal)
    && versionCode.Contains("GetAvailableBrowserVersionString", StringComparison.Ordinal)
    && versionCode.Contains("WindowsAppSdkVersion", StringComparison.Ordinal)
    && !versionCode.Contains("typeof(Microsoft.UI.Xaml.Application).Assembly.GetName().Version", StringComparison.Ordinal)
    && !versionCode.Contains("0.8.4", StringComparison.Ordinal), "Version info must report observed Windows App SDK/WebView2 runtime details without a hardcoded display fallback.");
Assert(loginXaml.Contains("WebView2", StringComparison.Ordinal) && loginCode.Contains("CoreWebView2", StringComparison.Ordinal), "Only LoginWindow may host WebView2.");
Assert(loginXaml.Contains("x:Name=\"RootGrid\"", StringComparison.Ordinal)
    && loginCode.Contains("ThemeService.ReadSavedPreference", StringComparison.Ordinal)
    && loginCode.Contains("_themeService.Apply", StringComparison.Ordinal), "LoginWindow must apply the persisted theme.");
foreach (var source in new[] { mainXaml, mainCode, loginXaml, loginCode })
{
    Assert(!source.Contains("ExecuteScriptAsync", StringComparison.Ordinal), "Script injection is forbidden.");
    Assert(!source.Contains("GetCookies", StringComparison.Ordinal) && !source.Contains("Request.Headers", StringComparison.Ordinal), "Cookie/header extraction is forbidden.");
}
Assert(mainCode.Contains("SetTitleBar(AppTitleBar)", StringComparison.Ordinal), "Native title bar must own drag/caption behavior.");
Assert(!mainCode.Contains("RightInset", StringComparison.Ordinal), "Manual title bar inset logic must be absent.");
Assert(!mainCode.Contains("fake", StringComparison.OrdinalIgnoreCase) && !mainXaml.Contains("fake", StringComparison.OrdinalIgnoreCase), "Native shell must not contain fake data.");
Assert(!loginXaml.Contains("MolHub sign in", StringComparison.Ordinal) && !loginXaml.Contains("Sign in to continue", StringComparison.Ordinal), "Login user-facing strings must come from resources.");
Assert(!mainXaml.Contains("Notifications\"", StringComparison.Ordinal) && !mainXaml.Contains("Profile settings\"", StringComparison.Ordinal), "MainWindow user-facing strings must come from resources.");
Assert(!mainXaml.Contains("email", StringComparison.OrdinalIgnoreCase) && !mainXaml.Contains("raw id", StringComparison.OrdinalIgnoreCase) && !mainXaml.Contains("token", StringComparison.OrdinalIgnoreCase), "Account flyout must not expose fake email/id/token data.");
Assert(loginCode.Contains("DeleteAllCookies", StringComparison.Ordinal), "Sign out must clear cookies through the active WebView2 profile.");
Assert(loginCode.Contains("ClearBrowsingDataAsync", StringComparison.Ordinal), "Sign out must clear profile browsing data through WebView2.");
Assert(loginCode.Contains("ShowSessionRecoveryError", StringComparison.Ordinal) && loginCode.Contains("SessionClearError", StringComparison.Ordinal), "Clear failure must remain in a localized retry/close state.");
var closedHandler = loginCode[loginCode.IndexOf("private void LoginWindow_Closed", StringComparison.Ordinal)..];
Assert(closedHandler.IndexOf("_themeService.Dispose()", StringComparison.Ordinal) >= 0
    && closedHandler.IndexOf("if (_core is not { } core) return;", StringComparison.Ordinal) > closedHandler.IndexOf("_themeService.Dispose()", StringComparison.Ordinal)
    && closedHandler.Contains("core.NavigationStarting -= Core_NavigationStarting", StringComparison.Ordinal), "LoginWindow must always dispose its theme service before conditionally detaching WebView events.");
Assert(mainCode.Contains("RollbackSetting", StringComparison.Ordinal)
    && mainCode.Contains("_updatingSettings = true", StringComparison.Ordinal), "Settings save failures must rollback radio selections under a guard.");
var resetSuccess = loginCode.IndexOf("SessionResetSucceeded?.Invoke", StringComparison.Ordinal);
var rootNavigate = loginCode.IndexOf("_core.Navigate(NavigationPolicy.AppUrl)", resetSuccess, StringComparison.Ordinal);
Assert(resetSuccess >= 0 && rootNavigate > resetSuccess, "Root navigation must occur only after profile clearing succeeds.");
Assert(!appCode.Contains("Directory.Delete", StringComparison.Ordinal), "Sign out must not delete the profile directory directly.");
var mainCreation = appCode.IndexOf("new MainWindow", StringComparison.Ordinal);
var oldLoginClose = appCode.IndexOf("login?.Close", StringComparison.Ordinal);
Assert(mainCreation >= 0 && oldLoginClose > mainCreation, "Authenticated MainWindow must activate before LoginWindow closes.");
var signOutStart = appCode.IndexOf("RequestSignOut", StringComparison.Ordinal);
var replacement = appCode.IndexOf("ShowLoginWindow(resetSession: true)", signOutStart, StringComparison.Ordinal);
var resetHandler = appCode.IndexOf("Login_SessionResetSucceeded", StringComparison.Ordinal);
var mainClose = appCode.IndexOf("main.Close()", resetHandler, StringComparison.Ordinal);
Assert(replacement >= 0 && resetHandler >= 0 && mainClose > resetHandler, "Sign out must create the replacement login window before closing MainWindow.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "FusionLedger.Windows.csproj")).Contains("<Version>0.8.4</Version>", StringComparison.Ordinal), "Version source of truth must be v0.8.4.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "Package.appxmanifest")).Contains("Version=\"0.8.4.0\"", StringComparison.Ordinal), "Manifest version must be v0.8.4.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "VERSION.md")).Contains("v0.8.4", StringComparison.Ordinal)
    && File.ReadAllText(Path.Combine(sourceRoot, "CHANGELOG.md")).Contains("## v0.8.4", StringComparison.Ordinal), "Version documentation must be updated.");
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    var resource = File.ReadAllText(Path.Combine(sourceRoot, "Strings", locale, "Resources.resw"));
    Assert(resource.Contains("NotConnected", StringComparison.Ordinal) && resource.Contains("Page_Dashboard", StringComparison.Ordinal)
        && resource.Contains("AccountMenuForFormat", StringComparison.Ordinal)
        && resource.Contains("SettingsDescription", StringComparison.Ordinal)
        && resource.Contains("VersionInfoDescription", StringComparison.Ordinal), $"Localization resources are incomplete: {locale}");
}
Assert(!File.Exists(Path.Combine(sourceRoot, "Assets", "Fonts", "MonaSans.ttf")), "Native Mona Sans binary must remain absent.");

var manifest = File.ReadAllText(Path.Combine(sourceRoot, "Package.appxmanifest"));
Assert(manifest.Contains("<DisplayName>MolHub for Windows</DisplayName>", StringComparison.Ordinal)
    && manifest.Contains("<PublisherDisplayName>MolHub</PublisherDisplayName>", StringComparison.Ordinal)
    && manifest.Contains("uap:VisualElements AppListEntry=\"default\" DisplayName=\"MolHub for Windows\"", StringComparison.Ordinal), "Package display names must use the MolHub brand.");
Assert(manifest.Contains("<Identity Name=\"FusionLedger.Windows\"", StringComparison.Ordinal), "Package identity must stay stable so upgrades keep the sign-in profile and settings.");
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    var resource = File.ReadAllText(Path.Combine(sourceRoot, "Strings", locale, "Resources.resw"));
    Assert(!resource.Contains("Fusion Ledger", StringComparison.Ordinal) && resource.Contains("<value>MolHub for Windows</value>", StringComparison.Ordinal), $"User-facing strings must use the MolHub brand: {locale}");
}
foreach (var logo in new[] { "Square44x44Logo.scale-100.png", "Square44x44Logo.targetsize-24_altform-unplated.png", "Square44x44Logo.targetsize-24_altform-lightunplated.png",
    "Square150x150Logo.scale-100.png", "Wide310x150Logo.scale-100.png", "SmallTile.scale-100.png", "LargeTile.scale-100.png", "StoreLogo.scale-100.png",
    "AppIconOnDark.png", "AppIconOnLight.png", "AppIconOnDark.ico", "AppIconOnLight.ico" })
{
    Assert(File.Exists(Path.Combine(sourceRoot, "Assets", logo)), $"Icon asset is missing: {logo}");
}
Assert(!File.Exists(Path.Combine(sourceRoot, "Assets", "fusion-ledger_ico.png")) && !manifest.Contains("fusion-ledger_ico", StringComparison.Ordinal), "The retired Fusion Ledger icon must not be referenced.");
Assert(AppIconAssets.TitleBarImageUri(true) == AppIconAssets.OnLightImageUri && AppIconAssets.TitleBarImageUri(false) == AppIconAssets.OnDarkImageUri
    && AppIconAssets.WindowIconFile(true) == AppIconAssets.OnLightIconFile && AppIconAssets.WindowIconFile(false) == AppIconAssets.OnDarkIconFile, "Icon variants must follow surface lightness.");
Assert(mainCode.Contains("ActualThemeChanged", StringComparison.Ordinal) && mainCode.Contains("WindowIcon.Apply(this)", StringComparison.Ordinal)
    && loginCode.Contains("WindowIcon.Apply(this)", StringComparison.Ordinal), "Both windows must set the MolHub icon and the title bar icon must follow the theme.");

Assert(LoginWindowLayoutPolicy.ClientSize(1.0, 2560, 1392) == new LoginWindowLayoutPolicy.PixelSize(481, 683), "Sign-in client area must be 481 x 683 DIP at 100%.");
Assert(LoginWindowLayoutPolicy.ClientSize(1.5, 2560, 1392) == new LoginWindowLayoutPolicy.PixelSize(722, 1025), "Sign-in client area must scale with DPI.");
Assert(LoginWindowLayoutPolicy.ClientSize(1.5, 1280, 672) == new LoginWindowLayoutPolicy.PixelSize(722, 624)
    && LoginWindowLayoutPolicy.ClientSize(2.0, 600, 400) == new LoginWindowLayoutPolicy.PixelSize(536, 336), "Sign-in window must stay inside the work area with a scaled margin.");
Assert(LoginWindowLayoutPolicy.ClientSize(double.NaN, 2560, 1392) == LoginWindowLayoutPolicy.ClientSize(1.0, 2560, 1392), "Invalid DPI scale must fall back to 100%.");
Assert(LoginWindowLayoutPolicy.CenteredPosition(483, 685, 0, 0, 2560, 1392) == new LoginWindowLayoutPolicy.PixelPoint(1038, 353)
    && LoginWindowLayoutPolicy.CenteredPosition(900, 900, 100, 50, 800, 600) == new LoginWindowLayoutPolicy.PixelPoint(100, 50), "Sign-in window must be centered and never start outside the work area.");
Assert(loginXaml.Contains("<RowDefinition Height=\"32\" />", StringComparison.Ordinal)
    && loginXaml.Contains("x:Name=\"LoginTitleBar\"", StringComparison.Ordinal)
    && loginCode.Contains("ExtendsContentIntoTitleBar = true", StringComparison.Ordinal)
    && loginCode.Contains("SetTitleBar(LoginTitleBar)", StringComparison.Ordinal), "Sign-in window must use a 32 DIP TitleBar-owned caption area.");
Assert(loginCode.Contains("LoginWindowLayoutPolicy.ClientSize", StringComparison.Ordinal)
    && loginCode.Contains("GetDpiForWindow", StringComparison.Ordinal)
    && loginCode.Contains("ResizeClient", StringComparison.Ordinal)
    && loginCode.Contains("GetClientRect(hwnd, out var rendered)", StringComparison.Ordinal)
    && loginCode.Contains("IsResizable = false", StringComparison.Ordinal), "Sign-in window must be sized from the DPI-aware compact layout policy.");
Assert(loginXaml.Contains("x:Name=\"LoginOverlay\"", StringComparison.Ordinal)
    && loginCode.Contains("core.NavigationCompleted -= Core_NavigationCompleted", StringComparison.Ordinal), "Loading overlay must hide on navigation completion and detach its handler on close.");
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    Assert(File.ReadAllText(Path.Combine(sourceRoot, "Strings", locale, "Resources.resw")).Contains("<data name=\"PleaseWait\">", StringComparison.Ordinal), $"Sign-in loading text must be localized: {locale}");
}

var backdropCode = File.ReadAllText(Path.Combine(sourceRoot, "ThinAcrylicBackdrop.cs"));
Assert(backdropCode.Contains("DesktopAcrylicKind.Thin", StringComparison.Ordinal) && backdropCode.Contains("IsInputActive = true", StringComparison.Ordinal)
    && backdropCode.Contains("AccessibilitySettings().HighContrast", StringComparison.Ordinal) && mainCode.Contains("UpdateFlyoutBackdropTheme", StringComparison.Ordinal), "Title-bar flyouts must draw active thin acrylic that follows the app theme and High Contrast.");
Assert(!FlyoutPlacementPolicy.OpenAbove(197, 1000, 50) && FlyoutPlacementPolicy.OpenAbove(139, 117, 1000)
    && !FlyoutPlacementPolicy.OpenAbove(139, 160, 1000) && !FlyoutPlacementPolicy.OpenAbove(440, 100, 60), "Flyouts must open above only when the work area below is too short and there is more room above.");
Assert(mainCode.Contains("ShowHeaderFlyout(NotificationsFlyout, NotificationsButton)", StringComparison.Ordinal)
    && mainCode.Contains("ShowHeaderFlyout(AccountFlyout, ProfileButton)", StringComparison.Ordinal)
    && mainCode.Contains("DisplayAreaFallback.Nearest).WorkArea", StringComparison.Ordinal), "Title-bar flyouts must choose their placement from the monitor work area.");

// Web data bridge: exact document, trusted source, fixed commands, bounded request/result shapes.
Assert(BridgePolicy.BridgeUri.AbsoluteUri == "https://fusion-ledger.desase0175.workers.dev/webview-bridge", "Bridge must load the canonical extensionless document on the production origin.");
Assert(BridgePolicy.IsBridgeDocument(BridgePolicy.BridgeUri)
    && !BridgePolicy.IsBridgeDocument(new Uri("https://fusion-ledger.desase0175.workers.dev/webview-bridge.html"))
    && !BridgePolicy.IsBridgeDocument(new Uri("https://fusion-ledger.desase0175.workers.dev/webview-bridge?x=1"))
    && !BridgePolicy.IsBridgeDocument(new Uri("https://fusion-ledger.desase0175.workers.dev/webview-bridge#x"))
    && !BridgePolicy.IsBridgeDocument(new Uri("http://fusion-ledger.desase0175.workers.dev/webview-bridge"))
    && !BridgePolicy.IsBridgeDocument(new Uri("https://example.com/webview-bridge"))
    && !BridgePolicy.IsBridgeDocument(new Uri("https://fusion-ledger.desase0175.workers.dev/")), "Only the exact bridge document may load.");
Assert(BridgePolicy.IsTrustedSource("https://fusion-ledger.desase0175.workers.dev/webview-bridge")
    && !BridgePolicy.IsTrustedSource("https://fusion-ledger.desase0175.workers.dev/")
    && !BridgePolicy.IsTrustedSource(null) && !BridgePolicy.IsTrustedSource("not a url"), "Only the bridge document may answer.");
Assert(BridgePolicy.IsKnownCommand("session") && BridgePolicy.IsKnownCommand("publishCommit") && !BridgePolicy.IsKnownCommand("deleteMember")
    && !BridgePolicy.IsKnownCommand("fetch") && !BridgePolicy.IsKnownCommand(null)
    && BridgePolicy.IsWrite("logout") && BridgePolicy.IsWrite("publishCommit") && !BridgePolicy.IsWrite("dashboard"), "Bridge commands must come from the fixed read/write allowlist.");
var bridgeId = BridgePolicy.NewRequestId();
Assert(BridgePolicy.IsValidRequestId(bridgeId) && !BridgePolicy.IsValidRequestId("bad id") && !BridgePolicy.IsValidRequestId(new string('a', 81)), "Request ids must match the bridge pattern.");
var built = System.Text.Json.JsonDocument.Parse(BridgePolicy.BuildRequest("project", "r1", new System.Text.Json.Nodes.JsonObject { ["projectId"] = "p1", ["requestId"] = "evil" })!).RootElement;
Assert(built.GetProperty("command").GetString() == "project" && built.GetProperty("requestId").GetString() == "r1"
    && built.GetProperty("payload").GetProperty("projectId").GetString() == "p1" && !built.GetProperty("payload").TryGetProperty("requestId", out _), "Bridge requests must carry command, requestId and a payload without a spoofed requestId.");
Assert(BridgePolicy.BuildRequest("fetch", "r1", null) is null && BridgePolicy.BuildRequest("session", "bad id", null) is null
    && BridgePolicy.BuildRequest("publishCommit", "r1", new System.Text.Json.Nodes.JsonObject { ["changes"] = new string('x', 50_001) }) is null, "Unknown commands, invalid ids and oversized payloads must never be sent.");
var readResult = BridgePolicy.ParseResult("{\"requestId\":\"r1\",\"status\":200,\"ok\":true,\"data\":{\"user\":null},\"meta\":{\"nextOffset\":null},\"retryable\":false}");
Assert(readResult is { Ok: true, Status: 200, Outcome: BridgeOutcome.None, ErrorCode: null } && readResult.Data?.GetProperty("user").ValueKind == System.Text.Json.JsonValueKind.Null && readResult.Meta is not null, "Read results must keep data and pagination meta.");
var timeoutResult = BridgePolicy.ParseResult("{\"requestId\":\"r2\",\"status\":0,\"ok\":false,\"error\":{\"code\":\"TIMEOUT\"},\"retryable\":false,\"outcome\":\"unknown\"}");
Assert(timeoutResult is { Status: 0, Ok: false, ErrorCode: "TIMEOUT", Outcome: BridgeOutcome.Unknown }, "Write timeouts must stay outcome Unknown.");
var conflictResult = BridgePolicy.ParseResult("{\"requestId\":\"r3\",\"status\":409,\"ok\":false,\"error\":{\"code\":\"STALE_VERSION\",\"details\":{\"head\":\"c9\"}},\"outcome\":\"rejected\"}");
Assert(conflictResult is { ErrorCode: "STALE_VERSION", Outcome: BridgeOutcome.Rejected } && conflictResult.ErrorDetails?.GetProperty("head").GetString() == "c9", "Conflict codes and details must reach the host.");
Assert(BridgePolicy.ParseResult("{\"requestId\":\"r4\",\"status\":500,\"ok\":false,\"outcome\":\"surprise\"}") is { ErrorCode: "SERVER_ERROR", Outcome: BridgeOutcome.Unknown }, "Unrecognised outcomes must be treated as Unknown.");
Assert(BridgePolicy.ParseResult("{\"requestId\":null,\"status\":400,\"ok\":false}") is null && BridgePolicy.ParseResult("[]") is null
    && BridgePolicy.ParseResult("not json") is null && BridgePolicy.ParseResult(new string(' ', BridgePolicy.MaxResultChars + 1)) is null, "Malformed, uncorrelated or oversized results must be dropped.");
Assert(BridgeResult.HostFailure("r5", "BRIDGE_UNAVAILABLE", false, BridgeOutcome.Rejected) is { Status: 0, Ok: false, Outcome: BridgeOutcome.Rejected }, "Host failures must report status 0 and an explicit outcome.");
var bridgeCode = File.ReadAllText(Path.Combine(sourceRoot, "WebBridgeClient.cs"));
Assert(bridgeCode.Contains("settings.IsWebMessageEnabled = true", StringComparison.Ordinal)
    && bridgeCode.Contains("settings.AreDevToolsEnabled = false", StringComparison.Ordinal)
    && bridgeCode.Contains("settings.AreHostObjectsAllowed = false", StringComparison.Ordinal)
    && bridgeCode.Contains("_controller.IsVisible = false", StringComparison.Ordinal)
    && bridgeCode.Contains("if (!BridgePolicy.IsTrustedSource(e.Source)) return;", StringComparison.Ordinal)
    && bridgeCode.Contains("if (BridgePolicy.IsBridgeDocument(TryParseUri(e.Uri))) return;", StringComparison.Ordinal)
    && bridgeCode.Contains("Core_FrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e) => e.Cancel = true", StringComparison.Ordinal)
    && bridgeCode.Contains("CoreWebView2PermissionState.Deny", StringComparison.Ordinal)
    && bridgeCode.Contains("e.Handled = true", StringComparison.Ordinal), "The hidden bridge must enable WebMessage only for the verified bridge document and deny everything else.");
Assert(!bridgeCode.Contains("ExecuteScriptAsync", StringComparison.Ordinal) && !bridgeCode.Contains("AddScriptToExecuteOnDocumentCreated", StringComparison.Ordinal)
    && !bridgeCode.Contains("AddHostObjectToScript", StringComparison.Ordinal) && !bridgeCode.Contains("CookieManager", StringComparison.Ordinal)
    && !bridgeCode.Contains("GetCookies", StringComparison.Ordinal), "The bridge host must not inject script, expose host objects or touch cookies.");
Assert(loginCode.Contains("IsWebMessageEnabled = false", StringComparison.Ordinal)
    && loginCode.Contains("WebViewProfile.GetEnvironmentAsync()", StringComparison.Ordinal)
    && bridgeCode.Contains("WebViewProfile.GetEnvironmentAsync()", StringComparison.Ordinal), "Sign-in keeps WebMessage disabled and shares the single WebView2 environment with the bridge.");
var signOutHandler = mainCode[mainCode.IndexOf("private async void SignOut_Click", StringComparison.Ordinal)..];
Assert(signOutHandler.IndexOf("RequestAsync(\"logout\")", StringComparison.Ordinal) >= 0
    && signOutHandler.IndexOf("RequestAsync(\"logout\")", StringComparison.Ordinal) < signOutHandler.IndexOf("App.RequestSignOut()", StringComparison.Ordinal)
    && mainCode.Contains("_bridge.Dispose()", StringComparison.Ordinal), "Sign-out must revoke the server session through the bridge before clearing local data, and the bridge must close with MainWindow.");

// Native Dashboard: server DTO parsing, web-equivalent counts and presentation rules.
var dashboardJson = """
{"projects":[
  {"id":"p1","name":"Sonic Nightmare","reservation":{"userId":"u1","username":"Azunel","startedAt":"2026-09-19T14:00:00.000Z"}},
  {"id":"p2","name":"Splats Framework","reservation":{"userId":"u2","username":"kalvin","startedAt":null}},
  {"id":"p3","name":"Scratch Edition","reservation":null},
  {"id":"","name":"Invalid"}],
 "commits":[
  {"id":"74a6ba6e-0000-4000-8000-000000000000","projectName":"Sonic Nightmare","title":"Sonic Nightmare","changes":"small fixes","url":"https://example.com/a","version":"v0.4.785B","createdAt":"2026-09-19T14:24:00.000Z","author":{"id":"u2","username":"kalvin","avatar":"https://example.com/a.png"}},
  {"id":"c2","projectName":"Splats","title":"","changes":"no title is skipped","author":{"username":"x"}}],
 "activity":[{"projectName":"Splats Framework","actor":"azunel","createdAt":"2026-09-18T09:29:00.000Z"}],
 "stats":{"commits":4,"projects":2},
 "pending":{"accounts":1,"requests":2}}
""";
var dashboard = DashboardModel.Parse(System.Text.Json.JsonDocument.Parse(dashboardJson).RootElement);
Assert(dashboard is { ActiveProjects: 3, InProgress: 2, PendingApprovals: 3 } && dashboard.ReservedBy("azunel").Count == 1 && dashboard.ReservedBy("nobody").Count == 0,
    "Dashboard counts must be computed from server data exactly like the web dashboard.");
Assert(dashboard!.Commits.Count == 1 && dashboard.Commits[0].AuthorName == "kalvin" && dashboard.Commits[0].AuthorAvatar is null && dashboard.Commits[0].Version == "v0.4.785B"
    && dashboard.Activity.Count == 1 && dashboard.Activity[0].Actor == "azunel", "Dashboard commits/activity must be parsed with bounded fields and validated avatars.");
Assert(DashboardModel.Parse(System.Text.Json.JsonDocument.Parse("{\"projects\":{}}").RootElement) is null
    && DashboardModel.Parse(System.Text.Json.JsonDocument.Parse("[]").RootElement) is null, "Unexpected dashboard shapes must not render.");
Assert(DashboardModel.Preview(new string('a', 421)) == new string('a', 420) + "…" && DashboardModel.Preview("  short  ") == "short"
    && DashboardModel.ShortId("74a6ba6e-0000") == "74a6ba6" && DashboardModel.ShortId("abc") == "abc", "Commit previews and short IDs must match the web feed.");
Assert(DashboardModel.FormatDate(DateTimeOffset.Parse("2026-09-19T14:24:00Z"), "en-US", TimeZoneInfo.Utc) == "Sep 19, 2026, 02:24 PM"
    && DashboardModel.FormatDate(DateTimeOffset.Parse("2026-09-19T14:24:00Z"), "ja-JP", TimeZoneInfo.Utc) == "2026/09/19 14:24"
    && DashboardModel.FormatDate(null, "en-US") == string.Empty, "Dashboard dates must be localized and never invented.");
Assert(DashboardModel.ShowsPendingApprovals("admin", false) && DashboardModel.ShowsPendingApprovals("member", true) && !DashboardModel.ShowsPendingApprovals("member", false),
    "Pending approvals are only shown to site admins and project owners.");
Assert(DashboardModel.ErrorFor(BridgeResult.HostFailure("r", "BRIDGE_UNAVAILABLE", true, BridgeOutcome.None)) == DashboardError.Connection
    && DashboardModel.ErrorFor(new BridgeResult("r", 401, false, null, null, "UNAUTHORIZED", null, false, BridgeOutcome.None)) == DashboardError.SessionEnded
    && DashboardModel.ErrorFor(new BridgeResult("r", 403, false, null, null, "NOT_APPROVED", null, false, BridgeOutcome.None)) == DashboardError.PendingApproval
    && DashboardModel.ErrorFor(new BridgeResult("r", 503, false, null, null, "MAINTENANCE", null, false, BridgeOutcome.None)) == DashboardError.Maintenance
    && DashboardModel.ErrorFor(new BridgeResult("r", 429, false, null, null, "RATE_LIMIT", null, true, BridgeOutcome.None)) == DashboardError.RateLimited
    && DashboardModel.ErrorFor(new BridgeResult("r", 500, false, null, null, "SERVER_ERROR", null, true, BridgeOutcome.None)) == DashboardError.Unexpected,
    "Dashboard errors must map bridge results to honest states.");
var dashboardCode = File.ReadAllText(Path.Combine(sourceRoot, "DashboardView.cs"));
var usedKeys = System.Text.RegularExpressions.Regex.Matches(dashboardCode, "\"(Dashboard_[A-Za-z]+)\"").Select(m => m.Groups[1].Value)
    .Concat(new[] { "ErrorConnection", "ErrorSession", "ErrorPending", "ErrorMaintenance", "ErrorRateLimit", "ErrorUnexpected" }.Select(k => "Dashboard_" + k + "Title"))
    .Distinct().ToList();
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    var resource = File.ReadAllText(Path.Combine(sourceRoot, "Strings", locale, "Resources.resw"));
    foreach (var key in usedKeys) Assert(resource.Contains($"<data name=\"{key}\">", StringComparison.Ordinal), $"Missing dashboard string {key} in {locale}");
}
Assert(mainCode.Contains("_bridge.RequestAsync(\"dashboard\")", StringComparison.Ordinal) && mainCode.Contains("if (page == NativePage.Dashboard) return CreateDashboardPage();", StringComparison.Ordinal)
    && dashboardCode.Contains("Dashboard_StorageNote", StringComparison.Ordinal)
    && !dashboardCode.Contains("Create project", StringComparison.OrdinalIgnoreCase) && !dashboardCode.Contains("publish:", StringComparison.Ordinal),
    "Dashboard must load through the bridge, keep the storage privacy note and render no actions without a native implementation.");
var avatarCode = File.ReadAllText(Path.Combine(sourceRoot, "AvatarImage.cs"));
Assert(avatarCode.Contains("picture.Loaded +=", StringComparison.Ordinal) && avatarCode.Contains("picture.ActualThemeChanged +=", StringComparison.Ordinal)
    && dashboardCode.Contains("AvatarImage.Attach(picture, avatar, 56)", StringComparison.Ordinal)
    && mainCode.Contains("AvatarImage.Attach(ProfilePicture, avatar, 64)", StringComparison.Ordinal) && mainCode.Contains("AvatarImage.Attach(AccountPicture, avatar, 96)", StringComparison.Ordinal),
    "Avatars must be decoded again whenever a picture is loaded or its theme changes, so a theme switch never leaves only initials.");
var captionCode = File.ReadAllText(Path.Combine(sourceRoot, "CaptionButtonTheme.cs"));
Assert(captionCode.Contains("TitleBar.PreferredTheme", StringComparison.Ordinal) && captionCode.Contains("root.ActualThemeChanged +=", StringComparison.Ordinal)
    && captionCode.Contains("TitleBarTheme.UseDefaultAppMode", StringComparison.Ordinal)
    && mainCode.Contains("CaptionButtonTheme.Attach(this, RootGrid)", StringComparison.Ordinal) && loginCode.Contains("CaptionButtonTheme.Attach(this, RootGrid)", StringComparison.Ordinal),
    "System caption buttons must follow the app theme (High Contrast keeps system colors) in both windows.");
Assert(mainCode.Contains("if (language == ReadLanguage()) return;", StringComparison.Ordinal) && mainCode.Contains("if (theme == ReadTheme()) return;", StringComparison.Ordinal),
    "Settings must ignore selections equal to the saved value so opening the page reports no save.");

Console.WriteLine("v0.8.4 native shell, dashboard, settings, version info, title bar, flyout, login boundary, policy, profile, concurrency, docs and localization tests passed.");
