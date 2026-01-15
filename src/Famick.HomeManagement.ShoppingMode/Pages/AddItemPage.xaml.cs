using System.Collections.ObjectModel;
using Famick.HomeManagement.ShoppingMode.Models;
using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode.Pages;

[QueryProperty(nameof(Barcode), "Barcode")]
[QueryProperty(nameof(ListId), "ListId")]
public partial class AddItemPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private readonly OfflineStorageService _offlineStorage;
    private readonly ConnectivityService _connectivityService;

    private string? _barcode;
    private Guid _listId;
    private int _quantity = 1;
    private StoreProductResult? _lookupResult;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<StoreProductResult> SearchResults { get; } = new();

    public string? Barcode
    {
        get => _barcode;
        set
        {
            _barcode = value;
            if (!string.IsNullOrEmpty(value))
            {
                BarcodeLabel.Text = value;
                BarcodeFrame.IsVisible = true;
            }
        }
    }

    public string ListId
    {
        set => _listId = Guid.Parse(value);
    }

    public AddItemPage(
        ShoppingApiClient apiClient,
        OfflineStorageService offlineStorage,
        ConnectivityService connectivityService)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _offlineStorage = offlineStorage;
        _connectivityService = connectivityService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Try to look up the barcode if online
        if (!string.IsNullOrEmpty(_barcode) && _connectivityService.IsOnline)
        {
            await LookupBarcodeAsync();
        }
    }

    private async Task LookupBarcodeAsync()
    {
        if (string.IsNullOrEmpty(_barcode)) return;

        SetLoading(true);

        try
        {
            var result = await _apiClient.LookupProductByBarcodeAsync(_listId, _barcode);

            if (result.Success && result.Data != null)
            {
                _lookupResult = result.Data;
                LookupProductName.Text = result.Data.ProductName;
                LookupPrice.Text = result.Data.Price.HasValue ? $"${result.Data.Price:F2}" : "";
                LookupResultFrame.IsVisible = true;

                // Pre-fill the product name
                ProductNameEntry.Text = result.Data.ProductName;
            }
        }
        catch
        {
            // Lookup is best-effort
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        _ = SearchProductsAsync();
    }

    private void OnSearchClicked(object? sender, EventArgs e)
    {
        _ = SearchProductsAsync();
    }

    private async Task SearchProductsAsync()
    {
        var query = ProductNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            SearchResultsView.IsVisible = false;
            NoResultsLabel.IsVisible = false;
            return;
        }

        // Check connectivity - if marked offline, try to verify server is reachable
        if (!_connectivityService.IsOnline)
        {
            // Try a fresh connectivity check
            var isReachable = await _connectivityService.CheckServerReachableAsync();
            if (!isReachable)
            {
                NoResultsLabel.Text = "Server not reachable - search unavailable";
                NoResultsLabel.IsVisible = true;
                SearchResultsView.IsVisible = false;
                return;
            }
        }

        // Cancel any previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        SetLoading(true);

        try
        {
            System.Diagnostics.Debug.WriteLine($"Searching for '{query}' on list {_listId}");
            var result = await _apiClient.SearchProductsAsync(_listId, query);
            System.Diagnostics.Debug.WriteLine($"Search result: Success={result.Success}, Count={result.Data?.Count ?? 0}");

            if (result.Success && result.Data != null && result.Data.Count > 0)
            {
                SearchResults.Clear();
                foreach (var product in result.Data.Take(10)) // Limit to 10 results
                {
                    SearchResults.Add(product);
                }
                SearchResultsView.ItemsSource = SearchResults;
                SearchResultsView.IsVisible = true;
                NoResultsLabel.IsVisible = false;
            }
            else
            {
                SearchResultsView.IsVisible = false;
                var message = result.Success
                    ? "No products found in store. Enter name manually."
                    : $"Search error: {result.ErrorMessage}";
                NoResultsLabel.Text = message;
                NoResultsLabel.IsVisible = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled, ignore
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search exception: {ex.Message}");
            SearchResultsView.IsVisible = false;
            NoResultsLabel.Text = $"Search failed: {ex.Message}";
            NoResultsLabel.IsVisible = true;
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void OnSearchResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not StoreProductResult selectedProduct)
            return;

        // Clear selection
        SearchResultsView.SelectedItem = null;

        // Apply the selected product
        _lookupResult = selectedProduct;
        ProductNameEntry.Text = selectedProduct.ProductName;

        // Show the lookup result frame
        LookupProductName.Text = selectedProduct.ProductName;
        LookupPrice.Text = selectedProduct.Price.HasValue ? $"${selectedProduct.Price:F2}" : "";
        LookupResultFrame.IsVisible = true;

        // Hide search results
        SearchResultsView.IsVisible = false;
        NoResultsLabel.IsVisible = false;
    }

    private void OnMinusClicked(object? sender, EventArgs e)
    {
        if (_quantity > 1)
        {
            _quantity--;
            QuantityLabel.Text = _quantity.ToString();
        }
    }

    private void OnPlusClicked(object? sender, EventArgs e)
    {
        if (_quantity < 99)
        {
            _quantity++;
            QuantityLabel.Text = _quantity.ToString();
        }
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        var productName = ProductNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(productName))
        {
            await DisplayAlertAsync("Error", "Please enter a product name", "OK");
            return;
        }

        SetLoading(true);

        try
        {
            // Create the new item
            var newItem = new CachedShoppingListItem
            {
                Id = Guid.NewGuid(),
                SessionId = _listId,
                ProductName = productName,
                Amount = _quantity,
                Note = NoteEntry.Text?.Trim(),
                Barcode = _barcode,
                IsPurchased = true, // Auto-check since we just scanned it
                PurchasedAt = DateTime.UtcNow,
                IsNewItem = true,
                Price = _lookupResult?.Price,
                Aisle = _lookupResult?.Aisle,
                Department = _lookupResult?.Department,
                ExternalProductId = _lookupResult?.ExternalProductId
            };

            // Add to local storage
            await _offlineStorage.AddItemToSessionAsync(_listId, newItem);

            // Queue sync operation
            await _offlineStorage.EnqueueOperationAsync(new OfflineOperation
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                OperationType = "AddItem",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ListId = _listId,
                    ProductName = productName,
                    Amount = _quantity,
                    Barcode = _barcode,
                    Note = NoteEntry.Text?.Trim(),
                    IsPurchased = true
                })
            });

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to add item: {ex.Message}", "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        AddButton.IsEnabled = !loading;
    }
}
