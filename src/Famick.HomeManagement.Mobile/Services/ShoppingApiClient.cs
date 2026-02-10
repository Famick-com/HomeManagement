using System.Net.Http.Json;
using Famick.HomeManagement.Mobile.Models;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// API client for shopping-related operations.
/// Auth headers are attached centrally by AuthenticatingHttpHandler.
/// </summary>
public class ShoppingApiClient
{
    private readonly HttpClient _httpClient;

    public ShoppingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Check if the server is reachable.
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
            {
                email,
                password
            });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, options);
                return result != null
                    ? ApiResult<LoginResponse>.Ok(result)
                    : ApiResult<LoginResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<LoginResponse>.Fail(ParseErrorMessage(error) ?? "Login failed");
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Change the authenticated user's password.
    /// </summary>
    public async Task<ApiResult<object>> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/profile/change-password", new
            {
                currentPassword,
                newPassword,
                confirmPassword
            });

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<object>.Ok(new object());
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<object>.Fail(ParseErrorMessage(error) ?? "Failed to change password");
        }
        catch (Exception ex)
        {
            return ApiResult<object>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all shopping lists.
    /// </summary>
    public async Task<ApiResult<List<ShoppingListSummary>>> GetShoppingListsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/shoppinglists").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ShoppingListSummary>>();
                return result != null
                    ? ApiResult<List<ShoppingListSummary>>.Ok(result)
                    : ApiResult<List<ShoppingListSummary>>.Fail("Invalid response");
            }

            return ApiResult<List<ShoppingListSummary>>.Fail("Failed to load shopping lists");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ShoppingListSummary>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a shopping list with all items.
    /// </summary>
    public async Task<ApiResult<ShoppingListDetail>> GetShoppingListAsync(Guid listId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/shoppinglists/{listId}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ShoppingListDetail>();
                return result != null
                    ? ApiResult<ShoppingListDetail>.Ok(result)
                    : ApiResult<ShoppingListDetail>.Fail("Invalid response");
            }

            return ApiResult<ShoppingListDetail>.Fail("Failed to load shopping list");
        }
        catch (Exception ex)
        {
            return ApiResult<ShoppingListDetail>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all shopping locations (stores).
    /// </summary>
    public async Task<ApiResult<List<StoreSummary>>> GetShoppingLocationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/shoppinglocations").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<StoreSummary>>();
                return result != null
                    ? ApiResult<List<StoreSummary>>.Ok(result)
                    : ApiResult<List<StoreSummary>>.Fail("Invalid response");
            }

            return ApiResult<List<StoreSummary>>.Fail("Failed to load stores");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StoreSummary>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle the purchased status of an item.
    /// </summary>
    public async Task<ApiResult<bool>> TogglePurchasedAsync(Guid listId, Guid itemId, DateTime? bestBeforeDate = null)
    {
        try
        {
            HttpResponseMessage response;
            if (bestBeforeDate.HasValue)
            {
                response = await _httpClient.PostAsJsonAsync(
                    $"api/v1/shoppinglists/{listId}/items/{itemId}/toggle-purchased",
                    new { BestBeforeDate = bestBeforeDate.Value.ToUniversalTime() });
            }
            else
            {
                response = await _httpClient.PostAsync(
                    $"api/v1/shoppinglists/{listId}/items/{itemId}/toggle-purchased",
                    null);
            }

            // Treat 404 Not Found as success - item doesn't exist, nothing to toggle
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine($"TogglePurchased: Item {itemId} not found, treating as success");
                return ApiResult<bool>.Ok(true);
            }

            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to update item");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Increment purchased quantity for an item via scan (default +1).
    /// </summary>
    public async Task<ApiResult<ScanPurchaseResult>> ScanPurchaseAsync(
        Guid listId,
        Guid itemId,
        decimal quantity = 1,
        DateTime? bestBeforeDate = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}/scan-purchase",
                new { Quantity = quantity, BestBeforeDate = bestBeforeDate });

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ApiResult<ScanPurchaseResult>.Fail("Item not found");
            }

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ScanPurchaseResult>(content, options);
                return result != null
                    ? ApiResult<ScanPurchaseResult>.Ok(result)
                    : ApiResult<ScanPurchaseResult>.Fail("Invalid response");
            }

            return ApiResult<ScanPurchaseResult>.Fail("Failed to process scan purchase");
        }
        catch (Exception ex)
        {
            return ApiResult<ScanPurchaseResult>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Quick add an item to a shopping list.
    /// </summary>
    public async Task<ApiResult<bool>> QuickAddItemAsync(
        Guid listId,
        string productName,
        decimal amount,
        string? barcode,
        string? note,
        bool isPurchased = true,
        string? aisle = null,
        string? department = null,
        string? externalProductId = null,
        decimal? price = null,
        string? imageUrl = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/shoppinglists/quick-add", new
            {
                shoppingListId = listId,
                productName,
                amount,
                barcode,
                note,
                lookupInStore = string.IsNullOrEmpty(externalProductId), // Only lookup if we don't have store data
                isPurchased,
                aisle,
                department,
                externalProductId,
                price,
                imageUrl
            });

            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to add item");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Scan a barcode against a shopping list to find matching items (direct or child products).
    /// </summary>
    public async Task<ApiResult<BarcodeScanResult>> ScanBarcodeAsync(Guid listId, string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/shoppinglists/{listId}/scan-barcode?barcode={Uri.EscapeDataString(barcode)}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<BarcodeScanResult>(content, options);
                return result != null
                    ? ApiResult<BarcodeScanResult>.Ok(result)
                    : ApiResult<BarcodeScanResult>.Fail("Invalid response");
            }

            return ApiResult<BarcodeScanResult>.Fail("Failed to scan barcode");
        }
        catch (Exception ex)
        {
            return ApiResult<BarcodeScanResult>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Lookup a product by barcode using store integration.
    /// </summary>
    public async Task<ApiResult<StoreProductResult>> LookupProductByBarcodeAsync(Guid listId, string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/shoppinglists/{listId}/lookup-barcode?barcode={Uri.EscapeDataString(barcode)}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StoreProductResult>();
                return result != null
                    ? ApiResult<StoreProductResult>.Ok(result)
                    : ApiResult<StoreProductResult>.Fail("Product not found");
            }

            return ApiResult<StoreProductResult>.Fail("Product not found");
        }
        catch (Exception ex)
        {
            return ApiResult<StoreProductResult>.Fail($"Lookup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Search for products by name using store integration.
    /// </summary>
    public async Task<ApiResult<List<StoreProductResult>>> SearchProductsAsync(Guid listId, string query)
    {
        try
        {
            var url = $"api/v1/shoppinglists/{listId}/search-products?query={Uri.EscapeDataString(query)}";
            System.Diagnostics.Debug.WriteLine($"SearchProducts URL: {_httpClient.BaseAddress}{url}");

            var response = await _httpClient.GetAsync(url);
            System.Diagnostics.Debug.WriteLine($"SearchProducts Response: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();

            // Check if response is HTML (indicates server error or missing endpoint)
            if (content.TrimStart().StartsWith('<'))
            {
                System.Diagnostics.Debug.WriteLine($"SearchProducts got HTML response - endpoint may not exist");
                return ApiResult<List<StoreProductResult>>.Fail("Search not available - server may need restart");
            }

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = System.Text.Json.JsonSerializer.Deserialize<List<StoreProductResult>>(content, options);
                System.Diagnostics.Debug.WriteLine($"SearchProducts Deserialized {result?.Count ?? 0} results");
                if (result?.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"First result Name: '{result[0].Name}', ProductName: '{result[0].ProductName}'");
                }
                return result != null
                    ? ApiResult<List<StoreProductResult>>.Ok(result)
                    : ApiResult<List<StoreProductResult>>.Ok(new List<StoreProductResult>());
            }

            System.Diagnostics.Debug.WriteLine($"SearchProducts Error: {content.Substring(0, Math.Min(200, content.Length))}");
            return ApiResult<List<StoreProductResult>>.Fail($"Search failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SearchProducts Exception: {ex.Message}");
            return ApiResult<List<StoreProductResult>>.Fail($"Search error: {ex.Message}");
        }
    }

    /// <summary>
    /// Autocomplete search for tenant products.
    /// </summary>
    public async Task<ApiResult<List<ProductAutocompleteResult>>> AutocompleteProductsAsync(string query, int maxResults = 10)
    {
        try
        {
            var url = $"api/v1/products/autocomplete?q={Uri.EscapeDataString(query)}&maxResults={maxResults}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<ProductAutocompleteResult>>(content, options);
                return result != null
                    ? ApiResult<List<ProductAutocompleteResult>>.Ok(result)
                    : ApiResult<List<ProductAutocompleteResult>>.Ok(new List<ProductAutocompleteResult>());
            }

            return ApiResult<List<ProductAutocompleteResult>>.Fail($"Search failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ProductAutocompleteResult>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a product from external lookup data.
    /// </summary>
    public async Task<ApiResult<ProductCreatedResult>> CreateProductFromLookupAsync(CreateProductFromLookupMobileRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/products/from-lookup", request);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProductCreatedResult>(content, options);
                return result != null
                    ? ApiResult<ProductCreatedResult>.Ok(result)
                    : ApiResult<ProductCreatedResult>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<ProductCreatedResult>.Fail(ParseErrorMessage(error) ?? "Failed to create product");
        }
        catch (Exception ex)
        {
            return ApiResult<ProductCreatedResult>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Move purchased items to inventory.
    /// </summary>
    public async Task<ApiResult<MoveToInventoryResponse>> MoveToInventoryAsync(MoveToInventoryRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/shoppinglists/{request.ShoppingListId}/move-to-inventory",
                request);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<MoveToInventoryResponse>(content, options);
                return result != null
                    ? ApiResult<MoveToInventoryResponse>.Ok(result)
                    : ApiResult<MoveToInventoryResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<MoveToInventoryResponse>.Fail(
                error.Length > 200 ? "Failed to move items to inventory" : error);
        }
        catch (Exception ex)
        {
            return ApiResult<MoveToInventoryResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get tenant information.
    /// </summary>
    public async Task<ApiResult<TenantInfo>> GetTenantAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/tenant");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = System.Text.Json.JsonSerializer.Deserialize<TenantInfo>(content, options);
                return result != null
                    ? ApiResult<TenantInfo>.Ok(result)
                    : ApiResult<TenantInfo>.Fail("Invalid response");
            }

            return ApiResult<TenantInfo>.Fail("Failed to get tenant info");
        }
        catch (Exception ex)
        {
            return ApiResult<TenantInfo>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get aisle order for a shopping location.
    /// </summary>
    public async Task<ApiResult<AisleOrderDto>> GetAisleOrderAsync(Guid locationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/shoppinglocations/{locationId}/aisle-order");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AisleOrderDto>();
                return result != null
                    ? ApiResult<AisleOrderDto>.Ok(result)
                    : ApiResult<AisleOrderDto>.Fail("Invalid response");
            }

            return ApiResult<AisleOrderDto>.Fail("Failed to load aisle order");
        }
        catch (Exception ex)
        {
            return ApiResult<AisleOrderDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Update aisle order for a shopping location.
    /// </summary>
    public async Task<ApiResult<AisleOrderDto>> UpdateAisleOrderAsync(Guid locationId, UpdateAisleOrderRequest request)
    {
        try
        {

            // Debug: Log what we're sending
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
            System.Diagnostics.Debug.WriteLine($"[UpdateAisleOrder] Sending: {requestJson}");

            var response = await _httpClient.PutAsJsonAsync(
                $"api/v1/shoppinglocations/{locationId}/aisle-order",
                request);

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[UpdateAisleOrder] Response {response.StatusCode}: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = System.Text.Json.JsonSerializer.Deserialize<AisleOrderDto>(responseContent, options);
                return result != null
                    ? ApiResult<AisleOrderDto>.Ok(result)
                    : ApiResult<AisleOrderDto>.Fail("Invalid response");
            }

            return ApiResult<AisleOrderDto>.Fail($"Failed: {response.StatusCode} - {responseContent}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateAisleOrder] Exception: {ex}");
            return ApiResult<AisleOrderDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Reset aisle order to default for a shopping location.
    /// </summary>
    public async Task<ApiResult<bool>> ResetAisleOrderAsync(Guid locationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/v1/shoppinglocations/{locationId}/aisle-order");

            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to reset aisle order");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an item's quantity (and note) on a shopping list.
    /// </summary>
    public async Task<ApiResult<bool>> UpdateItemQuantityAsync(Guid listId, Guid itemId, decimal amount, string? note)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}",
                new { Amount = amount, Note = note });

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiResult<bool>.Ok(true);

            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to update item quantity");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove an item from a shopping list.
    /// </summary>
    public async Task<ApiResult<bool>> RemoveItemAsync(Guid listId, Guid itemId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiResult<bool>.Ok(true);

            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to remove item");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    #region Dashboard APIs

    /// <summary>
    /// Get shopping list dashboard summary.
    /// </summary>
    public async Task<ApiResult<ShoppingListDashboardDto>> GetDashboardAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/shoppinglists/dashboard");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ShoppingListDashboardDto>(content, options);
                return result != null
                    ? ApiResult<ShoppingListDashboardDto>.Ok(result)
                    : ApiResult<ShoppingListDashboardDto>.Fail("Invalid response");
            }

            return ApiResult<ShoppingListDashboardDto>.Fail("Failed to load dashboard");
        }
        catch (Exception ex)
        {
            return ApiResult<ShoppingListDashboardDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get stock statistics for dashboard.
    /// </summary>
    public async Task<ApiResult<StockStatisticsDto>> GetStockStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/stock/statistics");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<StockStatisticsDto>(content, options);
                return result != null
                    ? ApiResult<StockStatisticsDto>.Ok(result)
                    : ApiResult<StockStatisticsDto>.Fail("Invalid response");
            }

            return ApiResult<StockStatisticsDto>.Fail("Failed to load stock statistics");
        }
        catch (Exception ex)
        {
            return ApiResult<StockStatisticsDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get overdue chores for dashboard.
    /// </summary>
    public async Task<ApiResult<List<ChoreSummaryDto>>> GetOverdueChoresAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/chores/overdue");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<ChoreSummaryDto>>(content, options);
                return result != null
                    ? ApiResult<List<ChoreSummaryDto>>.Ok(result)
                    : ApiResult<List<ChoreSummaryDto>>.Ok(new List<ChoreSummaryDto>());
            }

            return ApiResult<List<ChoreSummaryDto>>.Fail("Failed to load overdue chores");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ChoreSummaryDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get chores due within the specified number of days.
    /// </summary>
    public async Task<ApiResult<List<ChoreSummaryDto>>> GetDueSoonChoresAsync(int daysAhead = 7)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/chores/due-soon?daysAhead={daysAhead}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<ChoreSummaryDto>>(content, options);
                return result != null
                    ? ApiResult<List<ChoreSummaryDto>>.Ok(result)
                    : ApiResult<List<ChoreSummaryDto>>.Ok(new List<ChoreSummaryDto>());
            }

            return ApiResult<List<ChoreSummaryDto>>.Fail("Failed to load due soon chores");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ChoreSummaryDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region Registration APIs

    /// <summary>
    /// Start the registration process by sending a verification email.
    /// </summary>
    public async Task<ApiResult<StartRegistrationResponse>> StartRegistrationAsync(string householdName, string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/start-registration", new
            {
                householdName,
                email
            });

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<StartRegistrationResponse>(content, options);
                return result != null
                    ? ApiResult<StartRegistrationResponse>.Ok(result)
                    : ApiResult<StartRegistrationResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<StartRegistrationResponse>.Fail(ParseErrorMessage(error) ?? "Registration failed");
        }
        catch (Exception ex)
        {
            return ApiResult<StartRegistrationResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify email with the token from the verification email.
    /// </summary>
    public async Task<ApiResult<VerifyEmailResponse>> VerifyEmailAsync(string token)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/verify-email", new
            {
                token
            });

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<VerifyEmailResponse>(content, options);
                return result != null
                    ? ApiResult<VerifyEmailResponse>.Ok(result)
                    : ApiResult<VerifyEmailResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<VerifyEmailResponse>.Fail(ParseErrorMessage(error) ?? "Verification failed");
        }
        catch (Exception ex)
        {
            return ApiResult<VerifyEmailResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Complete registration after email verification.
    /// </summary>
    public async Task<ApiResult<CompleteRegistrationResponse>> CompleteRegistrationAsync(
        string token,
        string firstName,
        string lastName,
        string password)
    {
        try
        {
            Console.WriteLine($"[CompleteRegistration] Making request to: {_httpClient.BaseAddress}api/auth/complete-registration");

            var response = await _httpClient.PostAsJsonAsync("api/auth/complete-registration", new
            {
                token,
                firstName,
                lastName,
                password
            });

            Console.WriteLine($"[CompleteRegistration] Response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CompleteRegistration] Response content: {content}");
                var result = System.Text.Json.JsonSerializer.Deserialize<CompleteRegistrationResponse>(content, options);
                return result != null
                    ? ApiResult<CompleteRegistrationResponse>.Ok(result)
                    : ApiResult<CompleteRegistrationResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[CompleteRegistration] Error response: {error}");
            return ApiResult<CompleteRegistrationResponse>.Fail(ParseErrorMessage(error) ?? "Registration failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteRegistration] Exception: {ex.GetType().Name}: {ex.Message}");
            return ApiResult<CompleteRegistrationResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Resend the verification email.
    /// </summary>
    public async Task<ApiResult<StartRegistrationResponse>> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/resend-verification", new
            {
                email
            });

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<StartRegistrationResponse>(content, options);
                return result != null
                    ? ApiResult<StartRegistrationResponse>.Ok(result)
                    : ApiResult<StartRegistrationResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<StartRegistrationResponse>.Fail(ParseErrorMessage(error) ?? "Failed to resend");
        }
        catch (Exception ex)
        {
            return ApiResult<StartRegistrationResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region OAuth APIs

    /// <summary>
    /// Get the authentication configuration including enabled OAuth providers.
    /// </summary>
    public async Task<ApiResult<AuthConfiguration>> GetAuthConfigurationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/external/config");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthConfiguration>(content, options);
                return result != null
                    ? ApiResult<AuthConfiguration>.Ok(result)
                    : ApiResult<AuthConfiguration>.Fail("Invalid response");
            }

            return ApiResult<AuthConfiguration>.Fail("Failed to get auth configuration");
        }
        catch (Exception ex)
        {
            return ApiResult<AuthConfiguration>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get OAuth challenge URL for a provider.
    /// </summary>
    /// <param name="provider">Provider name (Google, Apple, OIDC)</param>
    /// <param name="callbackUrl">Custom callback URL for mobile app</param>
    public async Task<ApiResult<OAuthChallengeResponse>> GetOAuthChallengeAsync(string provider, string callbackUrl)
    {
        try
        {
            var encodedCallback = Uri.EscapeDataString(callbackUrl);
            var response = await _httpClient.GetAsync(
                $"api/auth/external/{provider}/challenge?callbackUrl={encodedCallback}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<OAuthChallengeResponse>(content, options);
                return result != null
                    ? ApiResult<OAuthChallengeResponse>.Ok(result)
                    : ApiResult<OAuthChallengeResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<OAuthChallengeResponse>.Fail(ParseErrorMessage(error) ?? "Failed to get authorization URL");
        }
        catch (Exception ex)
        {
            return ApiResult<OAuthChallengeResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process OAuth callback and exchange code for tokens.
    /// </summary>
    /// <param name="provider">Provider name (Google, Apple, OIDC)</param>
    /// <param name="code">Authorization code from provider</param>
    /// <param name="state">State parameter for CSRF validation</param>
    /// <param name="rememberMe">Whether to extend refresh token lifetime</param>
    public async Task<ApiResult<LoginResponse>> ProcessOAuthCallbackAsync(
        string provider,
        string code,
        string state,
        bool rememberMe = false)
    {
        try
        {
            var request = new OAuthCallbackRequest
            {
                Code = code,
                State = state,
                RememberMe = rememberMe
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"api/auth/external/{provider}/callback",
                request);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, options);
                return result != null
                    ? ApiResult<LoginResponse>.Ok(result)
                    : ApiResult<LoginResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            var errorMessage = ParseErrorMessage(error);

            // Handle specific error cases
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed. Please try again.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ApiResult<LoginResponse>.Fail("Your account is inactive. Please contact support.");
            }

            return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed");
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process native Apple Sign in from iOS devices.
    /// </summary>
    public async Task<ApiResult<LoginResponse>> ProcessNativeAppleSignInAsync(NativeAppleSignInRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/external/apple/native",
                request);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, options);
                return result != null
                    ? ApiResult<LoginResponse>.Ok(result)
                    : ApiResult<LoginResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            var errorMessage = ParseErrorMessage(error);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed. Please try again.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ApiResult<LoginResponse>.Fail("Your account is inactive. Please contact support.");
            }

            return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed");
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Process native Google Sign in from iOS and Android devices.
    /// </summary>
    public async Task<ApiResult<LoginResponse>> ProcessNativeGoogleSignInAsync(NativeGoogleSignInRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/external/google/native",
                request);

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, options);
                return result != null
                    ? ApiResult<LoginResponse>.Ok(result)
                    : ApiResult<LoginResponse>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            var errorMessage = ParseErrorMessage(error);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed. Please try again.");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ApiResult<LoginResponse>.Fail("Your account is inactive. Please contact support.");
            }

            return ApiResult<LoginResponse>.Fail(errorMessage ?? "Authentication failed");
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region Wizard APIs

    public async Task<ApiResult<WizardStateDto>> GetWizardStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/wizard/state");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<WizardStateDto>(content, options);
                return result != null
                    ? ApiResult<WizardStateDto>.Ok(result)
                    : ApiResult<WizardStateDto>.Fail("Invalid response");
            }
            return ApiResult<WizardStateDto>.Fail("Failed to load wizard state");
        }
        catch (Exception ex)
        {
            return ApiResult<WizardStateDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> SaveHouseholdInfoAsync(HouseholdInfoDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/wizard/household-info", dto);
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Ok(true);

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? $"Failed to save household info ({response.StatusCode})");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<NormalizedAddressResult>> NormalizeAddressAsync(NormalizeAddressRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/addresses/normalize", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<NormalizedAddressResult>(content, options);
                return result != null
                    ? ApiResult<NormalizedAddressResult>.Ok(result)
                    : ApiResult<NormalizedAddressResult>.Fail("No normalization result");
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiResult<NormalizedAddressResult>.Fail("Address not found");

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<NormalizedAddressResult>.Fail(ParseErrorMessage(error) ?? "Address normalization failed");
        }
        catch (Exception ex)
        {
            return ApiResult<NormalizedAddressResult>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> SaveHomeStatisticsAsync(HomeStatisticsDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/wizard/home-statistics", dto);
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Ok(true);

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? $"Failed to save home statistics ({response.StatusCode})");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> SaveMaintenanceItemsAsync(MaintenanceItemsDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/wizard/maintenance-items", dto);
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Ok(true);

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? $"Failed to save maintenance items ({response.StatusCode})");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<HouseholdMemberDto>>> GetHouseholdMembersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/wizard/members");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<HouseholdMemberDto>>(content, options);
                return result != null
                    ? ApiResult<List<HouseholdMemberDto>>.Ok(result)
                    : ApiResult<List<HouseholdMemberDto>>.Ok(new List<HouseholdMemberDto>());
            }
            return ApiResult<List<HouseholdMemberDto>>.Fail("Failed to load household members");
        }
        catch (Exception ex)
        {
            return ApiResult<List<HouseholdMemberDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> SaveCurrentUserContactAsync(SaveCurrentUserContactRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/wizard/members/me", request);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to save your info");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<HouseholdMemberDto>> AddHouseholdMemberAsync(AddHouseholdMemberRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/wizard/members", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<HouseholdMemberDto>(content, options);
                return result != null
                    ? ApiResult<HouseholdMemberDto>.Ok(result)
                    : ApiResult<HouseholdMemberDto>.Fail("Invalid response");
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<HouseholdMemberDto>.Fail(ParseErrorMessage(error) ?? "Failed to add member");
        }
        catch (Exception ex)
        {
            return ApiResult<HouseholdMemberDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> UpdateHouseholdMemberAsync(Guid contactId, UpdateHouseholdMemberRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/wizard/members/{contactId}", request);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to update member");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> DeleteHouseholdMemberAsync(Guid contactId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/wizard/members/{contactId}");
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to remove member");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<DuplicateContactResultDto>> CheckDuplicateContactAsync(CheckDuplicateContactRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/wizard/members/check-duplicate", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<DuplicateContactResultDto>(content, options);
                return result != null
                    ? ApiResult<DuplicateContactResultDto>.Ok(result)
                    : ApiResult<DuplicateContactResultDto>.Fail("Invalid response");
            }
            return ApiResult<DuplicateContactResultDto>.Fail("Failed to check for duplicates");
        }
        catch (Exception ex)
        {
            return ApiResult<DuplicateContactResultDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> CompleteWizardAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/v1/wizard/complete", null);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to complete wizard");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region Vehicle APIs

    public async Task<ApiResult<List<VehicleSummaryDto>>> GetVehiclesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/vehicles");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<VehicleSummaryDto>>(content, options);
                return result != null
                    ? ApiResult<List<VehicleSummaryDto>>.Ok(result)
                    : ApiResult<List<VehicleSummaryDto>>.Ok(new List<VehicleSummaryDto>());
            }
            return ApiResult<List<VehicleSummaryDto>>.Fail("Failed to load vehicles");
        }
        catch (Exception ex)
        {
            return ApiResult<List<VehicleSummaryDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<VehicleSummaryDto>> CreateVehicleAsync(CreateVehicleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/vehicles", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<VehicleSummaryDto>(content, options);
                return result != null
                    ? ApiResult<VehicleSummaryDto>.Ok(result)
                    : ApiResult<VehicleSummaryDto>.Fail("Invalid response");
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<VehicleSummaryDto>.Fail(ParseErrorMessage(error) ?? "Failed to create vehicle");
        }
        catch (Exception ex)
        {
            return ApiResult<VehicleSummaryDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> UpdateVehicleAsync(Guid id, UpdateVehicleRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/vehicles/{id}", request);
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to update vehicle");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> DeleteVehicleAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/vehicles/{id}");
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to delete vehicle");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region Quick Consume APIs

    /// <summary>
    /// Get a product by barcode from local inventory.
    /// </summary>
    public async Task<ApiResult<ProductDto>> GetProductByBarcodeAsync(string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/products/by-barcode/{Uri.EscapeDataString(barcode)}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProductDto>(content, options);
                return result != null
                    ? ApiResult<ProductDto>.Ok(result)
                    : ApiResult<ProductDto>.Fail("Invalid response");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ApiResult<ProductDto>.Fail("Product not found in inventory");
            }

            return ApiResult<ProductDto>.Fail("Failed to lookup product");
        }
        catch (Exception ex)
        {
            return ApiResult<ProductDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all stock entries for a product (sorted by expiry - FEFO order).
    /// </summary>
    public async Task<ApiResult<List<StockEntryDto>>> GetStockByProductAsync(Guid productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/stock/by-product/{productId}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<StockEntryDto>>(content, options);
                return result != null
                    ? ApiResult<List<StockEntryDto>>.Ok(result)
                    : ApiResult<List<StockEntryDto>>.Ok(new List<StockEntryDto>());
            }

            return ApiResult<List<StockEntryDto>>.Fail("Failed to load stock entries");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StockEntryDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Quick consume using FEFO (First Expired, First Out).
    /// </summary>
    public async Task<ApiResult<bool>> QuickConsumeAsync(QuickConsumeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/stock/quick-consume", request);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to consume stock");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Consume a specific stock entry by ID.
    /// </summary>
    public async Task<ApiResult<bool>> ConsumeStockEntryAsync(Guid stockEntryId, decimal amount, bool spoiled = false)
    {
        try
        {
            var request = new ConsumeStockRequest
            {
                Amount = amount,
                Spoiled = spoiled
            };
            var response = await _httpClient.PostAsJsonAsync($"api/v1/stock/{stockEntryId}/consume", request);

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to consume stock");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh widget data by fetching stock statistics and expiring products.
    /// This updates platform-specific widget data stores.
    /// </summary>
    public async Task RefreshWidgetDataAsync()
    {
        var result = await GetStockStatisticsAsync();
        if (result.Success && result.Data != null)
        {
#if IOS
            // Fetch expiring products for Lock Screen widget
            var products = await GetExpiringProductsForWidgetAsync();
            Platforms.iOS.WidgetDataService.UpdateWidgetDataWithProducts(
                result.Data.ExpiredCount,
                result.Data.DueSoonCount,
                products);
#elif ANDROID
            Platforms.Android.WidgetDataService.UpdateWidgetData(
                Android.App.Application.Context,
                result.Data.ExpiredCount,
                result.Data.DueSoonCount);
#endif
        }
    }

#if IOS
    /// <summary>
    /// Fetches the top expiring products (expired first, then due-soon) for widget display.
    /// </summary>
    private async Task<List<Platforms.iOS.WidgetProductItem>> GetExpiringProductsForWidgetAsync()
    {
        var widgetProducts = new List<Platforms.iOS.WidgetProductItem>();

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Fetch expired items sorted by nearest due date
            var expiredResponse = await _httpClient.GetAsync("api/v1/stock/overview?status=expired&sortBy=nextDueDate");
            if (expiredResponse.IsSuccessStatusCode)
            {
                var content = await expiredResponse.Content.ReadAsStringAsync();
                var items = System.Text.Json.JsonSerializer.Deserialize<List<StockOverviewItemDto>>(content, options);
                if (items != null)
                {
                    widgetProducts.AddRange(items.Select(MapToWidgetProduct));
                }
            }

            // Fetch due-soon items sorted by nearest due date
            var dueSoonResponse = await _httpClient.GetAsync("api/v1/stock/overview?status=dueSoon&sortBy=nextDueDate");
            if (dueSoonResponse.IsSuccessStatusCode)
            {
                var content = await dueSoonResponse.Content.ReadAsStringAsync();
                var items = System.Text.Json.JsonSerializer.Deserialize<List<StockOverviewItemDto>>(content, options);
                if (items != null)
                {
                    widgetProducts.AddRange(items.Select(MapToWidgetProduct));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShoppingApiClient] Error fetching expiring products for widget: {ex.Message}");
        }

        return widgetProducts.Take(5).ToList();
    }

    private static Platforms.iOS.WidgetProductItem MapToWidgetProduct(StockOverviewItemDto item)
    {
        return new Platforms.iOS.WidgetProductItem
        {
            ProductId = item.ProductId.ToString(),
            ProductName = item.ProductName,
            TotalAmount = (double)item.TotalAmount,
            QuantityUnit = item.QuantityUnitName,
            BestBeforeDate = item.NextDueDate?.ToString("yyyy-MM-dd"),
            DaysUntilExpiry = item.DaysUntilDue,
            IsExpired = item.IsExpired
        };
    }
#endif

    #endregion

    #region Property Link APIs

    public async Task<ApiResult<List<PropertyLinkDto>>> GetPropertyLinksAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/home/property-links");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<PropertyLinkDto>>(content, options);
                return result != null
                    ? ApiResult<List<PropertyLinkDto>>.Ok(result)
                    : ApiResult<List<PropertyLinkDto>>.Ok(new List<PropertyLinkDto>());
            }
            return ApiResult<List<PropertyLinkDto>>.Fail("Failed to load property links");
        }
        catch (Exception ex)
        {
            return ApiResult<List<PropertyLinkDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<PropertyLinkDto>> CreatePropertyLinkAsync(CreatePropertyLinkRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/home/property-links", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<PropertyLinkDto>(content, options);
                return result != null
                    ? ApiResult<PropertyLinkDto>.Ok(result)
                    : ApiResult<PropertyLinkDto>.Fail("Invalid response");
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<PropertyLinkDto>.Fail(ParseErrorMessage(error) ?? "Failed to create property link");
        }
        catch (Exception ex)
        {
            return ApiResult<PropertyLinkDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    public async Task<ApiResult<bool>> DeletePropertyLinkAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/home/property-links/{id}");
            return response.IsSuccessStatusCode
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Fail("Failed to delete property link");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region Child Product Operations

    /// <summary>
    /// Gets child products for a parent product on a shopping list item.
    /// Only returns children with store metadata for the shopping list's store.
    /// </summary>
    public async Task<ApiResult<List<ChildProductDto>>> GetChildProductsAsync(Guid listId, Guid itemId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/shoppinglists/{listId}/items/{itemId}/children");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ChildProductDto>>();
                return result != null
                    ? ApiResult<List<ChildProductDto>>.Ok(result)
                    : ApiResult<List<ChildProductDto>>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<List<ChildProductDto>>.Fail(ParseErrorMessage(error) ?? "Failed to get child products");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ChildProductDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks off a specific child product with quantity.
    /// </summary>
    public async Task<ApiResult<ShoppingListItemDto>> CheckOffChildAsync(
        Guid listId,
        Guid itemId,
        CheckOffChildRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}/check-off-child",
                request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ShoppingListItemDto>();
                return result != null
                    ? ApiResult<ShoppingListItemDto>.Ok(result)
                    : ApiResult<ShoppingListItemDto>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<ShoppingListItemDto>.Fail(ParseErrorMessage(error) ?? "Failed to check off child");
        }
        catch (Exception ex)
        {
            return ApiResult<ShoppingListItemDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Unchecks a child product purchase entry.
    /// </summary>
    public async Task<ApiResult<ShoppingListItemDto>> UncheckChildAsync(
        Guid listId,
        Guid itemId,
        Guid childProductId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}/uncheck-child/{childProductId}",
                null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ShoppingListItemDto>();
                return result != null
                    ? ApiResult<ShoppingListItemDto>.Ok(result)
                    : ApiResult<ShoppingListItemDto>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<ShoppingListItemDto>.Fail(ParseErrorMessage(error) ?? "Failed to uncheck child");
        }
        catch (Exception ex)
        {
            return ApiResult<ShoppingListItemDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a specific child product to the store's online cart.
    /// </summary>
    public async Task<ApiResult<SendToCartResult>> SendChildToCartAsync(
        Guid listId,
        Guid itemId,
        SendChildToCartRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}/send-child-to-cart",
                request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SendToCartResult>();
                return result != null
                    ? ApiResult<SendToCartResult>.Ok(result)
                    : ApiResult<SendToCartResult>.Fail("Invalid response");
            }

            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<SendToCartResult>.Fail(ParseErrorMessage(error) ?? "Failed to send to cart");
        }
        catch (Exception ex)
        {
            return ApiResult<SendToCartResult>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion


    #region Inventory Session APIs

    /// <summary>
    /// Get all locations.
    /// </summary>
    public async Task<ApiResult<List<LocationDto>>> GetLocationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/locations");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<LocationDto>>(content, options);
                return result != null
                    ? ApiResult<List<LocationDto>>.Ok(result)
                    : ApiResult<List<LocationDto>>.Ok(new List<LocationDto>());
            }
            return ApiResult<List<LocationDto>>.Fail("Failed to load locations");
        }
        catch (Exception ex)
        {
            return ApiResult<List<LocationDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get stock overview with optional filters.
    /// </summary>
    public async Task<ApiResult<List<StockOverviewItemDto>>> GetStockOverviewAsync(
        Guid? locationId = null, Guid? productGroupId = null,
        string? status = null, string? searchTerm = null)
    {
        try
        {
            var parts = new List<string>();
            if (locationId.HasValue) parts.Add($"locationId={locationId}");
            if (productGroupId.HasValue) parts.Add($"productGroupId={productGroupId}");
            if (!string.IsNullOrEmpty(status)) parts.Add($"status={status}");
            if (!string.IsNullOrEmpty(searchTerm)) parts.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";

            var response = await _httpClient.GetAsync($"api/v1/stock/overview{query}");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<StockOverviewItemDto>>(content, options);
                return result != null
                    ? ApiResult<List<StockOverviewItemDto>>.Ok(result)
                    : ApiResult<List<StockOverviewItemDto>>.Ok(new List<StockOverviewItemDto>());
            }
            return ApiResult<List<StockOverviewItemDto>>.Fail("Failed to load stock overview");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StockOverviewItemDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get stock entries for a product at a specific location.
    /// </summary>
    public async Task<ApiResult<List<StockEntryDto>>> GetStockByProductAndLocationAsync(Guid productId, Guid locationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/stock/by-product/{productId}/location/{locationId}");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<StockEntryDto>>(content, options);
                return result != null
                    ? ApiResult<List<StockEntryDto>>.Ok(result)
                    : ApiResult<List<StockEntryDto>>.Ok(new List<StockEntryDto>());
            }
            return ApiResult<List<StockEntryDto>>.Fail("Failed to load stock entries");
        }
        catch (Exception ex)
        {
            return ApiResult<List<StockEntryDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Add a batch of stock entries with individual dates.
    /// </summary>
    public async Task<ApiResult<bool>> AddStockBatchAsync(AddStockBatchRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/stock/batch", request);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to add stock");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Adjust a stock entry's amount or details.
    /// </summary>
    public async Task<ApiResult<bool>> AdjustStockAsync(Guid stockEntryId, AdjustStockRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/stock/{stockEntryId}", request);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to adjust stock");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark a stock entry as opened with tracking mode.
    /// </summary>
    public async Task<ApiResult<bool>> OpenStockEntryAsync(Guid stockEntryId, OpenProductRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/stock/{stockEntryId}/open", request);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to mark as opened");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Quick add 1 unit of stock using product defaults.
    /// </summary>
    public async Task<ApiResult<bool>> QuickAddStockAsync(Guid productId, decimal amount = 1)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/stock/quick-add/{productId}?amount={amount}", null);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to add stock");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Search products by name or description.
    /// </summary>
    public async Task<ApiResult<List<ProductDto>>> SearchProductsAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/products/search?q={Uri.EscapeDataString(query)}");
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(content, options);
                return result != null
                    ? ApiResult<List<ProductDto>>.Ok(result)
                    : ApiResult<List<ProductDto>>.Ok(new List<ProductDto>());
            }
            return ApiResult<List<ProductDto>>.Fail("Search failed");
        }
        catch (Exception ex)
        {
            return ApiResult<List<ProductDto>>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Look up a product via external APIs (OpenFoodFacts, USDA, store integrations).
    /// </summary>
    public async Task<ApiResult<ProductLookupResponse>> ProductLookupAsync(ProductLookupRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/product-lookup", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProductLookupResponse>(content, options);
                return result != null
                    ? ApiResult<ProductLookupResponse>.Ok(result)
                    : ApiResult<ProductLookupResponse>.Fail("Invalid response");
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<ProductLookupResponse>.Fail(ParseErrorMessage(error) ?? "Lookup failed");
        }
        catch (Exception ex)
        {
            return ApiResult<ProductLookupResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    public async Task<ApiResult<ProductDto>> CreateProductAsync(CreateProductRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/products", request);
            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProductDto>(content, options);
                return result != null
                    ? ApiResult<ProductDto>.Ok(result)
                    : ApiResult<ProductDto>.Fail("Invalid response");
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<ProductDto>.Fail(ParseErrorMessage(error) ?? "Failed to create product");
        }
        catch (Exception ex)
        {
            return ApiResult<ProductDto>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a todo item (e.g., for reviewing a product created during inventory).
    /// </summary>
    public async Task<ApiResult<bool>> CreateTodoItemAsync(CreateTodoItemRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/todoitems", request);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ApiResult<bool>.Fail(ParseErrorMessage(error) ?? "Failed to create todo item");
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"Connection error: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Parses an error message from API error response JSON.
    /// Handles formats like {"error_message":"..."} or {"message":"..."} or plain text.
    /// </summary>
    private static string? ParseErrorMessage(string errorResponse)
    {
        if (string.IsNullOrWhiteSpace(errorResponse))
            return null;

        // If it doesn't look like JSON, return as-is (but truncate if too long)
        if (!errorResponse.TrimStart().StartsWith("{"))
            return errorResponse.Length > 200 ? errorResponse[..200] + "..." : errorResponse;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(errorResponse);
            var root = doc.RootElement;

            // Try common error message field names
            if (root.TryGetProperty("error_message", out var errorMsg))
                return errorMsg.GetString();
            if (root.TryGetProperty("errorMessage", out var errorMsg2))
                return errorMsg2.GetString();
            if (root.TryGetProperty("message", out var msg))
                return msg.GetString();
            if (root.TryGetProperty("error", out var err))
                return err.GetString();
            if (root.TryGetProperty("title", out var title))
                return title.GetString();

            // Couldn't find a message field, return truncated JSON
            return errorResponse.Length > 200 ? errorResponse[..200] + "..." : errorResponse;
        }
        catch
        {
            // JSON parsing failed, return as-is
            return errorResponse.Length > 200 ? errorResponse[..200] + "..." : errorResponse;
        }
    }
}
