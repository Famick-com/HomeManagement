using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages.Onboarding;

public partial class CreatePasswordPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private readonly OnboardingService _onboardingService;
    private readonly ApiSettings _apiSettings;
    private readonly TokenStorage _tokenStorage;
    private readonly string _verificationToken;
    private readonly string _email;
    private readonly string _householdName;

    public CreatePasswordPage(
        ShoppingApiClient apiClient,
        OnboardingService onboardingService,
        ApiSettings apiSettings,
        TokenStorage tokenStorage,
        string verificationToken,
        string email,
        string householdName)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _onboardingService = onboardingService;
        _apiSettings = apiSettings;
        _tokenStorage = tokenStorage;
        _verificationToken = verificationToken;
        _email = email;
        _householdName = householdName;

        // Set the title to the household name
        Title = string.IsNullOrEmpty(householdName) ? "Create Account" : householdName;

        SubtitleLabel.Text = $"Creating account for {_householdName}";
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        // Validate inputs
        var firstName = FirstNameEntry.Text?.Trim();
        var lastName = LastNameEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(firstName))
        {
            ShowError("Please enter your first name");
            return;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            ShowError("Please enter your last name");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter a password");
            return;
        }

        if (password.Length < 8)
        {
            ShowError("Password must be at least 8 characters");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Passwords do not match");
            return;
        }

        SetLoading(true);
        HideError();

        try
        {
            var result = await _apiClient.CompleteRegistrationAsync(
                _verificationToken,
                firstName,
                lastName,
                password);

            Console.WriteLine($"[CreatePasswordPage] Result - Success: {result.Success}, Data null: {result.Data == null}");

            if (result.Success && result.Data != null)
            {
                Console.WriteLine("[CreatePasswordPage] Saving tokens...");
                // Save tokens
                await _tokenStorage.SetTokensAsync(
                    result.Data.AccessToken,
                    result.Data.RefreshToken);

                Console.WriteLine("[CreatePasswordPage] Configuring for cloud...");
                // Configure for cloud with tenant name
                _apiSettings.ConfigureForCloud(result.Data.Tenant?.Name ?? _householdName);

                Console.WriteLine("[CreatePasswordPage] Clearing onboarding state...");
                // Clear onboarding state
                _onboardingService.ClearPendingVerification();
                _onboardingService.MarkOnboardingCompleted();

                Console.WriteLine("[CreatePasswordPage] Transitioning to main app...");
                // Transition from onboarding NavigationPage to main AppShell
                App.TransitionToMainApp();
                Console.WriteLine("[CreatePasswordPage] TransitionToMainApp called");
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Registration failed. Please try again.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
        CreateAccountButton.IsEnabled = !isLoading;
        FirstNameEntry.IsEnabled = !isLoading;
        LastNameEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
        ConfirmPasswordEntry.IsEnabled = !isLoading;
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
