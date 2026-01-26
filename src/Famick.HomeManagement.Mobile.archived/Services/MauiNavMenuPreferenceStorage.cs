using Famick.HomeManagement.UI.Services;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI-based navigation menu preference storage using Preferences.
/// Stores which navigation groups are expanded as a comma-separated string.
/// </summary>
public class MauiNavMenuPreferenceStorage : INavMenuPreferenceStorage
{
    private const string ExpandedGroupsKey = "nav_expanded_groups";

    // Cache to avoid repeated Preferences reads for toggle operations
    private HashSet<string>? _cachedGroups;

    public Task<HashSet<string>> GetExpandedGroupsAsync()
    {
        if (_cachedGroups != null)
        {
            return Task.FromResult(_cachedGroups);
        }

        try
        {
            var value = Preferences.Default.Get<string?>(ExpandedGroupsKey, null);

            if (!string.IsNullOrEmpty(value))
            {
                var groups = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                _cachedGroups = new HashSet<string>(groups);
            }
            else
            {
                _cachedGroups = new HashSet<string>();
            }
        }
        catch
        {
            _cachedGroups = new HashSet<string>();
        }

        return Task.FromResult(_cachedGroups);
    }

    public Task SetExpandedGroupsAsync(HashSet<string> expandedGroups)
    {
        try
        {
            _cachedGroups = expandedGroups;
            var value = string.Join(",", expandedGroups);
            Preferences.Default.Set(ExpandedGroupsKey, value);
        }
        catch
        {
            // Handle storage exceptions
        }
        return Task.CompletedTask;
    }

    public async Task ToggleGroupAsync(string groupName, bool isExpanded)
    {
        var groups = await GetExpandedGroupsAsync();

        if (isExpanded)
        {
            groups.Add(groupName);
        }
        else
        {
            groups.Remove(groupName);
        }

        await SetExpandedGroupsAsync(groups);
    }
}
