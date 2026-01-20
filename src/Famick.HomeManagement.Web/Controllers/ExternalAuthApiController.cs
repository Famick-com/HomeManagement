using System.Security.Claims;
using Famick.HomeManagement.Core.DTOs.Authentication;
using Famick.HomeManagement.Core.DTOs.ExternalAuth;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers;

/// <summary>
/// API controller for external authentication providers (Google, Apple, OIDC)
/// </summary>
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthApiController : ControllerBase
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly ILogger<ExternalAuthApiController> _logger;

    public ExternalAuthApiController(
        IExternalAuthService externalAuthService,
        ILogger<ExternalAuthApiController> logger)
    {
        _externalAuthService = externalAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of enabled external authentication providers
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ExternalAuthProviderDto>), 200)]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await _externalAuthService.GetEnabledProvidersAsync(cancellationToken);
        return Ok(providers);
    }

    /// <summary>
    /// Gets the OAuth authorization URL for a provider
    /// </summary>
    /// <param name="provider">Provider name (Google, Apple, OIDC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{provider}/challenge")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalAuthChallengeResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetChallenge(
        string provider,
        CancellationToken cancellationToken)
    {
        try
        {
            var redirectUri = GetCallbackUri(provider);
            var response = await _externalAuthService.GetAuthorizationUrlAsync(provider, redirectUri, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error_message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OAuth challenge for {Provider}", provider);
            return StatusCode(500, new { error_message = "Failed to generate authorization URL" });
        }
    }

    /// <summary>
    /// Processes the OAuth callback and returns tokens
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="request">Callback request with code and state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{provider}/callback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ProcessCallback(
        string provider,
        [FromBody] ExternalAuthCallbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { error_message = "Authorization code is required" });
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            return BadRequest(new { error_message = "State parameter is required" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceInfo = HttpContext.Request.Headers.UserAgent.ToString();
        var redirectUri = GetCallbackUri(provider);

        try
        {
            var response = await _externalAuthService.ProcessCallbackAsync(
                provider, request, redirectUri, ipAddress, deviceInfo, cancellationToken);
            return Ok(response);
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning("Invalid OAuth callback for {Provider}: {Message}", provider, ex.Message);
            return Unauthorized(new { error_message = ex.Message });
        }
        catch (AccountInactiveException)
        {
            return StatusCode(403, new { error_message = "Account is inactive" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback for {Provider}", provider);
            return StatusCode(500, new { error_message = "Authentication failed. Please try again." });
        }
    }

    /// <summary>
    /// Gets the OAuth authorization URL for linking a provider to the current user's account
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="request">Challenge request with callback URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{provider}/link")]
    [Authorize]
    [ProducesResponseType(typeof(ExternalAuthChallengeResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetLinkChallenge(
        string provider,
        [FromBody] ExternalAuthLinkChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error_message = "User ID not found in token" });
        }

        try
        {
            // Use the provided callback URL or generate one
            var redirectUri = !string.IsNullOrWhiteSpace(request.CallbackUrl)
                ? request.CallbackUrl
                : GetCallbackUri(provider);

            // Generate authorization URL with link context
            var response = await _externalAuthService.GetLinkAuthorizationUrlAsync(
                userId.Value, provider, redirectUri, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error_message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OAuth link challenge for {Provider}", provider);
            return StatusCode(500, new { error_message = "Failed to generate authorization URL" });
        }
    }

    /// <summary>
    /// Verifies OAuth callback and links an external provider to the current user's account
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="request">Link request with code and state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{provider}/link/verify")]
    [Authorize]
    [ProducesResponseType(typeof(LinkedAccountDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> VerifyLinkProvider(
        string provider,
        [FromBody] ExternalAuthLinkRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error_message = "User ID not found in token" });
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
        {
            return BadRequest(new { error_message = "Code and state are required" });
        }

        var redirectUri = GetCallbackUri(provider);

        try
        {
            var result = await _externalAuthService.LinkProviderAsync(
                userId.Value, provider, request, redirectUri, cancellationToken);
            return Ok(result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new { error_message = ex.Message });
        }
        catch (InvalidCredentialsException ex)
        {
            return BadRequest(new { error_message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking {Provider} to user {UserId}", provider, userId);
            return StatusCode(500, new { error_message = "Failed to link account" });
        }
    }

    /// <summary>
    /// Unlinks an external provider from the current user's account
    /// </summary>
    /// <param name="provider">Provider name to unlink</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{provider}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnlinkProvider(
        string provider,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error_message = "User ID not found in token" });
        }

        try
        {
            await _externalAuthService.UnlinkProviderAsync(userId.Value, provider, cancellationToken);
            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return NotFound(new { error_message = $"{provider} is not linked to your account" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error_message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking {Provider} from user {UserId}", provider, userId);
            return StatusCode(500, new { error_message = "Failed to unlink account" });
        }
    }

    /// <summary>
    /// Gets the list of linked external accounts for the current user
    /// </summary>
    [HttpGet("linked")]
    [Authorize]
    [ProducesResponseType(typeof(List<LinkedAccountDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetLinkedAccounts(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error_message = "User ID not found in token" });
        }

        try
        {
            var accounts = await _externalAuthService.GetLinkedAccountsAsync(userId.Value, cancellationToken);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting linked accounts for user {UserId}", userId);
            return StatusCode(500, new { error_message = "Failed to get linked accounts" });
        }
    }

    /// <summary>
    /// Gets the current user ID from the JWT claims
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the callback URI for the specified provider
    /// </summary>
    private string GetCallbackUri(string provider)
    {
        return $"{Request.Scheme}://{Request.Host}/auth/external/callback/{provider.ToLower()}";
    }
}
