using Famick.HomeManagement.UI.Services;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI-based inventory session service using Preferences storage.
/// </summary>
public class MauiInventorySessionService : IInventorySessionService
{
    private const string SelectedLocationKey = "inventory_session_location";
    private const string LastQueryKey = "inventory_session_last_query";

    public Task<Guid?> GetSelectedLocationIdAsync()
    {
        var value = Preferences.Get(SelectedLocationKey, string.Empty);
        if (Guid.TryParse(value, out var locationId))
        {
            return Task.FromResult<Guid?>(locationId);
        }
        return Task.FromResult<Guid?>(null);
    }

    public Task SetSelectedLocationIdAsync(Guid locationId)
    {
        Preferences.Set(SelectedLocationKey, locationId.ToString());
        return Task.CompletedTask;
    }

    public Task ClearSelectedLocationAsync()
    {
        Preferences.Remove(SelectedLocationKey);
        return Task.CompletedTask;
    }

    public Task<string?> GetLastQueryAsync()
    {
        var value = Preferences.Get(LastQueryKey, string.Empty);
        return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
    }

    public Task SetLastQueryAsync(string query)
    {
        Preferences.Set(LastQueryKey, query);
        return Task.CompletedTask;
    }
}
