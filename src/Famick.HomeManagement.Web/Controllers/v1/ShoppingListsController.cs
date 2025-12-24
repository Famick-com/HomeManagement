using Famick.HomeManagement.Core.DTOs.ShoppingLists;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Web.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for managing shopping lists and their items
/// </summary>
[ApiController]
[Route("api/v1/shoppinglists")]
[Authorize]
public class ShoppingListsController : ApiControllerBase
{
    private readonly IShoppingListService _shoppingListService;
    private readonly IValidator<CreateShoppingListRequest> _createListValidator;
    private readonly IValidator<UpdateShoppingListRequest> _updateListValidator;
    private readonly IValidator<AddShoppingListItemRequest> _addItemValidator;
    private readonly IValidator<UpdateShoppingListItemRequest> _updateItemValidator;

    public ShoppingListsController(
        IShoppingListService shoppingListService,
        IValidator<CreateShoppingListRequest> createListValidator,
        IValidator<UpdateShoppingListRequest> updateListValidator,
        IValidator<AddShoppingListItemRequest> addItemValidator,
        IValidator<UpdateShoppingListItemRequest> updateItemValidator,
        ITenantProvider tenantProvider,
        ILogger<ShoppingListsController> logger)
        : base(tenantProvider, logger)
    {
        _shoppingListService = shoppingListService;
        _createListValidator = createListValidator;
        _updateListValidator = updateListValidator;
        _addItemValidator = addItemValidator;
        _updateItemValidator = updateItemValidator;
    }

    #region List Management (CRUD)

    /// <summary>
    /// Lists all shopping lists
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shopping lists (summary view)</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ShoppingListSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing shopping lists for tenant {TenantId}", TenantId);

        var shoppingLists = await _shoppingListService.ListAllAsync(cancellationToken);
        return ApiResponse(shoppingLists);
    }

    /// <summary>
    /// Gets a specific shopping list by ID
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="includeItems">Include list items (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shopping list details with items</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShoppingListDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] bool includeItems = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting shopping list {ShoppingListId} for tenant {TenantId}", id, TenantId);

        var shoppingList = await _shoppingListService.GetListByIdAsync(id, includeItems, cancellationToken);

        if (shoppingList == null)
        {
            return NotFoundResponse($"Shopping list with ID {id} not found");
        }

        return ApiResponse(shoppingList);
    }

    /// <summary>
    /// Creates a new shopping list
    /// </summary>
    /// <param name="request">Shopping list creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created shopping list</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ShoppingListDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShoppingListRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createListValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Creating shopping list '{Name}' for tenant {TenantId}", request.Name, TenantId);

        var shoppingList = await _shoppingListService.CreateListAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = shoppingList.Id },
            shoppingList
        );
    }

    /// <summary>
    /// Updates an existing shopping list
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="request">Shopping list update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated shopping list</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ShoppingListDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateShoppingListRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateListValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Updating shopping list {ShoppingListId} for tenant {TenantId}", id, TenantId);

        var shoppingList = await _shoppingListService.UpdateListAsync(id, request, cancellationToken);
        return ApiResponse(shoppingList);
    }

    /// <summary>
    /// Deletes a shopping list (soft delete)
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting shopping list {ShoppingListId} for tenant {TenantId}", id, TenantId);

        await _shoppingListService.DeleteListAsync(id, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Item Management

    /// <summary>
    /// Adds a new item to a shopping list
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="request">Item data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created shopping list item</returns>
    [HttpPost("{id}/items")]
    [ProducesResponseType(typeof(ShoppingListItemDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddItem(
        Guid id,
        [FromBody] AddShoppingListItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _addItemValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Adding item to shopping list {ShoppingListId} for tenant {TenantId}", id, TenantId);

        var item = await _shoppingListService.AddItemAsync(id, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            item
        );
    }

    /// <summary>
    /// Updates an existing shopping list item
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="itemId">Shopping list item ID</param>
    /// <param name="request">Item update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated shopping list item</returns>
    [HttpPut("{id}/items/{itemId}")]
    [ProducesResponseType(typeof(ShoppingListItemDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateShoppingListItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateItemValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Updating item {ItemId} in shopping list {ShoppingListId} for tenant {TenantId}",
            itemId, id, TenantId);

        var item = await _shoppingListService.UpdateItemAsync(itemId, request, cancellationToken);
        return ApiResponse(item);
    }

    /// <summary>
    /// Removes an item from a shopping list
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="itemId">Shopping list item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}/items/{itemId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing item {ItemId} from shopping list {ShoppingListId} for tenant {TenantId}",
            itemId, id, TenantId);

        await _shoppingListService.RemoveItemAsync(itemId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks a shopping list item as purchased
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="itemId">Shopping list item ID</param>
    /// <param name="request">Purchase details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/items/{itemId}/purchase")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> MarkItemAsPurchased(
        Guid id,
        Guid itemId,
        [FromBody] MarkItemPurchasedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Marking item {ItemId} as purchased in shopping list {ShoppingListId} for tenant {TenantId}",
            itemId, id, TenantId);

        await _shoppingListService.MarkItemAsPurchasedAsync(itemId, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Clears all purchased items from a shopping list
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/clear-purchased")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ClearPurchasedItems(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing purchased items from shopping list {ShoppingListId} for tenant {TenantId}",
            id, TenantId);

        await _shoppingListService.ClearPurchasedItemsAsync(id, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Smart Features

    /// <summary>
    /// Gets product suggestions for a shopping list based on purchase history
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suggested products</returns>
    [HttpGet("{id}/suggestions")]
    [ProducesResponseType(typeof(List<ProductSuggestionDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSuggestions(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product suggestions for shopping list {ShoppingListId} for tenant {TenantId}",
            id, TenantId);

        var suggestions = await _shoppingListService.SuggestProductsAsync(id, cancellationToken);
        return ApiResponse(suggestions);
    }

    /// <summary>
    /// Groups shopping list items by shopping location for optimized shopping
    /// </summary>
    /// <param name="id">Shopping list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shopping list items grouped by location</returns>
    [HttpGet("{id}/by-location")]
    [ProducesResponseType(typeof(ShoppingListByLocationDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GroupByLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Grouping shopping list {ShoppingListId} items by location for tenant {TenantId}",
            id, TenantId);

        var groupedList = await _shoppingListService.GroupItemsByLocationAsync(id, cancellationToken);
        return ApiResponse(groupedList);
    }

    #endregion
}
