namespace Famick.HomeManagement.Mobile.Models;

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
    public TenantInfo? Tenant { get; set; }
}

/// <summary>
/// Tenant information from the login response or tenant endpoint.
/// </summary>
public class TenantInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Subdomain { get; set; }
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
    public string? ImageUrl { get; set; }
    public decimal Amount { get; set; }
    public string? QuantityUnitName { get; set; }
    public string? Note { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public decimal PurchasedQuantity { get; set; }
    public DateTime? BestBeforeDate { get; set; }

    // Product tracking fields for date prompting logic
    public bool TracksBestBeforeDate { get; set; }
    public int DefaultBestBeforeDays { get; set; }
    public Guid? DefaultLocationId { get; set; }

    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public string? ExternalProductId { get; set; }
    public decimal? Price { get; set; }
    public string? Barcode { get; set; }

    /// <summary>
    /// All barcodes associated with the linked product (from ProductBarcodes).
    /// Used for offline barcode matching.
    /// </summary>
    public List<string> Barcodes { get; set; } = new();

    // Parent/child product support
    public bool IsParentProduct { get; set; }
    public bool HasChildren { get; set; }
    public int ChildProductCount { get; set; }
    public bool HasChildrenAtStore { get; set; }
    public decimal ChildPurchasedQuantity { get; set; }
    public decimal RemainingQuantity => IsParentProduct
        ? Amount - ChildPurchasedQuantity
        : Amount - PurchasedQuantity;
}

#region Child Product Models

/// <summary>
/// Child product option for a parent product on a shopping list.
/// </summary>
public class ChildProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExternalProductId { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public string? PriceUnit { get; set; }
    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public bool? InStock { get; set; }
    public string? ImageUrl { get; set; }
    public bool HasStoreMetadata { get; set; }
    public decimal PurchasedQuantity { get; set; }
}

/// <summary>
/// Tracks a purchase of a specific child product.
/// </summary>
public class ChildPurchaseEntry
{
    public Guid ChildProductId { get; set; }
    public string ChildProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? ExternalProductId { get; set; }
    public DateTime PurchasedAt { get; set; }
}

/// <summary>
/// Request to check off a child product.
/// </summary>
public class CheckOffChildRequest
{
    public Guid ChildProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public DateTime? BestBeforeDate { get; set; }
}

/// <summary>
/// Request to send a child product to cart.
/// </summary>
public class SendChildToCartRequest
{
    public Guid ChildProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
}

/// <summary>
/// Result of sending a product to the cart.
/// </summary>
public class SendToCartResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? CartUrl { get; set; }
}

#endregion

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

    // Alias properties so the shared DataTemplate bindings work for both result types
    public string? PrimaryImageUrl => ImageUrl;
    public string? Description => Brand;
    public string? ProductGroupName => null;
    public string? PreferredStoreAisle => Aisle;
    public string? PreferredStoreDepartment => Department;
}

/// <summary>
/// Lightweight product result for autocomplete searches
/// </summary>
public class ProductAutocompleteResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProductGroupName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? PreferredStoreAisle { get; set; }
    public string? PreferredStoreDepartment { get; set; }

    public string ProductName => Name;
}

/// <summary>
/// Request to create a product from external lookup data
/// </summary>
public class CreateProductFromLookupMobileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? OriginalSearchBarcode { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? SourceType { get; set; }
    public string? PluginId { get; set; }
    public Guid? ShoppingLocationId { get; set; }
    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public decimal? Price { get; set; }
}

/// <summary>
/// Result of a scan-purchase operation (incremental barcode scan)
/// </summary>
public class ScanPurchaseResult
{
    public ShoppingListItemDto Item { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public decimal RemainingQuantity { get; set; }
}

/// <summary>
/// Result of scanning a barcode against a shopping list
/// </summary>
public class BarcodeScanResult
{
    public bool Found { get; set; }
    public Guid? ItemId { get; set; }
    public string? ProductName { get; set; }
    public bool IsChildProduct { get; set; }
    public Guid? ChildProductId { get; set; }
    public string? ChildProductName { get; set; }
    public bool NeedsChildSelection { get; set; }
}

/// <summary>
/// Minimal product DTO returned from product creation
/// </summary>
public class ProductCreatedResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
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

    /// <summary>
    /// Product image URL from store integration
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Best before / expiration date for inventory tracking
    /// </summary>
    public DateTime? BestBeforeDate { get; set; }

    /// <summary>
    /// Storage location ID for this item in inventory
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// External product ID from store integration (for linking product to store)
    /// </summary>
    public string? ExternalProductId { get; set; }

    /// <summary>
    /// Shopping location ID (store) for linking product to store metadata
    /// </summary>
    public Guid? ShoppingLocationId { get; set; }

    /// <summary>
    /// Aisle location in store
    /// </summary>
    public string? Aisle { get; set; }

    /// <summary>
    /// Shelf location in store
    /// </summary>
    public string? Shelf { get; set; }

    /// <summary>
    /// Department in store
    /// </summary>
    public string? Department { get; set; }
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

/// <summary>
/// Aisle order configuration for a store.
/// </summary>
public class AisleOrderDto
{
    public List<string> OrderedAisles { get; set; } = new();
    public List<string> KnownAisles { get; set; } = new();
}

/// <summary>
/// Request to update the aisle order for a store.
/// </summary>
public class UpdateAisleOrderRequest
{
    public List<string>? OrderedAisles { get; set; }
}

#region Dashboard Models

/// <summary>
/// Dashboard summary of shopping lists.
/// </summary>
public class ShoppingListDashboardDto
{
    public List<StoreShoppingListSummary> StoresSummary { get; set; } = new();
    public int TotalItems { get; set; }
    public int UnpurchasedItems { get; set; }
    public int TotalLists { get; set; }
}

/// <summary>
/// Shopping list summary grouped by store.
/// </summary>
public class StoreShoppingListSummary
{
    public Guid ShoppingLocationId { get; set; }
    public string ShoppingLocationName { get; set; } = string.Empty;
    public bool HasIntegration { get; set; }
    public List<ShoppingListSummaryDto> Lists { get; set; } = new();
    public int TotalItems { get; set; }
}

/// <summary>
/// Shopping list summary for dashboard.
/// </summary>
public class ShoppingListSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int UnpurchasedItems { get; set; }
}

/// <summary>
/// Stock statistics for dashboard overview.
/// </summary>
public class StockStatisticsDto
{
    public int TotalProductCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public int ExpiredCount { get; set; }
    public int OverdueCount { get; set; }
    public int DueSoonCount { get; set; }
    public int BelowMinStockCount { get; set; }
}

/// <summary>
/// Chore summary for dashboard.
/// </summary>
public class ChoreSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public DateTime? NextExecutionDate { get; set; }
    public string? AssignedToUserName { get; set; }
    public bool IsOverdue { get; set; }
}

#endregion

#region Stock Overview Models

/// <summary>
/// Stock overview item for widget data (maps to server-side StockOverviewItemDto).
/// </summary>
public class StockOverviewItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string QuantityUnitName { get; set; } = string.Empty;
    public DateTime? NextDueDate { get; set; }
    public int? DaysUntilDue { get; set; }
    public bool IsExpired { get; set; }
    public bool IsDueSoon { get; set; }
}

#endregion

#region Quick Consume Models

/// <summary>
/// Product details from barcode lookup for quick consume.
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Guid QuantityUnitIdPurchase { get; set; }
    public string QuantityUnitPurchaseName { get; set; } = string.Empty;
    public Guid QuantityUnitIdStock { get; set; }
    public string QuantityUnitStockName { get; set; } = string.Empty;
    public decimal QuantityUnitFactorPurchaseToStock { get; set; }
    public decimal MinStockAmount { get; set; }
    public int DefaultBestBeforeDays { get; set; }
    public bool TracksBestBeforeDate { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalStockAmount { get; set; }
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Product barcode information.
/// </summary>
public class ProductBarcodeDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? Note { get; set; }
}

/// <summary>
/// Product image information for display.
/// </summary>
public class ProductImageDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? ExternalThumbnailUrl { get; set; }
    public bool IsPrimary { get; set; }
    public string DisplayUrl => !string.IsNullOrEmpty(ExternalUrl) ? ExternalUrl : Url;
    public string ThumbnailDisplayUrl => !string.IsNullOrEmpty(ExternalThumbnailUrl)
        ? ExternalThumbnailUrl : DisplayUrl;
}

/// <summary>
/// Stock entry with expiry, amount, and location info for quick consume selection.
/// </summary>
public class StockEntryDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductBarcode { get; set; }
    public decimal Amount { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public DateTime PurchasedDate { get; set; }
    public string StockId { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool Open { get; set; }
    public DateTime? OpenedDate { get; set; }
    public decimal? OriginalAmount { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? ShoppingLocationId { get; set; }
    public string? ShoppingLocationName { get; set; }
    public string? Note { get; set; }
    public string QuantityUnitName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if the item is expired based on BestBeforeDate.
    /// </summary>
    public bool IsExpired => BestBeforeDate.HasValue && BestBeforeDate.Value.Date < DateTime.UtcNow.Date;

    /// <summary>
    /// Days until expiry (negative if expired).
    /// </summary>
    public int? DaysUntilExpiry => BestBeforeDate.HasValue
        ? (int)(BestBeforeDate.Value.Date - DateTime.UtcNow.Date).TotalDays
        : null;
}

/// <summary>
/// Request for quick consume actions (FEFO-based consumption).
/// </summary>
public class QuickConsumeRequest
{
    public Guid ProductId { get; set; }
    public decimal Amount { get; set; } = 1;
    public bool ConsumeAll { get; set; }
}

/// <summary>
/// Request to consume a specific stock entry.
/// </summary>
public class ConsumeStockRequest
{
    public decimal? Amount { get; set; }
    public bool Spoiled { get; set; }
    public Guid? RecipeId { get; set; }
}

#endregion

#region Registration Models

/// <summary>
/// Request to start the registration process.
/// </summary>
public class StartRegistrationRequest
{
    public string HouseholdName { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>
/// Response from starting registration.
/// </summary>
public class StartRegistrationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? MaskedEmail { get; set; }
}

/// <summary>
/// Request to verify email.
/// </summary>
public class VerifyEmailRequest
{
    public string Token { get; set; } = "";
}

/// <summary>
/// Response from email verification.
/// </summary>
public class VerifyEmailResponse
{
    public bool Verified { get; set; }
    public string? Email { get; set; }
    public string? HouseholdName { get; set; }
    public string? Token { get; set; }
}

/// <summary>
/// Request to complete registration after email verification.
/// </summary>
public class CompleteRegistrationRequest
{
    public string Token { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Password { get; set; }
    public string? Provider { get; set; }
    public string? ProviderToken { get; set; }
}

/// <summary>
/// Response from completing registration.
/// </summary>
public class CompleteRegistrationResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserInfo? User { get; set; }
    public TenantInfo? Tenant { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User information from registration response.
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

#endregion
