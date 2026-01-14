using Famick.HomeManagement.ShoppingMode.Pages;
using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation (LoginPage is shown modally, not via Shell routing)
        Routing.RegisterRoute(nameof(ServerConfigPage), typeof(ServerConfigPage));
        Routing.RegisterRoute(nameof(ShoppingSessionPage), typeof(ShoppingSessionPage));
        Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
        Routing.RegisterRoute(nameof(BarcodeScannerPage), typeof(BarcodeScannerPage));
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var version = AppInfo.VersionString;
        var build = AppInfo.BuildString;

        await DisplayAlert(
            "About Famick Shopping",
            $"Version {version} (Build {build})\n\n" +
            "A companion app for managing your shopping lists.\n\n" +
            "Famick.com",
            "OK");
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;

        var confirm = await DisplayAlert("Sign Out", "Are you sure you want to sign out?", "Yes", "Cancel");
        if (!confirm) return;

        var tokenStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TokenStorage>();
        if (tokenStorage != null)
        {
            await tokenStorage.ClearTokensAsync();
        }

        var loginPage = Application.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
        if (loginPage != null)
        {
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
        }
    }
}
