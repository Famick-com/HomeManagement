using Famick.HomeManagement.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers;

/// <summary>
/// Base class for all API controllers with common functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ITenantProvider _tenantProvider;
    protected readonly ILogger _logger;

    protected ApiControllerBase(ITenantProvider tenantProvider, ILogger logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    protected Guid TenantId
    {
        get
        {
            if (!_tenantProvider.TenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context not set");
            }
            return _tenantProvider.TenantId.Value;
        }
    }

    /// <summary>
    /// Returns a successful API response with data
    /// </summary>
    protected IActionResult ApiResponse<T>(T data, bool cache = false)
    {
        if (!cache)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
        }

        return Ok(data);
    }

    /// <summary>
    /// Returns an empty successful API response (204 No Content)
    /// </summary>
    protected IActionResult EmptyApiResponse()
    {
        return NoContent();
    }

    /// <summary>
    /// Returns a generic error response
    /// </summary>
    protected IActionResult ErrorResponse(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new { error_message = message });
    }

    /// <summary>
    /// Returns a validation error response
    /// </summary>
    protected IActionResult ValidationErrorResponse(Dictionary<string, string[]> errors)
    {
        return BadRequest(new
        {
            error_message = "Validation failed",
            errors
        });
    }

    /// <summary>
    /// Returns a not found error response
    /// </summary>
    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        return NotFound(new { error_message = message });
    }

    /// <summary>
    /// Returns an unauthorized error response
    /// </summary>
    protected IActionResult UnauthorizedResponse(string message = "Unauthorized")
    {
        return Unauthorized(new { error_message = message });
    }

    /// <summary>
    /// Returns a forbidden error response
    /// </summary>
    protected IActionResult ForbiddenResponse(string message = "Forbidden")
    {
        return StatusCode(403, new { error_message = message });
    }
}
