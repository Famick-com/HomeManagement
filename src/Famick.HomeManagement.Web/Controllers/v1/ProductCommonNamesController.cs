using Famick.HomeManagement.Core.DTOs.ProductCommonNames;
using Famick.HomeManagement.Core.DTOs.ProductGroups;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Web.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for managing product common names (generic product names for grouping)
/// </summary>
[ApiController]
[Route("api/v1/productcommonnames")]
[Authorize]
public class ProductCommonNamesController : ApiControllerBase
{
    private readonly IProductCommonNameService _productCommonNameService;
    private readonly IValidator<CreateProductCommonNameRequest> _createValidator;
    private readonly IValidator<UpdateProductCommonNameRequest> _updateValidator;

    public ProductCommonNamesController(
        IProductCommonNameService productCommonNameService,
        IValidator<CreateProductCommonNameRequest> createValidator,
        IValidator<UpdateProductCommonNameRequest> updateValidator,
        ITenantProvider tenantProvider,
        ILogger<ProductCommonNamesController> logger)
        : base(tenantProvider, logger)
    {
        _productCommonNameService = productCommonNameService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all product common names with optional filtering
    /// </summary>
    /// <param name="filter">Optional filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product common names</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProductCommonNameDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> List(
        [FromQuery] ProductCommonNameFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing product common names for tenant {TenantId}", TenantId);

        var productCommonNames = await _productCommonNameService.ListAsync(filter, cancellationToken);
        return ApiResponse(productCommonNames);
    }

    /// <summary>
    /// Gets a specific product common name by ID
    /// </summary>
    /// <param name="id">Product common name ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product common name details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductCommonNameDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product common name {ProductCommonNameId} for tenant {TenantId}", id, TenantId);

        var productCommonName = await _productCommonNameService.GetByIdAsync(id, cancellationToken);

        if (productCommonName == null)
        {
            return NotFoundResponse($"Product common name with ID {id} not found");
        }

        return ApiResponse(productCommonName);
    }

    /// <summary>
    /// Creates a new product common name
    /// </summary>
    /// <param name="request">Product common name creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product common name</returns>
    [HttpPost]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(ProductCommonNameDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommonNameRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Creating product common name '{Name}' for tenant {TenantId}", request.Name, TenantId);

        var productCommonName = await _productCommonNameService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = productCommonName.Id },
            productCommonName
        );
    }

    /// <summary>
    /// Updates an existing product common name
    /// </summary>
    /// <param name="id">Product common name ID</param>
    /// <param name="request">Product common name update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product common name</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(ProductCommonNameDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductCommonNameRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Updating product common name {ProductCommonNameId} for tenant {TenantId}", id, TenantId);

        var productCommonName = await _productCommonNameService.UpdateAsync(id, request, cancellationToken);
        return ApiResponse(productCommonName);
    }

    /// <summary>
    /// Deletes a product common name
    /// </summary>
    /// <param name="id">Product common name ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product common name {ProductCommonNameId} for tenant {TenantId}", id, TenantId);

        await _productCommonNameService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets all products with a specific common name
    /// </summary>
    /// <param name="id">Product common name ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with this common name</returns>
    [HttpGet("{id}/products")]
    [ProducesResponseType(typeof(List<ProductSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetProducts(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting products with common name {ProductCommonNameId} for tenant {TenantId}", id, TenantId);

        var products = await _productCommonNameService.GetProductsWithCommonNameAsync(id, cancellationToken);
        return ApiResponse(products);
    }
}
