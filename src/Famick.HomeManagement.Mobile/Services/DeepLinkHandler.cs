using System.Web;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// Handles deep links for mobile app setup (famick:// scheme).
/// Parses URLs like: famick://setup?url=https://home.example.com&name=Home%20Server
/// </summary>
public class DeepLinkHandler
{
    private const string ServerNameKey = "server_name";

    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DeepLinkHandler> _logger;

    public DeepLinkHandler(ApiSettings apiSettings, ILogger<DeepLinkHandler> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the pending server URL from a deep link that hasn't been applied yet.
    /// This is used to show a confirmation before applying settings.
    /// </summary>
    public string? PendingServerUrl { get; private set; }

    /// <summary>
    /// Gets or sets the pending server name from a deep link that hasn't been applied yet.
    /// </summary>
    public string? PendingServerName { get; private set; }

    /// <summary>
    /// Event fired when a deep link is successfully processed and ready to apply.
    /// </summary>
    public event EventHandler<DeepLinkEventArgs>? DeepLinkReceived;

    /// <summary>
    /// Handles a URI and extracts setup parameters if it's a valid setup deep link.
    /// </summary>
    /// <param name="uri">The URI to handle</param>
    /// <returns>True if the URI was a valid setup link and was processed</returns>
    public bool HandleUri(Uri uri)
    {
        _logger.LogInformation("Handling deep link: {Uri}", uri);

        if (uri.Scheme.Equals("famick", StringComparison.OrdinalIgnoreCase) &&
            uri.Host.Equals("setup", StringComparison.OrdinalIgnoreCase))
        {
            return HandleSetupUri(uri);
        }

        _logger.LogWarning("Unrecognized deep link scheme or host: {Scheme}://{Host}", uri.Scheme, uri.Host);
        return false;
    }

    /// <summary>
    /// Handles a URI string and extracts setup parameters if it's a valid setup deep link.
    /// </summary>
    /// <param name="uriString">The URI string to handle</param>
    /// <returns>True if the URI was a valid setup link and was processed</returns>
    public bool HandleUri(string uriString)
    {
        if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return HandleUri(uri);
        }

        _logger.LogWarning("Failed to parse URI: {Uri}", uriString);
        return false;
    }

    /// <summary>
    /// Parses a setup URI and extracts the server URL and name.
    /// </summary>
    private bool HandleSetupUri(Uri uri)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            var serverUrl = query["url"];
            var serverName = query["name"];

            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogWarning("Deep link missing required 'url' parameter");
                return false;
            }

            // Validate the URL is well-formed
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var parsedUrl) ||
                (!parsedUrl.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                 !parsedUrl.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Deep link contains invalid server URL: {Url}", serverUrl);
                return false;
            }

            // Normalize the URL (remove trailing slash)
            serverUrl = serverUrl.TrimEnd('/');

            _logger.LogInformation("Parsed deep link - Server URL: {Url}, Name: {Name}", serverUrl, serverName);

            // Store pending values
            PendingServerUrl = serverUrl;
            PendingServerName = serverName;

            // Notify listeners
            DeepLinkReceived?.Invoke(this, new DeepLinkEventArgs(serverUrl, serverName));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing setup deep link");
            return false;
        }
    }

    /// <summary>
    /// Applies the pending server configuration from the last received deep link.
    /// </summary>
    /// <returns>True if settings were applied successfully</returns>
    public bool ApplyPendingSettings()
    {
        if (string.IsNullOrEmpty(PendingServerUrl))
        {
            _logger.LogWarning("No pending server URL to apply");
            return false;
        }

        _apiSettings.BaseUrl = PendingServerUrl;

        // Store server name if provided
        if (!string.IsNullOrEmpty(PendingServerName))
        {
            Preferences.Default.Set(ServerNameKey, PendingServerName);
        }

        _logger.LogInformation("Applied server settings - URL: {Url}, Name: {Name}",
            PendingServerUrl, PendingServerName);

        // Clear pending values
        ClearPendingSettings();

        return true;
    }

    /// <summary>
    /// Clears any pending settings without applying them.
    /// </summary>
    public void ClearPendingSettings()
    {
        PendingServerUrl = null;
        PendingServerName = null;
    }

    /// <summary>
    /// Gets the configured server name, if any.
    /// </summary>
    public string? GetServerName()
    {
        return Preferences.Default.Get<string?>(ServerNameKey, null);
    }
}

/// <summary>
/// Event arguments for deep link events.
/// </summary>
public class DeepLinkEventArgs : EventArgs
{
    public string ServerUrl { get; }
    public string? ServerName { get; }

    public DeepLinkEventArgs(string serverUrl, string? serverName)
    {
        ServerUrl = serverUrl;
        ServerName = serverName;
    }
}
