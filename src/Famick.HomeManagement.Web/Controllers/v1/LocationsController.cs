using Famick.HomeManagement.Core.DTOs.Locations;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for location lookups
/// </summary>
[ApiController]
[Route("api/v1/locations")]
[Authorize]
public class LocationsController : ApiControllerBase
{
    private readonly HomeManagementDbContext _dbContext;

    public LocationsController(
        HomeManagementDbContext dbContext,
        ITenantProvider tenantProvider,
        ILogger<LocationsController> logger)
        : base(tenantProvider, logger)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lists all locations for the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LocationDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing locations for tenant {TenantId}", TenantId);

        var locations = await _dbContext.Locations
            .Where(l => l.TenantId == TenantId)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                IsActive = l.IsActive,
                SortOrder = l.SortOrder
            })
            .ToListAsync(cancellationToken);

        return ApiResponse(locations);
    }
}
