using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// Custom HTTP handler that:
/// 1. Reads the base URL dynamically from ApiSettings for each request
/// 2. Bypasses SSL certificate validation in DEBUG mode for local development
/// </summary>
public class DynamicApiHttpHandler : HttpClientHandler
{
    private readonly ApiSettings _apiSettings;

    public DynamicApiHttpHandler(ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;

#if DEBUG
        // Bypass SSL certificate validation for local development
        ServerCertificateCustomValidationCallback = BypassCertificateValidation;
#endif
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If the request has a relative URI, combine it with the current base URL
        if (request.RequestUri != null && !request.RequestUri.IsAbsoluteUri)
        {
            var baseUrl = _apiSettings.BaseUrl.TrimEnd('/');
            var relativeUrl = request.RequestUri.OriginalString.TrimStart('/');
            request.RequestUri = new Uri($"{baseUrl}/{relativeUrl}");
        }

        return await base.SendAsync(request, cancellationToken);
    }

#if DEBUG
    private static bool BypassCertificateValidation(
        HttpRequestMessage message,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        // In DEBUG mode, accept all certificates for local development
        // This allows self-signed localhost certificates to work
        return true;
    }
#endif
}
