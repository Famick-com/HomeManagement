using System.Collections.ObjectModel;
using Famick.HomeManagement.ShoppingMode.Models;
using Famick.HomeManagement.ShoppingMode.Services;

namespace Famick.HomeManagement.ShoppingMode.Pages;

public partial class ListSelectionPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private readonly TokenStorage _tokenStorage;
    private readonly LocationService _locationService;
    private readonly OfflineStorageService _offlineStorage;

    public ObservableCollection<ShoppingListSummary> ShoppingLists { get; } = new();

    public ListSelectionPage(
        ShoppingApiClient apiClient,
        TokenStorage tokenStorage,
        LocationService locationService,
        OfflineStorageService offlineStorage)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _tokenStorage = tokenStorage;
        _locationService = locationService;
        _offlineStorage = offlineStorage;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if logged in
        var token = await _tokenStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            // Show login page modally
            var loginPage = Application.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
            if (loginPage != null)
            {
                await Navigation.PushModalAsync(new NavigationPage(loginPage));
            }
            return;
        }

        await LoadShoppingListsAsync();
    }

    private async Task LoadShoppingListsAsync()
    {
        ShowLoading();

        try
        {
            var result = await _apiClient.GetShoppingListsAsync();

            if (result.Success && result.Data != null)
            {
                // Get local purchase counts to merge with API data
                var localCounts = await _offlineStorage.GetAllLocalPurchaseCountsAsync();

                ShoppingLists.Clear();
                foreach (var list in result.Data)
                {
                    // Override with local counts if we have a cached session
                    if (localCounts.TryGetValue(list.Id, out var counts))
                    {
                        list.TotalItems = counts.total;
                        list.PurchasedItems = counts.purchased;
                    }
                    ShoppingLists.Add(list);
                }

                if (ShoppingLists.Count == 0)
                {
                    ShowEmpty();
                }
                else
                {
                    ShowContent();
                }
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Failed to load shopping lists");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
    }

    private async void OnListSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ShoppingListSummary selectedList)
            return;

        // Clear selection for next time
        ListsCollection.SelectedItem = null;

        // Check store location and navigate
        await StartShoppingSessionAsync(selectedList);
    }

    private async Task StartShoppingSessionAsync(ShoppingListSummary list)
    {
        // Try to detect current location and match to store
        var detectedStore = await DetectNearbyStoreAsync(list);

        if (detectedStore != null && detectedStore.Id != list.ShoppingLocationId)
        {
            // Ask user if they want to switch stores
            var switchStore = await DisplayAlert(
                "Different Store Detected",
                $"You appear to be at {detectedStore.Name}. Would you like to switch to this store?",
                "Yes, Switch",
                "No, Keep Original");

            // TODO: If switching, update the list's store (would need API call)
        }

        // Navigate to shopping session
        var navigationParameter = new Dictionary<string, object>
        {
            { "ListId", list.Id.ToString() },
            { "ListName", list.Name }
        };

        await Shell.Current.GoToAsync(nameof(ShoppingSessionPage), navigationParameter);
    }

    private async Task<StoreSummary?> DetectNearbyStoreAsync(ShoppingListSummary list)
    {
        try
        {
            if (!_locationService.IsLocationEnabled)
                return null;

            var location = await _locationService.GetCurrentLocationAsync();
            if (location == null)
                return null;

            // Get stores and find nearest
            var storesResult = await _apiClient.GetShoppingLocationsAsync();
            if (storesResult.Success && storesResult.Data != null)
            {
                return _locationService.FindNearestStore(storesResult.Data, location, maxDistanceMeters: 500);
            }
        }
        catch
        {
            // Location detection is best-effort
        }

        return null;
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        _ = LoadShoppingListsAsync();
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadShoppingListsAsync();
        RefreshContainer.IsRefreshing = false;
    }

    private void ShowLoading()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        RefreshContainer.IsVisible = false;
        EmptyState.IsVisible = false;
        ErrorFrame.IsVisible = false;
    }

    private void ShowContent()
    {
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        RefreshContainer.IsVisible = true;
        EmptyState.IsVisible = false;
        ErrorFrame.IsVisible = false;
    }

    private void ShowEmpty()
    {
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        RefreshContainer.IsVisible = false;
        EmptyState.IsVisible = true;
        ErrorFrame.IsVisible = false;
    }

    private void ShowError(string message)
    {
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        RefreshContainer.IsVisible = false;
        EmptyState.IsVisible = false;
        ErrorFrame.IsVisible = true;
        ErrorLabel.Text = message;
    }

}
