using FusionLedger.Windows;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

Assert(NavigationPolicy.IsAllowed(new Uri(NavigationPolicy.AppUrl)), "Production URL must be allowed.");
Assert(NavigationPolicy.IsAllowed(new Uri("https://fusion-ledger.desase0175.workers.dev/projects")), "Production paths must be allowed.");
Assert(!NavigationPolicy.IsAllowed(new Uri("http://fusion-ledger.desase0175.workers.dev/")), "HTTP must be rejected.");
Assert(!NavigationPolicy.IsAllowed(new Uri("https://example.com/")), "Other origins must be rejected.");
Assert(!NavigationPolicy.IsAllowed(new Uri("https://fusion-ledger.desase0175.workers.dev:444/")), "Other ports must be rejected.");
Assert(NavigationPolicy.IsExternalLaunchable(new Uri("https://example.com/")), "External HTTPS must be launchable.");
Assert(NavigationPolicy.IsExternalLaunchable(new Uri("mailto:help@example.com")), "Mail links must be launchable.");
Assert(!NavigationPolicy.IsExternalLaunchable(new Uri(NavigationPolicy.AppUrl)), "Internal URLs must not launch externally.");

Assert(ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me"), "GET"), "Exact /api/me GET must be accepted.");
Assert(!ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me?x=1"), "GET"), "/api/me queries must be rejected.");
Assert(!ProfilePayloadParser.IsMeGet(new Uri("https://fusion-ledger.desase0175.workers.dev/api/me"), "POST"), "Only GET /api/me must be accepted.");
Assert(!ProfilePayloadParser.IsMeGet(new Uri("https://example.com/api/me"), "GET"), "Other origins must be rejected.");
Assert(ProfilePayloadParser.IsSessionInvalidatingPost(new Uri("https://fusion-ledger.desase0175.workers.dev/api/logout"), "POST"), "Successful logout POST must invalidate the profile.");
Assert(ProfilePayloadParser.IsSessionInvalidatingPost(new Uri("https://fusion-ledger.desase0175.workers.dev/api/password"), "POST"), "Successful password POST must invalidate the profile.");
Assert(!ProfilePayloadParser.IsSessionInvalidatingPost(new Uri("https://fusion-ledger.desase0175.workers.dev/api/logout"), "GET"), "Only invalidating POSTs must be recognized.");

var signedPng = "iVBORw0KGgo=";
var parsed = ProfilePayloadParser.Parse(200, System.Text.Encoding.UTF8.GetBytes($"{{\"user\":{{\"username\":\"  azunel  \",\"avatar\":\"data:image/png;base64,{signedPng}\"}}}}"));
Assert(parsed?.Username == "azunel", "Username must be trimmed and parsed.");
Assert(parsed?.AvatarPng?.Length == 8, "Only the bounded PNG bytes must be retained.");
Assert(ProfilePayloadParser.Parse(200, System.Text.Encoding.UTF8.GetBytes("{\"user\":null}")) == ProfileSnapshot.Empty, "Null user must clear the profile.");
Assert(ProfilePayloadParser.Parse(401, System.Text.Encoding.UTF8.GetBytes("not-json")) == ProfileSnapshot.Empty, "401 must clear without parsing a body.");
Assert(ProfilePayloadParser.Parse(200, System.Text.Encoding.UTF8.GetBytes("{\"user\":{\"avatar\":\"https://example.com/avatar.png\"}}"))?.AvatarPng is null, "External avatars must be rejected.");
Assert(ProfilePayloadParser.Parse(200, System.Text.Encoding.UTF8.GetBytes("{\"user\":{\"avatar\":\"data:image/svg+xml;base64,PHN2Zz4=\"}}"))?.AvatarPng is null, "SVG avatars must be rejected.");
Assert(ProfilePayloadParser.Parse(200, new byte[ProfilePayloadParser.MaxResponseBytes + 1]) is null, "Oversized responses must be rejected before parsing.");

var coordinator = new ProfileResponseCoordinator();
var firstResponse = coordinator.BeginResponse();
var secondResponse = coordinator.BeginResponse();
Assert(!coordinator.IsCurrent(firstResponse), "An older /api/me response must become stale when a newer response starts.");
Assert(coordinator.IsCurrent(secondResponse), "The newest /api/me response must remain current.");
coordinator.Invalidate();
Assert(!coordinator.IsCurrent(secondResponse), "Logout/password invalidation must stale an in-flight /api/me response.");

var concurrentCoordinator = new ProfileResponseCoordinator();
var concurrentGenerations = await Task.WhenAll(
    Enumerable.Range(0, 64).Select(_ => Task.Run(concurrentCoordinator.BeginResponse)));
Assert(concurrentGenerations.Distinct().Count() == 64, "Concurrent response generations must be unique.");
var latestGeneration = concurrentGenerations.Max();
Assert(concurrentCoordinator.IsCurrent(latestGeneration), "The latest concurrent response generation must remain current.");
Assert(concurrentGenerations.Where(g => g != latestGeneration).All(g => !concurrentCoordinator.IsCurrent(g)), "Older concurrent responses must be stale.");

Assert(NativeStatePolicy.OnNavigationStarting(new Uri(NavigationPolicy.AppUrl)) == NativeAppState.Navigating, "Allowed navigation must enter navigating state.");
Assert(NativeStatePolicy.OnNavigationStarting(new Uri("https://example.com/")) == NativeAppState.ExternalOpenFailed, "Disallowed navigation must remain outside WebView.");
Assert(NativeStatePolicy.OnNavigationCompleted(true, false) == NativeAppState.Connected, "Successful navigation must enter connected state.");
Assert(NativeStatePolicy.OnNavigationCompleted(false, true) == NativeAppState.Offline, "Network failure must enter offline state.");
Assert(NativeStatePolicy.ShowsProgress(NativeAppState.Navigating), "Navigating state must show the reserved progress indicator.");
Assert(NativeStatePolicy.ShowsInfoBar(NativeAppState.ProcessFailed), "Process failure must show an InfoBar.");
Assert(NativeStatePolicy.IsRetryAllowed(new Uri(NavigationPolicy.AppUrl)), "Retry must allow only the production URL.");
Assert(!NativeStatePolicy.IsRetryAllowed(new Uri("https://example.com/")), "Retry must reject other origins.");

var sourceRoot = new DirectoryInfo(AppContext.BaseDirectory);
while (sourceRoot is not null && !File.Exists(Path.Combine(sourceRoot.FullName, "FusionLedger.Windows.csproj")))
{
    sourceRoot = sourceRoot.Parent;
}
Assert(sourceRoot is not null, "The native source root must be discoverable for resource checks.");
var nativeSourceFiles = new[] { "MainWindow.xaml", "MainWindow.xaml.cs", "App.xaml", "FusionLedger.Windows.csproj" };
foreach (var sourceFile in nativeSourceFiles)
{
    var source = File.ReadAllText(Path.Combine(sourceRoot!.FullName, sourceFile));
    Assert(!source.Contains("Mona Sans", StringComparison.OrdinalIgnoreCase), $"Native source must not reference Mona Sans: {sourceFile}");
    Assert(!source.Contains("Heroicons", StringComparison.OrdinalIgnoreCase), $"Native source must not reference Heroicons: {sourceFile}");
    Assert(!source.Contains("#C64F57", StringComparison.OrdinalIgnoreCase), $"Native source must not use the old hard-coded red: {sourceFile}");
    Assert(!source.Contains("FontSize=\"11", StringComparison.OrdinalIgnoreCase), $"Native source must not use 11px text: {sourceFile}");
}
Assert(!File.Exists(Path.Combine(sourceRoot!.FullName, "Assets", "Fonts", "MonaSans.ttf")), "Mona Sans binary must be removed from the native app.");
Assert(!File.Exists(Path.Combine(sourceRoot.FullName, "licenses", "mona-sans.txt")), "Mona Sans license must be removed with the native asset.");
foreach (var locale in new[] { "en-US", "ja-JP" })
{
    var resourceFile = Path.Combine(sourceRoot.FullName, "Strings", locale, "Resources.resw");
    Assert(File.Exists(resourceFile), $"Localization resource must exist: {locale}");
    var resource = File.ReadAllText(resourceFile);
    Assert(resource.Contains("StatusConnected", StringComparison.Ordinal), $"Localization resource must include status strings: {locale}");
    Assert(resource.Contains("Retry", StringComparison.Ordinal), $"Localization resource must include Retry: {locale}");
}

Console.WriteLine("Navigation, profile, concurrency, state, localization, and native source policy tests passed.");
