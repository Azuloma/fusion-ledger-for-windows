namespace FusionLedger.Windows;

public enum NativePage
{
    Dashboard,
    Projects,
    CommitHistory,
    ServerMaintenance,
    AppSettings,
    VersionInfo,
    ProfileSettings,
    Administration
}

public static class NativePageCatalog
{
    public static bool IsNested(NativePage page) => page is NativePage.ProfileSettings or NativePage.Administration;

    public static bool CanOpenMaintenance(AuthenticatedUser user) =>
        string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase)
        && string.Equals(user.Status, "approved", StringComparison.OrdinalIgnoreCase);

    public static string SearchKey(NativePage page) => page switch
    {
        NativePage.Dashboard => "Dashboard",
        NativePage.Projects => "Projects",
        NativePage.CommitHistory => "Commit history",
        NativePage.ServerMaintenance => "Server maintenance",
        NativePage.AppSettings => "App settings",
        NativePage.VersionInfo => "Version info",
        NativePage.ProfileSettings => "Profile settings",
        NativePage.Administration => "Administration",
        _ => string.Empty
    };
}
