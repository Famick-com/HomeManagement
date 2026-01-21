using System.Security.Claims;
using Famick.HomeManagement.Core.Configuration;
using Famick.HomeManagement.Core.DTOs.Authentication;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Famick.HomeManagement.Web.Controllers;

/// <summary>
/// API controller for authentication operations
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ISetupService _setupService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly ExternalAuthSettings _externalAuthSettings;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        IAuthenticationService authService,
        ISetupService setupService,
        IPasswordResetService passwordResetService,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IOptions<ExternalAuthSettings> externalAuthSettings,
        ILogger<AuthApiController> logger)
    {
        _authService = authService;
        _setupService = setupService;
        _passwordResetService = passwordResetService;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _externalAuthSettings = externalAuthSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration response with user ID and tokens</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Check if password authentication is enabled
        if (!_externalAuthSettings.PasswordAuthEnabled)
        {
            _logger.LogWarning("Password registration attempt blocked - password authentication is disabled");
            return StatusCode(403, new { error_message = "Password registration is disabled. Please use an external provider." });
        }

        // Check if registration is allowed (only when no users exist)
        var hasUsers = await _setupService.HasUsersAsync(cancellationToken);
        if (hasUsers)
        {
            _logger.LogWarning("Registration attempt blocked - users already exist");
            return StatusCode(403, new { error_message = "Registration is closed. The system has already been set up." });
        }

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { error_message = "All fields are required" });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { error_message = "Passwords do not match" });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { error_message = "Password must be at least 8 characters" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        try
        {
            var response = await _authService.RegisterAsync(request, ipAddress, deviceInfo, autoLogin: true, cancellationToken);
            return StatusCode(201, response);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new { error_message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { error_message = "Registration failed. Please try again." });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with access and refresh tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Check if password authentication is enabled
        if (!_externalAuthSettings.PasswordAuthEnabled)
        {
            _logger.LogWarning("Password login attempt blocked - password authentication is disabled");
            return StatusCode(403, new { error_message = "Password authentication is disabled. Please use an external provider." });
        }

        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error_message = "Validation failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        try
        {
            var response = await _authService.LoginAsync(request, ipAddress, deviceInfo, cancellationToken);
            return Ok(response);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { error_message = "Invalid email or password" });
        }
        catch (AccountInactiveException)
        {
            return StatusCode(403, new { error_message = "Account is inactive" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error_message = "Login failed. Please try again." });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error_message = "Refresh token is required" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        try
        {
            var response = await _authService.RefreshTokenAsync(request, ipAddress, deviceInfo, cancellationToken);
            return Ok(response);
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new { error_message = ex.Message });
        }
        catch (AccountInactiveException)
        {
            return StatusCode(403, new { error_message = "Account is inactive" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error_message = "Token refresh failed" });
        }
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return NoContent();
        }

        try
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return NoContent(); // Still return success even if revocation fails
        }
    }

    /// <summary>
    /// Logout from all devices (revoke all refresh tokens)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error_message = "User ID not found in token" });
        }

        try
        {
            await _authService.RevokeAllUserTokensAsync(userId.Value, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout-all for user {UserId}", userId);
            return StatusCode(500, new { error_message = "Logout failed" });
        }
    }

    /// <summary>
    /// Request a password reset email
    /// </summary>
    /// <param name="request">Email address for password reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response (always returns success to prevent email enumeration)</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ForgotPasswordResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        // Check if password authentication is enabled
        if (!_externalAuthSettings.PasswordAuthEnabled)
        {
            return StatusCode(403, new { error_message = "Password authentication is disabled." });
        }

        var validationResult = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error_message = "Validation failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var response = await _passwordResetService.RequestPasswordResetAsync(
            request, ipAddress, baseUrl, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Validate a password reset token
    /// </summary>
    /// <param name="token">The reset token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with email if valid</returns>
    [HttpGet("validate-reset-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ValidateResetTokenResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ValidateResetToken(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { error_message = "Token is required" });
        }

        var response = await _passwordResetService.ValidateResetTokenAsync(token, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Reset password using a valid token
    /// </summary>
    /// <param name="request">Reset password request with token and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResetPasswordResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        // Check if password authentication is enabled
        if (!_externalAuthSettings.PasswordAuthEnabled)
        {
            return StatusCode(403, new { error_message = "Password authentication is disabled." });
        }

        var validationResult = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error_message = "Validation failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var response = await _passwordResetService.ResetPasswordAsync(request, ipAddress, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(new { error_message = response.Message });
        }

        return Ok(response);
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
}
