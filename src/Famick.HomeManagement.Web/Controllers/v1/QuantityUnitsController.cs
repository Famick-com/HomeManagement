using Famick.HomeManagement.Core.DTOs.QuantityUnits;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for quantity unit lookups
/// </summary>
[ApiController]
[Route("api/v1/quantity-units")]
[Authorize]
public class QuantityUnitsController : ApiControllerBase
{
    private readonly HomeManagementDbContext _dbContext;

    public QuantityUnitsController(
        HomeManagementDbContext dbContext,
        ITenantProvider tenantProvider,
        ILogger<QuantityUnitsController> logger)
        : base(tenantProvider, logger)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lists all quantity units for the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<QuantityUnitDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing quantity units for tenant {TenantId}", TenantId);

        var units = await _dbContext.QuantityUnits
            .Where(u => u.TenantId == TenantId)
            .OrderBy(u => u.Name)
            .Select(u => new QuantityUnitDto
            {
                Id = u.Id,
                Name = u.Name,
                NamePlural = u.NamePlural,
                Description = u.Description,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return ApiResponse(units);
    }
}
