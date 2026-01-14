using SQLite;

namespace Famick.HomeManagement.ShoppingMode.Models;

/// <summary>
/// Cached shopping session for offline use.
/// </summary>
public class ShoppingSession
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public Guid ShoppingListId { get; set; }
    public string ShoppingListName { get; set; } = "";
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public bool IsActive { get; set; }

    [Ignore]
    public List<CachedShoppingListItem> Items { get; set; } = new();
}

/// <summary>
/// Cached shopping list item for offline use.
/// </summary>
public class CachedShoppingListItem
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid SessionId { get; set; }

    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? LocalImagePath { get; set; }
    public decimal Amount { get; set; }
    public string? QuantityUnitName { get; set; }
    public string? Note { get; set; }
    public bool IsPurchased { get; set; }
    public bool OriginalIsPurchased { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public string? ExternalProductId { get; set; }
    public decimal? Price { get; set; }
    public string? Barcode { get; set; }
    public int SortOrder { get; set; }
    public bool IsNewItem { get; set; }

    [Ignore]
    public bool HasPrice => Price.HasValue;

    [Ignore]
    public bool HasImage => !string.IsNullOrEmpty(LocalImagePath);

    [Ignore]
    public ImageSource? ImageSource => HasImage ? ImageSource.FromFile(LocalImagePath) : null;
}

/// <summary>
/// Queued offline operation for sync.
/// </summary>
public class OfflineOperation
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public string OperationType { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public bool IsCompleted { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// HTTP request log for offline request replay
/// </summary>
public class HttpRequestLog
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Headers { get; set; }
    public string? Body { get; set; }
    public int RetryCount { get; set; }
    public bool IsCompleted { get; set; }
    public string? LastError { get; set; }
}
