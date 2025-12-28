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
    /// iOS Device: needs Mac's actual IP address
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
            // Check if running on a real device vs simulator
            // On device, we need the Mac's IP address and HTTP (not HTTPS)
            // since the device won't trust the dev certificate
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                // TODO: Replace with your Mac's IP address for device testing
                // Find it with: ipconfig getifaddr en0
                // Using HTTP port 5002 to avoid SSL certificate issues
                return "http://10.18.16.105:5002";
            }
            // iOS simulator can use localhost (shares Mac network stack)
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
