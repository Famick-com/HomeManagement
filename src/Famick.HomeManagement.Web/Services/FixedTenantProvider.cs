using Famick.HomeManagement.Core.Interfaces;

namespace Famick.HomeManagement.Web.Services;

/// <summary>
/// Tenant provider for self-hosted mode with a fixed tenant ID
/// </summary>
public class FixedTenantProvider : ITenantProvider
{
    private readonly Guid _tenantId;

    public FixedTenantProvider(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public Guid? TenantId => _tenantId;

    public void SetTenantId(Guid tenantId)
    {
        // No-op in self-hosted mode - tenant ID is fixed
    }

    public void ClearTenantId()
    {
        // No-op in self-hosted mode - tenant ID is fixed
    }
}
