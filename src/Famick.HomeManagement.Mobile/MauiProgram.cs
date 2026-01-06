using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Mobile.Services;
using Famick.HomeManagement.UI.Localization;
using Famick.HomeManagement.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using ZXing.Net.Maui.Controls;

namespace Famick.HomeManagement.Mobile;

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

        builder.Services.AddMauiBlazorWebView();

        // Add MudBlazor services
        builder.Services.AddMudServices();

        // Add localization services (MAUI-specific implementation)
        builder.Services.AddScoped<ILanguagePreferenceStorage, MauiLanguagePreferenceStorage>();
        builder.Services.AddScoped<ILocalizationService, MauiLocalizationService>();
        builder.Services.AddScoped<ILocalizer, Localizer>();
        builder.Services.AddTransient<MudLocalizer, FamickMudLocalizer>();

        // Add API settings (configurable base URL)
        var apiSettings = new ApiSettings();
        builder.Services.AddSingleton(apiSettings);
        builder.Services.AddSingleton<IServerSettings>(apiSettings);

        // Configure HttpClient with dynamic base URL and SSL bypass for debug
        builder.Services.AddScoped(sp =>
        {
            var apiSettings = sp.GetRequiredService<ApiSettings>();
            var handler = new DynamicApiHttpHandler(apiSettings);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(apiSettings.BaseUrl)
            };
        });

        // Add authentication services
        builder.Services.AddScoped<ITokenStorage, MauiTokenStorage>();
        builder.Services.AddScoped<IApiClient, HttpApiClient>();
        builder.Services.AddScoped<ApiAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());

        builder.Services.AddAuthorizationCore();

        // Add barcode scanner service (MAUI implementation with camera)
        builder.Services.AddScoped<IBarcodeScannerService, MauiBarcodeScannerService>();

        // Add inventory session service
        builder.Services.AddScoped<IInventorySessionService, MauiInventorySessionService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
