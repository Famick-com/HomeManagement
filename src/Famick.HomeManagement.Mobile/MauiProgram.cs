using Famick.HomeManagement.Mobile.Services;
using Famick.HomeManagement.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Famick.HomeManagement.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();

        // Add MudBlazor services
        builder.Services.AddMudServices();

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
        builder.Services.AddScoped<ApiAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());
        builder.Services.AddScoped<IApiClient, HttpApiClient>();

        builder.Services.AddAuthorizationCore();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
