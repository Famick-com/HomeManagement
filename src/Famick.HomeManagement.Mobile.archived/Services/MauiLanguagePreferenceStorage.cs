using Famick.HomeManagement.UI.Localization;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI-based language preference storage using Preferences.
/// Uses Preferences instead of SecureStorage since language is not sensitive data.
/// </summary>
public class MauiLanguagePreferenceStorage : ILanguagePreferenceStorage
{
    private const string LanguagePreferenceKey = "preferred_language";

    public Task<string?> GetLanguagePreferenceAsync()
    {
        try
        {
            var value = Preferences.Default.Get<string?>(LanguagePreferenceKey, null);
            return Task.FromResult(value);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetLanguagePreferenceAsync(string languageCode)
    {
        try
        {
            Preferences.Default.Set(LanguagePreferenceKey, languageCode);
        }
        catch
        {
            // Handle storage exceptions
        }
        return Task.CompletedTask;
    }

    public Task ClearLanguagePreferenceAsync()
    {
        try
        {
            Preferences.Default.Remove(LanguagePreferenceKey);
        }
        catch
        {
            // Handle storage exceptions
        }
        return Task.CompletedTask;
    }
}
