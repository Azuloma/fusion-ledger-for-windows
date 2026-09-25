using Microsoft.UI.Xaml;

namespace FusionLedger.Windows;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static Window? LoginWindow { get; private set; }
    private bool _transitioning;
    private bool _signingOut;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args) => ShowLoginWindow();

    private LoginWindow ShowLoginWindow(bool resetSession = false)
    {
        var login = new LoginWindow(resetSession);
        LoginWindow = login;
        login.Authenticated += Login_Authenticated;
        login.SessionResetSucceeded += Login_SessionResetSucceeded;
        login.Closed += Login_Closed;
        login.Activate();
        return login;
    }

    private void Login_Authenticated(object? sender, AuthenticatedUser user)
    {
        if (_transitioning || (_signingOut && MainWindow is not null)) return;
        _transitioning = true;

        // Create and activate the authenticated window before closing the last login window.
        var authenticatedWindow = new MainWindow(user);
        MainWindow = authenticatedWindow;
        authenticatedWindow.Activate();

        var login = LoginWindow;
        LoginWindow = null;
        login?.Close();
        _signingOut = false;
        _transitioning = false;
    }

    private void Login_SessionResetSucceeded(object? sender, EventArgs args)
    {
        if (!_signingOut || MainWindow is null) return;

        // The replacement login window is already visible and has cleared its profile.
        // Only now remove the authenticated native window; there is never a zero-window gap.
        var main = MainWindow;
        MainWindow = null;
        main.Close();
        _signingOut = false;
    }

    private void Login_Closed(object sender, WindowEventArgs args)
    {
        if (_signingOut && MainWindow is not null)
        {
            _signingOut = false;
            return;
        }

        if (!_transitioning && MainWindow is null) Exit();
    }

    public static void RequestSignOut()
    {
        var app = (App)Current;
        if (app._signingOut || MainWindow is null) return;
        app._signingOut = true;
        // Keep MainWindow alive while the replacement LoginWindow initializes and clears
        // the same WebView2 profile. LoginWindow navigates only after that clear succeeds.
        app.ShowLoginWindow(resetSession: true);
    }
}
