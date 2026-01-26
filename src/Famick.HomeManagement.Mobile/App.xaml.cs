using Famick.HomeManagement.Mobile.Pages;
using Famick.HomeManagement.Mobile.Pages.Onboarding;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile;

public partial class App : Application
{
    private readonly OnboardingService _onboardingService;
    private readonly TokenStorage _tokenStorage;
    private readonly ApiSettings _apiSettings;

    /// <summary>
    /// Pending deep link to process when the app is ready
    /// </summary>
    public static DeepLinkInfo? PendingDeepLink { get; set; }

    /// <summary>
    /// Pending verification token from email deep link
    /// </summary>
    public static string? PendingVerificationToken { get; set; }

    public App(OnboardingService onboardingService, TokenStorage tokenStorage, ApiSettings apiSettings)
    {
        InitializeComponent();
        _onboardingService = onboardingService;
        _tokenStorage = tokenStorage;
        _apiSettings = apiSettings;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Determine the initial state based on onboarding/authentication status
        var state = _onboardingService.GetCurrentState(_tokenStorage, _apiSettings);

        Page startPage = state switch
        {
            OnboardingState.Welcome => CreateOnboardingNavigationPage(),
            OnboardingState.EmailVerification => CreateEmailVerificationPage(),
            OnboardingState.Login => new AppShell(),
            OnboardingState.LoggedIn => new AppShell(),
            _ => CreateOnboardingNavigationPage()
        };

        var window = new Window(startPage);

        // Handle pending deep links after window is created
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(500); // Give page time to initialize

            // Handle verification token if present
            if (!string.IsNullOrEmpty(PendingVerificationToken))
            {
                await ProcessPendingVerificationTokenAsync();
            }
            // Handle shopping deep link if present
            else if (PendingDeepLink != null)
            {
                await ProcessPendingDeepLinkAsync();
            }
        });

        return window;
    }

    private NavigationPage CreateOnboardingNavigationPage()
    {
        var services = Handler?.MauiContext?.Services;
        if (services == null)
        {
            // Fallback - create with properly configured dependencies
            var handler = new DynamicApiHttpHandler(_apiSettings);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_apiSettings.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            return new NavigationPage(new WelcomePage(
                new ShoppingApiClient(httpClient, new TokenStorage()),
                new OnboardingService()));
        }

        var welcomePage = services.GetRequiredService<WelcomePage>();
        return new NavigationPage(welcomePage);
    }

    private NavigationPage CreateEmailVerificationPage()
    {
        var email = _onboardingService.GetPendingVerificationEmail() ?? "";
        var householdName = ""; // TODO: Store household name in preferences if needed

        var services = Handler?.MauiContext?.Services;
        if (services == null)
        {
            return CreateOnboardingNavigationPage();
        }

        var apiClient = services.GetRequiredService<ShoppingApiClient>();
        var verificationPage = new EmailVerificationPage(apiClient, _onboardingService, email, householdName);
        return new NavigationPage(verificationPage);
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Check for pending deep links when app resumes
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (!string.IsNullOrEmpty(PendingVerificationToken))
            {
                await ProcessPendingVerificationTokenAsync();
            }
            else if (PendingDeepLink != null)
            {
                await ProcessPendingDeepLinkAsync();
            }
        });
    }

    private static async Task ProcessPendingDeepLinkAsync()
    {
        if (PendingDeepLink == null) return;

        var deepLink = PendingDeepLink;
        PendingDeepLink = null; // Clear to avoid re-processing

        try
        {
            // Navigate to the shopping session page with the list ID
            var navigationParameter = new Dictionary<string, object>
            {
                { "ListId", deepLink.ListId.ToString() },
                { "ListName", deepLink.ListName }
            };

            await Shell.Current.GoToAsync(nameof(ShoppingSessionPage), navigationParameter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process deep link: {ex.Message}");
        }
    }

    private async Task ProcessPendingVerificationTokenAsync()
    {
        if (string.IsNullOrEmpty(PendingVerificationToken)) return;

        var token = PendingVerificationToken;
        PendingVerificationToken = null; // Clear to avoid re-processing

        try
        {
            // Store the token for the verification page to use
            _onboardingService.SetPendingVerification(
                _onboardingService.GetPendingVerificationEmail() ?? "",
                token);

            // If we're on the email verification page, it will pick up the token
            // Otherwise, navigate to it
            if (Current?.MainPage is NavigationPage navPage)
            {
                if (navPage.CurrentPage is EmailVerificationPage verificationPage)
                {
                    // Page will handle it in OnAppearing
                    verificationPage.HandleVerificationToken(token);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process verification token: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles deep link from iOS/Android
    /// </summary>
    public static void HandleDeepLink(Uri uri)
    {
        if (uri == null) return;

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        // Handle verification deep link: famick://verify?token=...
        if (uri.Host == "verify" || uri.AbsolutePath.Contains("verify"))
        {
            var token = query["token"];
            if (!string.IsNullOrEmpty(token))
            {
                PendingVerificationToken = token;

                // If the app is already running, process immediately
                if (Current?.MainPage != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (Current is App app)
                        {
                            await app.ProcessPendingVerificationTokenAsync();
                        }
                    });
                }
            }
            return;
        }

        // Handle shopping deep link: famickshopping://shopping/session?ListId={guid}&ListName={name}
        var listId = query["ListId"];
        var listName = query["ListName"];

        if (!string.IsNullOrEmpty(listId) && Guid.TryParse(listId, out var parsedListId))
        {
            PendingDeepLink = new DeepLinkInfo
            {
                ListId = parsedListId,
                ListName = listName ?? "Shopping"
            };

            // If the app is already running, process immediately
            if (Current?.MainPage != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessPendingDeepLinkAsync();
                });
            }
        }
    }

    /// <summary>
    /// Transitions from onboarding to the main app shell
    /// </summary>
    public static void TransitionToMainApp()
    {
        Console.WriteLine("[App.TransitionToMainApp] Called");
        if (Current == null)
        {
            Console.WriteLine("[App.TransitionToMainApp] Current is null, returning");
            return;
        }

        Console.WriteLine("[App.TransitionToMainApp] Scheduling MainPage change on main thread");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                Console.WriteLine("[App.TransitionToMainApp] Setting MainPage to AppShell");
                Current.MainPage = new AppShell();
                Console.WriteLine("[App.TransitionToMainApp] MainPage set successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App.TransitionToMainApp] Error: {ex.Message}");
                Console.WriteLine($"[App.TransitionToMainApp] Stack: {ex.StackTrace}");
            }
        });
    }
}

/// <summary>
/// Information about a deep link to process
/// </summary>
public class DeepLinkInfo
{
    public Guid ListId { get; set; }
    public string ListName { get; set; } = string.Empty;
}
