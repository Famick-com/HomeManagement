using Famick.HomeManagement.Mobile.Pages.Wizard;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        var version = AppInfo.VersionString;
        var build = AppInfo.BuildString;

        var tenantName = "";
        try
        {
            var tenantStorage = Application.Current?.Handler?.MauiContext?.Services.GetService<TenantStorage>();
            if (tenantStorage != null)
            {
                tenantName = await tenantStorage.GetTenantNameAsync() ?? "";
            }
        }
        catch { }

        var aboutTitle = string.IsNullOrEmpty(tenantName) ? "About Famick Home" : $"About {tenantName}";

        await DisplayAlertAsync(
            aboutTitle,
            $"Version {version} (Build {build})\n\n" +
            "Famick Home Management\n" +
            "A companion app for managing your home." +
            (string.IsNullOrEmpty(tenantName) ? "" : $"\n\nHousehold: {tenantName}"),
            "OK");
    }

    private async void OnRerunWizardClicked(object? sender, EventArgs e)
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        var onboardingService = services?.GetService<OnboardingService>();

        // Reset wizard completed flag so it can be re-run
        if (onboardingService != null)
        {
            Preferences.Default.Remove("home_setup_wizard_completed");
        }

        var wizardPage = services?.GetService<WizardHouseholdInfoPage>();
        if (wizardPage != null)
        {
            await Navigation.PushAsync(wizardPage);
        }
    }
}
