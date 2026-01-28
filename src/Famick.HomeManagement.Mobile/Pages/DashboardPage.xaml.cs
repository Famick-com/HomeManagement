using Famick.HomeManagement.Mobile.Models;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private readonly TokenStorage _tokenStorage;
    private readonly TenantStorage _tenantStorage;

    private ShoppingListDashboardDto? _shoppingDashboard;
    private StockStatisticsDto? _stockStatistics;
    private int _overdueChoresCount;
    private int _dueThisWeekCount;

    public DashboardPage(
        ShoppingApiClient apiClient,
        TokenStorage tokenStorage,
        TenantStorage tenantStorage)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _tokenStorage = tokenStorage;
        _tenantStorage = tenantStorage;
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

        // Set page title with tenant name
        Title = await _tenantStorage.GetAppTitleAsync();

        // Refresh Shell title (tenant name may have changed after login)
        if (Shell.Current is AppShell appShell)
        {
            await appShell.RefreshTitleAsync();
        }

        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        ShowLoading(true);
        HideError();

        try
        {
            // Load all data in parallel
            await Task.WhenAll(
                LoadShoppingDashboardAsync(),
                LoadStockStatisticsAsync(),
                LoadChoresDashboardAsync()
            );

            UpdateUI();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load dashboard: {ex.Message}");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async Task LoadShoppingDashboardAsync()
    {
        var result = await _apiClient.GetDashboardAsync();
        if (result.Success && result.Data != null)
        {
            _shoppingDashboard = result.Data;
        }
    }

    private async Task LoadStockStatisticsAsync()
    {
        var result = await _apiClient.GetStockStatisticsAsync();
        if (result.Success && result.Data != null)
        {
            _stockStatistics = result.Data;
        }
    }

    private async Task LoadChoresDashboardAsync()
    {
        // Get overdue chores
        var overdueResult = await _apiClient.GetOverdueChoresAsync();
        if (overdueResult.Success && overdueResult.Data != null)
        {
            _overdueChoresCount = overdueResult.Data.Count;
        }

        // Get chores due this week
        var dueSoonResult = await _apiClient.GetDueSoonChoresAsync(7);
        if (dueSoonResult.Success && dueSoonResult.Data != null)
        {
            _dueThisWeekCount = dueSoonResult.Data.Count;
        }
    }

    private void UpdateUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Shopping stats
            if (_shoppingDashboard != null)
            {
                ShoppingCountLabel.Text = _shoppingDashboard.UnpurchasedItems.ToString();
                ShoppingSubtitleLabel.Text = _shoppingDashboard.TotalLists == 1
                    ? "item in 1 list"
                    : $"items in {_shoppingDashboard.TotalLists} lists";
            }
            else
            {
                ShoppingCountLabel.Text = "0";
                ShoppingSubtitleLabel.Text = "items to buy";
            }

            // Stock alerts
            if (_stockStatistics != null)
            {
                LowStockCountLabel.Text = _stockStatistics.BelowMinStockCount.ToString();
                LowStockSubtitleLabel.Text = _stockStatistics.BelowMinStockCount == 1
                    ? "item low"
                    : "items low";

                // Expiring soon
                ExpiringCountLabel.Text = _stockStatistics.DueSoonCount.ToString();
                ExpiringSubtitleLabel.Text = _stockStatistics.DueSoonCount == 1
                    ? "item expiring"
                    : "items expiring";

                // Update colors based on counts
                if (_stockStatistics.BelowMinStockCount == 0)
                {
                    LowStockCountLabel.TextColor = Color.FromArgb("#4CAF50"); // Green
                    StockIconLabel.TextColor = Color.FromArgb("#4CAF50");
                }

                if (_stockStatistics.DueSoonCount == 0)
                {
                    ExpiringCountLabel.TextColor = Color.FromArgb("#4CAF50"); // Green
                }
            }
            else
            {
                LowStockCountLabel.Text = "0";
                LowStockSubtitleLabel.Text = "items low";
                ExpiringCountLabel.Text = "0";
                ExpiringSubtitleLabel.Text = "items expiring";
            }

            // Chores
            var totalChoresDue = _overdueChoresCount + _dueThisWeekCount;
            ChoresCountLabel.Text = totalChoresDue.ToString();
            ChoresSubtitleLabel.Text = totalChoresDue == 1
                ? "due this week"
                : "due this week";

            // Update chores color based on overdue
            if (_overdueChoresCount > 0)
            {
                ChoresCountLabel.TextColor = Color.FromArgb("#E53935"); // Red
                ChoresIconLabel.TextColor = Color.FromArgb("#E53935");

                // Show overdue alert
                OverdueChoresAlert.IsVisible = true;
                OverdueChoresTitle.Text = _overdueChoresCount == 1
                    ? "1 Overdue Chore"
                    : $"{_overdueChoresCount} Overdue Chores";
                OverdueChoresSubtitle.Text = _overdueChoresCount == 1
                    ? "You have a chore that needs attention"
                    : "You have chores that need attention";
            }
            else
            {
                OverdueChoresAlert.IsVisible = false;
                if (totalChoresDue == 0)
                {
                    ChoresCountLabel.TextColor = Color.FromArgb("#4CAF50"); // Green
                }
            }
        });
    }

    private void ShowLoading(bool isLoading)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;
        });
    }

    private void ShowError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorLabel.Text = message;
            ErrorAlert.IsVisible = true;
        });
    }

    private void HideError()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorAlert.IsVisible = false;
        });
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadDashboardAsync();
        RefreshContainer.IsRefreshing = false;
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadDashboardAsync();
    }

    private async void OnShoppingCardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ListSelectionPage");
    }

    private async void OnStockCardTapped(object? sender, EventArgs e)
    {
        // Stock management is not yet available in mobile - show info
        await DisplayAlertAsync(
            "Stock Management",
            "Stock management is currently only available in the web app. Low stock items should be added to your shopping list.",
            "OK");
    }

    private async void OnChoresCardTapped(object? sender, EventArgs e)
    {
        // Chores are not yet available in mobile - show info
        await DisplayAlertAsync(
            "Chores",
            "Chores management is currently only available in the web app.",
            "OK");
    }

    private async void OnExpiringCardTapped(object? sender, EventArgs e)
    {
        // Expiring items are not yet available in mobile - show info
        await DisplayAlertAsync(
            "Expiring Items",
            "Expiring items management is currently only available in the web app.",
            "OK");
    }

    private async void OnStartShoppingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ListSelectionPage");
    }

    private async void OnViewListsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ListSelectionPage");
    }
}
