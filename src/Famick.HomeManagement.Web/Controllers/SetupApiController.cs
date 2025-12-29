using Famick.HomeManagement.Core.DTOs.Setup;
using Famick.HomeManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers;

/// <summary>
/// API controller for application setup operations
/// </summary>
[ApiController]
[Route("api/setup")]
public class SetupApiController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly ILogger<SetupApiController> _logger;

    public SetupApiController(
        ISetupService setupService,
        ILogger<SetupApiController> logger)
    {
        _setupService = setupService;
        _logger = logger;
    }

    /// <summary>
    /// Check if initial setup is required
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup status indicating if setup is needed</returns>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupStatusResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _setupService.GetSetupStatusAsync(cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking setup status");
            return StatusCode(500, new { error_message = "Failed to check setup status" });
        }
    }
}
