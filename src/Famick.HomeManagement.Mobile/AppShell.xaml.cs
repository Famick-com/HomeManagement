using Famick.HomeManagement.Mobile.Pages;
using Famick.HomeManagement.Mobile.Pages.Wizard;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile;

public partial class AppShell : Shell
{
    private string _appTitle = "Famick Home";

    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation (LoginPage is shown modally, not via Shell routing)
        Routing.RegisterRoute(nameof(ServerConfigPage), typeof(ServerConfigPage));
        Routing.RegisterRoute(nameof(ShoppingSessionPage), typeof(ShoppingSessionPage));
        Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
        Routing.RegisterRoute(nameof(BarcodeScannerPage), typeof(BarcodeScannerPage));
        Routing.RegisterRoute(nameof(AisleOrderPage), typeof(AisleOrderPage));

        // Wizard routes
        Routing.RegisterRoute(nameof(WizardHouseholdInfoPage), typeof(WizardHouseholdInfoPage));
        Routing.RegisterRoute(nameof(WizardMembersPage), typeof(WizardMembersPage));
        Routing.RegisterRoute(nameof(WizardHomeStatsPage), typeof(WizardHomeStatsPage));
        Routing.RegisterRoute(nameof(WizardMaintenancePage), typeof(WizardMaintenancePage));
        Routing.RegisterRoute(nameof(WizardVehiclesPage), typeof(WizardVehiclesPage));
        Routing.RegisterRoute(nameof(WizardVehicleEditPage), typeof(WizardVehicleEditPage));

        // Load tenant name and update title
        _ = LoadTenantNameAsync();
    }

    private async Task LoadTenantNameAsync()
    {
        try
        {
            // Wait for MauiContext to be available (may not be ready in constructor)
            TenantStorage? tenantStorage = null;
            ApiSettings? apiSettings = null;
            for (int i = 0; i < 10 && tenantStorage == null; i++)
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;
                tenantStorage = services?.GetService<TenantStorage>();
                apiSettings = services?.GetService<ApiSettings>();
                if (tenantStorage == null)
                {
                    await Task.Delay(100);
                }
            }

            if (tenantStorage != null)
            {
                _appTitle = await tenantStorage.GetAppTitleAsync();
                var tenantName = apiSettings?.TenantName ?? await tenantStorage.GetTenantNameAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Title = _appTitle;

                    // Update household name in navigation bar title
                    if (!string.IsNullOrEmpty(tenantName))
                    {
                        TitleLabel.Text = tenantName;
                        HouseholdNameLabel.Text = tenantName;
                    }
                    else
                    {
                        TitleLabel.Text = "Famick Home";
                    }
                });
            }
        }
        catch
        {
            // Ignore errors loading tenant name
        }
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var settingsPage = services?.GetService<SettingsPage>();
        if (settingsPage != null)
        {
            await Current.Navigation.PushAsync(settingsPage);
        }
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var confirm = await DisplayAlertAsync("Sign Out", "Are you sure you want to sign out?", "Yes", "Cancel");
        if (!confirm) return;

        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services == null) return;

        // Clear tokens
        var tokenStorage = services.GetService<TokenStorage>();
        if (tokenStorage != null)
        {
            await tokenStorage.ClearTokensAsync();
        }

        // Clear tenant name display (but keep server configuration)
        var tenantStorage = services.GetService<TenantStorage>();
        tenantStorage?.Clear();

        // Reset title to default
        _appTitle = "Famick Home";
        Title = _appTitle;

        // Navigate to login page (server is still configured, so they can log back in)
        var loginPage = services.GetService<LoginPage>();
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
