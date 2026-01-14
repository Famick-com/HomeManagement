namespace Famick.HomeManagement.ShoppingMode.Services;

/// <summary>
/// Server mode selection - Cloud (famick.com) or Self-Hosted.
/// </summary>
public enum ServerMode
{
    Cloud,
    SelfHosted
}

/// <summary>
/// Manages API settings including server mode and base URL.
/// Supports both Cloud (app.famick.com) and self-hosted deployments.
/// </summary>
public class ApiSettings
{
    private const string ServerModeKey = "server_mode";
    private const string SelfHostedUrlKey = "self_hosted_url";

    /// <summary>
    /// Fixed cloud URL for Famick cloud service.
    /// </summary>
    public const string CloudUrl = "https://app.famick.com";

    /// <summary>
    /// Default self-hosted URL based on platform (for development).
    /// </summary>
    private static string DefaultSelfHostedUrl
    {
        get
        {
#if ANDROID
            // Android emulator uses 10.0.2.2 to reach host's localhost
            return "https://10.0.2.2:5001";
#else
            // Check if running on a real device vs simulator
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                // Physical device needs host IP and HTTP (avoid SSL cert issues)
                return "http://10.18.16.105:5002";
            }
            // iOS simulator can use localhost
            return "https://localhost:5001";
#endif
        }
    }

    /// <summary>
    /// Gets or sets the current server mode.
    /// </summary>
    public ServerMode Mode
    {
        get
        {
            var stored = Preferences.Default.Get(ServerModeKey, nameof(ServerMode.Cloud));
            return Enum.TryParse<ServerMode>(stored, out var mode) ? mode : ServerMode.Cloud;
        }
        set => Preferences.Default.Set(ServerModeKey, value.ToString());
    }

    /// <summary>
    /// Gets or sets the self-hosted server URL.
    /// Only used when Mode is SelfHosted.
    /// </summary>
    public string SelfHostedUrl
    {
        get => Preferences.Default.Get(SelfHostedUrlKey, DefaultSelfHostedUrl);
        set => Preferences.Default.Set(SelfHostedUrlKey, value);
    }

    /// <summary>
    /// Gets the active API base URL based on current mode.
    /// </summary>
    public string BaseUrl => Mode == ServerMode.Cloud ? CloudUrl : SelfHostedUrl;

    /// <summary>
    /// Checks if the app has been configured (server mode selected).
    /// </summary>
    public bool IsConfigured => Preferences.Default.ContainsKey(ServerModeKey);

    /// <summary>
    /// Resets to default settings (Cloud mode).
    /// </summary>
    public void Reset()
    {
        Preferences.Default.Remove(ServerModeKey);
        Preferences.Default.Remove(SelfHostedUrlKey);
    }
}
