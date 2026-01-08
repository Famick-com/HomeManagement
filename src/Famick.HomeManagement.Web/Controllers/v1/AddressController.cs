using Famick.HomeManagement.Core.DTOs.Common;
using Famick.HomeManagement.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for address operations including normalization/geocoding
/// </summary>
[ApiController]
[Route("api/v1/addresses")]
[Authorize]
public class AddressController : ApiControllerBase
{
    private readonly IAddressNormalizationService _addressService;
    private readonly IValidator<NormalizeAddressRequest> _normalizeValidator;

    public AddressController(
        IAddressNormalizationService addressService,
        IValidator<NormalizeAddressRequest> normalizeValidator,
        ITenantProvider tenantProvider,
        ILogger<AddressController> logger)
        : base(tenantProvider, logger)
    {
        _addressService = addressService;
        _normalizeValidator = normalizeValidator;
    }

    /// <summary>
    /// Normalizes and geocodes an address via Geoapify
    /// </summary>
    /// <remarks>
    /// Returns the normalized/verified address with latitude/longitude coordinates.
    /// The response includes a confidence score indicating match quality.
    /// Returns null if the address could not be found or verified.
    /// </remarks>
    [HttpPost("normalize")]
    [ProducesResponseType(typeof(NormalizedAddressResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Normalize(
        [FromBody] NormalizeAddressRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _normalizeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Normalizing address: {AddressLine1}, {City}, {StateProvince}",
            request.AddressLine1, request.City, request.StateProvince);

        var result = await _addressService.NormalizeAsync(request, cancellationToken);

        if (result == null)
        {
            return NotFoundResponse("Could not normalize address. Please verify the address is correct.");
        }

        return ApiResponse(result);
    }
}
