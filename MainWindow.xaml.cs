using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace FusionLedger.Windows;

public sealed partial class MainWindow : Window
{
    private readonly AuthenticatedUser _user;
    private readonly ResourceLoader _strings = ResourceLoader.GetForViewIndependentUse();
    private readonly ObservableCollection<string> _searchSuggestions = [];
    private readonly Stack<NativePage> _history = new();
    private readonly ThemeService _themeService = new();
    private NativePage _currentPage = NativePage.Dashboard;
    private RadioButtons? _languageOptions;
    private RadioButtons? _themeOptions;
    private InfoBar? _settingsInfoBar;
    private bool _updatingSettings;
    private bool _syncingNavigationSelection;
    private bool _initialNavigationCompleted;

    public MainWindow(AuthenticatedUser user)
    {
        _user = user;
        InitializeComponent();
        Closed += MainWindow_Closed;
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureNativeTitleBar();
        WindowIcon.Apply(this);

        PageSearchBox.ItemsSource = _searchSuggestions;
        ConfigureProfile();
        ConfigureAccelerators();
        ApplyLocalizedStrings();
        RootGrid.ActualThemeChanged += (_, _) => { UpdateTitleBarIcon(); UpdateFlyoutBackdropTheme(); };
        ApplySavedTheme();
        UpdateTitleBarIcon();
        UpdateFlyoutBackdropTheme();
    }

    private void UpdateFlyoutBackdropTheme()
    {
        foreach (var flyout in new[] { AccountFlyout, NotificationsFlyout })
        {
            if (flyout.SystemBackdrop is ThinAcrylicBackdrop backdrop) backdrop.Theme = RootGrid.ActualTheme;
        }
    }

    private void UpdateTitleBarIcon()
    {
        AppTitleBar.IconSource = new ImageIconSource
        {
            ImageSource = new BitmapImage(new Uri(AppIconAssets.TitleBarImageUri(RootGrid.ActualTheme == ElementTheme.Light)))
        };
    }

    private string L(string key) => _strings.GetString(key);

    private void ConfigureNativeTitleBar()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new global::Windows.Graphics.SizeInt32(1464, 934));
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            }
        }
        catch
        {
            // Unpackaged or older host shells can omit AppWindow customization.
            // The standard WinUI TitleBar remains usable with its safe 48px layout.
        }
    }

    private void ApplySavedTheme()
    {
        _themeService.Apply(RootGrid, ReadTheme());
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args) => _themeService.Dispose();

    private void ApplyLocalizedStrings()
    {
        AppTitleBar.Title = L("ProductName");
        AppTitleBar.Subtitle = L("Beta");
        Title = L("MainWindowTitle");
        PageSearchBox.PlaceholderText = L("SearchPages");
        AutomationProperties.SetName(AppTitleBar, L("TitleBarName"));
        AutomationProperties.SetName(PageSearchBox, L("SearchPages"));
        AutomationProperties.SetName(Navigation, L("NavigationName"));
        ToolTipService.SetToolTip(NotificationsButton, L("Notifications"));
        var accountMenuName = string.Format(L("AccountMenuForFormat"), _user.Username);
        ToolTipService.SetToolTip(ProfileButton, accountMenuName);
        AutomationProperties.SetName(NotificationsButton, L("Notifications"));
        AutomationProperties.SetName(ProfileButton, accountMenuName);
        NotificationsHeader.Text = L("Notifications");
        NotificationsEmpty.Text = L("NotConnected");
        AccountName.Text = _user.Username;
        AccountStatus.Text = string.Format(L("AccountStatusFormat"), LocalizedValue("Status", _user.Status));
        AccountRole.Text = string.Format(L("AccountRoleFormat"), LocalizedValue("Role", _user.Role));
        foreach (var item in Navigation.MenuItems.OfType<NavigationViewItem>().Concat(Navigation.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (TryParsePage(item.Tag as string, out var page)) item.Content = L("Page_" + NativePageCatalog.SearchKey(page));
        }
        AdministrationLabel.Text = L("Administration");
        ProfileSettingsLabel.Text = L("ProfileSettings");
        SignOutLabel.Text = L("SignOut");
        AutomationProperties.SetName(ProfileSettingsButton, L("ProfileSettings"));
        AutomationProperties.SetName(AdministrationButton, L("Administration"));
        AutomationProperties.SetName(SignOutButton, L("SignOut"));
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySearchWidth(RootGrid.ActualWidth);
        if (_initialNavigationCompleted) return;
        // Let NavigationView finish its initial selection/layout before showing the first page.
        _initialNavigationCompleted = true;
        NavigateTo(NativePage.Dashboard, false);
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplySearchWidth(e.NewSize.Width);
    }

    private void ApplySearchWidth(double clientWidth)
    {
        PageSearchBox.Width = TitleBarLayoutPolicy.SearchWidthForClient(clientWidth);
        PageSearchBox.Visibility = Visibility.Visible;
    }

    private void ConfigureProfile()
    {
        AdministrationButton.Visibility = NativePageCatalog.CanOpenMaintenance(_user)
            ? Visibility.Visible : Visibility.Collapsed;
        MaintenanceItem.Visibility = NativePageCatalog.CanOpenMaintenance(_user)
            ? Visibility.Visible : Visibility.Collapsed;
        ProfilePicture.DisplayName = _user.Username;
        AccountPicture.DisplayName = _user.Username;
        if (_user.AvatarPng is { Length: > 0 } avatar)
        {
            _ = ApplyAvatarAsync(avatar);
        }
    }

    private async Task ApplyAvatarAsync(byte[] avatar)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(avatar.AsBuffer());
            stream.Seek(0);
            var image = new BitmapImage();
            await image.SetSourceAsync(stream);
            ProfilePicture.ProfilePicture = image;
            AccountPicture.ProfilePicture = image;
        }
        catch
        {
            // PersonPicture keeps the username initial fallback.
        }
    }

    private void ConfigureAccelerators()
    {
        // Windows.System.VirtualKey does not name OEM comma; 0xBC is VK_OEM_COMMA (Ctrl+,).
        AddAccelerator((global::Windows.System.VirtualKey)0xBC, global::Windows.System.VirtualKeyModifiers.Control, (_, e) =>
        {
            NavigateTo(NativePage.AppSettings);
            e.Handled = true;
        });
        AddAccelerator(global::Windows.System.VirtualKey.N, global::Windows.System.VirtualKeyModifiers.Menu, (_, e) =>
        {
            ShowHeaderFlyout(NotificationsFlyout, NotificationsButton);
            e.Handled = true;
        });
        AddAccelerator(global::Windows.System.VirtualKey.Left, global::Windows.System.VirtualKeyModifiers.Menu, (_, e) =>
        {
            GoBack();
            e.Handled = true;
        });
    }

    private void PageSearchKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        PageSearchBox.Focus(FocusState.Keyboard);
        args.Handled = true;
    }

    private void AddAccelerator(global::Windows.System.VirtualKey key, global::Windows.System.VirtualKeyModifiers modifiers,
        global::Windows.Foundation.TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += handler;
        RootGrid.KeyboardAccelerators.Add(accelerator);
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args) => GoBack();

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_syncingNavigationSelection
            && args.SelectedItem is NavigationViewItem item
            && TryParsePage(item.Tag as string, out var page))
        {
            NavigateTo(page);
        }
    }

    private void NavigateTo(NativePage page, bool remember = true)
    {
        if (remember && page != _currentPage) _history.Push(_currentPage);
        _currentPage = page;
        ContentFrame.Content = CreatePlaceholder(page);
        AppTitleBar.IsBackButtonVisible = NativePageCatalog.IsNested(page);
        AppTitleBar.IsBackButtonEnabled = _history.Count > 0;
        SyncNavigationSelection(page);
    }

    private void GoBack()
    {
        if (_history.Count == 0) return;
        var page = _history.Pop();
        NavigateTo(page, false);
    }

    private FrameworkElement CreatePlaceholder(NativePage page)
    {
        if (page == NativePage.AppSettings) return CreateSettingsPage();
        if (page == NativePage.VersionInfo) return CreateVersionInfoPage();

        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(32) };
        panel.Children.Add(new TextBlock { Text = L("Page_" + NativePageCatalog.SearchKey(page)), Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] });
        panel.Children.Add(new TextBlock { Text = L("NotConnected"), TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private FrameworkElement CreateSettingsPage()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { Spacing = 16, Padding = new Thickness(32) };
        panel.Children.Add(new TextBlock
        {
            Text = L("Page_App settings"),
            Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
        });
        panel.Children.Add(new TextBlock { Text = L("SettingsDescription"), TextWrapping = TextWrapping.Wrap });

        panel.Children.Add(new TextBlock { Text = L("LanguageHeader"), Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"] });
        _updatingSettings = true;
        _languageOptions = new RadioButtons();
        AutomationProperties.SetName(_languageOptions, L("LanguageHeader"));
        _languageOptions.Items.Add(new RadioButton { Content = L("LanguageEnglish"), Tag = "en-US" });
        _languageOptions.Items.Add(new RadioButton { Content = L("LanguageJapanese"), Tag = "ja-JP" });
        _languageOptions.SelectedItem = FindRadioButton(_languageOptions, ReadLanguage());
        _languageOptions.SelectionChanged += SettingsLanguage_SelectionChanged;
        panel.Children.Add(_languageOptions);

        panel.Children.Add(new TextBlock { Text = L("ThemeHeader"), Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"] });
        _themeOptions = new RadioButtons();
        AutomationProperties.SetName(_themeOptions, L("ThemeHeader"));
        _themeOptions.Items.Add(new RadioButton { Content = L("ThemeSystem"), Tag = "System" });
        _themeOptions.Items.Add(new RadioButton { Content = L("ThemeLight"), Tag = "Light" });
        _themeOptions.Items.Add(new RadioButton { Content = L("ThemeDark"), Tag = "Dark" });
        _themeOptions.SelectedItem = FindRadioButton(_themeOptions, ReadTheme());
        _themeOptions.SelectionChanged += SettingsTheme_SelectionChanged;
        panel.Children.Add(_themeOptions);
        _updatingSettings = false;

        _settingsInfoBar = new InfoBar { IsOpen = false, IsClosable = false };
        panel.Children.Add(_settingsInfoBar);
        scroll.Content = panel;
        return scroll;
    }

    private FrameworkElement CreateVersionInfoPage()
    {
        var info = RuntimeVersionInfo.GetSnapshot();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(32) };
        panel.Children.Add(new TextBlock { Text = L("Page_Version info"), Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] });
        panel.Children.Add(new TextBlock { Text = L("VersionInfoDescription"), TextWrapping = TextWrapping.Wrap });
        AddVersionRow(panel, "VersionLabel", info.DisplayVersion);
        AddVersionRow(panel, "BetaLabel", L("Beta"));
        AddVersionRow(panel, "FrameworkLabel", info.Framework);
        AddVersionRow(panel, "WindowsAppSdkLabel", info.WindowsAppSdkVersion);
        AddVersionRow(panel, "WebViewLabel", info.WebViewDescription);
        AddVersionRow(panel, "ArchitectureLabel", info.Architecture);
        AddVersionRow(panel, "PackageIdentityLabel", info.PackageIdentity);
        scroll.Content = panel;
        return scroll;
    }

    private void AddVersionRow(StackPanel panel, string labelKey, string value)
    {
        var row = new StackPanel { Spacing = 2 };
        row.Children.Add(new TextBlock { Text = L(labelKey), Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"] });
        row.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(row);
    }

    private static RadioButton? FindRadioButton(RadioButtons buttons, string tag) =>
        buttons.Items.OfType<RadioButton>().FirstOrDefault(button => string.Equals(button.Tag as string, tag, StringComparison.Ordinal));

    private string ReadLanguage()
    {
        try { return SettingsPolicy.NormalizeLanguage(ApplicationData.Current.LocalSettings.Values[SettingsPolicy.LanguageKey] as string); }
        catch { return SettingsPolicy.DefaultLanguage; }
    }

    private string ReadTheme()
    {
        return ThemeService.ReadSavedPreference();
    }

    private void SettingsLanguage_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_updatingSettings || _languageOptions?.SelectedItem is not RadioButton item) return;
        var language = SettingsPolicy.NormalizeLanguage(item.Tag as string);
        PersistSetting(SettingsPolicy.LanguageKey, language, restartRequired: true);
    }

    private void SettingsTheme_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_updatingSettings || _themeOptions?.SelectedItem is not RadioButton item) return;
        var theme = SettingsPolicy.NormalizeTheme(item.Tag as string);
        if (PersistSetting(SettingsPolicy.ThemeKey, theme, restartRequired: false)) ApplySavedTheme();
    }

    private bool PersistSetting(string key, string value, bool restartRequired)
    {
        var previous = key == SettingsPolicy.LanguageKey ? ReadLanguage() : ReadTheme();
        try
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
            if (restartRequired)
            {
                ShowSettingsInfo(L("SettingsRestartTitle"), L("SettingsRestartMessage"), InfoBarSeverity.Informational);
            }
            else
            {
                ShowSettingsInfo(L("SettingsThemeSavedTitle"), L("SettingsThemeSavedMessage"), InfoBarSeverity.Informational);
            }
            return true;
        }
        catch
        {
            RollbackSetting(key, previous);
            ShowSettingsInfo(L("SettingsSaveErrorTitle"), L("SettingsSaveErrorMessage"), InfoBarSeverity.Error);
            return false;
        }
    }

    private void RollbackSetting(string key, string value)
    {
        _updatingSettings = true;
        try
        {
            if (key == SettingsPolicy.LanguageKey && _languageOptions is not null)
                _languageOptions.SelectedItem = FindRadioButton(_languageOptions, value);
            else if (key == SettingsPolicy.ThemeKey && _themeOptions is not null)
                _themeOptions.SelectedItem = FindRadioButton(_themeOptions, value);
        }
        finally
        {
            _updatingSettings = false;
        }
    }

    private void ShowSettingsInfo(string title, string message, InfoBarSeverity severity)
    {
        if (_settingsInfoBar is null) return;
        _settingsInfoBar.Title = title;
        _settingsInfoBar.Message = message;
        _settingsInfoBar.Severity = severity;
        _settingsInfoBar.IsOpen = true;
    }

    private void SyncNavigationSelection(NativePage page)
    {
        foreach (var item in Navigation.MenuItems.OfType<NavigationViewItem>().Concat(Navigation.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (TryParsePage(item.Tag as string, out var itemPage) && itemPage == page)
            {
                _syncingNavigationSelection = true;
                try
                {
                    Navigation.SelectedItem = item;
                }
                finally
                {
                    _syncingNavigationSelection = false;
                }
                return;
            }
        }
    }

    private void PageSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var query = sender.Text.Trim();
        _searchSuggestions.Clear();
        if (query.Length == 0) return;
        foreach (var page in Enum.GetValues<NativePage>())
        {
            if (!IsPageAvailable(page)) continue;
            var name = NativePageCatalog.SearchKey(page);
            if (name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || LocalizedValue("Page", name).Contains(query, StringComparison.CurrentCultureIgnoreCase))
            {
                _searchSuggestions.Add(LocalizedValue("Page", name));
            }
        }
    }

    private void PageSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion as string ?? sender.Text.Trim();
        var page = Enum.GetValues<NativePage>().FirstOrDefault(candidate =>
        {
            if (!IsPageAvailable(candidate)) return false;
            var key = NativePageCatalog.SearchKey(candidate);
            return key.Equals(query, StringComparison.CurrentCultureIgnoreCase)
                || LocalizedValue("Page", key).Equals(query, StringComparison.CurrentCultureIgnoreCase);
        });
        if (NativePageCatalog.SearchKey(page).Equals(query, StringComparison.CurrentCultureIgnoreCase)
            || LocalizedValue("Page", NativePageCatalog.SearchKey(page)).Equals(query, StringComparison.CurrentCultureIgnoreCase))
        {
            NavigateTo(page);
            sender.Text = string.Empty;
        }
    }

    private void NotificationsButton_Click(object sender, RoutedEventArgs e) => ShowHeaderFlyout(NotificationsFlyout, NotificationsButton);
    private void ProfileButton_Click(object sender, RoutedEventArgs e) => ShowHeaderFlyout(AccountFlyout, ProfileButton);

    private void ShowHeaderFlyout(Flyout flyout, FrameworkElement target)
    {
        // Set before showing: the Button's own flyout opening reads the same placement.
        flyout.Placement = HeaderFlyoutOpensAbove(flyout, target)
            ? Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.TopEdgeAlignedRight
            : Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedRight;
        flyout.ShowAt(target);
    }

    private bool HeaderFlyoutOpensAbove(Flyout flyout, FrameworkElement target)
    {
        try
        {
            if (flyout.Content is not FrameworkElement content || target.XamlRoot is null) return false;
            content.Measure(new global::Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            // Before the first open a templated root may not measure yet; fall back to its maximum height.
            var height = content.DesiredSize.Height > 0 ? content.DesiredSize.Height : content.MaxHeight;
            if (double.IsInfinity(height) || height <= 0) return false;

            var scale = target.XamlRoot.RasterizationScale;
            var bounds = target.TransformToVisual(null).TransformBounds(new global::Windows.Foundation.Rect(0, 0, target.ActualWidth, target.ActualHeight));
            var hwnd = WindowNative.GetWindowHandle(this);
            var origin = new NativePoint();
            if (!ClientToScreen(hwnd, ref origin)) return false;
            var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
            var work = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            var spaceAbove = (origin.Y + bounds.Top * scale - work.Y) / scale;
            var spaceBelow = (work.Y + work.Height - (origin.Y + bounds.Bottom * scale)) / scale;
            return FlyoutPlacementPolicy.OpenAbove(height, spaceBelow, spaceAbove);
        }
        catch
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);

    private struct NativePoint
    {
        public int X;
        public int Y;
    }
    private void ProfileSettings_Click(object sender, RoutedEventArgs e) { AccountFlyout.Hide(); NavigateTo(NativePage.ProfileSettings); }
    private void Administration_Click(object sender, RoutedEventArgs e) { AccountFlyout.Hide(); if (NativePageCatalog.CanOpenMaintenance(_user)) NavigateTo(NativePage.Administration); }
    private void SignOut_Click(object sender, RoutedEventArgs e) { AccountFlyout.Hide(); App.RequestSignOut(); }

    private string LocalizedValue(string prefix, string value)
    {
        var localized = L(prefix + "_" + value);
        return string.IsNullOrEmpty(localized) ? value : localized;
    }

    private bool IsPageAvailable(NativePage page) =>
        page is not (NativePage.ServerMaintenance or NativePage.Administration)
        || NativePageCatalog.CanOpenMaintenance(_user);

    private static bool TryParsePage(string? tag, out NativePage page) =>
        Enum.TryParse(tag, ignoreCase: false, out page);
}
