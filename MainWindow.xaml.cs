using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;

namespace FusionLedger.Windows;

public sealed partial class MainWindow : Window
{
    private readonly AuthenticatedUser _user;
    private readonly ResourceLoader _strings = ResourceLoader.GetForViewIndependentUse();
    private readonly ObservableCollection<string> _searchSuggestions = [];
    private readonly Stack<NativePage> _history = new();
    private NativePage _currentPage = NativePage.Dashboard;

    public MainWindow(AuthenticatedUser user)
    {
        _user = user;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBar.IconSource = new ImageIconSource
        {
            ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/fusion-ledger_ico.png"))
        };

        PageSearchBox.ItemsSource = _searchSuggestions;
        ConfigureProfile();
        ConfigureAccelerators();
        ApplyLocalizedStrings();
        Navigation.SelectedItem = Navigation.MenuItems[0];
        NavigateTo(NativePage.Dashboard, false);
    }

    private string L(string key) => _strings.GetString(key);

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
        ToolTipService.SetToolTip(ProfileButton, L("Account"));
        AutomationProperties.SetName(NotificationsButton, L("Notifications"));
        AutomationProperties.SetName(ProfileButton, L("Account"));
        NotificationsHeader.Text = L("Notifications");
        NotificationsEmpty.Text = L("NotConnected");
        AccountName.Text = _user.Username;
        AccountStatus.Text = string.Format(L("AccountStatusFormat"), LocalizedValue("Status", _user.Status));
        AccountRole.Text = string.Format(L("AccountRoleFormat"), LocalizedValue("Role", _user.Role));
        foreach (var item in Navigation.MenuItems.OfType<NavigationViewItem>().Concat(Navigation.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (TryParsePage(item.Tag as string, out var page)) item.Content = L("Page_" + NativePageCatalog.SearchKey(page));
        }
        AdministrationButton.Content = L("Administration");
        ProfileSettingsButton.Content = L("ProfileSettings");
        SignOutButton.Content = L("SignOut");
        AutomationProperties.SetName(ProfileSettingsButton, L("ProfileSettings"));
        AutomationProperties.SetName(AdministrationButton, L("Administration"));
        AutomationProperties.SetName(SignOutButton, L("SignOut"));
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
        AddAccelerator(global::Windows.System.VirtualKey.K, global::Windows.System.VirtualKeyModifiers.Control, (_, e) =>
        {
            PageSearchBox.Focus(FocusState.Keyboard);
            e.Handled = true;
        });
        // Windows.System.VirtualKey does not name OEM comma; 0xBC is VK_OEM_COMMA (Ctrl+,).
        AddAccelerator((global::Windows.System.VirtualKey)0xBC, global::Windows.System.VirtualKeyModifiers.Control, (_, e) =>
        {
            NavigateTo(NativePage.AppSettings);
            e.Handled = true;
        });
        AddAccelerator(global::Windows.System.VirtualKey.N, global::Windows.System.VirtualKeyModifiers.Menu, (_, e) =>
        {
            NotificationsFlyout.ShowAt(NotificationsButton);
            e.Handled = true;
        });
        AddAccelerator(global::Windows.System.VirtualKey.Left, global::Windows.System.VirtualKeyModifiers.Menu, (_, e) =>
        {
            GoBack();
            e.Handled = true;
        });
    }

    private void AddAccelerator(global::Windows.System.VirtualKey key, global::Windows.System.VirtualKeyModifiers modifiers,
        global::Windows.Foundation.TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += handler;
        RootGrid.KeyboardAccelerators.Add(accelerator);
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        Navigation.IsPaneOpen = !Navigation.IsPaneOpen;
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args) => GoBack();

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && TryParsePage(item.Tag as string, out var page))
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
        var panel = new StackPanel { Spacing = 12, Padding = new Thickness(32) };
        panel.Children.Add(new TextBlock { Text = L("Page_" + NativePageCatalog.SearchKey(page)), Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] });
        panel.Children.Add(new TextBlock { Text = L("NotConnected"), TextWrapping = TextWrapping.Wrap });
        if (page == NativePage.VersionInfo)
        {
            var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.4.0";
            panel.Children.Add(new TextBlock { Text = string.Format(L("VersionFormat"), $"v{version}") });
        }
        return panel;
    }

    private void SyncNavigationSelection(NativePage page)
    {
        foreach (var item in Navigation.MenuItems.OfType<NavigationViewItem>().Concat(Navigation.FooterMenuItems.OfType<NavigationViewItem>()))
        {
            if (TryParsePage(item.Tag as string, out var itemPage) && itemPage == page)
            {
                Navigation.SelectedItem = item;
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

    private void NotificationsButton_Click(object sender, RoutedEventArgs e) => NotificationsFlyout.ShowAt(NotificationsButton);
    private void ProfileButton_Click(object sender, RoutedEventArgs e) => AccountFlyout.ShowAt(ProfileButton);
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
