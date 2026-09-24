using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using WinRT.Interop;

namespace FusionLedger.Windows;

public sealed partial class MainWindow : Window
{
    private bool _isReady;
    private readonly ProfileResponseCoordinator _profileResponses = new();
    private readonly ResourceLoader _strings = ResourceLoader.GetForViewIndependentUse();
    private AppWindow? _appWindow;
    private Button? _retryButton;
    private NativeAppState _state = NativeAppState.Initializing;

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.3.0";
        Title = $"Fusion Ledger for Windows v{version}";
        VersionText.Text = string.Format(L("VersionFormat"), $"v{version}");
        ApplyLocalizedStrings();
        _retryButton = new Button { Content = L("Retry") };
        _retryButton.Click += RetryButton_Click;
        StateInfoBar.ActionButton = _retryButton;

        ConfigureTitleBar();
        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
        BrowserCommandBar.SizeChanged += BrowserCommandBar_SizeChanged;
        Browser.Loaded += Browser_Loaded;
        SetNativeState(NativeAppState.Initializing);
    }

    private string L(string key) => _strings.GetString(key);

    private void ApplyLocalizedStrings()
    {
        AutomationProperties.SetName(AppTitleBar, L("TitleBarName"));
        AutomationProperties.SetName(MenuButton, L("MenuName"));
        ToolTipService.SetToolTip(MenuButton, L("MenuTooltip"));
        AutomationProperties.SetName(ProfilePicture, L("ProfileName"));
        BetaText.Text = L("Beta");
        BackButton.Label = L("Back");
        ForwardButton.Label = L("Forward");
        ReloadButton.Label = L("Refresh");
        AutomationProperties.SetName(BackButton, L("Back"));
        AutomationProperties.SetName(ForwardButton, L("Forward"));
        AutomationProperties.SetName(ReloadButton, L("Refresh"));
        ToolTipService.SetToolTip(BackButton, L("BackTooltip"));
        ToolTipService.SetToolTip(ForwardButton, L("ForwardTooltip"));
        ToolTipService.SetToolTip(ReloadButton, L("RefreshTooltip"));
        AutomationProperties.SetName(StatusIcon, L("StatusIconName"));
        AutomationProperties.SetLiveSetting(StatusRegion, AutomationLiveSetting.Polite);
    }

    private void ConfigureTitleBar()
    {
        // The installed WinUI package predates Microsoft.UI.Xaml.Controls.TitleBar.
        // SetTitleBar still provides the same native drag/caption-button contract.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                _appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                _appWindow.Changed += AppWindow_Changed;
                UpdateTitleBarInsets();
            }
        }
        catch (Exception exception)
        {
            // Windows 10 configurations without title-bar customization still retain
            // the standard caption buttons and the content title bar.
            Debug.WriteLine(exception.GetType().Name);
        }
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        UpdateTitleBarInsets();
    }

    private void UpdateTitleBarInsets()
    {
        if (_appWindow?.TitleBar is { } titleBar && AppWindowTitleBar.IsCustomizationSupported())
        {
            AppTitleBar.Padding = new Thickness(8, 0, Math.Max(8, titleBar.RightInset + 8), 0);
        }
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        BetaBadge.Visibility = width < 560 ? Visibility.Collapsed : Visibility.Visible;
        TitleText.Visibility = width < 480 ? Visibility.Collapsed : Visibility.Visible;
        TitleText.MaxWidth = Math.Max(80, width - 250);
    }

    private void BrowserCommandBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        StatusText.Visibility = e.NewSize.Width < 400 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void StatusRegion_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        StatusText.Visibility = BrowserCommandBar.ActualWidth < 400 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        // Button.Flyout opens automatically. This handler intentionally has no other action.
    }

    private void MenuKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        MenuButton.Flyout?.ShowAt(MenuButton);
        args.Handled = true;
    }

    private void BackKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        BackButton_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }

    private void ForwardKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ForwardButton_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }

    private void ReloadKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ReloadButton_Click(sender, new RoutedEventArgs());
        args.Handled = true;
    }

    private async void Browser_Loaded(object sender, RoutedEventArgs e)
    {
        Browser.Loaded -= Browser_Loaded;
        await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            SetNativeState(NativeAppState.Initializing);
            var profilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FusionLedger.WebView2");
            Directory.CreateDirectory(profilePath);
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, profilePath, new CoreWebView2EnvironmentOptions());
            await Browser.EnsureCoreWebView2Async(environment);

            var core = Browser.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsWebMessageEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.NavigationStarting += Core_NavigationStarting;
            core.NavigationCompleted += Core_NavigationCompleted;
            core.NewWindowRequested += Core_NewWindowRequested;
            core.PermissionRequested += Core_PermissionRequested;
            core.WebResourceResponseReceived += Core_WebResourceResponseReceived;
            core.ProcessFailed += Core_ProcessFailed;
            _isReady = true;
            core.Navigate(NavigationPolicy.AppUrl);
        }
        catch (Exception exception)
        {
            _isReady = false;
            SetNativeState(NativeAppState.WebViewInitializationFailed);
            Debug.WriteLine(exception.GetType().Name);
        }
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var uri = TryParseUri(e.Uri);
        if (!NavigationPolicy.IsAllowed(uri))
        {
            e.Cancel = true;
            OpenExternalNavigationAsync(uri);
            return;
        }

            SetNativeState(NativeStatePolicy.OnNavigationStarting(uri));
    }

    private void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            SetNativeState(NativeStatePolicy.OnNavigationCompleted(true, false));
        }
        else
        {
            SetNativeState(NativeStatePolicy.OnNavigationCompleted(false, IsOfflineError(e.WebErrorStatus)));
        }

        UpdateNavigationButtons();
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        var uri = TryParseUri(e.Uri);
        if (NavigationPolicy.IsAllowed(uri))
        {
            Browser.CoreWebView2.Navigate(uri!.AbsoluteUri);
        }
        else
        {
            OpenExternalNavigationAsync(uri);
        }
    }

    private static void Core_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Deny;
        e.Handled = true;
    }

    private void Core_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        SetNativeState(NativeAppState.ProcessFailed);
    }

    private async void Core_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        var uri = TryParseUri(e.Request.Uri);
        var method = e.Request.Method;
        var statusCode = e.Response.StatusCode;

        if (ProfilePayloadParser.IsSessionInvalidatingPost(uri, method))
        {
            if (statusCode is >= 200 and < 300)
            {
                _profileResponses.Invalidate();
                ClearProfile();
            }

            return;
        }

        if (!ProfilePayloadParser.IsMeGet(uri, method) || (statusCode != 200 && statusCode != 401))
        {
            return;
        }

        var generation = _profileResponses.BeginResponse();
        if (statusCode == 401)
        {
            ClearProfile();
            return;
        }

        try
        {
            // Do not inspect response/request headers. The WebView2 profile owns cookies.
            using var content = await e.Response.GetContentAsync();
            using var contentStream = content.AsStream();
            var body = await ReadAtMostAsync(contentStream, ProfilePayloadParser.MaxResponseBytes);
            if (body is null || !_profileResponses.IsCurrent(generation))
            {
                return;
            }

            var snapshot = ProfilePayloadParser.Parse(statusCode, body);
            if (snapshot is null || !_profileResponses.IsCurrent(generation))
            {
                return;
            }

            if (ReferenceEquals(snapshot, ProfileSnapshot.Empty))
            {
                if (_profileResponses.IsCurrent(generation))
                {
                    ClearProfile();
                }

                return;
            }

            await ApplyProfileAsync(snapshot, generation);
        }
        catch
        {
            // A failed or oversized response is ignored. Never log response content.
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
            if (total > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private async Task ApplyProfileAsync(ProfileSnapshot snapshot, long generation)
    {
        if (!_profileResponses.IsCurrent(generation))
        {
            return;
        }

        BitmapImage? bitmap = null;
        if (snapshot.AvatarPng is { Length: > 0 } avatar)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(avatar.AsBuffer());
                stream.Seek(0);
                bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
            }
            catch
            {
                // The username remains available as the PersonPicture initial fallback.
            }
        }

        if (_profileResponses.IsCurrent(generation))
        {
            ProfilePicture.DisplayName = snapshot.Username ?? string.Empty;
            ProfilePicture.ProfilePicture = bitmap;
        }
    }

    private void ClearProfile()
    {
        ProfilePicture.DisplayName = string.Empty;
        ProfilePicture.ProfilePicture = null;
    }

    private async void OpenExternalNavigationAsync(Uri? uri)
    {
        var opened = await OpenExternalAsync(uri);
        if (opened)
        {
            SetStatusOnly(L("StatusExternalOpened"));
        }
        else
        {
            SetNativeState(NativeAppState.ExternalOpenFailed);
        }
    }

    private static async Task<bool> OpenExternalAsync(Uri? uri)
    {
        if (!NavigationPolicy.IsExternalLaunchable(uri)) return false;
        try
        {
            return await Launcher.LaunchUriAsync(uri!);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception.GetType().Name);
            return false;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReady && Browser.CoreWebView2.CanGoBack) Browser.CoreWebView2.GoBack();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReady && Browser.CoreWebView2.CanGoForward) Browser.CoreWebView2.GoForward();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReady) Browser.CoreWebView2.Reload();
    }

    private void UpdateNavigationButtons()
    {
        if (!_isReady) return;
        BackButton.IsEnabled = Browser.CoreWebView2.CanGoBack;
        ForwardButton.IsEnabled = Browser.CoreWebView2.CanGoForward;
    }

    private void SetNativeState(NativeAppState state)
    {
        _state = state;
        LoadingBar.IsIndeterminate = NativeStatePolicy.ShowsProgress(state);
        StatusText.Text = StateText(state);
        StatusIcon.Symbol = NativeStatePolicy.IsError(state) ? Symbol.Important : state == NativeAppState.Connected ? Symbol.Accept : Symbol.Sync;
        StateInfoBar.IsOpen = NativeStatePolicy.ShowsInfoBar(state);
        StateInfoBar.Severity = state == NativeAppState.Offline ? InfoBarSeverity.Warning : InfoBarSeverity.Error;
        StateInfoBar.Title = InfoTitle(state);
        StateInfoBar.Message = InfoMessage(state);
        if (_retryButton is not null)
        {
            _retryButton.Content = L("Retry");
            _retryButton.IsEnabled = NativeStatePolicy.IsRetryAllowed(new Uri(NavigationPolicy.AppUrl));
        }
        UpdateNavigationButtons();
    }

    private void SetStatusOnly(string status)
    {
        StateInfoBar.IsOpen = false;
        StatusText.Text = status;
        StatusIcon.Symbol = Symbol.Accept;
    }

    private string StateText(NativeAppState state) => L(state switch
    {
        NativeAppState.Initializing => "StatusInitializing",
        NativeAppState.Navigating => "StatusNavigating",
        NativeAppState.Connected => "StatusConnected",
        NativeAppState.Offline => "StatusOffline",
        NativeAppState.NavigationFailed => "StatusNavigationFailed",
        NativeAppState.WebViewInitializationFailed => "StatusWebViewInitializationFailed",
        NativeAppState.ProcessFailed => "StatusProcessFailed",
        NativeAppState.ExternalOpenFailed => "StatusExternalOpenFailed",
        _ => "StatusConnected"
    });

    private string InfoTitle(NativeAppState state) => L(state switch
    {
        NativeAppState.Offline => "InfoOfflineTitle",
        NativeAppState.NavigationFailed => "InfoNavigationFailedTitle",
        NativeAppState.WebViewInitializationFailed => "InfoWebViewInitializationFailedTitle",
        NativeAppState.ProcessFailed => "InfoProcessFailedTitle",
        NativeAppState.ExternalOpenFailed => "InfoExternalOpenFailedTitle",
        _ => "StatusConnected"
    });

    private string InfoMessage(NativeAppState state) => L(state switch
    {
        NativeAppState.Offline => "InfoOfflineMessage",
        NativeAppState.NavigationFailed => "InfoNavigationFailedMessage",
        NativeAppState.WebViewInitializationFailed => "InfoWebViewInitializationFailedMessage",
        NativeAppState.ProcessFailed => "InfoProcessFailedMessage",
        NativeAppState.ExternalOpenFailed => "InfoExternalOpenFailedMessage",
        _ => "StatusConnected"
    });

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        var retryUri = new Uri(NavigationPolicy.AppUrl);
        if (!NativeStatePolicy.IsRetryAllowed(retryUri))
        {
            return;
        }

        if (!_isReady)
        {
            _ = InitializeBrowserAsync();
            return;
        }

        SetNativeState(NativeAppState.Navigating);
        Browser.CoreWebView2.Navigate(retryUri.AbsoluteUri);
    }

    private static bool IsOfflineError(CoreWebView2WebErrorStatus status)
        => status is CoreWebView2WebErrorStatus.ConnectionAborted
            or CoreWebView2WebErrorStatus.ConnectionReset
            or CoreWebView2WebErrorStatus.Disconnected
            or CoreWebView2WebErrorStatus.HostNameNotResolved
            or CoreWebView2WebErrorStatus.Timeout
            or CoreWebView2WebErrorStatus.ServerUnreachable;

    private static Uri? TryParseUri(string? rawUri)
    {
        return Uri.TryCreate(rawUri, UriKind.Absolute, out var uri) ? uri : null;
    }
}
