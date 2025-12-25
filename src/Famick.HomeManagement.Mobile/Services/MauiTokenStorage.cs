using Famick.HomeManagement.UI.Services;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI-based token storage using SecureStorage.
/// Provides secure, platform-native storage for tokens.
/// </summary>
public class MauiTokenStorage : ITokenStorage
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch
        {
            // Handle storage exceptions (e.g., secure storage not available)
        }
    }

    public Task ClearTokensAsync()
    {
        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
        }
        catch
        {
            // Handle storage exceptions
        }
        return Task.CompletedTask;
    }
}
