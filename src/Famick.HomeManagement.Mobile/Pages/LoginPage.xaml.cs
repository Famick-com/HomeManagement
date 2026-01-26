using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiSettings _apiSettings;
    private readonly TokenStorage _tokenStorage;
    private readonly TenantStorage _tenantStorage;
    private readonly ShoppingApiClient _apiClient;
    private readonly OnboardingService _onboardingService;

    public LoginPage(
        ApiSettings apiSettings,
        TokenStorage tokenStorage,
        TenantStorage tenantStorage,
        ShoppingApiClient apiClient,
        OnboardingService onboardingService)
    {
        InitializeComponent();
        _apiSettings = apiSettings;
        _tokenStorage = tokenStorage;
        _tenantStorage = tenantStorage;
        _apiClient = apiClient;
        _onboardingService = onboardingService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Show tenant name if available
        var tenantName = _apiSettings.TenantName;
        if (!string.IsNullOrEmpty(tenantName))
        {
            TenantNameLabel.Text = tenantName;
            TenantFrame.IsVisible = true;
        }
        else
        {
            TenantFrame.IsVisible = false;
        }

        // Show server settings link for self-hosted users
        if (_apiSettings.Mode == ServerMode.SelfHosted)
        {
            ServerSettingsSection.IsVisible = true;
            ServerInfoLabel.Text = $"Server: {GetDisplayUrl(_apiSettings.SelfHostedUrl)}";
        }
        else
        {
            ServerSettingsSection.IsVisible = false;
        }
    }

    private static string GetDisplayUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            ShowError("Please enter your email");
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("Please enter your password");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            var result = await _apiClient.LoginAsync(EmailEntry.Text.Trim(), PasswordEntry.Text);

            if (result.Success && result.Data != null)
            {
                await _tokenStorage.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);

                // Get tenant name - try from login response first, then fetch separately
                var tenantName = result.Data.Tenant?.Name;
                if (string.IsNullOrEmpty(tenantName))
                {
                    var tenantResult = await _apiClient.GetTenantAsync();
                    if (tenantResult.Success && tenantResult.Data != null)
                    {
                        tenantName = tenantResult.Data.Name;
                    }
                }

                // Update tenant name in settings and storage
                if (!string.IsNullOrEmpty(tenantName))
                {
                    _apiSettings.TenantName = tenantName;
                }
                await _tenantStorage.SetTenantNameAsync(tenantName);

                // Mark onboarding as complete and server as configured
                _onboardingService.MarkOnboardingCompleted();
                _apiSettings.MarkServerConfigured();

                // Navigate to main app
                await Shell.Current.GoToAsync("//ListSelectionPage");
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Login failed. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnChangeServerTapped(object? sender, EventArgs e)
    {
        // Navigate to server configuration page
        await Navigation.PushAsync(
            Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<ServerConfigPage>());
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        LoginButton.IsEnabled = !isLoading;
        EmailEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void HideError()
    {
        ErrorLabel.IsVisible = false;
    }
}
