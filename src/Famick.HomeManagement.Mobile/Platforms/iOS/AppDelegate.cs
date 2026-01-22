using Foundation;
using UIKit;

namespace Famick.HomeManagement.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Handle deep links when app is opened via URL scheme (cold start).
    /// </summary>
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (url != null)
        {
            var uri = url.ToString();
            System.Diagnostics.Debug.WriteLine($"Deep link received (iOS): {uri}");

            // Store the deep link URI for the app to process
            iOSDeepLinkManager.PendingDeepLink = uri;
        }

        return base.OpenUrl(app, url, options);
    }
}

/// <summary>
/// Simple static class to hold pending deep link between iOS and MAUI app.
/// </summary>
public static class iOSDeepLinkManager
{
    public static string? PendingDeepLink { get; set; }
}
