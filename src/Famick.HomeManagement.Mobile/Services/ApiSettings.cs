using Famick.HomeManagement.UI.Services;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// Manages API settings including the configurable base URL.
/// </summary>
public class ApiSettings : IServerSettings
{
    private const string BaseUrlKey = "api_base_url";

    /// <summary>
    /// Gets the default base URL based on platform.
    /// iOS Simulator: localhost works (shares Mac network stack)
    /// Android Emulator: 10.0.2.2 is special alias to host machine
    /// </summary>
    private static string DefaultBaseUrl
    {
        get
        {
#if ANDROID
            // Android emulator uses 10.0.2.2 to reach host's localhost
            return "https://10.0.2.2:5001";
#else
            // iOS simulator and other platforms can use localhost
            return "https://localhost:5001";
#endif
        }
    }

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
