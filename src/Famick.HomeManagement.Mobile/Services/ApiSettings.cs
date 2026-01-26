namespace Famick.HomeManagement.Mobile.Services;

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
    private const string TenantNameKey = "tenant_name";
    private const string ServerConfiguredKey = "server_configured";

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
#if DEBUG
    #if ANDROID
            // Android emulator uses 10.0.2.2 to reach host's localhost
            return "https://10.0.2.2:5001";
    #else
            // iOS simulator - use 127.0.0.1 instead of localhost for reliability
            return "https://127.0.0.1:5001";
    #endif
#else
    #if ANDROID
            return "https://10.0.2.2:5001";
    #else
            // Check if running on a real device vs simulator
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                // Physical device needs host IP and HTTP (avoid SSL cert issues)
                return "http://10.18.16.105:5002";
            }
            return "https://localhost:5001";
    #endif
#endif
        }
    }

    /// <summary>
    /// Gets or sets the current server mode.
    /// Default is SelfHosted for debug builds, Cloud for release builds.
    /// </summary>
    public ServerMode Mode
    {
        get
        {
#if DEBUG
            // In DEBUG mode, always use SelfHosted to connect to local dev server
            return ServerMode.SelfHosted;
#else
            var defaultMode = nameof(ServerMode.Cloud);
            var stored = Preferences.Default.Get(ServerModeKey, defaultMode);
            return Enum.TryParse<ServerMode>(stored, out var mode) ? mode : ServerMode.Cloud;
#endif
        }
        set
        {
#if !DEBUG
            Preferences.Default.Set(ServerModeKey, value.ToString());
#endif
        }
    }

    /// <summary>
    /// Gets or sets the self-hosted server URL.
    /// Only used when Mode is SelfHosted.
    /// </summary>
    public string SelfHostedUrl
    {
#if DEBUG
        // In DEBUG mode, always use the compile-time default to avoid stale cached URLs
        get => DefaultSelfHostedUrl;
        set { } // Ignore sets in DEBUG mode
#else
        get => Preferences.Default.Get(SelfHostedUrlKey, DefaultSelfHostedUrl);
        set => Preferences.Default.Set(SelfHostedUrlKey, value);
#endif
    }

    /// <summary>
    /// Gets the active API base URL based on current mode.
    /// </summary>
    public string BaseUrl
    {
        get
        {
            var mode = Mode;
            var url = mode == ServerMode.Cloud ? CloudUrl : SelfHostedUrl;
            Console.WriteLine($"[ApiSettings] BaseUrl called - Mode: {mode}, URL: {url}");
            return url;
        }
    }

    /// <summary>
    /// Checks if the app has been configured (server mode selected).
    /// </summary>
    public bool IsConfigured => Preferences.Default.ContainsKey(ServerModeKey);

    /// <summary>
    /// Gets or sets the tenant/household name (displayed on login page).
    /// </summary>
    public string? TenantName
    {
        get => Preferences.Default.Get<string?>(TenantNameKey, null);
        set
        {
            if (value != null)
                Preferences.Default.Set(TenantNameKey, value);
            else
                Preferences.Default.Remove(TenantNameKey);
        }
    }

    /// <summary>
    /// Checks if a server has been configured (via QR scan or previous login).
    /// This determines whether to show the login page or welcome page on first run.
    /// </summary>
    public bool HasServerConfigured()
    {
        return Preferences.Default.Get(ServerConfiguredKey, false);
    }

    /// <summary>
    /// Marks that a server has been configured (after QR scan or successful login).
    /// </summary>
    public void MarkServerConfigured()
    {
        Preferences.Default.Set(ServerConfiguredKey, true);
    }

    /// <summary>
    /// Configures the server from a QR code scan result.
    /// </summary>
    /// <param name="url">Server URL from QR code</param>
    /// <param name="tenantName">Tenant/household name from QR code</param>
    public void ConfigureFromQrCode(string url, string? tenantName)
    {
        Mode = ServerMode.SelfHosted;
        SelfHostedUrl = url;
        TenantName = tenantName;
        MarkServerConfigured();
    }

    /// <summary>
    /// Configures for cloud mode with tenant name.
    /// </summary>
    /// <param name="tenantName">Tenant/household name</param>
    public void ConfigureForCloud(string? tenantName)
    {
        Mode = ServerMode.Cloud;
        TenantName = tenantName;
        MarkServerConfigured();
    }

    /// <summary>
    /// Resets to default settings (Cloud mode).
    /// </summary>
    public void Reset()
    {
        Preferences.Default.Remove(ServerModeKey);
        Preferences.Default.Remove(SelfHostedUrlKey);
        Preferences.Default.Remove(TenantNameKey);
        Preferences.Default.Remove(ServerConfiguredKey);
    }
}
