using System.Collections.ObjectModel;
using Famick.HomeManagement.ShoppingMode.Models;
using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode.Pages;

[QueryProperty(nameof(ListId), "ListId")]
[QueryProperty(nameof(ListName), "ListName")]
public partial class ShoppingSessionPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private readonly OfflineStorageService _offlineStorage;
    private readonly ConnectivityService _connectivityService;
    private readonly ImageCacheService _imageCacheService;

    private Guid _listId;
    private ShoppingSession? _session;
    private bool _isPopulatingItems;

    public string ListId
    {
        set => _listId = Guid.Parse(value);
    }

    public string ListName { get; set; } = "Shopping";

    public ObservableCollection<ItemGroup> GroupedItems { get; } = new();

    public ShoppingSessionPage(
        ShoppingApiClient apiClient,
        OfflineStorageService offlineStorage,
        ConnectivityService connectivityService,
        ImageCacheService imageCacheService)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _offlineStorage = offlineStorage;
        _connectivityService = connectivityService;
        _imageCacheService = imageCacheService;

        BindingContext = this;

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = ListName;
        UpdateConnectivityUI();
        await LoadSessionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }

    private async Task LoadSessionAsync()
    {
        ShowLoading(true);

        try
        {
            // Try to load from cache first
            _session = await _offlineStorage.GetCachedSessionAsync(_listId);

            if (_session == null && _connectivityService.IsOnline)
            {
                // Fetch from API and cache
                var result = await _apiClient.GetShoppingListAsync(_listId);
                if (result.Success && result.Data != null)
                {
                    _session = await _offlineStorage.CacheShoppingSessionAsync(_listId, result.Data);
                    await _imageCacheService.CacheImagesForSessionAsync(_session);
                }
                else
                {
                    await DisplayAlertAsync("Error", result.ErrorMessage ?? "Failed to load list", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }

            if (_session != null)
            {
                PopulateItems();
                UpdateSubtotal();
            }
            else
            {
                await DisplayAlertAsync("Error", "Unable to load shopping list. Please try again online.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void PopulateItems()
    {
        if (_session == null) return;

        // Set flag to prevent OnItemCheckedChanged from queueing operations during UI binding
        _isPopulatingItems = true;
        Console.WriteLine("PopulateItems: Setting _isPopulatingItems = true");

        try
        {
            GroupedItems.Clear();

            // Group items by aisle/department
            var groups = _session.Items
                .OrderBy(i => i.SortOrder)
                .GroupBy(i => string.IsNullOrEmpty(i.Aisle) ? i.Department ?? "Other" : $"Aisle {i.Aisle}")
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                var itemGroup = new ItemGroup(group.Key, group.ToList());
                GroupedItems.Add(itemGroup);
            }

            // Log items with their original states
            foreach (var item in _session.Items)
            {
                Console.WriteLine($"PopulateItems: {item.ProductName} - IsPurchased={item.IsPurchased}, OriginalIsPurchased={item.OriginalIsPurchased}");
            }
        }
        finally
        {
            // Use a small delay to ensure UI binding is complete before enabling change tracking
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPopulatingItems = false;
                Console.WriteLine("PopulateItems: Setting _isPopulatingItems = false");
            });
        }
    }

    private void UpdateSubtotal()
    {
        if (_session == null) return;

        var itemsWithPrice = _session.Items.Where(i => i.Price.HasValue).ToList();
        var totalItems = _session.Items.Count;

        if (itemsWithPrice.Count == 0)
        {
            SubtotalLabel.Text = "No prices";
            SubtotalNote.Text = "";
        }
        else if (itemsWithPrice.Count < totalItems)
        {
            var subtotal = itemsWithPrice.Sum(i => i.Price!.Value * i.Amount);
            SubtotalLabel.Text = $"${subtotal:F2}";
            SubtotalNote.Text = $"({totalItems - itemsWithPrice.Count} items missing prices)";
        }
        else
        {
            var subtotal = itemsWithPrice.Sum(i => i.Price!.Value * i.Amount);
            SubtotalLabel.Text = $"${subtotal:F2}";
            SubtotalNote.Text = "Estimated total";
        }
    }

    private async void OnItemCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Skip during initial UI population - we only want to track user-initiated changes
        if (_isPopulatingItems)
        {
            return;
        }

        if (sender is not CheckBox checkBox || checkBox.BindingContext is not CachedShoppingListItem item)
            return;

        // Note: Two-way binding already updated item.IsPurchased to e.Value
        // So we just need to update the timestamp and handle storage/queueing
        item.PurchasedAt = e.Value ? DateTime.UtcNow : null;

        // Update local storage first
        await _offlineStorage.UpdateItemStateAsync(item);

        // For existing items (not new), try real-time sync
        if (!item.IsNewItem)
        {
            if (_connectivityService.IsOnline)
            {
                // Try to sync immediately
                var result = await _apiClient.TogglePurchasedAsync(_listId, item.Id);
                if (result.Success)
                {
                    // Update original state to match server
                    item.OriginalIsPurchased = item.IsPurchased;
                    await _offlineStorage.UpdateItemStateAsync(item);
                    Console.WriteLine($"Real-time sync: {item.ProductName} toggled to {item.IsPurchased}");
                }
                else
                {
                    // Log for later replay
                    await LogToggleRequestAsync(item);
                    Console.WriteLine($"Real-time sync failed, logged for replay: {item.ProductName}");
                }
            }
            else
            {
                // Offline - log for later replay
                await LogToggleRequestAsync(item);
                Console.WriteLine($"Offline - logged toggle for replay: {item.ProductName}");
            }
        }

        UpdateSubtotal();
    }

    private async Task LogToggleRequestAsync(CachedShoppingListItem item)
    {
        // Remove any existing pending requests for this item (to avoid duplicates)
        await _offlineStorage.RemovePendingToggleOperationsAsync(_listId, item.Id);

        // Only log if current state differs from original
        if (item.IsPurchased != item.OriginalIsPurchased)
        {
            var url = $"api/v1/shoppinglists/{_listId}/items/{item.Id}/toggle-purchased";
            await _offlineStorage.LogHttpRequestAsync(new HttpRequestLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Method = "POST",
                Url = url,
                Body = null
            });
        }
    }

    private async void OnAddItemClicked(object? sender, EventArgs e)
    {
        // Navigate to AddItemPage without a barcode for manual entry
        var navigationParameter = new Dictionary<string, object>
        {
            { "ListId", _listId.ToString() }
        };
        await Shell.Current.GoToAsync(nameof(AddItemPage), navigationParameter);
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        var scannerPage = new BarcodeScannerPage();
        await Navigation.PushModalAsync(scannerPage);
        var barcode = await scannerPage.ScanAsync();

        if (string.IsNullOrEmpty(barcode))
            return;

        await HandleScannedBarcodeAsync(barcode);
    }

    private async Task HandleScannedBarcodeAsync(string barcode)
    {
        if (_session == null) return;

        // Check if item is in current list
        var existingItem = _session.Items.FirstOrDefault(i =>
            i.Barcode?.Equals(barcode, StringComparison.OrdinalIgnoreCase) == true);

        if (existingItem != null)
        {
            // Mark as purchased and scroll to it
            existingItem.IsPurchased = true;
            existingItem.PurchasedAt = DateTime.UtcNow;
            await _offlineStorage.UpdateItemStateAsync(existingItem);
            PopulateItems();
            UpdateSubtotal();

            // TODO: Scroll to item
            await DisplayAlertAsync("Found!", $"{existingItem.ProductName} checked off", "OK");
        }
        else
        {
            // Item not in list - offer to add it
            var navigationParameter = new Dictionary<string, object>
            {
                { "Barcode", barcode },
                { "ListId", _listId.ToString() }
            };
            await Shell.Current.GoToAsync(nameof(AddItemPage), navigationParameter);
        }
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (!_connectivityService.IsOnline)
        {
            await DisplayAlertAsync("Offline", "Please connect to the internet to complete shopping.", "OK");
            return;
        }

        if (_session == null)
        {
            await DisplayAlertAsync("Error", "No shopping session found.", "OK");
            return;
        }

        var purchasedItems = _session.Items.Where(i => i.IsPurchased).ToList();
        if (purchasedItems.Count == 0)
        {
            var confirmEmpty = await DisplayAlertAsync(
                "No Items Purchased",
                "You haven't marked any items as purchased. Do you want to exit without completing?",
                "Exit",
                "Cancel");

            if (confirmEmpty)
            {
                await _offlineStorage.ClearSessionAsync(_listId);
                await Shell.Current.GoToAsync("..");
            }
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Complete Shopping?",
            $"This will move {purchasedItems.Count} purchased item(s) to your inventory.",
            "Complete",
            "Cancel");

        if (!confirm) return;

        ShowLoading(true);

        try
        {
            // First, replay any pending HTTP requests
            await ReplayPendingRequestsAsync();

            // Build move-to-inventory request
            var request = new MoveToInventoryRequest
            {
                ShoppingListId = _listId,
                Items = purchasedItems.Select(i => new MoveToInventoryItem
                {
                    ShoppingListItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Amount = i.Amount,
                    Price = i.Price,
                    Barcode = i.Barcode
                }).ToList()
            };

            // Call move-to-inventory endpoint
            var result = await _apiClient.MoveToInventoryAsync(request);

            if (result.Success && result.Data != null)
            {
                var message = $"Added {result.Data.ItemsAddedToStock} item(s) to inventory.";
                if (result.Data.TodoItemsCreated > 0)
                {
                    message += $"\n{result.Data.TodoItemsCreated} item(s) need product setup.";
                }
                if (result.Data.Errors.Count > 0)
                {
                    message += $"\n{result.Data.Errors.Count} error(s) occurred.";
                }

                await DisplayAlertAsync("Shopping Complete", message, "OK");

                // Clear local cache
                await _offlineStorage.ClearSessionAsync(_listId);
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlertAsync("Error", result.ErrorMessage ?? "Failed to complete shopping", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to complete shopping: {ex.Message}", "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async Task ReplayPendingRequestsAsync()
    {
        var pendingRequests = await _offlineStorage.GetPendingHttpRequestsAsync();
        Console.WriteLine($"ReplayPendingRequests: Found {pendingRequests.Count} pending requests");

        foreach (var request in pendingRequests)
        {
            try
            {
                // For now, we handle toggle-purchased requests
                if (request.Method == "POST" && request.Url.Contains("toggle-purchased"))
                {
                    // Extract listId and itemId from URL
                    // URL format: api/v1/shoppinglists/{listId}/items/{itemId}/toggle-purchased
                    var parts = request.Url.Split('/');
                    if (parts.Length >= 6)
                    {
                        var listId = Guid.Parse(parts[3]);
                        var itemId = Guid.Parse(parts[5]);

                        var result = await _apiClient.TogglePurchasedAsync(listId, itemId);
                        if (result.Success)
                        {
                            await _offlineStorage.MarkHttpRequestCompletedAsync(request.Id);
                            Console.WriteLine($"Replayed toggle request for item {itemId}");
                        }
                        else
                        {
                            await _offlineStorage.UpdateHttpRequestErrorAsync(request.Id, result.ErrorMessage ?? "Unknown error");
                        }
                    }
                }
                else
                {
                    // Mark as completed for unsupported request types
                    await _offlineStorage.MarkHttpRequestCompletedAsync(request.Id);
                }
            }
            catch (Exception ex)
            {
                await _offlineStorage.UpdateHttpRequestErrorAsync(request.Id, ex.Message);
                Console.WriteLine($"Failed to replay request {request.Id}: {ex.Message}");
            }
        }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        if (_connectivityService.IsOnline)
        {
            await LoadSessionAsync();
        }
        RefreshContainer.IsRefreshing = false;
    }

    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(UpdateConnectivityUI);
    }

    private void UpdateConnectivityUI()
    {
        var isOnline = _connectivityService.IsOnline;
        OfflineBanner.IsVisible = !isOnline;
        CompleteButton.IsEnabled = isOnline;
        CompleteButton.Opacity = isOnline ? 1.0 : 0.5;
    }

    private void ShowLoading(bool show)
    {
        LoadingIndicator.IsRunning = show;
        LoadingIndicator.IsVisible = show;
    }
}

/// <summary>
/// Grouping class for CollectionView
/// </summary>
public class ItemGroup : ObservableCollection<CachedShoppingListItem>
{
    public string Key { get; }

    public ItemGroup(string key, IEnumerable<CachedShoppingListItem> items) : base(items)
    {
        Key = key;
    }
}
