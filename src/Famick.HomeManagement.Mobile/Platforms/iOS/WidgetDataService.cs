#if IOS
using Foundation;

namespace Famick.HomeManagement.Mobile.Platforms.iOS;

/// <summary>
/// Service for sharing data with iOS widgets via App Group shared container.
/// </summary>
public static class WidgetDataService
{
    private const string AppGroupId = "group.com.famick.homemanagement";

    /// <summary>
    /// Updates widget data in the shared App Group container.
    /// </summary>
    /// <param name="expiringCount">Number of expired items</param>
    /// <param name="dueSoonCount">Number of items expiring soon</param>
    public static void UpdateWidgetData(int expiringCount, int dueSoonCount)
    {
        try
        {
            var userDefaults = new NSUserDefaults(AppGroupId, NSUserDefaultsType.SuiteName);
            if (userDefaults == null)
            {
                Console.WriteLine("[WidgetDataService] Failed to access App Group container");
                return;
            }

            userDefaults.SetInt(expiringCount, "ExpiringItemCount");
            userDefaults.SetInt(dueSoonCount, "DueSoonItemCount");
            userDefaults.SetDouble(NSDate.Now.SecondsSince1970, "LastUpdated");
            userDefaults.Synchronize();

            Console.WriteLine($"[WidgetDataService] Updated widget data: Expiring={expiringCount}, DueSoon={dueSoonCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WidgetDataService] Error updating widget data: {ex.Message}");
        }
    }
}
#endif
