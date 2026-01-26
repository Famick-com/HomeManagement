using Foundation;
using UIKit;
using Microsoft.Maui.Platform;

namespace Famick.HomeManagement.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);

        // Ensure the main window background is white to match the app theme and cover the safe area
        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0)) // Available iOS 13+
        {
            foreach (var scene in application.ConnectedScenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    var window = windowScene.Windows.FirstOrDefault();
                    if (window != null)
                    {
                        // Use SystemBackground for proper light/dark mode support
                        window.BackgroundColor = UIColor.SystemBackground;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handle URL scheme deep links (iOS 9+)
    /// </summary>
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        if (url != null && url.Scheme == "famickshopping")
        {
            var uri = new Uri(url.AbsoluteString ?? string.Empty);
            App.HandleDeepLink(uri);
            return true;
        }

        return base.OpenUrl(application, url, options);
    }
}
