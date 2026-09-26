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
Assert(mainXaml.Contains("<ContentControl x:Name=\"ContentFrame\"", StringComparison.Ordinal)
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
Assert(mainXaml.Contains("Glyph=\"&#xEA8F;\"", StringComparison.Ordinal) && mainXaml.Contains("Width=\"20\"", StringComparison.Ordinal), "Notifications must retain the Segoe Fluent Ringer glyph.");
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
Assert(mainXaml.IndexOf("ProfileSettingsButton", StringComparison.Ordinal) < mainXaml.IndexOf("AdministrationButton", StringComparison.Ordinal)
    && mainXaml.IndexOf("AdministrationButton", StringComparison.Ordinal) < mainXaml.IndexOf("SignOutButton", StringComparison.Ordinal), "Account rows must retain profile/admin/sign-out order.");
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
    && !versionCode.Contains("0.6.1", StringComparison.Ordinal), "Version info must report observed Windows App SDK/WebView2 runtime details without a hardcoded display fallback.");
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
Assert(File.ReadAllText(Path.Combine(sourceRoot, "FusionLedger.Windows.csproj")).Contains("<Version>0.6.1</Version>", StringComparison.Ordinal), "Version source of truth must be v0.6.1.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "Package.appxmanifest")).Contains("Version=\"0.6.1.0\"", StringComparison.Ordinal), "Manifest version must be v0.6.1.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "VERSION.md")).Contains("v0.6.1", StringComparison.Ordinal)
    && File.ReadAllText(Path.Combine(sourceRoot, "CHANGELOG.md")).Contains("## v0.6.1", StringComparison.Ordinal), "Version documentation must be updated.");
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

Console.WriteLine("v0.6.1 native shell, settings, version info, title bar, flyout, login boundary, policy, profile, concurrency, docs and localization tests passed.");
