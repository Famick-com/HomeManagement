using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Famick.HomeManagement.Mobile.Platforms.Android;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "famickshopping",
    DataHost = "shopping")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent?.Data != null)
        {
            HandleDeepLink(intent);
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Handle deep link if app was opened via deep link
        if (Intent?.Data != null)
        {
            HandleDeepLink(Intent);
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        // Handle Google Sign-In result
        GoogleSignInService.HandleActivityResult(requestCode, resultCode, data);
    }

    private void HandleDeepLink(Intent intent)
    {
        var uri = intent.Data;
        if (uri == null) return;

        // Parse the deep link: famickshopping://shopping/session?ListId={guid}&ListName={name}
        var path = uri.Path;
        var listId = uri.GetQueryParameter("ListId");
        var listName = uri.GetQueryParameter("ListName");

        if (!string.IsNullOrEmpty(listId) && Guid.TryParse(listId, out var parsedListId))
        {
            // Navigate to the shopping session page
            // The App.xaml.cs will handle this via MessagingCenter or a static property
            App.PendingDeepLink = new DeepLinkInfo
            {
                ListId = parsedListId,
                ListName = listName ?? "Shopping"
            };
        }
    }
}
