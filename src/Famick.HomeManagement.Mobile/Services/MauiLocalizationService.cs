using System.Text.Json;
using Famick.HomeManagement.UI.Localization;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Mobile.Services;

/// <summary>
/// MAUI-specific localization service that reads locale files from the app bundle.
/// </summary>
public class MauiLocalizationService : ILocalizationService
{
    private readonly ILanguagePreferenceStorage _preferenceStorage;
    private readonly ILogger<MauiLocalizationService> _logger;

    private Dictionary<string, string> _translations = new();
    private List<LanguageInfo> _availableLanguages = new();
    private string _currentLanguage = "en";
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public string CurrentLanguage => _currentLanguage;
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _availableLanguages;

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    public MauiLocalizationService(
        ILanguagePreferenceStorage preferenceStorage,
        ILogger<MauiLocalizationService> logger)
    {
        _preferenceStorage = preferenceStorage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            await LoadAvailableLanguagesAsync();
            var preferredLanguage = await ResolvePreferredLanguageAsync();
            await LoadLanguageAsync(preferredLanguage);

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize localization");
            _currentLanguage = "en";
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public string GetString(string key)
    {
        if (_translations.TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }

    public string GetString(string key, params object[] arguments)
    {
        var template = GetString(key);
        if (arguments.Length == 0)
        {
            return template;
        }

        try
        {
            var result = template;
            for (var i = 0; i < arguments.Length; i++)
            {
                result = result.Replace($"{{{i}}}", arguments[i]?.ToString() ?? "");
            }
            return result;
        }
        catch
        {
            return template;
        }
    }

    public string GetPluralString(string key, int count, params object[] arguments)
    {
        var pluralKey = count switch
        {
            0 => $"{key}.zero",
            1 => $"{key}.one",
            _ => $"{key}.other"
        };

        var template = _translations.TryGetValue(pluralKey, out var value)
            ? value
            : _translations.TryGetValue($"{key}.other", out var otherValue)
                ? otherValue
                : key;

        template = template.Replace("{count}", count.ToString());

        if (arguments.Length > 0)
        {
            for (var i = 0; i < arguments.Length; i++)
            {
                template = template.Replace($"{{{i}}}", arguments[i]?.ToString() ?? "");
            }
        }

        return template;
    }

    public async Task SetLanguageAsync(string languageCode)
    {
        if (_currentLanguage == languageCode) return;

        var oldLanguage = _currentLanguage;
        await LoadLanguageAsync(languageCode);
        await _preferenceStorage.SetLanguagePreferenceAsync(languageCode);

        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, languageCode));
    }

    private async Task LoadAvailableLanguagesAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/_content/Famick.HomeManagement.UI/locales/languages.json");
            var config = await JsonSerializer.DeserializeAsync<LanguagesConfig>(stream);

            if (config?.Languages != null)
            {
                _availableLanguages = config.Languages
                    .Select(l => new LanguageInfo(l.Code, l.NativeName, l.EnglishName, l.IsRtl))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load available languages, using default");
            _availableLanguages = new List<LanguageInfo>
            {
                new("en", "English", "English", false)
            };
        }
    }

    private async Task<string> ResolvePreferredLanguageAsync()
    {
        var storedPreference = await _preferenceStorage.GetLanguagePreferenceAsync();
        if (!string.IsNullOrEmpty(storedPreference) &&
            _availableLanguages.Any(l => l.Code == storedPreference))
        {
            return storedPreference;
        }
        return "en";
    }

    private async Task LoadLanguageAsync(string languageCode)
    {
        try
        {
            var path = $"wwwroot/_content/Famick.HomeManagement.UI/locales/{languageCode}.json";
            using var stream = await FileSystem.OpenAppPackageFileAsync(path);
            var doc = await JsonDocument.ParseAsync(stream);

            _translations.Clear();
            FlattenJson(doc.RootElement, "", _translations);
            _currentLanguage = languageCode;

            _logger.LogDebug("Loaded {Count} translations for language {Language}",
                _translations.Count, languageCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load language {Language}, falling back to en", languageCode);
            if (languageCode != "en")
            {
                await LoadLanguageAsync("en");
            }
        }
    }

    private static void FlattenJson(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.StartsWith("_")) continue;

                    var newPrefix = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    FlattenJson(property.Value, newPrefix, result);
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString() ?? prefix;
                break;

            case JsonValueKind.Number:
                result[prefix] = element.ToString();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;
        }
    }
}

internal record LanguagesConfig(
    string DefaultLanguage,
    List<LanguageConfigItem> Languages
);

internal record LanguageConfigItem(
    string Code,
    string NativeName,
    string EnglishName,
    bool IsRtl = false
);
