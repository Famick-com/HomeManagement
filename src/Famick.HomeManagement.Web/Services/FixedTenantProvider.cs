using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Famick.HomeManagement.Core.Interfaces;

namespace Famick.HomeManagement.Web.Services;

/// <summary>
/// Tenant provider for self-hosted mode with a fixed tenant ID.
/// User ID is resolved from HttpContext claims.
/// </summary>
public class FixedTenantProvider : ITenantProvider
{
    private readonly Guid _tenantId;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FixedTenantProvider(Guid tenantId, IHttpContextAccessor httpContextAccessor)
    {
        _tenantId = tenantId;
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => _tenantId;

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return null;

            // Try multiple claim types - JWT "sub" can be mapped differently
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? user.FindFirst("sub")?.Value
                           ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public void SetTenantId(Guid tenantId)
    {
        // No-op in self-hosted mode - tenant ID is fixed
    }

    public void SetUserId(Guid userId)
    {
        // No-op - user ID is resolved from HttpContext claims
    }

    public void ClearTenantId()
    {
        // No-op in self-hosted mode - tenant ID is fixed
    }

    public void ClearUserId()
    {
        // No-op - user ID is resolved from HttpContext claims
    }
}
