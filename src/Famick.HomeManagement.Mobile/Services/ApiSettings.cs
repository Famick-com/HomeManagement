namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// Manages API settings including the configurable base URL.
/// </summary>
public class ApiSettings
{
    private const string BaseUrlKey = "api_base_url";
    private const string DefaultBaseUrl = "https://localhost:7258";

    /// <summary>
    /// Gets the configured API base URL.
    /// </summary>
    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, DefaultBaseUrl);
        set => Preferences.Default.Set(BaseUrlKey, value);
    }

    /// <summary>
    /// Checks if a custom API URL has been configured.
    /// </summary>
    public bool IsConfigured => Preferences.Default.ContainsKey(BaseUrlKey);

    /// <summary>
    /// Resets to the default base URL.
    /// </summary>
    public void Reset()
    {
        Preferences.Default.Remove(BaseUrlKey);
    }
}
