using System.Net.Http.Headers;
using System.Net.Http.Json;
using Famick.HomeManagement.ShoppingMode.Models;

namespace Famick.HomeManagement.ShoppingMode.Services;

/// <summary>
/// API client for shopping-related operations.
/// </summary>
public class ShoppingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenStorage _tokenStorage;

    public ShoppingApiClient(HttpClient httpClient, TokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
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
            return ApiResult<LoginResponse>.Fail(error.Length > 200 ? "Login failed" : error);
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResponse>.Fail($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all shopping lists.
    /// </summary>
    public async Task<ApiResult<List<ShoppingListSummary>>> GetShoppingListsAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("api/v1/shoppinglists");

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
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"api/v1/shoppinglists/{listId}");

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
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("api/v1/shoppinglocations");

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
    public async Task<ApiResult<bool>> TogglePurchasedAsync(Guid listId, Guid itemId)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsync(
                $"api/v1/shoppinglists/{listId}/items/{itemId}/toggle-purchased",
                null);

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
    /// Quick add an item to a shopping list.
    /// </summary>
    public async Task<ApiResult<bool>> QuickAddItemAsync(Guid listId, string productName, decimal amount, string? barcode, string? note, bool isPurchased = true)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("api/v1/shoppinglists/quick-add", new
            {
                shoppingListId = listId,
                productName,
                amount,
                barcode,
                note,
                lookupInStore = true,
                isPurchased
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
    /// Lookup a product by barcode using store integration.
    /// </summary>
    public async Task<ApiResult<StoreProductResult>> LookupProductByBarcodeAsync(Guid listId, string barcode)
    {
        try
        {
            await SetAuthHeaderAsync();
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
            await SetAuthHeaderAsync();
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
    /// Move purchased items to inventory.
    /// </summary>
    public async Task<ApiResult<MoveToInventoryResponse>> MoveToInventoryAsync(MoveToInventoryRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
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
            await SetAuthHeaderAsync();
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

    private async Task SetAuthHeaderAsync()
    {
        var token = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
