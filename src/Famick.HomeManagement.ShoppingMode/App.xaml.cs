using Famick.HomeManagement.ShoppingMode.Pages;

namespace Famick.HomeManagement.ShoppingMode;

public partial class App : Application
{
    /// <summary>
    /// Pending deep link to process when the app is ready
    /// </summary>
    public static DeepLinkInfo? PendingDeepLink { get; set; }

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Handle pending deep link after window is created
        if (PendingDeepLink != null)
        {
            // Defer navigation to allow the shell to initialize
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500); // Give shell time to initialize
                await ProcessPendingDeepLinkAsync();
            });
        }

        return window;
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Check for pending deep link when app resumes
        if (PendingDeepLink != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ProcessPendingDeepLinkAsync();
            });
        }
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

    /// <summary>
    /// Handles deep link from iOS
    /// </summary>
    public static void HandleDeepLink(Uri uri)
    {
        if (uri == null) return;

        // Parse the deep link: famickshopping://shopping/session?ListId={guid}&ListName={name}
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
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
}

/// <summary>
/// Information about a deep link to process
/// </summary>
public class DeepLinkInfo
{
    public Guid ListId { get; set; }
    public string ListName { get; set; } = string.Empty;
}
