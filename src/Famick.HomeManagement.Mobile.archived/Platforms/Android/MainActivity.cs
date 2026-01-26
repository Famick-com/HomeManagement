using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Famick.HomeManagement.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "famick",
    DataHost = "setup")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Handle deep link if app was launched with one
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        // Handle deep link when app is already running
        if (intent != null)
        {
            HandleIntent(intent);
        }
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent?.Action != Intent.ActionView || intent.Data == null)
            return;

        var uri = intent.Data.ToString();
        if (!string.IsNullOrEmpty(uri))
        {
            System.Diagnostics.Debug.WriteLine($"Deep link received: {uri}");

            // Store the deep link URI for the app to process
            // The App class will retrieve and process this
            DeepLinkManager.PendingDeepLink = uri;
        }
    }
}

/// <summary>
/// Simple static class to hold pending deep link between Android activity and MAUI app.
/// </summary>
public static class DeepLinkManager
{
    public static string? PendingDeepLink { get; set; }
}
