namespace Famick.HomeManagement.ShoppingMode.Models;

/// <summary>
/// Generic API response wrapper.
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Login response from the API.
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

/// <summary>
/// Summary of a shopping list for list selection.
/// </summary>
public class ShoppingListSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid ShoppingLocationId { get; set; }
    public string? ShoppingLocationName { get; set; }
    public bool HasStoreIntegration { get; set; }
    public int TotalItems { get; set; }
    public int PurchasedItems { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Full shopping list with items.
/// </summary>
public class ShoppingListDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid ShoppingLocationId { get; set; }
    public string? ShoppingLocationName { get; set; }
    public bool HasStoreIntegration { get; set; }
    public List<ShoppingListItemDto> Items { get; set; } = new();
}

/// <summary>
/// Shopping list item from the API.
/// </summary>
public class ShoppingListItemDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public decimal Amount { get; set; }
    public string? QuantityUnitName { get; set; }
    public string? Note { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public string? ExternalProductId { get; set; }
    public decimal? Price { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Shopping location (store) summary.
/// </summary>
public class StoreSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? IntegrationType { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Product lookup result from store integration.
/// </summary>
public class StoreProductResult
{
    public string? ExternalProductId { get; set; }

    // The API returns "name" (lowercase) - map it to ProductName
    public string Name { get; set; } = "";

    // Computed property for UI binding
    public string ProductName => Name;

    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public decimal? Price { get; set; }
    public string? Aisle { get; set; }
    public string? Department { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Request to move purchased shopping list items to inventory
/// </summary>
public class MoveToInventoryRequest
{
    public Guid ShoppingListId { get; set; }
    public List<MoveToInventoryItem> Items { get; set; } = new();
}

/// <summary>
/// Individual item to move to inventory
/// </summary>
public class MoveToInventoryItem
{
    public Guid ShoppingListItemId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Amount { get; set; }
    public decimal? Price { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Response from moving shopping items to inventory
/// </summary>
public class MoveToInventoryResponse
{
    public int ItemsAddedToStock { get; set; }
    public int TodoItemsCreated { get; set; }
    public List<Guid> StockEntryIds { get; set; } = new();
    public List<Guid> TodoItemIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
