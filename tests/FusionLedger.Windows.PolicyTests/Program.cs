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
Assert(mainXaml.Contains("controls:TitleBar", StringComparison.Ordinal), "MainWindow must use the WinUI TitleBar control.");
Assert(mainXaml.Contains("PaneToggleRequested", StringComparison.Ordinal) && mainXaml.Contains("BackRequested", StringComparison.Ordinal), "TitleBar events must be wired.");
Assert(mainXaml.Contains("NavigationView", StringComparison.Ordinal) && mainXaml.Contains("FooterMenuItems", StringComparison.Ordinal), "Native navigation must expose footer items.");
Assert(mainXaml.Contains("IsPaneToggleButtonVisible=\"False\"", StringComparison.Ordinal), "NavigationView pane toggle must be controlled by TitleBar only.");
Assert(!mainXaml.Contains("WebView2", StringComparison.Ordinal) && !mainCode.Contains("WebView2", StringComparison.Ordinal), "MainWindow must not host WebView2.");
Assert(loginXaml.Contains("WebView2", StringComparison.Ordinal) && loginCode.Contains("CoreWebView2", StringComparison.Ordinal), "Only LoginWindow may host WebView2.");
foreach (var source in new[] { mainXaml, mainCode, loginXaml, loginCode })
{
    Assert(!source.Contains("ExecuteScriptAsync", StringComparison.Ordinal), "Script injection is forbidden.");
    Assert(!source.Contains("GetCookies", StringComparison.Ordinal) && !source.Contains("Request.Headers", StringComparison.Ordinal), "Cookie/header extraction is forbidden.");
}
Assert(mainCode.Contains("SetTitleBar(AppTitleBar)", StringComparison.Ordinal), "Native title bar must own drag/caption behavior.");
Assert(!mainCode.Contains("RightInset", StringComparison.Ordinal) && !mainCode.Contains("AppWindow", StringComparison.Ordinal), "Manual title bar inset logic must be absent.");
Assert(!mainCode.Contains("fake", StringComparison.OrdinalIgnoreCase) && !mainXaml.Contains("fake", StringComparison.OrdinalIgnoreCase), "Native shell must not contain fake data.");
Assert(!loginXaml.Contains("Fusion Ledger sign in", StringComparison.Ordinal) && !loginXaml.Contains("Sign in to continue", StringComparison.Ordinal), "Login user-facing strings must come from resources.");
Assert(!mainXaml.Contains("Notifications\"", StringComparison.Ordinal) && !mainXaml.Contains("Profile settings\"", StringComparison.Ordinal), "MainWindow user-facing strings must come from resources.");
Assert(loginCode.Contains("DeleteAllCookies", StringComparison.Ordinal), "Sign out must clear cookies through the active WebView2 profile.");
Assert(loginCode.Contains("ClearBrowsingDataAsync", StringComparison.Ordinal), "Sign out must clear profile browsing data through WebView2.");
Assert(loginCode.Contains("ShowSessionRecoveryError", StringComparison.Ordinal) && loginCode.Contains("SessionClearError", StringComparison.Ordinal), "Clear failure must remain in a localized retry/close state.");
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
Assert(File.ReadAllText(Path.Combine(sourceRoot, "FusionLedger.Windows.csproj")).Contains("<Version>0.4.0</Version>", StringComparison.Ordinal), "Version source of truth must be v0.4.0.");
Assert(File.ReadAllText(Path.Combine(sourceRoot, "Package.appxmanifest")).Contains("Version=\"0.4.0.0\"", StringComparison.Ordinal), "Manifest version must be v0.4.0.");
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    var resource = File.ReadAllText(Path.Combine(sourceRoot, "Strings", locale, "Resources.resw"));
    Assert(resource.Contains("NotConnected", StringComparison.Ordinal) && resource.Contains("Page_Dashboard", StringComparison.Ordinal), $"Localization resources are incomplete: {locale}");
}
Assert(!File.Exists(Path.Combine(sourceRoot, "Assets", "Fonts", "MonaSans.ttf")), "Native Mona Sans binary must remain absent.");

Console.WriteLine("v0.4 native shell, login boundary, policy, profile, concurrency, docs and localization tests passed.");
