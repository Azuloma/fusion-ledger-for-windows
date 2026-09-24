namespace FusionLedger.Windows;

public enum NativeAppState
{
    Initializing,
    Navigating,
    Connected,
    Offline,
    NavigationFailed,
    WebViewInitializationFailed,
    ProcessFailed,
    ExternalOpenFailed
}

public static class NativeStatePolicy
{
    public static bool ShowsProgress(NativeAppState state)
        => state is NativeAppState.Initializing or NativeAppState.Navigating;

    public static bool ShowsInfoBar(NativeAppState state)
        => state is NativeAppState.Offline
            or NativeAppState.NavigationFailed
            or NativeAppState.WebViewInitializationFailed
            or NativeAppState.ProcessFailed
            or NativeAppState.ExternalOpenFailed;

    public static bool IsError(NativeAppState state)
        => state is NativeAppState.NavigationFailed
            or NativeAppState.WebViewInitializationFailed
            or NativeAppState.ProcessFailed
            or NativeAppState.ExternalOpenFailed;

    public static bool IsRetryAllowed(Uri? uri) => NavigationPolicy.IsAllowed(uri);

    public static NativeAppState OnNavigationStarting(Uri? uri)
        => NavigationPolicy.IsAllowed(uri) ? NativeAppState.Navigating : NativeAppState.ExternalOpenFailed;

    public static NativeAppState OnNavigationCompleted(bool success, bool offline)
        => success ? NativeAppState.Connected : offline ? NativeAppState.Offline : NativeAppState.NavigationFailed;
}
