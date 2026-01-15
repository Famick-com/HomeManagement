using Famick.HomeManagement.ShoppingMode.Pages;
using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode;

public partial class AppShell : Shell
{
    private string _appTitle = "Shopping";

    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation (LoginPage is shown modally, not via Shell routing)
        Routing.RegisterRoute(nameof(ServerConfigPage), typeof(ServerConfigPage));
        Routing.RegisterRoute(nameof(ShoppingSessionPage), typeof(ShoppingSessionPage));
        Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
        Routing.RegisterRoute(nameof(BarcodeScannerPage), typeof(BarcodeScannerPage));

        // Load tenant name and update title
        _ = LoadTenantNameAsync();
    }

    private async Task LoadTenantNameAsync()
    {
        try
        {
            // Wait for MauiContext to be available (may not be ready in constructor)
            TenantStorage? tenantStorage = null;
            for (int i = 0; i < 10 && tenantStorage == null; i++)
            {
                tenantStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TenantStorage>();
                if (tenantStorage == null)
                {
                    await Task.Delay(100);
                }
            }

            if (tenantStorage != null)
            {
                _appTitle = await tenantStorage.GetAppTitleAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Title = _appTitle;
                });
            }
        }
        catch
        {
            // Ignore errors loading tenant name
        }
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var version = AppInfo.VersionString;
        var build = AppInfo.BuildString;

        // Get tenant name for About dialog
        var tenantName = "Shopping";
        try
        {
            var tenantStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TenantStorage>();
            if (tenantStorage != null)
            {
                var name = await tenantStorage.GetTenantNameAsync();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    tenantName = name;
                }
            }
        }
        catch
        {
            // Use default
        }

        await DisplayAlertAsync(
            $"About {tenantName} Shopping",
            $"Version {version} (Build {build})\n\n" +
            "A companion app for managing your shopping lists.\n\n" +
            $"{tenantName}",
            "OK");
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var confirm = await DisplayAlertAsync("Sign Out", "Are you sure you want to sign out?", "Yes", "Cancel");
        if (!confirm) return;

        var tokenStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TokenStorage>();
        if (tokenStorage != null)
        {
            await tokenStorage.ClearTokensAsync();
        }

        // Clear tenant name on sign out
        var tenantStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TenantStorage>();
        tenantStorage?.Clear();

        // Reset title to default
        _appTitle = "Shopping";
        Title = _appTitle;

        var loginPage = Application.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
        if (loginPage != null)
        {
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
        }
    }

    /// <summary>
    /// Refreshes the app title from tenant storage. Call this after login.
    /// </summary>
    public async Task RefreshTitleAsync()
    {
        await LoadTenantNameAsync();
    }
}
