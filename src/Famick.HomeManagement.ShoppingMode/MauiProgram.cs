using Famick.HomeManagement.ShoppingMode.Pages;
using Famick.HomeManagement.ShoppingMode.Services;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace Famick.HomeManagement.ShoppingMode;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if IOS
        // Disable iOS Password AutoFill floating button
        Platforms.iOS.DisableAutoFillHandler.Register();
#endif

        // API Settings (singleton - configures server URL)
        var apiSettings = new ApiSettings();
        builder.Services.AddSingleton(apiSettings);

        // Configure HttpClient with dynamic base URL
        builder.Services.AddScoped(sp =>
        {
            var settings = sp.GetRequiredService<ApiSettings>();
            var handler = new DynamicApiHttpHandler(settings);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(settings.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        });

        // Core Services
        builder.Services.AddSingleton<TokenStorage>();
        builder.Services.AddSingleton<TenantStorage>();
        builder.Services.AddScoped<ShoppingApiClient>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<OfflineStorageService>();
        builder.Services.AddScoped<ImageCacheService>();

        // ConnectivityService needs ShoppingApiClient, register as scoped to match ShoppingApiClient's lifetime
        builder.Services.AddScoped<ConnectivityService>();

        // Pages (registered for DI navigation)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ServerConfigPage>();
        builder.Services.AddTransient<ListSelectionPage>();
        builder.Services.AddTransient<ShoppingSessionPage>();
        builder.Services.AddTransient<AddItemPage>();
        builder.Services.AddTransient<BarcodeScannerPage>();
        builder.Services.AddTransient<AisleOrderPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
