using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Famick.HomeManagement.Mobile.Models;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages;

/// <summary>
/// Message sent when the user taps "Check Off Parent Directly" on the child selection page.
/// </summary>
public sealed class CheckOffParentMessage(Guid itemId) : ValueChangedMessage<Guid>(itemId);

/// <summary>
/// Message sent when the user taps "Done" on the child selection page, indicating child quantities may have changed.
/// </summary>
public sealed class ChildSelectionDoneMessage(Guid itemId) : ValueChangedMessage<Guid>(itemId);

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
    private bool _needsRefreshAfterAisleOrderChange;
    private bool _needsRefreshAfterChildSelection;

    public string ListId
    {
        set => _listId = Guid.Parse(value);
    }

    public string ListName { get; set; } = "Shopping";

    public ObservableCollection<ItemGroup> GroupedItems { get; } = new();

    public ICommand RemoveItemCommand { get; }
    public ICommand ToggleItemCommand { get; }
    public ICommand ShowItemDetailCommand { get; }

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

        RemoveItemCommand = new Command<CachedShoppingListItem>(async item => await RemoveItemAsync(item));
        ToggleItemCommand = new Command<CachedShoppingListItem>(async item => await ToggleItemAsync(item));
        ShowItemDetailCommand = new Command<CachedShoppingListItem>(ShowItemDetail);

        BindingContext = this;

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;

        WeakReferenceMessenger.Default.Register<CheckOffParentMessage>(this, async (recipient, message) =>
        {
            var item = _session?.Items.FirstOrDefault(i => i.Id == message.Value);
            if (item != null && !item.IsPurchased)
            {
                await ToggleItemAsync(item);
            }
        });

        WeakReferenceMessenger.Default.Register<ChildSelectionDoneMessage>(this, (recipient, message) =>
        {
            _needsRefreshAfterChildSelection = true;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PageTitleLabel.Text = ListName;
        UpdateConnectivityUI();

        // If returning from child selection, force a refresh to get updated child purchased quantities
        if (_needsRefreshAfterChildSelection && _connectivityService.IsOnline)
        {
            _needsRefreshAfterChildSelection = false;
            await _offlineStorage.ClearSessionAsync(_listId);
            _session = null;
        }

        await LoadSessionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private async Task LoadSessionAsync()
    {
        ShowLoading(true);

        try
        {
            // If returning from aisle order change, clear cache to get new sort order
            if (_needsRefreshAfterAisleOrderChange && _connectivityService.IsOnline)
            {
                _needsRefreshAfterAisleOrderChange = false;
                await _offlineStorage.ClearSessionAsync(_listId);
                _session = null;
            }
            else
            {
                // Try to load from cache first
                _session = await _offlineStorage.GetCachedSessionAsync(_listId);
            }

            if (_session == null && _connectivityService.IsOnline)
            {
                // Fetch from API and cache
                var result = await _apiClient.GetShoppingListAsync(_listId);
                if (result.Success && result.Data != null)
                {
                    _session = await _offlineStorage.CacheShoppingSessionAsync(_listId, result.Data);
                    await _imageCacheService.CacheImagesForSessionAsync(_session, result.Data.Items);

                    // Persist cached image paths back to SQLite
                    foreach (var item in _session.Items.Where(i => !string.IsNullOrEmpty(i.LocalImagePath)))
                    {
                        await _offlineStorage.UpdateItemStateAsync(item);
                    }
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

        try
        {
            GroupedItems.Clear();

            // Separate unpurchased and purchased items
            var unpurchased = _session.Items.Where(i => !i.IsPurchased).OrderBy(i => i.SortOrder).ToList();
            var purchased = _session.Items.Where(i => i.IsPurchased).OrderBy(i => i.SortOrder).ToList();

            // Group unpurchased items by aisle/department, ordered by custom aisle order
            var groups = unpurchased
                .GroupBy(i => string.IsNullOrEmpty(i.Aisle)
                    ? i.Department ?? "Other"
                    : int.TryParse(i.Aisle, out _) ? $"Aisle {i.Aisle}" : i.Aisle)
                .OrderBy(g => g.Min(i => i.SortOrder)); // Order groups by their first item's sort order

            foreach (var group in groups)
            {
                var itemGroup = new ItemGroup(group.Key, group.ToList());
                GroupedItems.Add(itemGroup);
            }

            // Add purchased items as a separate group at the bottom
            if (purchased.Count > 0)
            {
                var purchasedGroup = new ItemGroup($"Purchased ({purchased.Count})", purchased);
                GroupedItems.Add(purchasedGroup);
            }
        }
        finally
        {
            // Use a small delay to ensure UI binding is complete before enabling change tracking
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPopulatingItems = false;
            });
        }
    }

    /// <summary>
    /// Moves an item from its current group to the correct group (purchased or aisle)
    /// without clearing and rebuilding the entire GroupedItems collection.
    /// This preserves the CollectionView scroll position.
    /// </summary>
    private void MoveItemBetweenGroups(CachedShoppingListItem item)
    {
        _isPopulatingItems = true;
        try
        {
            // Remove item from its current group
            foreach (var group in GroupedItems)
            {
                if (group.Remove(item))
                    break;
            }

            // Remove any now-empty groups
            for (int i = GroupedItems.Count - 1; i >= 0; i--)
            {
                if (GroupedItems[i].Count == 0)
                    GroupedItems.RemoveAt(i);
            }

            if (item.IsPurchased)
            {
                // Add to purchased group
                var purchasedGroup = GroupedItems.FirstOrDefault(g => g.Key.StartsWith("Purchased"));
                if (purchasedGroup != null)
                {
                    // Update the group key to reflect new count
                    var purchasedCount = purchasedGroup.Count + 1;
                    var idx = GroupedItems.IndexOf(purchasedGroup);
                    purchasedGroup.Add(item);

                    // Replace group to update the header text with new count
                    GroupedItems[idx] = new ItemGroup($"Purchased ({purchasedCount})", purchasedGroup);
                }
                else
                {
                    GroupedItems.Add(new ItemGroup("Purchased (1)", new[] { item }));
                }
            }
            else
            {
                // Determine the correct aisle/department group
                var groupKey = string.IsNullOrEmpty(item.Aisle)
                    ? item.Department ?? "Other"
                    : int.TryParse(item.Aisle, out _) ? $"Aisle {item.Aisle}" : item.Aisle;

                // Find existing group (exclude purchased group)
                var targetGroup = GroupedItems.FirstOrDefault(g => g.Key == groupKey && !g.Key.StartsWith("Purchased"));
                if (targetGroup != null)
                {
                    // Insert at correct sort position
                    var insertIdx = 0;
                    for (int i = 0; i < targetGroup.Count; i++)
                    {
                        if (targetGroup[i].SortOrder > item.SortOrder)
                            break;
                        insertIdx = i + 1;
                    }
                    targetGroup.Insert(insertIdx, item);
                }
                else
                {
                    // Create new group and insert before the Purchased group
                    var newGroup = new ItemGroup(groupKey, new[] { item });
                    var purchasedIdx = -1;
                    for (int i = 0; i < GroupedItems.Count; i++)
                    {
                        if (GroupedItems[i].Key.StartsWith("Purchased"))
                        {
                            purchasedIdx = i;
                            break;
                        }
                    }
                    if (purchasedIdx >= 0)
                        GroupedItems.Insert(purchasedIdx, newGroup);
                    else
                        GroupedItems.Add(newGroup);
                }

                // Update the purchased group header count if it still exists
                var existingPurchasedGroup = GroupedItems.FirstOrDefault(g => g.Key.StartsWith("Purchased"));
                if (existingPurchasedGroup != null)
                {
                    var idx = GroupedItems.IndexOf(existingPurchasedGroup);
                    GroupedItems[idx] = new ItemGroup($"Purchased ({existingPurchasedGroup.Count})", existingPurchasedGroup);
                }
            }
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => _isPopulatingItems = false);
        }
    }

    /// <summary>
    /// Removes an item from the GroupedItems without full rebuild.
    /// </summary>
    private void RemoveItemFromGroups(CachedShoppingListItem item)
    {
        _isPopulatingItems = true;
        try
        {
            foreach (var group in GroupedItems)
            {
                if (group.Remove(item))
                    break;
            }

            // Remove empty groups
            for (int i = GroupedItems.Count - 1; i >= 0; i--)
            {
                if (GroupedItems[i].Count == 0)
                    GroupedItems.RemoveAt(i);
            }

            // Update purchased group header count
            var purchasedGroup = GroupedItems.FirstOrDefault(g => g.Key.StartsWith("Purchased"));
            if (purchasedGroup != null)
            {
                var idx = GroupedItems.IndexOf(purchasedGroup);
                GroupedItems[idx] = new ItemGroup($"Purchased ({purchasedGroup.Count})", purchasedGroup);
            }
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => _isPopulatingItems = false);
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

    private async Task ToggleItemAsync(CachedShoppingListItem? item)
    {
        if (item == null || _session == null) return;

        // Parent products with children at this store navigate to child selection
        if (item.NeedsChildSelection)
        {
            await NavigateToChildSelectionAsync(item);
            return;
        }

        item.IsPurchased = !item.IsPurchased;
        item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null;

        await _offlineStorage.UpdateItemStateAsync(item);

        if (!item.IsNewItem)
        {
            if (_connectivityService.IsOnline)
            {
                var result = await _apiClient.TogglePurchasedAsync(_listId, item.Id);
                if (result.Success)
                {
                    item.OriginalIsPurchased = item.IsPurchased;
                    await _offlineStorage.UpdateItemStateAsync(item);
                }
                else
                {
                    await EnqueueToggleOperationAsync(item);
                }
            }
            else
            {
                await EnqueueToggleOperationAsync(item);
            }
        }

        MoveItemBetweenGroups(item);
        UpdateSubtotal();
    }

    private async Task RemoveItemAsync(CachedShoppingListItem? item)
    {
        if (item == null || _session == null) return;

        // Remove from in-memory session
        _session.Items.Remove(item);

        // Remove from SQLite cache
        await _offlineStorage.RemoveItemFromSessionAsync(item.Id);

        // Try API delete if online, otherwise queue
        if (!item.IsNewItem)
        {
            if (_connectivityService.IsOnline)
            {
                var result = await _apiClient.RemoveItemAsync(_listId, item.Id);
                if (!result.Success)
                {
                    await EnqueueRemoveOperationAsync(item.Id);
                }
            }
            else
            {
                await EnqueueRemoveOperationAsync(item.Id);
            }
        }

        RemoveItemFromGroups(item);
        UpdateSubtotal();
    }

    private async Task EnqueueRemoveOperationAsync(Guid itemId)
    {
        await _offlineStorage.EnqueueOperationAsync(new OfflineOperation
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            OperationType = "RemoveItem",
            PayloadJson = JsonSerializer.Serialize(new { ListId = _listId, ItemId = itemId })
        });
    }

    private async void OnItemCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Skip during initial UI population - we only want to track user-initiated changes
        if (_isPopulatingItems)
            return;

        if (sender is not CheckBox checkBox || checkBox.BindingContext is not CachedShoppingListItem item)
            return;

        // Parent products needing child selection: undo checkbox and navigate
        if (item.NeedsChildSelection)
        {
            item.IsPurchased = false;
            _ = NavigateToChildSelectionAsync(item);
            return;
        }

        item.PurchasedAt = e.Value ? DateTime.UtcNow : null;

        // Update local storage first
        await _offlineStorage.UpdateItemStateAsync(item);

        // For existing items (not new), try real-time sync
        if (!item.IsNewItem)
        {
            if (_connectivityService.IsOnline)
            {
                var result = await _apiClient.TogglePurchasedAsync(_listId, item.Id);
                if (result.Success)
                {
                    item.OriginalIsPurchased = item.IsPurchased;
                    await _offlineStorage.UpdateItemStateAsync(item);
                }
                else
                {
                    await EnqueueToggleOperationAsync(item);
                }
            }
            else
            {
                await EnqueueToggleOperationAsync(item);
            }
        }

        // Move item between groups without clearing/rebuilding the entire list
        MoveItemBetweenGroups(item);
        UpdateSubtotal();
    }

    private async Task EnqueueToggleOperationAsync(CachedShoppingListItem item)
    {
        // Remove any existing pending toggle for this item
        await _offlineStorage.RemovePendingToggleOperationsAsync(_listId, item.Id);

        // Only enqueue if current state differs from original
        if (item.IsPurchased != item.OriginalIsPurchased)
        {
            await _offlineStorage.EnqueueOperationAsync(new OfflineOperation
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                OperationType = "TogglePurchased",
                PayloadJson = JsonSerializer.Serialize(new { ListId = _listId, ItemId = item.Id, item.IsPurchased })
            });
        }
    }

    private async void OnAisleOrderClicked(object? sender, EventArgs e)
    {
        if (_session == null)
        {
            await DisplayAlertAsync("Error", "Shopping session not loaded", "OK");
            return;
        }

        // Flag to refresh from API when returning (to apply new aisle order)
        _needsRefreshAfterAisleOrderChange = true;

        var navigationParameter = new Dictionary<string, object>
        {
            { "LocationId", _session.StoreId.ToString() },
            { "StoreName", _session.StoreName }
        };
        await Shell.Current.GoToAsync(nameof(AisleOrderPage), navigationParameter);
    }

    private async void OnAddItemClicked(object? sender, EventArgs e)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "ListId", _listId.ToString() }
        };
        await Shell.Current.GoToAsync(nameof(AddItemPage), navigationParameter);
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        // Request camera permission before opening scanner
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(
                    "Camera Required",
                    "Camera permission is needed to scan barcodes. Please enable it in Settings.",
                    "OK");
                return;
            }
        }

        var scannerPage = new BarcodeScannerPage();
        await Navigation.PushAsync(scannerPage);
        var barcode = await scannerPage.ScanAsync();

        if (string.IsNullOrEmpty(barcode))
            return;

        await HandleScannedBarcodeAsync(barcode);
    }

    private async Task HandleScannedBarcodeAsync(string barcode)
    {
        if (_session == null) return;

        var existingItem = _session.Items.FirstOrDefault(i =>
            i.Barcode?.Equals(barcode, StringComparison.OrdinalIgnoreCase) == true);

        if (existingItem != null)
        {
            existingItem.IsPurchased = true;
            existingItem.PurchasedAt = DateTime.UtcNow;
            await _offlineStorage.UpdateItemStateAsync(existingItem);
            MoveItemBetweenGroups(existingItem);
            UpdateSubtotal();

            await DisplayAlertAsync("Found!", $"{existingItem.ProductName} checked off", "OK");
        }
        else
        {
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
            // Sync any pending offline operations first
            await _offlineStorage.SyncPendingOperationsAsync(_apiClient);

            // Get the shopping location ID from the session
            var shoppingLocationId = _session?.StoreId;

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
                    Barcode = i.Barcode,
                    ImageUrl = i.ImageUrl,
                    BestBeforeDate = i.BestBeforeDate,
                    LocationId = i.DefaultLocationId,
                    ExternalProductId = i.ExternalProductId,
                    ShoppingLocationId = shoppingLocationId,
                    Aisle = i.Aisle,
                    Shelf = i.Shelf,
                    Department = i.Department
                }).ToList()
            };

            var result = await _apiClient.MoveToInventoryAsync(request);

            if (result.Success && result.Data != null)
            {
                var message = $"Added {result.Data.ItemsAddedToStock} item(s) to inventory.";
                if (result.Data.TodoItemsCreated > 0)
                    message += $"\n{result.Data.TodoItemsCreated} item(s) need product setup.";
                if (result.Data.Errors.Count > 0)
                    message += $"\n{result.Data.Errors.Count} error(s) occurred.";

                await DisplayAlertAsync("Shopping Complete", message, "OK");

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

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        if (_connectivityService.IsOnline)
        {
            // Sync pending operations, then reload
            await _offlineStorage.SyncPendingOperationsAsync(_apiClient);
            await _offlineStorage.ClearSessionAsync(_listId);
            await LoadSessionAsync();
        }
        RefreshContainer.IsRefreshing = false;
    }

    private async void OnConnectivityChanged(object? sender, bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(UpdateConnectivityUI);

        if (isOnline)
        {
            // Auto-sync pending operations when coming back online
            await _offlineStorage.SyncPendingOperationsAsync(_apiClient);
        }
    }

    private void UpdateConnectivityUI()
    {
        var isOnline = _connectivityService.IsOnline;
        OfflineBanner.IsVisible = !isOnline;
        CompleteButton.IsEnabled = isOnline;
        CompleteButton.Opacity = isOnline ? 1.0 : 0.5;
    }

    private void ShowItemDetail(CachedShoppingListItem? item)
    {
        if (item == null) return;

        // Any unpurchased parent product navigates to child selection
        if (item.IsParentProduct && !item.IsPurchased)
        {
            _ = NavigateToChildSelectionAsync(item);
            return;
        }

        DetailProductName.Text = item.ProductName;
        DetailQuantity.Text = item.Amount.ToString("G");

        // Image
        if (item.HasImage)
        {
            DetailImage.Source = item.ImageSource;
            DetailImage.IsVisible = true;
            DetailNoImage.IsVisible = false;
        }
        else
        {
            DetailImage.IsVisible = false;
            DetailNoImage.IsVisible = true;
        }

        // Location
        DetailAisle.Text = !string.IsNullOrEmpty(item.Aisle)
            ? (int.TryParse(item.Aisle, out _) ? $"Aisle {item.Aisle}" : item.Aisle)
            : "—";
        DetailShelf.Text = !string.IsNullOrEmpty(item.Shelf) ? item.Shelf : "—";
        DetailDepartment.Text = !string.IsNullOrEmpty(item.Department) ? item.Department : "—";

        // Price
        DetailPriceSection.IsVisible = item.HasPrice;
        DetailPrice.Text = item.Price.HasValue ? $"${item.Price:F2}" : "";

        // Note
        if (!string.IsNullOrEmpty(item.Note))
        {
            DetailNote.Text = item.Note;
            DetailNote.IsVisible = true;
        }
        else
        {
            DetailNote.IsVisible = false;
        }

        DetailOverlay.IsVisible = true;
    }

    private void OnDetailOverlayTapped(object? sender, TappedEventArgs e)
    {
        DetailOverlay.IsVisible = false;
    }

    private void OnDetailCloseClicked(object? sender, EventArgs e)
    {
        DetailOverlay.IsVisible = false;
    }

    private async Task NavigateToChildSelectionAsync(CachedShoppingListItem item)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "ListId", _listId.ToString() },
            { "ItemId", item.Id.ToString() },
            { "ParentName", item.ProductName },
            { "ParentAmount", item.Amount.ToString("G") },
            { "ParentImageUrl", item.ImageUrl ?? "" }
        };
        await Shell.Current.GoToAsync(nameof(ChildProductSelectionPage), navigationParameter);
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
