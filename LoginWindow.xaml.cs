using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;

namespace FusionLedger.Windows;

public sealed partial class LoginWindow : Window
{
    private readonly ProfileResponseCoordinator _responses = new();
    private readonly ResourceLoader _strings = ResourceLoader.GetForViewIndependentUse();
    private readonly bool _resetSessionBeforeNavigate;
    private CoreWebView2? _core;
    private bool _initialized;
    private bool _authenticationRaised;
    private bool _closed;

    public event EventHandler<AuthenticatedUser>? Authenticated;
    public event EventHandler? SessionResetSucceeded;

    public LoginWindow(bool resetSessionBeforeNavigate = false)
    {
        _resetSessionBeforeNavigate = resetSessionBeforeNavigate;
        InitializeComponent();
        LoginWebView.Loaded += LoginWebView_Loaded;
        Closed += LoginWindow_Closed;
        ExtendsContentIntoTitleBar = false;
        ApplyLocalizedStrings();
    }

    private string L(string key) => _strings.GetString(key);

    private void ApplyLocalizedStrings()
    {
        Title = L("LoginTitle");
        LoginTitle.Text = L("ProductName");
        LoginStatus.Text = L("SignInPrompt");
        AutomationProperties.SetName(LoginWebView, L("LoginWebViewName"));
        ResetSessionButton.Content = L("Retry");
        CloseSessionButton.Content = L("Close");
        AutomationProperties.SetName(ResetSessionButton, L("Retry"));
        AutomationProperties.SetName(CloseSessionButton, L("Close"));
    }

    private async void LoginWebView_Loaded(object sender, RoutedEventArgs e)
    {
        LoginWebView.Loaded -= LoginWebView_Loaded;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_initialized || _closed) return;
        try
        {
            var profilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FusionLedger.WebView2");
            Directory.CreateDirectory(profilePath);
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(
                browserExecutableFolder: null,
                userDataFolder: profilePath,
                options: new CoreWebView2EnvironmentOptions());
            await LoginWebView.EnsureCoreWebView2Async(environment);
            _core = LoginWebView.CoreWebView2;
            _core.Settings.AreDevToolsEnabled = false;
            _core.Settings.AreHostObjectsAllowed = false;
            _core.Settings.IsWebMessageEnabled = false;
            _core.Settings.IsStatusBarEnabled = false;
            _core.NavigationStarting += Core_NavigationStarting;
            _core.NewWindowRequested += Core_NewWindowRequested;
            _core.PermissionRequested += Core_PermissionRequested;
            _core.WebResourceResponseReceived += Core_WebResourceResponseReceived;
            _initialized = true;

            if (_resetSessionBeforeNavigate)
            {
                await ResetSessionAndNavigateAsync();
            }
            else
            {
                _core.Navigate(NavigationPolicy.AppUrl);
            }
        }
        catch
        {
            ShowSessionRecoveryError();
        }
    }

    private async Task ResetSessionAndNavigateAsync()
    {
        if (_core is null || _closed) return;
        _responses.Invalidate();
        SessionRecoveryPanel.Visibility = Visibility.Collapsed;
        LoginStatus.Text = L("SigningOut");
        try
        {
            _core.Profile.CookieManager.DeleteAllCookies();
            await _core.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllSite);
            if (_closed) return;
            SessionResetSucceeded?.Invoke(this, EventArgs.Empty);
            _core.Navigate(NavigationPolicy.AppUrl);
        }
        catch
        {
            ShowSessionRecoveryError();
        }
    }

    private void ShowSessionRecoveryError()
    {
        LoginStatus.Text = L("SessionClearError");
        SessionRecoveryPanel.Visibility = Visibility.Visible;
    }

    private async void ResetSessionButton_Click(object sender, RoutedEventArgs e)
    {
        await ResetSessionAndNavigateAsync();
    }

    private void CloseSessionButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var uri = TryParseUri(e.Uri);
        if (NavigationPolicy.IsAllowed(uri)) return;
        e.Cancel = true;
        _ = OpenExternalAsync(uri);
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        var uri = TryParseUri(e.Uri);
        if (NavigationPolicy.IsAllowed(uri)) _core?.Navigate(uri!.AbsoluteUri);
        else _ = OpenExternalAsync(uri);
    }

    private static void Core_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Deny;
        e.Handled = true;
    }

    private async void Core_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        var uri = TryParseUri(e.Request.Uri);
        if (!ProfilePayloadParser.IsMeGet(uri, e.Request.Method)
            || e.Response.StatusCode is not (200 or 401) || _closed) return;

        var generation = _responses.BeginResponse();
        if (e.Response.StatusCode == 401)
        {
            _responses.Invalidate();
            LoginStatus.Text = L("SignInRequired");
            return;
        }

        try
        {
            using var content = await e.Response.GetContentAsync();
            using var stream = content.AsStream();
            var body = await ReadAtMostAsync(stream, ProfilePayloadParser.MaxResponseBytes);
            if (body is null || !_responses.IsCurrent(generation) || _closed) return;
            var user = ProfilePayloadParser.ParseAuthenticatedUser(e.Response.StatusCode, body);
            if (user is null || !_responses.IsCurrent(generation) || _closed)
            {
                LoginStatus.Text = L("SignInRequired");
                return;
            }

            if (!_authenticationRaised)
            {
                _authenticationRaised = true;
                Authenticated?.Invoke(this, user);
            }
        }
        catch
        {
            LoginStatus.Text = L("SignInRequired");
        }
    }

    private static async Task<byte[]?> ReadAtMostAsync(Stream stream, int maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(chunk, 0, chunk.Length)) > 0)
        {
            total += read;
            if (total > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static async Task<bool> OpenExternalAsync(Uri? uri)
    {
        if (!NavigationPolicy.IsExternalLaunchable(uri)) return false;
        try { return await Launcher.LaunchUriAsync(uri!); }
        catch { return false; }
    }

    private void LoginWindow_Closed(object sender, WindowEventArgs args)
    {
        _closed = true;
        _responses.Invalidate();
        if (_core is null) return;
        _core.NavigationStarting -= Core_NavigationStarting;
        _core.NewWindowRequested -= Core_NewWindowRequested;
        _core.PermissionRequested -= Core_PermissionRequested;
        _core.WebResourceResponseReceived -= Core_WebResourceResponseReceived;
    }

    private static Uri? TryParseUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
}
