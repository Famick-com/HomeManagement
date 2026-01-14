using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiSettings _apiSettings;
    private readonly TokenStorage _tokenStorage;
    private readonly ShoppingApiClient _apiClient;
    private ServerMode _selectedMode = ServerMode.Cloud;

    public LoginPage(ApiSettings apiSettings, TokenStorage tokenStorage, ShoppingApiClient apiClient)
    {
        InitializeComponent();
        _apiSettings = apiSettings;
        _tokenStorage = tokenStorage;
        _apiClient = apiClient;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Restore last server mode
        _selectedMode = _apiSettings.Mode;
        UpdateServerModeUI();

        // Restore self-hosted URL if configured
        if (_selectedMode == ServerMode.SelfHosted)
        {
            ServerUrlEntry.Text = _apiSettings.SelfHostedUrl;
        }
    }

    private void OnCloudSelected(object? sender, EventArgs e)
    {
        _selectedMode = ServerMode.Cloud;
        UpdateServerModeUI();
    }

    private void OnSelfHostedSelected(object? sender, EventArgs e)
    {
        _selectedMode = ServerMode.SelfHosted;
        UpdateServerModeUI();
    }

    private void UpdateServerModeUI()
    {
        var selectedColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#1565C0")
            : Color.FromArgb("#1976D2");
        var unselectedColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#424242")
            : Color.FromArgb("#E0E0E0");

        CloudButton.BackgroundColor = _selectedMode == ServerMode.Cloud ? selectedColor : unselectedColor;
        CloudButton.TextColor = _selectedMode == ServerMode.Cloud ? Colors.White : Colors.Black;

        SelfHostedButton.BackgroundColor = _selectedMode == ServerMode.SelfHosted ? selectedColor : unselectedColor;
        SelfHostedButton.TextColor = _selectedMode == ServerMode.SelfHosted ? Colors.White : Colors.Black;

        SelfHostedUrlSection.IsVisible = _selectedMode == ServerMode.SelfHosted;
    }

    private async void OnTestConnection(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ServerUrlEntry.Text))
        {
            ShowConnectionStatus("Please enter a server URL", false);
            return;
        }

        TestConnectionButton.IsEnabled = false;
        ShowConnectionStatus("Testing connection...", null);

        try
        {
            // Temporarily set the URL to test
            _apiSettings.Mode = ServerMode.SelfHosted;
            _apiSettings.SelfHostedUrl = ServerUrlEntry.Text.Trim();

            var isHealthy = await _apiClient.CheckHealthAsync();
            ShowConnectionStatus(isHealthy ? "Connection successful!" : "Server not reachable", isHealthy);
        }
        catch (Exception ex)
        {
            ShowConnectionStatus($"Error: {ex.Message}", false);
        }
        finally
        {
            TestConnectionButton.IsEnabled = true;
        }
    }

    private void ShowConnectionStatus(string message, bool? success)
    {
        ConnectionStatusLabel.Text = message;
        ConnectionStatusLabel.IsVisible = true;
        ConnectionStatusLabel.TextColor = success switch
        {
            true => Colors.Green,
            false => Colors.Red,
            null => Colors.Gray
        };
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

        if (_selectedMode == ServerMode.SelfHosted && string.IsNullOrWhiteSpace(ServerUrlEntry.Text))
        {
            ShowError("Please enter a server URL");
            return;
        }

        // Save settings
        _apiSettings.Mode = _selectedMode;
        if (_selectedMode == ServerMode.SelfHosted)
        {
            _apiSettings.SelfHostedUrl = ServerUrlEntry.Text!.Trim();
        }

        SetLoading(true);
        HideError();

        try
        {
            var result = await _apiClient.LoginAsync(EmailEntry.Text.Trim(), PasswordEntry.Text);

            if (result.Success && result.Data != null)
            {
                await _tokenStorage.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
                // Pop the modal to return to ListSelectionPage
                await Navigation.PopModalAsync();
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

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        LoginButton.IsEnabled = !isLoading;
        EmailEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
        CloudButton.IsEnabled = !isLoading;
        SelfHostedButton.IsEnabled = !isLoading;
        ServerUrlEntry.IsEnabled = !isLoading;
        TestConnectionButton.IsEnabled = !isLoading;
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
