using Famick.HomeManagement.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Famick.HomeManagement.Mobile;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public static IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>
    /// Event fired when a deep link is received and processed.
    /// </summary>
    public static event EventHandler<DeepLinkEventArgs>? DeepLinkProcessed;

    public App()
    {
        InitializeComponent();

        // Add global exception handlers for debugging
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex?.Message}\n{ex?.StackTrace}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var page = Current?.Windows?.FirstOrDefault()?.Page;
                if (page != null)
                {
                    page.DisplayAlert(
                        "Unhandled Exception",
                        $"{ex?.GetType().Name}: {ex?.Message}",
                        "OK");
                }
            });
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception?.Message}\n{e.Exception?.StackTrace}");
            e.SetObserved();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Wrap MainPage in NavigationPage to enable modal navigation (required for barcode scanner)
        var mainPage = new MainPage();
        NavigationPage.SetHasNavigationBar(mainPage, false);
        var navPage = new NavigationPage(mainPage);

        var window = new Window(navPage)
        {
            Title = "Famick.HomeManagement.Mobile"
        };

        // Check for pending deep links after window is created
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Small delay to ensure services are ready
            await Task.Delay(500);
            CheckPendingDeepLinks();
        });

        return window;
    }

    /// <summary>
    /// Sets the service provider for the app.
    /// Called from MauiProgram after building the app.
    /// </summary>
    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Processes a deep link URI.
    /// </summary>
    public static void ProcessDeepLink(string uri)
    {
        if (_serviceProvider == null)
        {
            System.Diagnostics.Debug.WriteLine("Cannot process deep link: ServiceProvider not set");
            return;
        }

        var deepLinkHandler = _serviceProvider.GetService<DeepLinkHandler>();
        if (deepLinkHandler == null)
        {
            System.Diagnostics.Debug.WriteLine("Cannot process deep link: DeepLinkHandler not registered");
            return;
        }

        if (deepLinkHandler.HandleUri(uri))
        {
            System.Diagnostics.Debug.WriteLine($"Deep link processed: {uri}");
            DeepLinkProcessed?.Invoke(null, new DeepLinkEventArgs(
                deepLinkHandler.PendingServerUrl!,
                deepLinkHandler.PendingServerName));
        }
    }

    /// <summary>
    /// Checks for pending deep links from platform-specific code.
    /// </summary>
    private void CheckPendingDeepLinks()
    {
        string? pendingLink = null;

#if ANDROID
        pendingLink = DeepLinkManager.PendingDeepLink;
        DeepLinkManager.PendingDeepLink = null;
#elif IOS
        pendingLink = iOSDeepLinkManager.PendingDeepLink;
        iOSDeepLinkManager.PendingDeepLink = null;
#endif

        if (!string.IsNullOrEmpty(pendingLink))
        {
            System.Diagnostics.Debug.WriteLine($"Processing pending deep link: {pendingLink}");
            ProcessDeepLink(pendingLink);
        }
    }
}
